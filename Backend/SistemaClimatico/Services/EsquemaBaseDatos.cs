using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Agrega a la base de datos lo que necesita la Fase C (comunidades y ubicación de
// los sensores) la primera vez que arranca la API. Es seguro ejecutarlo muchas veces:
// cada paso solo actúa si lo que crea todavía no existe, y no borra ni cambia datos.
//
// El mismo SQL está en Database/Scripts/02_comunidades.sql por si prefieres
// ejecutarlo a mano (por ejemplo, en Azure Data Studio).
public static class EsquemaBaseDatos
{
    private static readonly string[] Pasos =
    [
        // 1. Tabla de comunidades
        @"IF OBJECT_ID(N'dbo.Comunidades', N'U') IS NULL
          CREATE TABLE dbo.Comunidades (
              IdComunidad INT IDENTITY(1,1) PRIMARY KEY,
              Nombre NVARCHAR(100) NOT NULL,
              Descripcion NVARCHAR(300) NULL,
              Latitud DECIMAL(9,6) NOT NULL,
              Longitud DECIMAL(9,6) NOT NULL,
              Activa BIT NOT NULL DEFAULT 1,
              FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
          );",

        // 2. Columnas nuevas en Sensores
        @"IF COL_LENGTH(N'dbo.Sensores', N'IdComunidad') IS NULL
              ALTER TABLE dbo.Sensores ADD IdComunidad INT NULL;",
        @"IF COL_LENGTH(N'dbo.Sensores', N'Latitud') IS NULL
              ALTER TABLE dbo.Sensores ADD Latitud DECIMAL(9,6) NULL;",
        @"IF COL_LENGTH(N'dbo.Sensores', N'Longitud') IS NULL
              ALTER TABLE dbo.Sensores ADD Longitud DECIMAL(9,6) NULL;",

        // 3. Llave foránea Sensores -> Comunidades (en un paso aparte: la columna ya debe existir)
        @"IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Sensores_Comunidades')
              ALTER TABLE dbo.Sensores ADD CONSTRAINT FK_Sensores_Comunidades
                  FOREIGN KEY (IdComunidad) REFERENCES dbo.Comunidades (IdComunidad);",
    ];

    public static async Task AplicarAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SistemaClimaticoContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("EsquemaBaseDatos");

        foreach (var paso in Pasos)
            await db.Database.ExecuteSqlRawAsync(paso);

        logger.LogInformation("Esquema de base de datos al día (comunidades y ubicación de sensores).");
    }
}
