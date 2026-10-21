# Despliegue con Docker y en una VPS

El sistema completo corre en **3 contenedores** que se levantan con un solo comando:

| Contenedor | Qué hace | Se ve desde internet |
|---|---|---|
| `sqlserver` | SQL Server 2022 (base de datos) | No (solo `127.0.0.1:1434` dentro de la VPS) |
| `api` | API .NET 10 + SignalR (tiempo real) | No (solo la red interna de Docker) |
| `web` | Caddy: entrega Angular, reenvía `/api` y `/hubs` a la API y gestiona el HTTPS | **Sí**, puertos 80 y 443 |

La primera vez que arranca, la API **crea sola la base de datos y las tablas** (no hay scripts que ejecutar a mano) y el administrador inicial.

---

## 1. Probarlo primero en tu PC (Docker Desktop)

1. En la carpeta `Docker/` copia `.env.example` como `.env` y edita los valores (ver sección 4).
2. En esa misma carpeta:
   ```powershell
   docker compose up -d --build
   ```
   La primera vez tarda varios minutos (descarga imágenes y compila Angular y .NET).
3. Mira cómo arranca la API (la base tarda ~30-60 s en estar lista; es normal ver "Esperando a SQL Server…"):
   ```powershell
   docker compose logs -f api
   ```
   Cuando veas `Application started` abre **http://localhost** e inicia sesión con `ADMIN_CORREO` / `ADMIN_PASSWORD`.
4. Para apagarlo: `docker compose down` (los datos se conservan). Para borrar también los datos: `docker compose down -v`.

> El puerto 80 debe estar libre en tu PC. Tu flujo de desarrollo de siempre (3 terminales) sigue igual: usa `docker compose up -d sqlserver` para levantar **solo** la base de datos.

---

## 2. Qué necesitas para la VPS

- Una VPS con **Ubuntu 24.04** (o similar), **mínimo 2 GB de RAM** (recomendado 4 GB; SQL Server es pesado) y 20 GB de disco.
- Acceso por SSH.
- *(Opcional)* un dominio para tener HTTPS.

## 3. Preparar la VPS (una sola vez)

Conéctate por SSH y ejecuta:

```bash
# Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER      # luego cierra la sesión SSH y vuelve a entrar

# Firewall: solo SSH, web y HTTPS
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 443/udp
sudo ufw enable

# Solo si la VPS tiene 2 GB de RAM: agrega 2 GB de swap
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile
sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

Baja el proyecto:

```bash
git clone https://github.com/TU-USUARIO/TU-REPO.git sistema-climatico
cd sistema-climatico/Docker
cp .env.example .env
nano .env
```

## 4. Configurar `Docker/.env`

| Variable | Qué poner |
|---|---|
| `MSSQL_SA_PASSWORD` | Contraseña fuerte para SQL Server (mayúsculas, minúsculas, números y símbolos, mínimo 8). |
| `MSSQL_MEMORY_LIMIT_MB` | `2048` (o `1024` si la VPS tiene 2 GB de RAM). |
| `JWT_KEY` | Clave larga y aleatoria: `openssl rand -hex 32` |
| `ADMIN_CORREO` / `ADMIN_PASSWORD` | Tu usuario administrador inicial. |
| `SITE_ADDRESS` | `:80` si no tienes dominio, o `tudominio.com` para HTTPS automático. |
| `PUBLIC_URL` | `http://IP-DE-LA-VPS` o `https://tudominio.com`. |

**No uses** los caracteres `$ ; "` ni espacios en las contraseñas.

## 5. Levantar el sistema

```bash
cd ~/sistema-climatico/Docker
docker compose up -d --build
docker compose logs -f api        # espera "Application started" (Ctrl+C para salir del log)
```

Abre `http://IP-DE-LA-VPS` en el navegador.

### Con dominio (HTTPS gratis y automático)

1. En el panel de tu dominio crea un registro **A** que apunte a la IP de la VPS (puede tardar unos minutos en propagarse).
2. En `.env`: `SITE_ADDRESS=tudominio.com` y `PUBLIC_URL=https://tudominio.com`.
3. `docker compose up -d` — Caddy pide el certificado a Let's Encrypt solo y lo renueva solo. Los puertos 80 y 443 deben estar abiertos.

## 6. Operación diaria

```bash
docker compose ps                    # estado de los contenedores
docker compose logs -f api           # logs de la API (también: web, sqlserver)
docker compose restart api           # reiniciar solo la API

# Actualizar a una versión nueva del código
cd ~/sistema-climatico && git pull
cd Docker && docker compose up -d --build
```

Los datos viven en el volumen `sqlserver_data`: sobreviven a reinicios y actualizaciones.

### Copia de seguridad de la base de datos

```bash
cd ~/sistema-climatico/Docker
source .env
docker exec sistema-climatico-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
  -Q "BACKUP DATABASE SistemaClimatico TO DISK='/var/opt/mssql/data/respaldo.bak' WITH INIT"
docker cp sistema-climatico-sql:/var/opt/mssql/data/respaldo.bak ./respaldo-$(date +%F).bak
```

(Si la ruta `/opt/mssql-tools18/...` no existe en tu imagen, prueba `/opt/mssql-tools/bin/sqlcmd` y quita la opción `-C`.)

## 7. Si algo falla

| Síntoma | Qué revisar |
|---|---|
| La API se reinicia sola | `docker compose logs api`. Casi siempre es `JWT_KEY` vacía/corta (mínimo 32 caracteres) o contraseña de SQL Server que no cumple la política. |
| "Esperando a SQL Server…" muchas veces | Normal los primeros ~60 s. Si pasa de 3 minutos: `docker compose logs sqlserver` (suele ser poca RAM o contraseña débil). |
| `sqlserver` se detiene | La VPS no tiene suficiente memoria: agrega swap y baja `MSSQL_MEMORY_LIMIT_MB`. |
| La página no abre desde internet | Puertos 80/443 abiertos en `ufw` **y** en el panel del proveedor de la VPS. |
| No sale el candado HTTPS | El registro A del dominio debe apuntar a la IP de la VPS; revisa `docker compose logs web`. |
| El mapa se ve sin calles | El navegador necesita internet para descargar los mapas de OpenStreetMap. |
| Cambié la contraseña de SQL y ya no conecta | La contraseña se fija la primera vez que se crea el volumen. Borra los datos con `docker compose down -v` (¡pierdes la base!) o cámbiala dentro de SQL Server. |

## 8. Notas de seguridad

- Cambia **todas** las contraseñas de `.env.example`; el archivo `.env` nunca se sube a GitHub.
- La base de datos solo escucha en `127.0.0.1`: para verla desde tu PC usa un túnel SSH (`ssh -L 1434:127.0.0.1:1434 usuario@IP`) y conecta tu herramienta a `localhost,1434`.
- Las fechas se guardan en **UTC**; cada navegador las muestra en la hora local de quien las ve.
