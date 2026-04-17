IoT Network — Docker (API + PostgreSQL)
========================================

From the repository root:

  1. Optional: copy ..\.env.example to ..\.env and adjust ports.
  2. docker compose up -d --build
  3. API:    http://localhost:8080  (health: /api/health)
  4. Postgres from your PC: host localhost, port 55432 (default), database iotnetwork, user iot, password iot

Run the API on the host but use Postgres in Docker:

  - Visual Studio / VS Code: use launch profile "http (Docker Postgres)".
  - Or set environment variable:
      ConnectionStrings__DefaultConnection=Host=localhost;Port=55432;Database=iotnetwork;Username=iot;Password=iot

Blazor PWA (wwwroot/appsettings.json) already uses Api:BaseUrl http://localhost:8080/ — start the stack before running the PWA.

Stop:

  docker compose down

Remove data volume (reset DB):

  docker compose down -v
