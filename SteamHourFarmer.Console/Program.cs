using Microsoft.Extensions.Logging;
using SteamHourFarmer.Infrastructure;
using SteamHourFarmer.Core.Interfaces;
using SteamHourFarmer.Infrastructure.Helpers;

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
        
        string? cliConfig = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase))
            {
                cliConfig = args[i + 1];
                break;
            }
        }
        var envConfig = Environment.GetEnvironmentVariable("CONFIG_PATH");
        var configPath = cliConfig ?? envConfig ?? "./config.json";
        var resolvedConfigPath = PathHelper.ConvertRelativePath(configPath);
        
        var tokenStorageDir = Environment.GetEnvironmentVariable("TOKEN_STORAGE_DIRECTORY") ?? "./tokens";
        var resolvedTokenDir = PathHelper.ConvertRelativePath(tokenStorageDir);
        
        logger.LogInformation("Starting sentry storage");
        logger.LogInformation("Using config path: {ConfigPath}", resolvedConfigPath);
        logger.LogInformation("Using token storage directory: {TokenDir}", resolvedTokenDir);

        try
        {
            var config = await ConfigLoader.LoadConfigAsync(resolvedConfigPath);
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