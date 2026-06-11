# Deploy — Open Case

## Variáveis de ambiente

| Variável | Descrição |
|---|---|
| `DATABASE_URL` | Connection string PostgreSQL (ex.: `Host=db;Port=5432;Database=opencase;Username=postgres;Password=postgres`). Tem prioridade sobre o appsettings. |
| `ASPNETCORE_ENVIRONMENT` | `Development` ou `Production`. |

## Desenvolvimento local (com Docker)

```bash
docker compose up --build
# aplicação: http://localhost:8080
# health check: http://localhost:8080/health
```

O `docker-compose.yml` sobe PostgreSQL 17 + a aplicação. As migrations e o seed
rodam automaticamente na inicialização quando há banco configurado.

## Desenvolvimento local (sem Docker)

Requer .NET SDK 10 e um PostgreSQL local (ou apenas rode sem banco — o estado
das partidas vive em memória).

```bash
dotnet restore
dotnet build
dotnet test
dotnet ef database update --project src/OpenCase.Infrastructure
dotnet run --project src/OpenCase.Web
```

## Produção

1. Construa a imagem: `docker build -t opencase .`
2. Forneça `DATABASE_URL` e `ASPNETCORE_ENVIRONMENT=Production`.
3. Exponha a porta 8080 (ou ajuste `ASPNETCORE_URLS`).
4. Monitore `GET /health` (responde `Healthy`).
