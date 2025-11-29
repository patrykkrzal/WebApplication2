using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Rent;
using Rent.Models;
using Rent.Data;
using Rent.Interfaces;
using Rent.Ropository;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Logowanie connection stringa
Console.WriteLine("Connection string: " + builder.Configuration.GetConnectionString("DefaultConnection"));

// -------------------------------
//  DODAWANIE SERWISÓW
// -------------------------------

// Controllers
builder.Services.AddControllers();

// Repozytoria
builder.Services.AddScoped<IRentalInfoRepository, RentalInfoRepository>();
builder.Services.AddScoped<Seed>();

// DbContext — TYLKO RAZ
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

// Identity Core (Minimal API Identity)
builder.Services.AddIdentityCore<User>(options =>
{
    // opcjonalnie, regu³y dla hase³ itp.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<DataContext>()
.AddApiEndpoints();

// Auth
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddAuthorization();

// -------------------------------
//  BUDOWANIE APLIKACJI
// -------------------------------
var app = builder.Build();

// -------------------------------
//  SEEDOWANIE
// -------------------------------
if (args.Length == 1 && args[0].ToLower() == "seeddata")
{
    SeedData(app);
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

app.UseAuthentication();  // <-- BRAKOWA£O
app.UseAuthorization();

// -------------------------------
//  ROUTING
// -------------------------------
app.MapControllers();          // kontrolery MVC
app.MapIdentityApi<User>();    // Minimal API Identity

// -------------------------------
//  Minimal endpoint (opcjonalny)
// -------------------------------
app.MapGet("/api/users", async (DataContext context) =>
{
    return await context.Users.ToListAsync();
});

// -------------------------------
app.Run();


// =====================================
//  FUNKCJA SEEDOWANIA
// =====================================
void SeedData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<Seed>();
    service.SeedDataContext();
}
