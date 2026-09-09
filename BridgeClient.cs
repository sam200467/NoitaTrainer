using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace NoitaTrainer;

internal sealed class BridgeClient
{
    public const string ModFolderName = "codex_noita_trainer_bridge";

    public string GameRoot { get; set; } = "";
    public string ModRoot => Path.Combine(GameRoot, "mods", ModFolderName);
    public string BridgeDirectory => Path.Combine(ModRoot, "bridge");
    public string CommandPath => Path.Combine(BridgeDirectory, "command.txt");
    public string StatusPath => Path.Combine(BridgeDirectory, "status.txt");
    public string WandSnapshotPath => Path.Combine(BridgeDirectory, "wands.txt");
    public string CharacterSnapshotPath => Path.Combine(BridgeDirectory, "character.txt");
    public string NextSeedPath => Path.Combine(BridgeDirectory, "next_seed.txt");

    public bool IsInstalled =>
        File.Exists(Path.Combine(ModRoot, "mod.xml")) &&
        File.Exists(Path.Combine(ModRoot, "init.lua"));

    public void InstallOrRepair()
    {
        if (!GameLocator.IsNoitaRoot(GameRoot))
            throw new InvalidOperationException("请选择正确的 Noita 游戏目录。");

        Directory.CreateDirectory(BridgeDirectory);
        var entityDirectory = Path.Combine(ModRoot, "entities");
        Directory.CreateDirectory(entityDirectory);
        File.WriteAllText(Path.Combine(ModRoot, "mod.xml"), ReadResource("Bridge.mod.xml"), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(ModRoot, "init.lua"), ReadResource("Bridge.init.lua"), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(entityDirectory, "powder_stash_empty.xml"),
            ReadResource("Bridge.powder_stash_empty.xml"), new UTF8Encoding(false));

        if (!File.Exists(CommandPath))
            File.WriteAllText(CommandPath, "0\nNOOP\n", new UTF8Encoding(false));
        File.WriteAllText(StatusPath, "connected=0\nmessage=等待 Noita 加载配套模组\n", new UTF8Encoding(false));
    }

    public string Send(string command, params object[] arguments)
    {
        if (!IsInstalled)
            throw new InvalidOperationException("请先安装配套模组。");

        var id = Guid.NewGuid().ToString("N");
        var lines = new List<string> { id, command };
        lines.AddRange(arguments.Select(SerializeArgument));
        var payload = string.Join("\n", lines) + "\n";

        Directory.CreateDirectory(BridgeDirectory);
        var temporary = Path.Combine(BridgeDirectory, $"command.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporary, payload, new UTF8Encoding(false));
        File.Move(temporary, CommandPath, true);
        return id;
    }

    public void WriteNextSeed(uint seed)
    {
        if (seed == 0)
            throw new ArgumentOutOfRangeException(nameof(seed), "种子必须在 1 到 4294967295 之间。");
        if (!IsInstalled)
            throw new InvalidOperationException("请先安装配套模组。");

        Directory.CreateDirectory(BridgeDirectory);
        var temporary = Path.Combine(BridgeDirectory, $"next_seed.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporary, seed.ToString(CultureInfo.InvariantCulture) + "\n", new UTF8Encoding(false));
        File.Move(temporary, NextSeedPath, true);
    }

    public void ClearNextSeed()
    {
        if (File.Exists(NextSeedPath))
            File.Delete(NextSeedPath);
    }

    public uint? ReadNextSeed()
    {
        if (!File.Exists(NextSeedPath))
            return null;
        var text = File.ReadLines(NextSeedPath).FirstOrDefault()?.Trim();
        return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed) && seed > 0
            ? seed
            : null;
    }

    public BridgeStatus? ReadStatus()
    {
        if (!File.Exists(StatusPath))
            return null;

        // 状态文件会被 mod 高频重写（约 30 次/秒），偶发读取冲突时短暂重试
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var written = File.GetLastWriteTimeUtc(StatusPath);
                var values = File.ReadLines(StatusPath)
                    .Select(line => line.Split('=', 2))
                    .Where(parts => parts.Length == 2)
                    .GroupBy(parts => parts[0], StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Last()[1], StringComparer.OrdinalIgnoreCase);

                return new BridgeStatus
                {
                    IsFresh = DateTime.UtcNow - written < TimeSpan.FromSeconds(3),
                    Protocol = ParseInt(values.GetValueOrDefault("protocol")),
                    PlayerPresent = values.GetValueOrDefault("player") == "1",
                    Hp = ParseDouble(values.GetValueOrDefault("hp")),
                    MaxHp = ParseDouble(values.GetValueOrDefault("max_hp")),
                    X = ParseDouble(values.GetValueOrDefault("x")),
                    Y = ParseDouble(values.GetValueOrDefault("y")),
                    CameraX = ParseDouble(values.GetValueOrDefault("camera_x")),
                    CameraY = ParseDouble(values.GetValueOrDefault("camera_y")),
                    Playtime = ParseDouble(values.GetValueOrDefault("playtime")),
                    EnemiesKilled = ParseDouble(values.GetValueOrDefault("enemies_killed")),
                    DamageTaken = ParseDouble(values.GetValueOrDefault("damage_taken")),
                    ProjectilesShot = ParseDouble(values.GetValueOrDefault("projectiles_shot")),
                    GoldAll = ParseDouble(values.GetValueOrDefault("gold_all")),
                    WorldSeed = ParseDouble(values.GetValueOrDefault("world_seed")),
                    HealedCustom = ParseDouble(values.GetValueOrDefault("healed_custom")),
                    PlacesVisited = ParseDouble(values.GetValueOrDefault("places_visited")),
                    Items = ParseDouble(values.GetValueOrDefault("items")),
                    WandsPicked = ParseDouble(values.GetValueOrDefault("wands_picked")),
                    HeartContainers = ParseDouble(values.GetValueOrDefault("heart_containers")),
                    Kicks = ParseDouble(values.GetValueOrDefault("kicks")),
                    ShowEnemyHp = values.GetValueOrDefault("show_enemy_hp") == "1",
                    ShowPlayerCoordinates = values.GetValueOrDefault("show_player_coords") == "1",
                    CollectGold = values.GetValueOrDefault("collect_gold") == "1",
                    GhostVision = values.GetValueOrDefault("ghost_vision") == "1",
                    LastId = values.GetValueOrDefault("last_id") ?? "",
                    LastOk = values.GetValueOrDefault("last_ok") == "1",
                    LastMessage = values.GetValueOrDefault("last_message") ?? values.GetValueOrDefault("message") ?? ""
                };
            }
            catch (IOException)
            {
                Thread.Sleep(3);
            }
        }

