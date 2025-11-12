namespace SteamHourFarmer.Core.Interfaces;

/// <summary>
/// Defines the contract for a single Steam account session.
/// </summary>
public interface ISteamSession
{
    /// <summary>
    /// Starts the bot session, connects to Steam, and begins idling.
    /// This task completes when the session has permanently stopped.
    /// </summary>
    /// <param name="cancellationToken">A token to signal graceful shutdown.</param>
    Task StartAsync(CancellationToken cancellationToken);
}