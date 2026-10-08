# Beacon

A job-search assistant: track applications on a kanban board, upload your resume and job descriptions, and ask questions answered from your own documents (RAG with citations).

**Stack:** .NET 10 · ASP.NET Core minimal APIs · Blazor (Interactive Server + SSR) · EF Core + PostgreSQL/pgvector · Microsoft.Extensions.AI · Ollama · Docker Compose

## Run it

Requires Docker Desktop.

```bash
git clone https://github.com/shakybone5dloc/Beacon.git && cd Beacon
cp .env.example .env
docker compose up --build -d
```

Open http://localhost:8080.

On first start, Ollama downloads about 2.5 GB of models. The board works right away. Uploads and Ask come online when the download finishes (`docker compose logs -f ollama-pull`).

**NVIDIA GPU?** Uncomment `COMPOSE_FILE` in `.env` to give Ollama the GPU.

## Architecture

```
src/
  Beacon.Domain          entities, state machine (no dependencies)
  Beacon.Application     use cases, RAG prompt building
  Beacon.Contracts       DTOs shared by API and Web
  Beacon.Infrastructure  EF Core, pgvector, Ollama, background document processing
  Beacon.Api             minimal API, SSE streaming, rate limiting, health checks
  Beacon.Web             Blazor UI (talks to the API over HTTP only)
tests/                   xUnit v3 + Testcontainers, bUnit
```

**Containers:** `db` (pgvector) · `ollama` · `migrate` (one-shot EF migration bundle) · `ollama-pull` (one-shot model download) · `api` · `web`

## Develop

```bash
docker compose up -d db ollama            # infrastructure only
dotnet tool restore                       # pins dotnet-ef
dotnet run --project src/Beacon.Api
dotnet run --project src/Beacon.Web
dotnet test
```