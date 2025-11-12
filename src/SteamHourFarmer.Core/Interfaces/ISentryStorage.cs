namespace SteamHourFarmer.Core.Interfaces;

/// <summary>
/// Interface for storing and retrieving Steam sentry files (authentication tokens).
/// </summary>
public interface ISentryStorage
{
    Task<byte[]?> GetSentryDataAsync(string username);
    Task SetSentryDataAsync(string username, byte[] sentryData);
    Task DeleteSentryDataAsync(string username);
}