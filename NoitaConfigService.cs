using System.Text;
using System.Text.RegularExpressions;

namespace NoitaTrainer;

internal static partial class NoitaConfigService
{
    public static string ConfigPath
    {
        get
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.GetFullPath(Path.Combine(local, "..", "LocalLow", "Nolla_Games_Noita", "save_shared", "config.xml"));
        }
    }

    public static bool? ReadAllowBackgroundRunning()
        => ReadAllowBackgroundRunningFromPath(ConfigPath);

    internal static bool? ReadAllowBackgroundRunningFromPath(string path)
    {
        if (!File.Exists(path))
            return null;
        var content = File.ReadAllText(path, Encoding.UTF8);
        var match = PauseWhenUnfocusedRegex().Match(content);
        return match.Success ? match.Groups[1].Value == "0" : null;
    }

    public static void WriteAllowBackgroundRunning(bool allow)
        => WriteAllowBackgroundRunningToPath(ConfigPath, allow);

    internal static void WriteAllowBackgroundRunningToPath(string path, bool allow)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("尚未找到 Noita 的用户配置文件；请至少启动一次游戏。", path);

        var content = File.ReadAllText(path, Encoding.UTF8);
        var pauseValue = allow ? "0" : "1";
        string updated;
        if (PauseWhenUnfocusedRegex().IsMatch(content))
        {
            updated = PauseWhenUnfocusedRegex().Replace(content, $"application_pause_when_unfocused=\"{pauseValue}\"", 1);
        }
        else
        {
            updated = ConfigElementRegex().Replace(content,
                match => match.Value + Environment.NewLine + $"  application_pause_when_unfocused=\"{pauseValue}\" ", 1);
            if (updated == content)
                throw new InvalidDataException("Noita 用户配置文件中没有找到 Config 根元素。 ");
        }

        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("无法确定 Noita 配置目录。");
        var temporary = Path.Combine(directory, $"config.codex.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporary, updated, new UTF8Encoding(false));
        File.Move(temporary, path, true);
    }

    [GeneratedRegex("application_pause_when_unfocused\\s*=\\s*\"([01])\"", RegexOptions.IgnoreCase)]
    private static partial Regex PauseWhenUnfocusedRegex();

    [GeneratedRegex("<Config\\b", RegexOptions.IgnoreCase)]
    private static partial Regex ConfigElementRegex();
}
