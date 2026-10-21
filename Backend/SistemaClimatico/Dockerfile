# API .NET 10. El contexto de construccion es la RAIZ del repositorio
# (el docker-compose ya lo indica con "context: ..").

# ---------- 1. Compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo el .csproj y el script SQL (que va embebido en la API): asi Docker reutiliza
# la descarga de paquetes NuGet mientras no cambien las dependencias.
COPY Backend/SistemaClimatico/SistemaClimatico.csproj Backend/SistemaClimatico/
COPY Database/Scripts/01_create_tables.sql Database/Scripts/
RUN dotnet restore Backend/SistemaClimatico/SistemaClimatico.csproj

COPY Backend/SistemaClimatico/ Backend/SistemaClimatico/
RUN dotnet publish Backend/SistemaClimatico/SistemaClimatico.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- 2. Imagen final (solo lo necesario para ejecutar) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# La imagen trae un usuario sin privilegios ("app"): la API no corre como root.
USER $APP_UID

# Puerto interno (el sitio web / Caddy se conecta aqui; no se publica al exterior)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaClimatico.dll"]
