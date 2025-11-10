using Microsoft.Extensions.Options;
using SteamHourFarmer.Core;
using SteamHourFarmer.Infrastructure;

namespace SteamHourFarmer.Worker.Bots;

public class BotWorker(
    ILogger<BotWorker> logger,
    IOptions<List<AccountConfig>> accounts,
    ILoggerFactory loggerFactory)
    : BackgroundService
{
    private readonly List<AccountConfig> _accounts = accounts.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_accounts == null || _accounts.Count == 0)
        {
            logger.LogWarning("No accounts found in configuration. Exiting.");
            return;
        }

        logger.LogInformation("Bot Worker starting with {Count} accounts.", _accounts.Count);
        
        try
        {
            var botTasks = new List<Task>();
            foreach (var entry in _accounts)
            {
                var botLogger = loggerFactory.CreateLogger<SteamSession>();
                
                var bot = new SteamSession(entry, botLogger);
                
                botTasks.Add(bot.StartAsync(stoppingToken));
            }

            await Task.WhenAll(botTasks);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Shutdown signal received. Stopping bots.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "A fatal, unhandled error occurred in the bot worker.");
        }
        finally
        {
            logger.LogInformation("Bot Worker has stopped.");
        }
    }
}