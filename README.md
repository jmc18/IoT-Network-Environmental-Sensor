# IoT Network

## Solución principal: `IoT_Network.sln` / `IoT_Network.slnx`

Abre **`IoT_Network.sln`** (Visual Studio) o **`IoT_Network.slnx`** (.NET SDK / VS 17.13+). Incluye únicamente:

| Proyecto | Ruta | Rol |
|----------|------|-----|
| **Backend** | `src/Backend/` | Azure Functions (isolated worker) |
| **IoTNetwork.Core** | `src/IoTNetwork.Core/` | Dominio / contratos |
| **IoTNetwork.Infrastructure** | `src/IoTNetwork.Infrastructure/` | EF Core, PostgreSQL |
| **IoTNetwork.Api** | `src/IoTNetwork.Api/` | API web (Minimal APIs) |
| **IoTNetwork.Pwa** | `src/IoTNetwork.Pwa/` | Blazor WebAssembly |
| **docker-compose** | `docker-compose.dcproj` | API + PostgreSQL + Adminer (desarrollo) |

### Fuera de esta solución

La carpeta **`IoTBackend/`** (Functions + `IoTBackend.Shared` + `IoTBackend.slnx`) es un **stack aparte** y no forma parte de `IoT_Network.sln`.

## Desarrollo local

- **API + Postgres**: ver [README.Docker.md](README.Docker.md) (`docker compose up`).
- **PWA**: perfil *https* contra API en `localhost:8080`, o *https (Staging API / Docker)* contra `localhost:5099`.

## Build de la solución

```bash
dotnet build IoT_Network.sln -c Release
# o
dotnet build IoT_Network.slnx -c Release
```
