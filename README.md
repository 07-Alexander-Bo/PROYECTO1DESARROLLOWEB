# 🌦️ Sistema Web de Monitoreo y Alerta Temprana Climática

Proyecto I – Desarrollo Web. Sistema que permite registrar sensores climáticos (temperatura, humedad, viento, lluvia, nivel de río), recibir sus lecturas, generar alertas automáticas según umbrales de riesgo y llevar un historial de eventos, todo a través de una API REST protegida con JWT y un frontend en Angular.

## 📑 Tabla de contenidos

- [Arquitectura](#-arquitectura)
- [Tecnologías](#-tecnologías)
- [Estructura del repositorio](#-estructura-del-repositorio)
- [Modelo de datos](#-modelo-de-datos)
- [Motor de alertas](#-motor-de-alertas)
- [Requisitos previos](#-requisitos-previos)
- [Instalación y puesta en marcha](#-instalación-y-puesta-en-marcha)
  - [1. Base de datos (Docker + SQL Server)](#1-base-de-datos-docker--sql-server)
  - [2. Backend (.NET API)](#2-backend-net-api)
  - [3. Frontend (Angular)](#3-frontend-angular)
- [Endpoints de la API](#-endpoints-de-la-api)
- [Roles y autenticación](#-roles-y-autenticación)
- [Rutas del frontend](#-rutas-del-frontend)
- [Configuración de entornos](#-configuración-de-entornos)

## 🏗️ Arquitectura

El proyecto sigue una arquitectura de tres capas:

```
┌─────────────────────┐       HTTP/JWT       ┌──────────────────────┐       EF Core       ┌──────────────────┐
│   Frontend Angular   │  ───────────────────▶ │   Backend ASP.NET     │ ──────────────────▶ │  SQL Server 2022  │
│ (SistemaClimaticoWeb)│ ◀─────────────────── │   Core Web API         │ ◀────────────────── │  (Docker)         │
└─────────────────────┘                       │   (SistemaClimatico)   │                     └──────────────────┘
                                               └──────────────────────┘
```

- El **frontend** (Angular) consume la API mediante `HttpClient`, adjunta el token JWT vía interceptor y protege rutas con guards.
- El **backend** (.NET) expone controladores REST, valida tokens JWT y usa Entity Framework Core para hablar con SQL Server.
- La **base de datos** corre en un contenedor Docker de SQL Server 2022.

## 🛠️ Tecnologías

**Backend**
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 (SQL Server)
- Autenticación JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Hash de contraseñas con `PasswordHasher<Usuario>` (ASP.NET Identity)
- OpenAPI

**Frontend**
- Angular 22 (standalone components, SSR con Express)
- RxJS
- TypeScript

**Base de datos**
- SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) vía Docker Compose

## 📂 Estructura del repositorio

```
PROYECTO1DESARROLLOWEB-main/
├── Backend/
│   └── SistemaClimatico/
│       ├── Controllers/       # AuthController, SensoresController, AlertasController, HistorialController
│       ├── Models/            # Entidades EF Core (Usuario, Sensore, Alerta, LecturasSensore, HistorialEvento, BitacoraAccione)
│       ├── Dtos/               # Objetos de transferencia de datos por controlador
│       ├── Services/          # TokenService (JWT), MotorAlertas (lógica de umbrales)
│       ├── Program.cs          # Configuración de la app (DI, CORS, JWT, EF Core)
│       └── appsettings.json    # Connection string, configuración JWT
├── Frontend/
│   └── SistemaClimaticoWeb/
│       └── src/app/
│           ├── core/           # Servicios HTTP, interceptor de auth, guards, modelos
│           └── features/       # login, registro, dashboard, sensores, alertas, historial, admin, shell
├── Database/
│   └── Scripts/
│       ├── 01_create_tables.sql
│       └── Scripts de consulta (usuarios, sensores, alertas, lecturas, info general)
├── Docker/
│   └── docker-compose.yml      # Contenedor de SQL Server
└── .gitignore
```

## 🗄️ Modelo de datos

| Tabla | Descripción |
|---|---|
| **Usuarios** | Cuentas del sistema. Rol restringido a `Administrador`, `Operador` o `Usuario` (constraint `CK_Usuarios_Rol`). |
| **Sensores** | Catálogo de sensores. Tipo restringido a `Temperatura`, `Humedad`, `Viento`, `Lluvia`, `NivelRio` u `Otro`. |
| **LecturasSensores** | Lecturas históricas de cada sensor (valor + fecha/hora). |
| **Alertas** | Alertas generadas automáticamente al evaluar una lectura (nivel, tipo de fenómeno, mensaje, si fue atendida). |
| **HistorialEventos** | Bitácora de eventos climáticos asociados a alertas. |
| **BitacoraAcciones** | Registro de acciones realizadas por los usuarios en el sistema. |

## 🚨 Motor de alertas

El `MotorAlertas` (servicio estático en el backend) evalúa cada lectura de sensor contra umbrales predefinidos y devuelve un nivel de riesgo (`Verde`, `Amarillo`, `Naranja`, `Rojo`) junto con el fenómeno detectado:

| Tipo de sensor | Fenómenos que puede detectar |
|---|---|
| Temperatura | Helada (frío extremo) / Incendio forestal (calor extremo) |
| Humedad | Sequía |
| Viento | Tormenta |
| Lluvia | Tormenta / Inundación |
| NivelRio | Inundación |

> ⚠️ Los umbrales están simulados con fines académicos y no representan valores meteorológicos oficiales.

## 📋 Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (compatible con Angular 22) y npm
- [Angular CLI](https://angular.dev/tools/cli) (`npm install -g @angular/cli`)
- [Docker](https://www.docker.com/) y Docker Compose

## 🚀 Instalación y puesta en marcha

### 1. Base de datos (Docker + SQL Server)

```bash
cd Docker
docker compose up -d
```

Esto levanta SQL Server 2022 en el puerto **1434** con la contraseña definida en `docker-compose.yml` (`SistemaClima2026!`).

Luego, crea la base de datos `SistemaClimatico` y ejecuta el script de creación de tablas:

```bash
# Con sqlcmd o tu cliente SQL favorito (Azure Data Studio, SSMS, DBeaver, etc.)
CREATE DATABASE SistemaClimatico;
GO
```

```bash
sqlcmd -S localhost,1434 -U sa -P "SistemaClima2026!" -d SistemaClimatico -i Database/Scripts/01_create_tables.sql
```

Los demás scripts en `Database/Scripts/` (`VER USUARIOS.sql`, `VER SENSORES.sql`, `VER ALERTAS.sql`, `VER LECTURA SENSORES.sql`, `INFO GENERAL.sql`) son consultas útiles para inspeccionar los datos.

### 2. Backend (.NET API)

```bash
cd Backend/SistemaClimatico
dotnet restore
dotnet run
```

Antes de ejecutar, revisa `appsettings.json` / `appsettings.Development.json`:

- `ConnectionStrings:SistemaClimatico`: cadena de conexión a SQL Server.
- `Jwt:Key`: clave secreta para firmar tokens (mínimo 32 caracteres). **Cámbiala antes de producción.**
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes`.

La API queda disponible por defecto en la URL configurada en `Properties/launchSettings.json`, y expone un endpoint de prueba de conexión en `GET /db-test`.

### 3. Frontend (Angular)

```bash
cd Frontend/SistemaClimaticoWeb
npm install
npm start
```

La app corre en `http://localhost:4200` (el backend tiene habilitado CORS para este origen mediante la política `AngularDev`).

## 🔌 Endpoints de la API

Prefijo base: `api/`

### Autenticación — `api/auth`
| Método | Ruta | Descripción | Acceso |
|---|---|---|---|
| POST | `/api/auth/registro` | Crea un nuevo usuario | Público |
| POST | `/api/auth/login` | Inicia sesión y devuelve un JWT | Público |
| GET | `/api/auth/perfil` | Devuelve el perfil del usuario autenticado | Autenticado |
| GET | `/api/auth/admin-only` | Endpoint de ejemplo restringido a administradores | Rol `Administrador` |

### Sensores — `api/sensores`
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/sensores` | Lista los sensores |
| GET | `/api/sensores/{id}` | Obtiene un sensor por id |
| POST | `/api/sensores` | Crea un sensor |
| PUT | `/api/sensores/{id}` | Actualiza un sensor |
| GET | `/api/sensores/{id}/lecturas` | Lista lecturas de un sensor |
| POST | `/api/sensores/{id}/lecturas` | Registra una nueva lectura (dispara el motor de alertas) |

### Alertas — `api/alertas`
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/alertas` | Lista las alertas |
| GET | `/api/alertas/{id}` | Obtiene una alerta por id |
| GET | `/api/alertas/estado-actual` | Estado de alerta actual del sistema |

### Historial — `api/historial`
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/historial` | Lista eventos históricos |
| GET | `/api/historial/{id}` | Obtiene un evento por id |

## 🔐 Roles y autenticación

El sistema usa JWT Bearer. Los roles válidos (definidos también como constraint en la base de datos) son:

- `Administrador`
- `Operador`
- `Usuario`

El token se genera en `TokenService` al hacer login y debe enviarse en el header `Authorization: Bearer <token>` para acceder a los endpoints protegidos.

## 🧭 Rutas del frontend

| Ruta | Componente | Protección |
|---|---|---|
| `/login` | Login | Pública |
| `/registro` | Registro | Pública |
| `/dashboard` | Dashboard | `authGuard` |
| `/sensores` | Sensores | `authGuard` |
| `/alertas` | Alertas | `authGuard` |
| `/historial` | Historial | `authGuard` |
| `/admin` | Admin | `authGuard` + `adminGuard` |

## ⚙️ Configuración de entornos

El frontend define la URL base de la API en:

```
Frontend/SistemaClimaticoWeb/src/environments/environment.ts
Frontend/SistemaClimaticoWeb/src/environments/environment.development.ts
```

Ajusta la URL de la API en estos archivos si tu backend corre en un puerto distinto al configurado por defecto.

---

> 📌 **Nota:** cambia la contraseña de SQL Server (`docker-compose.yml`) y la clave JWT (`appsettings.json`) antes de usar este proyecto fuera de un entorno académico/local.
