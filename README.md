# Open Case

Jogo de dedução multiplayer estilo detetive, jogado em tempo real no navegador.

**Status: em desenvolvimento** (estrutura inicial)

## Stack

- C# / ASP.NET Core
- Blazor Web App
- SignalR (tempo real)
- PostgreSQL + Entity Framework Core
- xUnit (testes)

## Arquitetura planejada

```
OpenCase/
├─ src/
│  ├─ OpenCase.Web/            # Blazor + SignalR Hub
│  ├─ OpenCase.Domain/         # Entidades e regras de domínio
│  ├─ OpenCase.Application/    # Serviços de jogo (engine)
│  ├─ OpenCase.Infrastructure/ # EF Core + PostgreSQL
│  └─ OpenCase.Shared/         # DTOs públicos/privados
├─ tests/
│  ├─ OpenCase.Domain.Tests/
│  └─ OpenCase.Application.Tests/
├─ docs/
└─ OpenCase.sln
```

## Roadmap

Desenvolvimento em 21 tarefas, da engine para a UI — ver `docs/`.
