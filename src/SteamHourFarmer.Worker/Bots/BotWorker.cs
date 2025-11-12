using Microsoft.Extensions.Options;
using SteamHourFarmer.Core;
using SteamHourFarmer.Infrastructure;
using SteamKit2.GC.TF2.Internal;

namespace SteamHourFarmer.Worker.Bots;

public class BotWorker : BackgroundService
{
    private readonly ILogger<BotWorker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AccountConfig _account;
    
    public BotWorker(ILogger<BotWorker> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;

        try
        {
            var gamesString = configuration["STEAM_GAMES"] ?? "";
            var gamesList = new List<int>();

            if (!String.IsNullOrEmpty(gamesString))
            {
                foreach (var idStr in gamesString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(idStr, out var id))
                        gamesList.Add(id);
                    else
                        _logger.LogWarning("Invalid game ID in STEAM_GAMES: {IdStr}", idStr);
                }
            }

            _account = new AccountConfig(
                configuration["STEAM_USERNAME"] ??
                throw new InvalidOperationException("STEAM_USERNAME env var not set."),
                configuration["STEAM_PASSWORD"] ??
                throw new InvalidOperationException("STEAM_PASSWORD env var not set."),
                gamesList.ToArray(),
                bool.TryParse(configuration["STEAM_ONLINE"], out var online) && online
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load account configuration.");
            throw;
        }
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot Worker starting to {Username}", _account.Username);
        
        try
        {
            var botLogger = _loggerFactory.CreateLogger<SteamSession>();
            var bot = new SteamSession(_account, botLogger);

            await bot.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Shutdown signal received to {Username}. Stopping bot...", _account.Username);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "A fatal, unhandled error occurred in the bot worker.");
        }
        finally
        {
            _logger.LogInformation("Bot Worker has stopped.");
        }
    }
}