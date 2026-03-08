# Docker - IoT Network Backend (estructura como A_PG)

Este proyecto incluye una configuración de Docker Compose que ejecuta las Azure Functions, Cosmos DB Emulator y Azurite para desarrollo local.

## Requisitos previos

- Docker Desktop ([Instalar Docker](https://docs.docker.com/get-docker/))
- Docker Compose v2 (incluido en Docker Desktop)
- Al menos 4GB de RAM para Cosmos DB Emulator

## Inicio rápido

```bash
# Construir e iniciar todo (Functions + Cosmos DB + Azurite)
docker compose up -d --build

# Ver logs
docker compose logs -f iot.functions
```

**URLs disponibles:**
- API Functions: http://localhost:7071
- Cosmos Data Explorer: https://localhost:8081
- Azurite Blob: http://localhost:10000

## Estructura de archivos (como A_PG)

```
IoT_Network/
├── docker-compose.yml              # Producción (Functions + Cosmos + Azurite)
├── docker-compose.dcproj          # Proyecto Docker Compose para Visual Studio
├── docker-compose.override.yml.example  # Template para override local
├── IoTBackend/Functions/
│   └── Dockerfile                 # Dockerfile de las Functions
└── .dockerignore
```

## Configuración

### Override local (opcional)

Copia el ejemplo para configuraciones locales que no se versionan:

```bash
cp docker-compose.override.yml.example docker-compose.override.yml
```

### Variables de entorno en compose

Las Functions reciben automáticamente:
- `AzureWebJobsStorage`: conexión a Azurite (blob, queue, table)
- `CosmosDb__ConnectionString`: Cosmos Emulator vía `cosmosdb:8081`
- `IOT_INGEST_API_KEY`: API key para ingesta

## Comandos útiles

```bash
# Iniciar servicios
docker compose up -d

# Ver logs
docker compose logs -f iot.functions

# Detener
docker compose down

# Reconstruir solo Functions
docker compose up -d --build iot.functions
```

## Probar la API

```bash
# Ingestar dato (con API key)
curl -X POST http://localhost:7071/api/ingest \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-change-in-production" \
  -d '{"nodeId":"node-1","lat":40.41,"lon":-3.70,"temperature":22.5,"humidity":55}'

# Consultar lecturas
curl "http://localhost:7071/api/readings?nodeId=node-1"
```

## Referencias

- [Azure Functions Docker](https://github.com/Azure/azure-functions-docker)
- [Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/emulator)
