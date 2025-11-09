using System.Text.Json;
using SteamHourFarmer.Core;
using SteamHourFarmer.Infrastructure;
using SteamHourFarmer.Infrastructure.Helpers;

namespace SteamHourFarmer.Console;

/// <summary>
/// Handles loading and parsing the JSON configuration file.
/// Supports two formats:
/// 1) An array of accounts (config.json)
/// 2) An object with property "SteamAccounts": [...] (appsettings.json)
/// </summary>
public static class ConfigLoader
{
    private sealed class AppSettingsShape
    {
        public List<AccountConfig>? SteamAccounts { get; set; }
    }

    public static async Task<List<AccountConfig>> LoadConfigAsync(string path)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // 1) Try the provided path first (default is ./config.json)
        var resolvedPath = PathHelper.ConvertRelativePath(path);
        var tried = new List<string>();

        async Task<List<AccountConfig>?> TryLoadAsync(string fullPath)
        {
            tried.Add(fullPath);
            if (!File.Exists(fullPath)) return null;

            await using var stream = File.OpenRead(fullPath);
            using var doc = await JsonDocument.ParseAsync(stream);

            switch (doc.RootElement.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    // Direct array of AccountConfig
                    stream.Position = 0;
                    var list = await JsonSerializer.DeserializeAsync<List<AccountConfig>>(stream, options);
                    if (list != null && list.Count > 0) return list;
                    break;
                }
                case JsonValueKind.Object:
                {
                    // appsettings-like shape
                    stream.Position = 0;
                    var root = await JsonSerializer.DeserializeAsync<AppSettingsShape>(stream, options);
                    if (root?.SteamAccounts != null && root.SteamAccounts.Count > 0)
                        return root.SteamAccounts;
                    break;
                }
            }
            return new List<AccountConfig>();
        }

        try
        {
            var loaded = await TryLoadAsync(resolvedPath);
            if (loaded != null && loaded.Count > 0)
                return loaded;

            // 2) Fallback to appsettings.json next to the executable
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            loaded = await TryLoadAsync(appSettingsPath);
            if (loaded != null && loaded.Count > 0)
                return loaded;

            // 3) Fallback to appsettings.json relative to provided path (if it was a directory)
            var dir = Path.GetDirectoryName(resolvedPath) ?? AppContext.BaseDirectory;
            var appSettingsInDir = Path.Combine(dir, "appsettings.json");
            if (!string.Equals(appSettingsInDir, appSettingsPath, StringComparison.OrdinalIgnoreCase))
            {
                loaded = await TryLoadAsync(appSettingsInDir);
                if (loaded != null && loaded.Count > 0)
                    return loaded;
            }

            System.Console.Error.WriteLine(
                $"No valid config found. Tried: {string.Join(", ", tried.Distinct())}. " +
                "Provide CONFIG_PATH or place config.json (array) or appsettings.json (object with SteamAccounts) next to the exe.");
            return new List<AccountConfig>();
        }
        catch (Exception e)
        {
            System.Console.Error.WriteLine($"Failed to load config. Paths tried: {string.Join(", ", tried.Distinct())}. Error: {e.Message}");
            return new List<AccountConfig>();
        }
    }
}