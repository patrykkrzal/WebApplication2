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
using Microsoft.AspNetCore.CookiePolicy; // cookie policy
using Microsoft.AspNetCore.Http; // SameSiteMode
using Rent.DTO; // UpdateUserDto

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // DemoMode from config or env
        static bool GetBool(string? v)
            => !string.IsNullOrWhiteSpace(v) && (v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase));
        var demoFromCfg = builder.Configuration["DemoMode"];
        var demoFromEnv = Environment.GetEnvironmentVariable("DEMO_MODE");
        var demoMode = GetBool(demoFromEnv) || GetBool(demoFromCfg);

        // -------------------------------
        // CONNECTION STRING OVERRIDES
        // -------------------------------
        // Umo¿liwienie zdalnego connection stringa bez zmiany plików:
        //1) RENT_DB (np. Server=host,1433;Database=RentDb;User Id=...;Password=...;TrustServerCertificate=True)
        //2) ConnectionStrings__DefaultConnection (standardowy sposób dla ENV)
        var rentDbEnv = Environment.GetEnvironmentVariable("RENT_DB");
        var csEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(csEnv))
        {
            builder.Configuration["ConnectionStrings:DefaultConnection"] = csEnv;
            Console.WriteLine("Using ConnectionStrings__DefaultConnection from environment.");
        }
        else if (!string.IsNullOrWhiteSpace(rentDbEnv))
        {
            builder.Configuration["ConnectionStrings:DefaultConnection"] = rentDbEnv;
            Console.WriteLine("Using RENT_DB from environment.");
        }
        else
        {
            // If LocalDB and a fixed Initial Catalog are used, create per-user DB to avoid cross-account conflicts
            var currentCs = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(currentCs) && currentCs.Contains("(localdb)\\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var b = new SqlConnectionStringBuilder(currentCs);
                    var baseName = string.IsNullOrWhiteSpace(b.InitialCatalog) ? "RentDb" : b.InitialCatalog;
                    var uname = Environment.UserName ?? "User";
                    var sanitized = new string(uname.Where(char.IsLetterOrDigit).ToArray());
                    if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "User";
                    var perUser = baseName + "_" + sanitized;
                    b.InitialCatalog = perUser;
                    builder.Configuration["ConnectionStrings:DefaultConnection"] = b.ToString();
                    Console.WriteLine($"Using per-user LocalDB database: {perUser}");
                }
                catch { /* ignore and keep original */ }
            }
        }

        // -------------------------------
        // LOGOWANIE CONNECTION STRING
        // -------------------------------
        Console.WriteLine("Connection string: " + builder.Configuration.GetConnectionString("DefaultConnection"));
        Console.WriteLine("DemoMode: " + demoMode);

        // -------------------------------
        // DODAWANIE SERWISÓW
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

        // Wymuœ bezpieczne ciasteczka dla HTTPS
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // tylko po HTTPS
            options.Cookie.SameSite = SameSiteMode.Lax; // wspó³dzia³a z fetch + credentials
        });

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
        bool migrated = false;
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            try
            {
                context.Database.Migrate();
                migrated = true;
                Console.WriteLine("Database migrated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database migrate failed: " + ex.Message);
            }

            try
            {
                if (migrated)
                {
                    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
                    var basePath = AppContext.BaseDirectory;
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
                        if (!string.IsNullOrWhiteSpace(cs))
                        {
                            var script = await File.ReadAllTextAsync(sqlPath);
                            await ExecuteSqlScriptBatchedAsync(cs, script);
                            Console.WriteLine($"Executed DatabaseObjects.sql from '{sqlPath}'.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("DatabaseObjects.sql not found; skipping SP/func initialization.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Executing DatabaseObjects.sql failed: " + ex.Message);
            }

            if (migrated)
            {
                var seeder = scope.ServiceProvider.GetRequiredService<Seed>();
                seeder.SeedDataContext();
            }
        }

        // -------------------------------
        // AUTOMATYCZNE TWORZENIE RÓL + DEMO USER
        // -------------------------------
        string? demoUserId = null;
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

            if (demoMode)
            {
                var demoEmail = "demo@demo.local";
                var demo = await userManager.FindByEmailAsync(demoEmail);
                if (demo == null)
                {
                    demo = new User { UserName = demoEmail, Email = demoEmail, First_name = "Demo", Last_name = "User" };
                    var r = await userManager.CreateAsync(demo, "Demo1234,");
                    if (!r.Succeeded)
                        Console.WriteLine("Demo user create failed: " + string.Join(", ", r.Errors.Select(e => e.Description)));
                }
                // Ensure roles for demo user
                var demoRoles = await userManager.GetRolesAsync(demo);
                string[] want = new[] { "Admin", "Worker", "User" };
                foreach (var rr in want)
                    if (!demoRoles.Contains(rr)) await userManager.AddToRoleAsync(demo, rr);

                demoUserId = demo.Id;
                Console.WriteLine($"Demo user ready: {demoEmail} ({demoUserId})");
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
            c.RoutePrefix = "swagger"; // Swagger pod /swagger
        });

        // -------------------------------
        //  MIDDLEWARE
        // -------------------------------
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts(); // w produkcji HSTS
        }
        app.UseHttpsRedirection();
        app.UseDefaultFiles(); // Index.html jako strona g³ówna
        app.UseStaticFiles();

        app.UseAuthentication();

        // Demo auto-auth: jeœli brak logowania, podstaw u¿ytkownika demo
        if (demoMode && !string.IsNullOrEmpty(demoUserId))
        {
            app.Use(async (ctx, next) =>
            {
                if (!(ctx.User?.Identity?.IsAuthenticated ?? false))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, demoUserId!),
                        new Claim(ClaimTypes.Name, "demo"),
                        new Claim(ClaimTypes.Email, "demo@demo.local"),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim(ClaimTypes.Role, "Worker"),
                        new Claim(ClaimTypes.Role, "User"),
                    };
                    var id = new ClaimsIdentity(claims, authenticationType: "Demo");
                    ctx.User = new ClaimsPrincipal(id);
                }
                await next();
            });
        }

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

        // PATCH: update current user's UserName
        app.MapMethods("/api/users/me/username", new[] { "PATCH" }, async (
            ClaimsPrincipal claims,
            UserManager<User> userManager,
            UpdateUserDto req) =>
        {
            var userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            if (req is null || string.IsNullOrWhiteSpace(req.UserName))
                return Results.BadRequest(new { Message = "UserName is required" });

            var exists = await userManager.FindByNameAsync(req.UserName);
            if (exists != null && exists.Id != userId)
                return Results.Conflict(new { Message = "UserName already taken" });

            var user = await userManager.FindByIdAsync(userId);
            if (user is null) return Results.NotFound();
            user.UserName = req.UserName;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Results.BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return Results.Ok(new { user.Id, user.UserName, user.Email });
        }).RequireAuthorization();

        // -------------------------------
        // START APLIKACJI
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
