using SteamHourFarmer.Core.Interfaces;
using SteamHourFarmer.Infrastructure.Helpers;

namespace SteamHourFarmer.Infrastructure;

public class FileSystemSentryStorage : ISentryStorage
{
    private readonly string _directory;

    public FileSystemSentryStorage(string directory)
    {
        _directory = PathHelper.ConvertRelativePath(directory);
        
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
            Console.WriteLine($"Error reading sentry file for {username}: {ex.Message}");
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
            Console.WriteLine($"Error deleting sentry file for {username}: {ex.Message}");
        }
        return Task.CompletedTask;
    }
}