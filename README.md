# Open Case 🔍

Jogo de dedução multiplayer estilo detetive, jogado em tempo real no navegador.
Reúna de 3 a 8 investigadores, percorra a mansão, dê palpites e descubra
**quem**, **onde** e **com o quê** — antes dos outros.

**Status: em desenvolvimento** — engine, persistência, hub em tempo real e UI
funcionais; ainda sem arte final, sem reconexão de sessão e sem partidas
persistidas entre reinícios do servidor.

## Regras

- A cada partida o jogo sorteia uma solução secreta: 1 suspeito + 1 local + 1 arma.
- As demais cartas são embaralhadas e distribuídas entre os jogadores (distribuição desigual é permitida).
- A ordem dos turnos é definida por dado inicial (1–6); empates no maior valor são re-sorteados.
- No turno, o jogador rola um dado de 1 a 12 e move o peão pela planta 30x30 da mansão (sem diagonais, sem atravessar paredes; é preciso usar todo o dado, exceto ao entrar em um local).
- Dentro de um local, o jogador pode dar um **palpite** (suspeito + arma; o local é sempre o atual). O peão do suspeito citado é puxado para o local.
- A **refutação** começa no jogador anterior ao acusador e segue em ordem reversa: quem tiver carta citada é obrigado a mostrar uma — apenas ao acusador.
- Se ninguém refutar, o autor pode fazer a **acusação final** com aquele palpite. Acertou: venceu. Errou: perde o próximo turno e a solução continua secreta.
- Alguns locais têm **passagens secretas**; usá-las consome a ação do turno.
- **Dicas** aparecem em momentos aleatórios: privadas, públicas ou apenas ruído (que nunca mente).
- Cada jogador tem um **bloco de notas privado** para marcar cartas descartadas.

## Stack

- C# / ASP.NET Core (.NET 10)
- Blazor Web App (Interactive Server)
- SignalR (tempo real)
- PostgreSQL + Entity Framework Core
- xUnit + bUnit (testes)
- Docker / docker-compose

## Arquitetura

```
OpenCase/
├─ src/
│  ├─ OpenCase.Web/            # Blazor UI + GameHub (SignalR)
│  ├─ OpenCase.Domain/         # Entidades e enums (domínio puro, sem EF)
│  ├─ OpenCase.Application/    # Engine: serviços de jogo e mapeamento p/ DTOs
│  ├─ OpenCase.Infrastructure/ # EF Core + PostgreSQL (DbContext, migrations, seed)
│  └─ OpenCase.Shared/         # DTOs públicos e privados
├─ tests/
│  ├─ OpenCase.Domain.Tests/
│  └─ OpenCase.Application.Tests/
├─ docs/                       # Roadmap e instruções de deploy
├─ Dockerfile
├─ docker-compose.yml
└─ OpenCase.slnx
```

Princípios: o estado público enviado pelo hub **nunca** contém a solução
secreta, cartas alheias, carta mostrada em refutação ou notas privadas (há
teste garantindo isso). O frontend nunca escolhe o local do palpite nem a
combinação da acusação final — o backend deriva ambos.

## Como rodar

Com Docker (sobe PostgreSQL + aplicação):

```bash
docker compose up --build
# http://localhost:8080
```

Sem Docker (requer .NET SDK 10; banco é opcional em desenvolvimento):

```bash
dotnet restore
dotnet build
dotnet run --project src/OpenCase.Web
```

Com PostgreSQL local, configure `ConnectionStrings:Default`
(em `appsettings.Development.json`) ou a variável `DATABASE_URL` e rode:

```bash
dotnet ef database update --project src/OpenCase.Infrastructure
```

Mais detalhes em [docs/deploy.md](docs/deploy.md).

## Como testar

```bash
dotnet test
```

Cobre engine (cartas, salas, turnos, tabuleiro, movimento, palpite, refutação,
acusação final, dicas, passagens secretas), persistência (SQLite em memória),
DTOs (anti-vazamento), contrato do hub, componentes Blazor (bUnit) e health check.

## Roadmap

O MVP foi construído em 21 tarefas — ver [docs/roadmap-mvp.md](docs/roadmap-mvp.md).
Próximos passos:

- Reconexão de jogador no meio da partida
- Persistir partidas em andamento no banco
- Geração contínua de dicas durante a partida (timer)
- Arte final (cartas, peões, tabuleiro ilustrado)
- Salas privadas com senha e espectadores

## Screenshots

*(em breve — a UI atual usa placeholders visuais com o tema dark investigativo aprovado)*

## Licença

[MIT](LICENSE).

## Originalidade

Open Case é um jogo original inspirado no gênero clássico de dedução em
tabuleiro. Nomes, mapa, mecânicas de dicas e identidade visual são próprios e
não reproduzem material protegido de outros jogos.
