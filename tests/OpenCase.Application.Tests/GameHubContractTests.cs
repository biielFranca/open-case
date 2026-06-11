using System.Reflection;
using OpenCase.Web.Hubs;

namespace OpenCase.Application.Tests;

public class GameHubContractTests
{
    [Theory]
    [InlineData("CreateRoom")]
    [InlineData("JoinRoom")]
    [InlineData("LeaveRoom")]
    [InlineData("ChoosePawn")]
    [InlineData("SetReady")]
    [InlineData("StartGame")]
    [InlineData("RollInitialDice")]
    [InlineData("RollTurnDice")]
    [InlineData("MovePawn")]
    [InlineData("MakeSuggestion")]
    [InlineData("PassRefutation")]
    [InlineData("ShowRefutationCard")]
    [InlineData("MakeFinalAccusation")]
    [InlineData("UseSecretPassage")]
    public void GameHub_ExposesRequiredHubMethod(string methodName)
    {
        var method = typeof(GameHub).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }

    [Fact]
    public void GameHub_IsASignalRHub()
    {
        Assert.True(typeof(Microsoft.AspNetCore.SignalR.Hub).IsAssignableFrom(typeof(GameHub)));
    }
}
