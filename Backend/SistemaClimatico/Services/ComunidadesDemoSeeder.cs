using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Si no hay ninguna comunidad, crea dos de ejemplo y ubica en la primera todos los
// sensores que todavía no tienen comunidad (repartidos alrededor de su centro para
// que se vean separados en el mapa). Si ya hay comunidades, no toca nada.
//
// Se puede desactivar con Simulador:CrearSensoresDemo = false.
public static class ComunidadesDemoSeeder
{
    public static async Task SembrarAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ComunidadesDemoSeeder");

        if (!config.GetValue("Simulador:CrearSensoresDemo", true))
            return;

        try
        {
            var db = services.GetRequiredService<SistemaClimaticoContext>();

            if (await db.Comunidades.AnyAsync())
                return;

            var principal = new Comunidad
            {
                Nombre = "Aldea Los Encinos",
                Descripcion = "Comunidad rural de ejemplo, cerca del río y de la colina.",
                Latitud = 14.9400m,
                Longitud = -91.1100m,
                Activa = true,
                FechaCreacion = DateTime.Now
            };
            var secundaria = new Comunidad
            {
                Nombre = "Caserío El Rosario",
                Descripcion = "Segunda comunidad de ejemplo (todavía sin sensores).",
                Latitud = 14.9050m,
                Longitud = -91.0550m,
                Activa = true,
                FechaCreacion = DateTime.Now
            };

            db.Comunidades.Add(principal);
            db.Comunidades.Add(secundaria);
            await db.SaveChangesAsync();

            // Posiciones alrededor del centro: ~±0.004° equivale a unos 400 m.
            decimal[,] desplazamientos =
            {
                { 0.0000m,  0.0000m }, { 0.0006m,  0.0010m }, { 0.0035m, -0.0030m },
                { 0.0040m,  0.0030m }, { -0.0030m, 0.0035m }, { -0.0035m, -0.0030m },
            };

            var sinComunidad = await db.Sensores
                .Where(s => s.IdComunidad == null)
                .OrderBy(s => s.IdSensor)
                .ToListAsync();

            for (var i = 0; i < sinComunidad.Count; i++)
            {
                var d = i % (desplazamientos.GetLength(0));
                sinComunidad[i].IdComunidad = principal.IdComunidad;
                sinComunidad[i].Latitud = principal.Latitud + desplazamientos[d, 0];
                sinComunidad[i].Longitud = principal.Longitud + desplazamientos[d, 1];
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Se crearon 2 comunidades de ejemplo y se asignaron {Cantidad} sensores.", sinComunidad.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron crear las comunidades de ejemplo.");
        }
    }
}
