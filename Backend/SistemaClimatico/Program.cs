using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaClimatico.Hubs;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

var builder = WebApplication.CreateBuilder(args);

// Los secretos (cadena de conexión, clave JWT, admin inicial) NO viven en el repositorio.
// En desarrollo salen de 'dotnet user-secrets'; en Docker/producción, de variables de entorno.

// Conexión a SQL Server
var connectionString = builder.Configuration.GetConnectionString("SistemaClimatico");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings:SistemaClimatico. Configúrala con 'dotnet user-secrets set' " +
        "o con la variable de entorno ConnectionStrings__SistemaClimatico.");
}

builder.Services.AddDbContext<SistemaClimaticoContext>(options =>
    options.UseSqlServer(connectionString));

// Controllers (Sensores, Alertas, Historial, Auth, Bitácora)
builder.Services.AddControllers();

// CORS: orígenes permitidos configurables (Cors:AllowedOrigins). Por defecto, Angular en local.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // necesario para SignalR (negociación de la conexión)
    });
});

// SignalR: canal de tiempo real hacia el navegador (/hubs/monitoreo)
builder.Services.AddSignalR();

// Tiempo real y simulación de sensores
builder.Services.AddSingleton<SimuladorControl>();
builder.Services.AddSingleton<NotificadorTiempoReal>();
builder.Services.AddScoped<ProcesadorLecturas>();
builder.Services.AddHostedService<SimuladorLecturasService>();

// OpenAPI
builder.Services.AddOpenApi();

// Servicio de tokens JWT
builder.Services.AddScoped<TokenService>();

// Bitácora de acciones de usuarios (necesita acceso al usuario de la petición actual)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BitacoraService>();

// Autenticación con JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Falta Jwt:Key o es demasiado corta (mínimo 32 caracteres). Configúrala con " +
        "'dotnet user-secrets set \"Jwt:Key\" \"...\"' o con la variable de entorno Jwt__Key.");
}

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

    // SignalR con WebSockets no puede enviar el encabezado Authorization,
    // así que el navegador manda el token como ?access_token=... en la URL del hub.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];

            if (!string.IsNullOrEmpty(token) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Crea el primer Administrador si no existe ninguno (ver Services/AdminSeeder.cs)
using (var scope = app.Services.CreateScope())
{
    await AdminSeeder.SembrarAsync(scope.ServiceProvider);
    await SensoresDemoSeeder.SembrarAsync(scope.ServiceProvider);
}

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// CORS: permitir peticiones desde Angular
app.UseCors("Frontend");

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Canal de tiempo real (solo usuarios autenticados, ver MonitoreoHub)
app.MapHub<MonitoreoHub>("/hubs/monitoreo");

// Prueba de conexión con la base de datos (ahora requiere estar autenticado)
app.MapGet("/db-test", async (SistemaClimaticoContext db) =>
{
    var cantidadSensores = await db.Sensores.CountAsync();

    return Results.Ok(new
    {
        mensaje = "Conexión a SQL Server correcta",
        sensores = cantidadSensores
    });
}).RequireAuthorization();

app.Run();
