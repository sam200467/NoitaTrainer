using System.Text;

namespace NoitaTrainer;

internal static class TrainerSettingsService
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexNoitaTrainer",
        "settings.txt");

    public static bool ReadCollectGold() => ReadBoolean("collect_gold");
    public static bool ReadGhostVision() => ReadBoolean("ghost_vision");

    public static void WriteCollectGold(bool enabled) => WriteBoolean("collect_gold", enabled);
    public static void WriteGhostVision(bool enabled) => WriteBoolean("ghost_vision", enabled);

    private static bool ReadBoolean(string key)
    {
        if (!File.Exists(SettingsPath))
            return false;

        return ReadValues().TryGetValue(key, out var value) && value == "1";
    }

    private static void WriteBoolean(string key, bool enabled)
    {
        var values = ReadValues();
        values[key] = enabled ? "1" : "0";

        var path = SettingsPath;
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("无法确定设置目录。");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $"settings.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
        var content = string.Join("\n", values.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key}={pair.Value}")) + "\n";
        File.WriteAllText(temporary, content, new UTF8Encoding(false));
        File.Move(temporary, path, true);
    }

    private static Dictionary<string, string> ReadValues()
    {
        if (!File.Exists(SettingsPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return File.ReadLines(SettingsPath)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => parts[0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last()[1], StringComparer.OrdinalIgnoreCase);
    }
}
