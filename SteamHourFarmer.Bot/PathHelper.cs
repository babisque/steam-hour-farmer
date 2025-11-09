namespace SteamHourFarmer.Bot;

public class PathHelper
{
    /// <summary>
    /// Converts a path relative to the app's executable directory into an absolute path.
    /// </summary>
    /// <param name="path">The relative or absolute path.</param>
    /// <returns>A full, absolute path.</returns>
    public static string ConvertRelativePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;

        var exeDirectory = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(exeDirectory, path));
    }
}