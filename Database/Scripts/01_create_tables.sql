USE SistemaClimatico;
GO

-- =========================================================
-- SISTEMA WEB DE MONITOREO Y ALERTA TEMPRANA
-- Proyecto I - Desarrollo Web
-- SQL Server 2022+
-- =========================================================


-- =========================================================
-- 1. TABLA DE USUARIOS
-- =========================================================

CREATE TABLE Usuarios (
    IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
    NombreCompleto NVARCHAR(150) NOT NULL,
    Correo NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Rol NVARCHAR(50) NOT NULL DEFAULT 'Usuario',
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT CK_Usuarios_Rol
        CHECK (Rol IN ('Administrador', 'Operador', 'Usuario'))
);
GO


-- =========================================================
-- 2. TABLA DE SENSORES
-- =========================================================

CREATE TABLE Sensores (
    IdSensor INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Tipo NVARCHAR(50) NOT NULL,
    UnidadMedida NVARCHAR(30) NOT NULL,
    Ubicacion NVARCHAR(200) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT CK_Sensores_Tipo
        CHECK (
            Tipo IN (
                'Temperatura',
                'Humedad',
                'Viento',
                'Lluvia',
                'NivelRio',
                'Otro'
            )
        )
);
GO


-- =========================================================
-- 3. TABLA DE LECTURAS DE SENSORES
-- =========================================================

CREATE TABLE LecturasSensores (
    IdLectura BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdSensor INT NOT NULL,
    Valor DECIMAL(10,2) NOT NULL,
    FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_LecturasSensores_Sensores
        FOREIGN KEY (IdSensor)
        REFERENCES Sensores(IdSensor)
);
GO


-- =========================================================
-- 4. TABLA DE ALERTAS
-- =========================================================

CREATE TABLE Alertas (
    IdAlerta BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdSensor INT NULL,
    Nivel NVARCHAR(20) NOT NULL,
    TipoFenomeno NVARCHAR(50) NOT NULL,
    Mensaje NVARCHAR(500) NOT NULL,
    ValorDetectado DECIMAL(10,2) NULL,
    FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Atendida BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_Alertas_Sensores
        FOREIGN KEY (IdSensor)
        REFERENCES Sensores(IdSensor),

    CONSTRAINT CK_Alertas_Nivel
        CHECK (
            Nivel IN (
                'Verde',
                'Amarillo',
                'Naranja',
                'Rojo'
            )
        ),

    CONSTRAINT CK_Alertas_TipoFenomeno
        CHECK (
            TipoFenomeno IN (
                'Inundacion',
                'Sequia',
                'Tormenta',
                'Helada',
                'IncendioForestal',
                'Otro'
            )
        )
);
GO


-- =========================================================
-- 5. HISTORIAL DE EVENTOS
-- =========================================================

CREATE TABLE HistorialEventos (
    IdEvento BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdAlerta BIGINT NULL,
    TipoFenomeno NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(500) NOT NULL,
    FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_HistorialEventos_Alertas
        FOREIGN KEY (IdAlerta)
        REFERENCES Alertas(IdAlerta),

    CONSTRAINT CK_HistorialEventos_TipoFenomeno
        CHECK (
            TipoFenomeno IN (
                'Inundacion',
                'Sequia',
                'Tormenta',
                'Helada',
                'IncendioForestal',
                'Otro'
            )
        )
);
GO


-- =========================================================
-- 6. BITÁCORA DE ACCIONES DE USUARIOS
-- =========================================================

CREATE TABLE BitacoraAcciones (
    IdBitacora BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NULL,
    Accion NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_BitacoraAcciones_Usuarios
        FOREIGN KEY (IdUsuario)
        REFERENCES Usuarios(IdUsuario)
);
GO


-- =========================================================
-- ÍNDICES
-- =========================================================

CREATE INDEX IX_LecturasSensores_IdSensor_FechaHora
ON LecturasSensores(IdSensor, FechaHora);
GO

CREATE INDEX IX_Alertas_FechaHora
ON Alertas(FechaHora);
GO

CREATE INDEX IX_HistorialEventos_FechaHora
ON HistorialEventos(FechaHora);
GO

CREATE INDEX IX_BitacoraAcciones_FechaHora
ON BitacoraAcciones(FechaHora);
GO


PRINT 'Base de datos SistemaClimatico creada correctamente.';
PRINT 'Tablas, relaciones e indices creados correctamente.';
GO