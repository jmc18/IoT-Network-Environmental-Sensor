# Docker - Cosmos DB (legacy)

La configuración principal de Docker está en la **raíz del proyecto**:

```bash
# Desde la raíz
docker compose up -d --build
```

Ver [README.Docker.md](../README.Docker.md) en la raíz para documentación completa.

## Configuración

1. Copia `IoTBackend/Functions/local.settings.example.json` a `local.settings.json`
2. La connection string del emulador ya está en el ejemplo
3. Ejecuta las Functions localmente: `func start` o F5 desde IoTBackend/Functions

## Connection string del emulador

```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;DisableServerCertificateValidation=True;
```

## Detener

```bash
docker compose -f docker/docker-compose.yml down
```
