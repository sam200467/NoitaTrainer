using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace NoitaTrainer;

internal static partial class GameLocator
{
    public static string? FindNoitaRoot()
    {
        foreach (var candidate in CandidateRoots().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (IsNoitaRoot(candidate))
                return Path.GetFullPath(candidate);
        }
        return null;
    }

    public static bool IsNoitaRoot(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        File.Exists(Path.Combine(path, "noita.exe")) &&
        Directory.Exists(Path.Combine(path, "mods"));

    private static IEnumerable<string> CandidateRoots()
    {
        var steamRoots = new List<string>();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steamPath = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(steamPath))
                steamRoots.Add(steamPath.Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            // Registry access is optional; fixed-drive fallbacks remain available.
        }

        foreach (var steamRoot in steamRoots.ToArray())
        {
            var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFile))
                continue;

            string text;
            try { text = File.ReadAllText(libraryFile); }
            catch { continue; }

            foreach (Match match in LibraryPathRegex().Matches(text))
            {
                var path = match.Groups[1].Value.Replace("\\\\", "\\");
                if (!string.IsNullOrWhiteSpace(path))
                    steamRoots.Add(path);
            }
        }

        foreach (var root in steamRoots)
            yield return Path.Combine(root, "steamapps", "common", "Noita");

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.DriveType is not (DriveType.Fixed or DriveType.Removable))
                continue;

            yield return Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "common", "Noita");
            yield return Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Noita");
        }
    }

    [GeneratedRegex("\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryPathRegex();
}
