using System.Text.Json.Serialization;

namespace SteamHourFarmer.Bot;

/// <summary>
/// Representes the configuration for a single Steam account.
/// It directly maps to the objects in your appsettings.json file.
/// </summary>
public record AccountConfig(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("games")] int[] Games,
    [property: JsonPropertyName("online")] bool? Online = false
);