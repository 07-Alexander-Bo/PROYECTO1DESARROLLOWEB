using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

var builder = WebApplication.CreateBuilder(args);

// Conexión a SQL Server
builder.Services.AddDbContext<SistemaClimaticoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SistemaClimatico")
    ));

// Controllers (Sensores, Alertas, Historial, Auth, etc.)
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Servicio de tokens JWT
builder.Services.AddScoped<TokenService>();

// Autenticación con JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// El orden importa: primero autenticación, luego autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Prueba de conexión con la base de datos (la dejamos, no estorba)
app.MapGet("/db-test", async (SistemaClimaticoContext db) =>
{
    var cantidadSensores = await db.Sensores.CountAsync();

    return Results.Ok(new
    {
        mensaje = "Conexión a SQL Server correcta",
        sensores = cantidadSensores
    });
});

app.Run();
