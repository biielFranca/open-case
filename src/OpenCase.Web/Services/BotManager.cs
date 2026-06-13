using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace OpenCase.Web.Services;

/// <summary>
/// Cria e mantém os bots da aplicação. Cada bot é um cliente SignalR real
/// conectado ao próprio servidor.
/// </summary>
public class BotManager(IServer server, ILogger<BotManager> logger)
{
    private static readonly string[] BotNames =
    [
        "Watson", "Poirot", "Marple", "Columbo", "Dupin", "Kogoro", "Vance",
    ];

    private readonly ConcurrentDictionary<string, List<BotClient>> _botsByRoom = new();

    public async Task<string> AddBotAsync(string roomCode)
    {
        var bots = _botsByRoom.GetOrAdd(roomCode, _ => []);
        var name = BotNames
            .Select(n => $"{n} (bot)")
            .FirstOrDefault(n => bots.All(b => b.Name != n))
            ?? throw new InvalidOperationException("Limite de bots atingido para esta sala.");

        var bot = await BotClient.JoinAsync(GetSelfHubUrl(), roomCode, name, logger);
        lock (bots)
        {
            bots.Add(bot);
        }

        logger.LogInformation("Bot {Bot} entrou na sala {Room}.", name, roomCode);
        return name;
    }

    private string GetSelfHubUrl()
    {
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("Endereço do servidor indisponível.");

        // Endereços de bind como http://[::]:8080 ou 0.0.0.0 não são conectáveis; usar loopback.
        var uri = new Uri(address.Replace("[::]", "localhost").Replace("0.0.0.0", "localhost").Replace("+", "localhost"));
        return $"{uri.Scheme}://localhost:{uri.Port}/gamehub";
    }
}
