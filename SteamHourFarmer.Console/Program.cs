using Microsoft.Extensions.Logging;
using SteamHourFarmer.Infrastructure;
using SteamHourFarmer.Core.Interfaces;

namespace SteamHourFarmer.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("SteamHourFarmer", LogLevel.Debug)
                .AddConsole();
        });
        var logger = loggerFactory.CreateLogger("Program");
        
        logger.LogInformation("Program started");
        
        var configPath = Environment.GetEnvironmentVariable("CONFIG_PATH") ?? "./config.json";
        var tokenStorageDir = Environment.GetEnvironmentVariable("TOKEN_STORAGE_DIRECTORY") ?? "./tokens";
        
        ISentryStorage sentryStorage = new FileSystemSentryStorage(tokenStorageDir);
        logger.LogInformation("Starting sentry storage");

        try
        {
            var config = await ConfigLoader.LoadConfigAsync(configPath);
            if (config.Count == 0)
            {
                logger.LogWarning("Config file is empty or invalid. Exiting.");
                return;
            }
            
            logger.LogInformation($"Loaded config for {config.Count} accounts.");
            
            var botTasks = new List<Task>();
            var cts = new CancellationTokenSource();

            System.Console.CancelKeyPress += (sender, e) =>
            {
                logger.LogInformation("Exiting...");
                e.Cancel = true;
                cts.Cancel();
            };

            foreach (var entry in config)
            {
                var botLogger = loggerFactory.CreateLogger<SteamSession>();
                var bot = new SteamSession(entry, botLogger);
                
                botTasks.Add(bot.StartAsync(cts.Token));
            }

            await Task.WhenAll(botTasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during execution.");
            Environment.Exit(1);
        }
        
        logger.LogInformation("All bots have stopped. Exiting program.");
    }
}