        return null;
    }

    public IReadOnlyList<WandSnapshot> ReadWandSnapshot(string requestId)
    {
        if (!File.Exists(WandSnapshotPath))
            throw new InvalidOperationException("游戏尚未生成魔杖栏读取结果。");

        var lines = File.ReadAllLines(WandSnapshotPath, Encoding.UTF8);
        if (lines.Length == 0 || !lines[0].Equals($"request_id\t{requestId}", StringComparison.Ordinal))
            throw new InvalidOperationException("魔杖栏读取结果尚未更新，请稍后重试。");

        var result = new List<WandSnapshot>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var fields = line.Split('\t');
            if (fields.Length != 11 ||
                !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityId) ||
                !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var slot) ||
                !int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionsPerRound) ||
                !decimal.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var castDelay) ||
                !decimal.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var rechargeTime) ||
                !decimal.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var manaMax) ||
                !decimal.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var manaChargeSpeed) ||
                !int.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity) ||
                !decimal.TryParse(fields[9], NumberStyles.Float, CultureInfo.InvariantCulture, out var spread) ||
                !decimal.TryParse(fields[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var speedMultiplier))
                throw new InvalidDataException("魔杖栏读取结果格式无效。");

            result.Add(new WandSnapshot
            {
                EntityId = entityId,
                Slot = slot,
                Shuffle = fields[2] == "1",
                ActionsPerRound = actionsPerRound,
                CastDelaySeconds = castDelay,
                RechargeTimeSeconds = rechargeTime,
                ManaMax = manaMax,
                ManaChargeSpeed = manaChargeSpeed,
                Capacity = capacity,
                SpreadDegrees = spread,
                SpeedMultiplier = speedMultiplier
            });
        }
        return result;
    }

    public CharacterSnapshot ReadCharacterSnapshot(string requestId)
    {
        if (!File.Exists(CharacterSnapshotPath))
            throw new InvalidOperationException("游戏尚未生成角色信息读取结果。");

        var lines = File.ReadAllLines(CharacterSnapshotPath, Encoding.UTF8);
        if (lines.Length < 2 || !lines[0].Equals($"request_id\t{requestId}", StringComparison.Ordinal))
            throw new InvalidOperationException("角色信息读取结果尚未更新，请稍后重试。");

        var fields = lines[1].Split('\t');
        if (fields.Length != 13 ||
            !decimal.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var money) ||
            !decimal.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hp) ||
            !decimal.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var maxHp) ||
            !decimal.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var lungCapacity) ||
            !decimal.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var flyTimeMax) ||
            !decimal.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var flyTimeLeft) ||
            !decimal.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var flyRechargeSpeed) ||
            !decimal.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var flyRechargeGroundSpeed) ||
            !decimal.TryParse(fields[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var currentAir) ||
            !decimal.TryParse(fields[9], NumberStyles.Float, CultureInfo.InvariantCulture, out var runVelocity) ||
            !decimal.TryParse(fields[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var flyVelocityX) ||
            !decimal.TryParse(fields[11], NumberStyles.Float, CultureInfo.InvariantCulture, out var jumpVelocityY) ||
            !decimal.TryParse(fields[12], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixelGravity))
            throw new InvalidDataException("角色信息读取结果格式无效。");

        return new CharacterSnapshot
        {
            Money = money,
            Hp = hp,
            MaxHp = maxHp,
            LungCapacitySeconds = lungCapacity,
            FlyTimeMaxSeconds = flyTimeMax,
            FlyTimeLeftSeconds = flyTimeLeft,
            FlyRechargeSpeed = flyRechargeSpeed,
            FlyRechargeGroundSpeed = flyRechargeGroundSpeed,
            CurrentAirSeconds = currentAir,
            RunVelocity = runVelocity,
            FlyVelocityX = flyVelocityX,
            JumpVelocityY = jumpVelocityY,
            PixelGravity = pixelGravity
        };
    }

    public void LaunchNoita()
    {
        Process.Start(new ProcessStartInfo("steam://rungameid/881100") { UseShellExecute = true });
    }

    private static string SerializeArgument(object value) => value switch
    {
        bool boolean => boolean ? "1" : "0",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString() ?? ""
    };

    private static double? ParseDouble(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private static string ReadResource(string suffix)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .First(resource => resource.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"缺少资源 {suffix}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

internal sealed class BridgeStatus
{
    public bool IsFresh { get; init; }
    public int Protocol { get; init; }
    public bool PlayerPresent { get; init; }
    public double? Hp { get; init; }
    public double? MaxHp { get; init; }
    public double? X { get; init; }
    public double? Y { get; init; }
    public double? CameraX { get; init; }
    public double? CameraY { get; init; }
    public double? Playtime { get; init; }
    public double? EnemiesKilled { get; init; }
    public double? DamageTaken { get; init; }
    public double? ProjectilesShot { get; init; }
    public double? GoldAll { get; init; }
    public double? WorldSeed { get; init; }
    public double? HealedCustom { get; init; }
    public double? PlacesVisited { get; init; }
    public double? Items { get; init; }
    public double? WandsPicked { get; init; }
    public double? HeartContainers { get; init; }
    public double? Kicks { get; init; }
    public bool ShowEnemyHp { get; init; }
    public bool ShowPlayerCoordinates { get; init; }
    public bool CollectGold { get; init; }
    public bool GhostVision { get; init; }
    public string LastId { get; init; } = "";
    public bool LastOk { get; init; }
    public string LastMessage { get; init; } = "";
}

internal sealed class WandSnapshot
{
    public int EntityId { get; init; }
    public int Slot { get; init; }
    public bool Shuffle { get; init; }
    public int ActionsPerRound { get; init; }
    public decimal CastDelaySeconds { get; init; }
    public decimal RechargeTimeSeconds { get; init; }
    public decimal ManaMax { get; init; }
    public decimal ManaChargeSpeed { get; init; }
    public int Capacity { get; init; }
    public decimal SpreadDegrees { get; init; }
    public decimal SpeedMultiplier { get; init; }
}

internal sealed class CharacterSnapshot
{
    public decimal Money { get; init; }
    public decimal Hp { get; init; }
    public decimal MaxHp { get; init; }
    public decimal LungCapacitySeconds { get; init; }
    public decimal FlyTimeMaxSeconds { get; init; }
    public decimal FlyTimeLeftSeconds { get; init; }
    public decimal FlyRechargeSpeed { get; init; }
    public decimal FlyRechargeGroundSpeed { get; init; }
    public decimal CurrentAirSeconds { get; init; }
    public decimal RunVelocity { get; init; }
    public decimal FlyVelocityX { get; init; }
    public decimal JumpVelocityY { get; init; }
    public decimal PixelGravity { get; init; }
}
