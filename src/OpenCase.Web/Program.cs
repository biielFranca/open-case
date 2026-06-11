using Microsoft.EntityFrameworkCore;
using OpenCase.Application.Services;
using OpenCase.Infrastructure.Persistence;
using OpenCase.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();

// DATABASE_URL (deploy) tem prioridade sobre appsettings (desenvolvimento).
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("Default");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<OpenCaseDbContext>(options => options.UseNpgsql(connectionString));
}

// Serviços da engine: estado das partidas vive em memória, compartilhado por sala.
builder.Services.AddSingleton<RoomService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<GameSetupService>();
builder.Services.AddSingleton<TurnService>();
builder.Services.AddSingleton<MovementService>();
builder.Services.AddSingleton<SuggestionService>();
builder.Services.AddSingleton<RefutationService>();
builder.Services.AddSingleton<FinalAccusationService>();
builder.Services.AddSingleton<HintService>();
builder.Services.AddSingleton<SecretPassageService>();
builder.Services.AddSingleton<GameEventLogger>();
builder.Services.AddSingleton<NotesService>();
builder.Services.AddSingleton<GameManager>();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OpenCaseDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapHub<OpenCase.Web.Hubs.GameHub>("/gamehub");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
