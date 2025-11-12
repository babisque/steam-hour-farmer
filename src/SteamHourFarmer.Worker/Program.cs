using SteamHourFarmer.Core.Interfaces;
using SteamHourFarmer.Infrastructure;
using SteamHourFarmer.Infrastructure.Helpers;
using SteamHourFarmer.Worker.Bots;

namespace SteamHourFarmer.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<ISentryStorage>(sp =>
                {
                    var tokenStorageDir = Environment.GetEnvironmentVariable("TOKEN_STORAGE_DIRECTORY") ?? "./tokens";
                    var resolvedTokenDir = PathHelper.ConvertRelativePath(tokenStorageDir);
                    
                    return new FileSystemSentryStorage(
                        resolvedTokenDir,
                        sp.GetRequiredService<ILogger<FileSystemSentryStorage>>()
                    );
                });
                
                services.AddHostedService<BotWorker>();
            })
            .Build();

        await host.RunAsync();
    }
}