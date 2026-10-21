using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Deja la base de datos lista la primera vez que arranca la API (por ejemplo, en una
// VPS con un SQL Server recién creado en Docker y sin ninguna base):
//
//   1. Espera a que SQL Server acepte conexiones (en Docker tarda unos 20-40 s).
//   2. Si la base "SistemaClimatico" no existe, la crea.
//   3. Si no existen las tablas, ejecuta Database/Scripts/01_create_tables.sql
//      (el script va incluido dentro de la API como recurso embebido).
//
// Si todo ya existe (tu base de desarrollo, por ejemplo), no hace nada y no toca tus datos.
// Después, EsquemaBaseDatos agrega las tablas/columnas de las fases posteriores.
//
// Configuración opcional:  BaseDatos:EsperaMaximaSegundos (por defecto 180)
public static class InicializadorBaseDatos
{
    private const string NombreScript = "01_create_tables.sql";
    private static readonly Regex NombreValido = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static async Task InicializarAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("InicializadorBaseDatos");

        var cadena = config.GetConnectionString("SistemaClimatico")!;
        var espera = TimeSpan.FromSeconds(Math.Max(5, config.GetValue("BaseDatos:EsperaMaximaSegundos", 180)));
        var limite = DateTime.UtcNow + espera;
        var intento = 0;

        while (true)
        {
            intento++;
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<SistemaClimaticoContext>();

                if (!await ExistenTablasAsync(db, ct))
                {
                    logger.LogInformation("La base de datos existe pero no tiene tablas: se crean ahora.");
                    await CrearTablasAsync(db, ct);
                    logger.LogInformation("Tablas, relaciones e índices creados correctamente.");
                }
                else if (intento > 1)
                {
                    logger.LogInformation("Base de datos lista.");
                }
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= limite)
                    throw new InvalidOperationException(
                        $"No se pudo preparar la base de datos después de {espera.TotalSeconds:0} s: {ex.Message}", ex);

                logger.LogInformation(
                    "Esperando a SQL Server / la base de datos (intento {Intento}): {Motivo}", intento, ex.Message);

                // Si SQL Server ya responde pero la base todavía no existe, se crea.
                await IntentarCrearBaseAsync(cadena, logger, ct);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    private static async Task<bool> ExistenTablasAsync(SistemaClimaticoContext db, CancellationToken ct)
    {
        var resultado = await db.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .ToListAsync(ct);
        return resultado.Count > 0 && resultado[0] == 1;
    }

    private static async Task CrearTablasAsync(SistemaClimaticoContext db, CancellationToken ct)
    {
        foreach (var lote in ExtraerLotes(LeerScript()))
            await db.Database.ExecuteSqlRawAsync(lote, ct);
    }

    // Conecta a la base "master" (que siempre existe) y crea la base si falta.
    private static async Task IntentarCrearBaseAsync(string cadena, ILogger logger, CancellationToken ct)
    {
        try
        {
            var (cadenaMaster, nombreBase) = CadenaMaster(cadena);

            var opciones = new DbContextOptionsBuilder().UseSqlServer(cadenaMaster).Options;
            await using var master = new DbContext(opciones);

            await master.Database.ExecuteSqlRawAsync(SqlCrearBase(nombreBase), ct);
        }
        catch (Exception ex)
        {
            // Normal mientras SQL Server termina de arrancar: se vuelve a intentar.
            logger.LogDebug("Todavía no se pudo crear la base: {Motivo}", ex.Message);
        }
    }

    // ---------- Piezas puras (se prueban sin base de datos) ----------

    // Devuelve la misma cadena de conexión pero apuntando a "master", y el nombre de la base real.
    public static (string CadenaMaster, string NombreBase) CadenaMaster(string cadena)
    {
        var b = new DbConnectionStringBuilder { ConnectionString = cadena };

        string nombre = "SistemaClimatico";
        foreach (var clave in new[] { "Initial Catalog", "Database" })
        {
            if (b.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor?.ToString()))
                nombre = valor!.ToString()!;
            b.Remove(clave);
        }

        if (!NombreValido.IsMatch(nombre))
            throw new InvalidOperationException(
                $"El nombre de base de datos '{nombre}' no es válido (solo letras, números y guion bajo).");

        b["Initial Catalog"] = "master";
        return (b.ConnectionString, nombre);
    }

    public static string SqlCrearBase(string nombre)
        => $"IF DB_ID(N'{nombre}') IS NULL CREATE DATABASE [{nombre}];";

    // Parte el script en lotes usando las líneas "GO" y quita las líneas "USE ...;"
    // (la conexión ya apunta a la base correcta) y los PRINT informativos.
    public static IReadOnlyList<string> ExtraerLotes(string script)
    {
        var lotes = new List<string>();
        foreach (var bloque in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            var lineas = bloque
                .Split('\n')
                .Where(l => !Regex.IsMatch(l, @"^\s*USE\s+\[?\w+\]?\s*;?\s*$", RegexOptions.IgnoreCase))
                .Where(l => !Regex.IsMatch(l, @"^\s*PRINT\s", RegexOptions.IgnoreCase));

            var sql = string.Join('\n', lineas).Trim();

            // Un lote que solo tiene comentarios no hace falta enviarlo.
            var sinComentarios = Regex.Replace(sql, @"^\s*--.*$", "", RegexOptions.Multiline).Trim();
            if (sinComentarios.Length > 0)
                lotes.Add(sql);
        }
        return lotes;
    }

    private static string LeerScript()
    {
        var assembly = typeof(InicializadorBaseDatos).Assembly;
        using var stream = assembly.GetManifestResourceStream(NombreScript)
            ?? throw new InvalidOperationException(
                $"No se encontró el script '{NombreScript}' dentro de la API (recurso embebido).");
        using var lector = new StreamReader(stream);
        return lector.ReadToEnd();
    }
}
