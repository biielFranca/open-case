# Roadmap do MVP — Open Case

Desenvolvimento em 21 tarefas, nesta ordem (engine antes da UI):

1. **Tarefa 01 — Estrutura** — solution .NET, projetos, refs, build e teste básico
2. **Tarefa 02 — Domínio** — enums e modelos centrais (domínio limpo, sem EF Core)
3. **Tarefa 03 — Engine de cartas** — GameSetupService (solução secreta + distribuição)
4. **Tarefa 04 — RoomService** — lobby, código de sala, host, peões, pronto, início
5. **Tarefa 05 — TurnService** — ordem inicial, empate, avanço, SkipNextTurn, dado 1–12
6. **Tarefa 06 — BoardService** — grid 20x20, 12 locais, entradas, passagens secretas
7. **Tarefa 07 — MovementService** — validação de caminho (sem diagonal, sem parede)
8. **Tarefa 08 — SuggestionService** — palpite em local (backend define o local)
9. **Tarefa 09 — RefutationService** — ordem reversa, carta privada só ao acusador
10. **Tarefa 10 — FinalAccusationService** — usa último palpite não refutado
11. **Tarefa 11 — HintService** — dicas Private/Public/Noise
12. **Tarefa 12 — EF Core/PostgreSQL** — DbContext, migrations, seed
13. **Tarefa 13 — SignalR Hub** — GameHub com grupos por sala
14. **Tarefa 14 — DTOs** — públicos vs privados, sem vazar solução
15. **Tarefa 15 — Lobby UI** — Blazor, tema dark investigativo
16. **Tarefa 16 — Game UI** — tabuleiro, cartas, modais, mobile com abas
17. **Tarefa 17 — NotesGrid** — bloco de notas privado
18. **Tarefa 18 — Passagens secretas** — ação de turno
19. **Tarefa 19 — Logs técnicos** — auditoria interna de eventos
20. **Tarefa 20 — Deploy** — Docker, docker-compose, appsettings, health check
21. **Tarefa 21 — README final** — status real do projeto

Regras do processo: não pular etapas; cada tarefa entra com testes; uma tarefa por commit.
