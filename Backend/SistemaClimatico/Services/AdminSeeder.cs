using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Crea el primer Administrador al arrancar la API, SOLO si todavía no existe ninguno.
//
// Como el registro de usuarios ahora exige ser Administrador, alguien tiene que
// ser el primero. Los datos salen de la configuración (user-secrets o variables
// de entorno), nunca del código:
//   SeedAdmin:Correo    SeedAdmin:Password    SeedAdmin:Nombre (opcional)
public static class AdminSeeder
{
    public static async Task SembrarAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        var correo = config["SeedAdmin:Correo"];
        var password = config["SeedAdmin:Password"];
        var nombre = config["SeedAdmin:Nombre"];

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SeedAdmin no configurado: no se crea administrador inicial.");
            return;
        }

        try
        {
            var db = services.GetRequiredService<SistemaClimaticoContext>();

            if (await db.Usuarios.AnyAsync(u => u.Rol == "Administrador"))
                return;

            if (await db.Usuarios.AnyAsync(u => u.Correo == correo))
            {
                logger.LogWarning(
                    "SeedAdmin: ya existe un usuario con el correo {Correo} que no es Administrador. No se creó nada.",
                    correo);
                return;
            }

            var admin = new Usuario
            {
                NombreCompleto = string.IsNullOrWhiteSpace(nombre) ? "Administrador" : nombre,
                Correo = correo,
                Rol = "Administrador",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            admin.PasswordHash = new PasswordHasher<Usuario>().HashPassword(admin, password);

            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();

            db.BitacoraAcciones.Add(new BitacoraAccione
            {
                IdUsuario = admin.IdUsuario,
                Accion = "ADMIN_INICIAL_CREADO",
                Descripcion = $"Se creó el administrador inicial {correo}.",
                FechaHora = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            logger.LogInformation("Administrador inicial creado: {Correo}", correo);
        }
        catch (Exception ex)
        {
            // Si SQL Server no está arriba o la base/tablas no existen, no tumbamos la API:
            // avisamos y seguimos (los endpoints fallarán con su propio error claro).
            logger.LogWarning(ex,
                "No se pudo crear el administrador inicial. ¿SQL Server está corriendo y ya ejecutaste 01_create_tables.sql?");
        }
    }
}
