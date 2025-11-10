using Microsoft.Extensions.Logging;
using SteamHourFarmer.Core.Interfaces;
using SteamHourFarmer.Infrastructure.Helpers;

namespace SteamHourFarmer.Infrastructure;

public class FileSystemSentryStorage : ISentryStorage
{
    private readonly string _directory;
    private readonly ILogger<FileSystemSentryStorage> _logger;

    public FileSystemSentryStorage(string directory, ILogger<FileSystemSentryStorage> logger)
    {
        _directory = PathHelper.ConvertRelativePath(directory);
        _logger = logger;
        
        if (!Directory.Exists(_directory))
            Directory.CreateDirectory(_directory);
    }
    
    private string GetFilePath(string username) => Path.Combine(_directory, username);
    
    /// <summary>
    /// Reads the sentry file bytes for a user.
    /// </summary>
    public async Task<byte[]?> GetSentryDataAsync(string username)
    {
        var path = GetFilePath(username);
        try
        {
            return await File.ReadAllBytesAsync(path);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading sentry file for {Username}", username);
            return null;
        }
    }
    
    /// <summary>
    /// Writes the sentry file bytes for a user.
    /// </summary>
    public async Task SetSentryDataAsync(string username, byte[] data)
    {
        var path = GetFilePath(username);
        await File.WriteAllBytesAsync(path, data);
    }
    
    /// <summary>
    /// Deletes the sentry file for a user.
    /// Equivalent to deleteToken.
    /// </summary>
    public Task DeleteSentryDataAsync(string username)
    {
        var path = GetFilePath(username);
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sentry file for {Username}", username);
        }
        return Task.CompletedTask;
    }
}