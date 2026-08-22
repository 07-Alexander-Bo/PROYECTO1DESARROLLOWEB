using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

var builder = WebApplication.CreateBuilder(args);

// Conexión a SQL Server
builder.Services.AddDbContext<SistemaClimaticoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SistemaClimatico")
    ));

// Controladores (API REST)
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

// Prueba de conexión con la base de datos
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