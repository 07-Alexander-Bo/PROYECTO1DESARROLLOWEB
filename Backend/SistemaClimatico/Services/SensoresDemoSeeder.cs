using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Si la tabla Sensores está vacía, crea 5 sensores de ejemplo (uno por cada tipo
// que pide el enunciado) para que el simulador tenga algo que medir desde el
// primer arranque. Si ya hay sensores, no toca nada.
//
// Se puede desactivar con Simulador:CrearSensoresDemo = false.
public static class SensoresDemoSeeder
{
    public static async Task SembrarAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SensoresDemoSeeder");

        if (!config.GetValue("Simulador:CrearSensoresDemo", true))
            return;

        try
        {
            var db = services.GetRequiredService<SistemaClimaticoContext>();

            if (await db.Sensores.AnyAsync())
                return;

            var demo = new[]
            {
                new Sensore { Nombre = "Termómetro Plaza Central", Tipo = "Temperatura", UnidadMedida = "°C",   Ubicacion = "Plaza central" },
                new Sensore { Nombre = "Higrómetro Plaza Central", Tipo = "Humedad",     UnidadMedida = "%",    Ubicacion = "Plaza central" },
                new Sensore { Nombre = "Anemómetro Colina",        Tipo = "Viento",      UnidadMedida = "km/h", Ubicacion = "Mirador de la colina" },
                new Sensore { Nombre = "Pluviómetro Norte",        Tipo = "Lluvia",      UnidadMedida = "mm",   Ubicacion = "Sector norte" },
                new Sensore { Nombre = "Medidor Río Los Encinos",  Tipo = "NivelRio",    UnidadMedida = "m",    Ubicacion = "Puente del río" },
            };

            foreach (var sensor in demo)
            {
                sensor.Activo = true;
                sensor.FechaCreacion = DateTime.Now;
                db.Sensores.Add(sensor);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Se crearon {Cantidad} sensores de ejemplo.", demo.Length);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron crear los sensores de ejemplo (¿base de datos lista?).");
        }
    }
}
