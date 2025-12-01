using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Rent;
using Rent.Models;
using Rent.Data;
using Rent.Interfaces;
using Rent.Ropository;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json.Serialization; // enum string converter
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // -------------------------------
        //  LOGOWANIE CONNECTION STRING
        // -------------------------------
        Console.WriteLine("Connection string: " + builder.Configuration.GetConnectionString("DefaultConnection"));

        // -------------------------------
        //  DODAWANIE SERWISÓW
        // -------------------------------

        // Controllers
        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // enumy jako pe³ne nazwy
                o.JsonSerializerOptions.PropertyNamingPolicy = null; // Zachowaj oryginalne nazwy w³aœciwoœci (PascalCase)
                o.JsonSerializerOptions.DictionaryKeyPolicy = null; // Zachowaj klucze s³owników w oryginalnej formie
            });

        // Repozytoria
        builder.Services.AddScoped<IRentalInfoRepository, RentalInfoRepository>();
        builder.Services.AddScoped<Seed>();

        // DbContext
        builder.Services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Rent API",
                Version = "v1"
            });
        });

        // Identity Core z obs³ug¹ ról
        builder.Services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = true; // zaostrzenie has³a
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true; // unikalny email
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<DataContext>()
        .AddSignInManager()
        .AddApiEndpoints();

        // Authentication & Authorization
        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddBearerToken(IdentityConstants.BearerScheme)
            .AddCookie(IdentityConstants.ApplicationScheme);

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("IsWorker", policy => policy.RequireRole("Worker"));
            options.AddPolicy("IsAdmin", policy => policy.RequireRole("Admin"));
        });

        // -------------------------------
        //  BUDOWANIE APLIKACJI
        // -------------------------------
        var app = builder.Build();

        // -------------------------------
        //  MIGRACJE DB (AUTO) + DDL skrypty
        // -------------------------------
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            try
            {
                context.Database.Migrate();
                Console.WriteLine("Database migrated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database migrate failed: " + ex.Message);
            }

            // Execute DatabaseObjects.sql to create functions/procedures required by controllers
            try
            {
                var cs = builder.Configuration.GetConnectionString("DefaultConnection");
                var basePath = AppContext.BaseDirectory;
                // Try to locate file relative to content root
                var contentRoot = builder.Environment.ContentRootPath;
                var sqlPathCandidates = new[]
                {
                    Path.Combine(contentRoot, "WebApplication2", "DatabaseObjects.sql"),
                    Path.Combine(contentRoot, "DatabaseObjects.sql"),
                    Path.Combine(basePath, "DatabaseObjects.sql")
                };
                string? sqlPath = sqlPathCandidates.FirstOrDefault(File.Exists);
                if (sqlPath != null)
                {
                    var script = await File.ReadAllTextAsync(sqlPath);
                    await ExecuteSqlScriptBatchedAsync(cs, script);
                    Console.WriteLine($"Executed DatabaseObjects.sql from '{sqlPath}'.");
                }
                else
                {
                    Console.WriteLine("DatabaseObjects.sql not found; skipping SP/func initialization.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Executing DatabaseObjects.sql failed: " + ex.Message);
            }

            // Seed po migracji (jeœli pusty RentalInfo)
            var seeder = scope.ServiceProvider.GetRequiredService<Seed>();
            seeder.SeedDataContext();
        }

        // -------------------------------
        //  AUTOMATYCZNE TWORZENIE RÓL
        // -------------------------------
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = new[] { "Worker", "User", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"Role '{role}' created.");
                }
            }
        }
        using (var scope = app.Services.CreateScope())
        {
            // U¿ywaj UserManager<User>, bo tak skonfigurowano Identity
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            string email = "admin@admin.com";
            string password = "Test1234,";

            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new User
                {
                    UserName = email,
                    Email = email,
                    First_name = "Admin",
                    Last_name = "Admin"
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    Console.WriteLine("Admin create failed: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }

        // -------------------------------
        //  SEEDOWANIE
        // -------------------------------
        if (args.Length == 1 && args[0].ToLower() == "seeddata")
        {
            using var scope = app.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<Seed>();
            service.SeedDataContext();
            Console.WriteLine("Seedowanie zakoñczone!");
            return;
        }

        // -------------------------------
        //  SWAGGER
        // -------------------------------
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rent API V1");
        });

        // -------------------------------
        //  MIDDLEWARE
        // -------------------------------
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        // -------------------------------
        //  ROUTING
        // -------------------------------
        app.MapControllers();
        app.MapIdentityApi<User>();

        // Minimal endpoint — bie¿¹cy u¿ytkownik (z list¹ ról)
        app.MapGet("/api/users/me", async (ClaimsPrincipal claims, UserManager<User> userManager) =>
        {
            var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return Results.NotFound();

            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.First_name,
                user.Last_name,
                user.Login,
                Roles = roles
            });
        }).RequireAuthorization();

        // Minimal endpoint — wszyscy u¿ytkownicy
        app.MapGet("/api/users", async (DataContext context) =>
        {
            return await context.Users.ToListAsync();
        }).RequireAuthorization("IsAdmin"); // ograniczenie publicznych endpointów

        // -------------------------------
        //  START APLIKACJI
        // -------------------------------
        app.Run();
    }

    private static async Task ExecuteSqlScriptBatchedAsync(string connectionString, string script)
    {
        // Split on lines containing only GO (case-insensitive)
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(s => !string.IsNullOrWhiteSpace(s));
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        foreach (var batch in batches)
        {
            using var cmd = new SqlCommand(batch, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
