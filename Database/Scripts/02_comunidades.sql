USE SistemaClimatico;
GO

-- =========================================================
-- FASE C: comunidades y ubicacion de los sensores en el mapa
-- Es seguro ejecutarlo varias veces.
-- (La API ya lo hace sola al arrancar; este script es opcional.)
-- =========================================================

IF OBJECT_ID(N'dbo.Comunidades', N'U') IS NULL
    CREATE TABLE dbo.Comunidades (
        IdComunidad INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(100) NOT NULL,
        Descripcion NVARCHAR(300) NULL,
        Latitud DECIMAL(9,6) NOT NULL,
        Longitud DECIMAL(9,6) NOT NULL,
        Activa BIT NOT NULL DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
GO

IF COL_LENGTH(N'dbo.Sensores', N'IdComunidad') IS NULL
    ALTER TABLE dbo.Sensores ADD IdComunidad INT NULL;
GO
IF COL_LENGTH(N'dbo.Sensores', N'Latitud') IS NULL
    ALTER TABLE dbo.Sensores ADD Latitud DECIMAL(9,6) NULL;
GO
IF COL_LENGTH(N'dbo.Sensores', N'Longitud') IS NULL
    ALTER TABLE dbo.Sensores ADD Longitud DECIMAL(9,6) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Sensores_Comunidades')
    ALTER TABLE dbo.Sensores ADD CONSTRAINT FK_Sensores_Comunidades
        FOREIGN KEY (IdComunidad) REFERENCES dbo.Comunidades (IdComunidad);
GO
