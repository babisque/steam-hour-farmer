using SteamHourFarmer.Bot;

Console.WriteLine("Starting Steam Hour Booster (.NET Edition)");

var configPath = Environment.GetEnvironmentVariable("CONFIG_PATH") ?? "./config.json";
var tokenStorageDir = Environment.GetEnvironmentVariable("TOKEN_STORAGE_DIRECTORY") ?? "./tokens";
var steamDataDirectory = Environment.GetEnvironmentVariable("STEAM_DATA_DIRECTORY") ?? "./steam-data";

try
{
    var config = await ConfigLoader.LoadConfigAsync(configPath);
    if (config.Count == 0)
    {
        Console.WriteLine("Config file is empty or invalid. Exiting.");
        return;
    }

    ISentryStorage sentryStorage = new FileSystemSentryStorage(tokenStorageDir);

    var botTasks = new List<Task>();
    foreach (var entry in config)
    {
        var bot = new Bot(
            entry,
            steamDataDirectory,
            sentryStorage
        );

        botTasks.Add(bot.StartAsync());
    }

    await Task.WhenAll(botTasks);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"A fatal error occurred: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}