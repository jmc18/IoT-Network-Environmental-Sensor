# Docker — API ASP.NET Core + PostgreSQL (patrón InvoiceMe / A_PG)

Stack local: **PostgreSQL**, **IoTNetwork.Api**, **Adminer** (como InvoiceMe).

## Requisitos

- Docker Desktop y Docker Compose v2

## Inicio rápido

```bash
# Raíz del repo
docker compose up -d --build
```

**URLs (por defecto):**

| Servicio | URL |
|-----------|-----|
| API       | http://localhost:5099 |
| PostgreSQL| `localhost:55432` (usuario `iot`, contraseña `iot`, BD `iotnetwork`) |
| Adminer   | http://localhost:8082 |

Variables opcionales: copia [.env.example](.env.example) a `.env` y ajusta `POSTGRES_PORT`, `API_PORT`, `ADMINER_PORT`.

## PWA contra API en Docker

En Visual Studio, ejecuta la PWA con el perfil **https (Staging API / Docker)** (`ASPNETCORE_ENVIRONMENT=Staging`).Los `wwwroot/appsettings.Staging.json` apuntan a `http://localhost:5099/`.

Para API en local (`dotnet run`, puerto 8080), usa el perfil **https** (Development).

## Overrides locales

```bash
cp docker-compose.override.yml.example docker-compose.override.yml
```

## Azure Functions en esta solución

Las Functions incluidas en **`IoT_Network.sln`** están en **`src/Backend/`** (no en la carpeta `IoTBackend/`). Para ejecutarlas en local, usa `local.settings.json` en ese proyecto.
