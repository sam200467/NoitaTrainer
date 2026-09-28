using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NoitaTrainer;

internal sealed partial class MainForm : Form
{
    private const int CatalogGridColumns = 3;
    private const int CatalogButtonIconSize = 36;

    private readonly CatalogRoot catalog = CatalogService.Load();
    private readonly Bitmap iconAtlas = CatalogService.LoadIconAtlas();
    private readonly Bitmap materialPreviewAtlas = CatalogService.LoadMaterialPreviewAtlas();
    private readonly Bitmap itemIconAtlas = CatalogService.LoadItemIconAtlas();
    private readonly BridgeClient bridge = new();
    private readonly System.Windows.Forms.Timer statusTimer = new() { Interval = 100 };
    private readonly int showInstanceMessageId;
    private readonly InstanceMessageWindow instanceMessageWindow;
    private readonly Dictionary<ComboBox, int> catalogIconSizes = new();
    private readonly ToolTip quickActionToolTip = new()
    {
        InitialDelay = 650,
        ReshowDelay = 150,
        AutoPopDelay = 15000,
        ShowAlways = true
    };
    private readonly List<Bitmap> catalogButtonImages = [];
    private readonly Font catalogButtonFont = new("Microsoft YaHei UI", 10.5F);

    private readonly TextBox gamePathText = new();
    private readonly Label connectionLabel = new();
    private readonly Label liveStatsLabel = new();
    private readonly TextBox playerPositionValue = InformationTextBox();
    private readonly TextBox cameraPositionValue = InformationTextBox();
    private readonly TextBox gameTimeValue = InformationTextBox();
    private readonly TextBox worldSeedValue = InformationTextBox();
    private readonly TextBox enemiesKilledValue = InformationTextBox();
    private readonly TextBox damageTakenValue = InformationTextBox();
    private readonly TextBox healedValue = InformationTextBox();
    private readonly TextBox projectilesShotValue = InformationTextBox();
    private readonly TextBox goldAllValue = InformationTextBox();
    private readonly TextBox placesVisitedValue = InformationTextBox();
    private readonly TextBox itemsValue = InformationTextBox();
    private readonly TextBox heartContainersValue = InformationTextBox();
    private readonly TextBox kicksValue = InformationTextBox();
    private readonly TextBox wandsPickedValue = InformationTextBox();
    private readonly TextBox nextSeedText = new() { Width = 260, PlaceholderText = "1–4294967295" };
    private readonly Label nextSeedStatus = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly TextBox teleportPositionValue = InformationTextBox();
    private readonly CheckBox showEnemyHpCheckBox = new() { Text = "显示怪物血量", AutoSize = true };
    private readonly CheckBox showPlayerCoordinatesCheckBox = new() { Text = "显示玩家坐标", AutoSize = true };
    private readonly CheckBox collectGoldCheckBox = new() { Text = "自动收集金块", AutoSize = true };
    private readonly CheckBox ghostVisionCheckBox = new() { Text = "幽灵透视", AutoSize = true };
    private readonly CheckBox allowBackgroundRunningCheckBox = new() { Text = "允许游戏后台运行", AutoSize = true };
    private readonly TextBox teleportXText = new() { Width = 180, PlaceholderText = "输入 X 坐标" };
    private readonly TextBox teleportYText = new() { Width = 180, PlaceholderText = "输入 Y 坐标" };
    private readonly RichTextBox logBox = new();
    private readonly ComboBox spellCombo = new();
    private readonly ComboBox perkCombo = new();
    private readonly ComboBox wandCombo = new();
    private readonly TextBox hpText = new() { Text = "100" };
    private readonly TextBox maxHpText = new() { Text = "100" };
    private readonly CheckBox healWithMaxCheck = new() { Text = "同时回满生命", Checked = true, AutoSize = true };
    private readonly NumericUpDown spellCount = CountInput(100);
    private readonly NumericUpDown perkCount = CountInput(100);
    private readonly NumericUpDown wandCount = CountInput(20);
    private readonly Label catalogStatsLabel = new();
    private readonly RadioButton spellSpawnMode = new() { Text = "生成在玩家脚下", Checked = true, AutoSize = true };
    private readonly RadioButton spellGiveMode = new() { Text = "直接获得该法术", AutoSize = true };
    private readonly RadioButton perkSpawnMode = new() { Text = "生成在玩家脚下", Checked = true, AutoSize = true };
    private readonly RadioButton perkGiveMode = new() { Text = "直接获得该天赋", AutoSize = true };
    private readonly RadioButton itemSpawnMode = new() { Text = "生成在玩家脚下", Checked = true, AutoSize = true };
    private readonly RadioButton itemGiveMode = new() { Text = "直接获得该物品", AutoSize = true };
    private readonly NumericUpDown itemCount = CountInput(100);
    private readonly RadioButton statusTemporaryMode = new() { Text = "临时效果", Checked = true, AutoSize = true };
    private readonly RadioButton statusPermanentMode = new() { Text = "永久效果", AutoSize = true };
    private readonly NumericUpDown statusDuration = new() { Minimum = 1, Maximum = 3600, Value = 30, Width = 70, Anchor = AnchorStyles.Left };
    private readonly TableLayoutPanel wandManagementContent = new();
    private readonly Label wandManagementStatus = new();
    private readonly DataGridView managedWandGrid = new();
    private readonly CheckBox managedWandShuffle = new() { Text = "乱序", AutoSize = true };
    private readonly TextBox managedWandActions = new();
    private readonly TextBox managedWandCastDelay = new();
    private readonly TextBox managedWandRecharge = new();
    private readonly TextBox managedWandManaMax = new();
    private readonly TextBox managedWandManaCharge = new();
    private readonly TextBox managedWandCapacity = new();
    private readonly TextBox managedWandSpread = new();
    private readonly TextBox managedWandSpeed = new();
    private readonly TableLayoutPanel characterManagementContent = new();
    private readonly Label characterManagementStatus = new();
    private readonly TextBox characterMoney = new() { Width = 240 };
    private readonly TextBox characterHp = new() { Width = 240 };
    private readonly TextBox characterMaxHp = new() { Width = 240 };
    private readonly TextBox characterLungCapacity = new() { Width = 240 };
    private readonly TextBox characterFlyTimeMax = new() { Width = 240 };
    private readonly TextBox characterFlyTimeLeft = new() { Width = 240 };
    private readonly TextBox characterFlyRecharge = new() { Width = 240 };
    private readonly TextBox characterFlyRechargeGround = new() { Width = 240 };
    private readonly TextBox characterCurrentAir = new() { Width = 240 };
    private readonly TextBox characterRunVelocity = new() { Width = 240 };
    private readonly TextBox characterFlyVelocityX = new() { Width = 240 };
    private readonly TextBox characterJumpVelocityY = new() { Width = 240 };
    private readonly TextBox characterPixelGravity = new() { Width = 240 };

    // 添加药水 tab 状态
    private string currentPotionCategory = "spark";
    private readonly List<PotionRecipeEntry> potionRecipe = [];
    private readonly TableLayoutPanel potionMaterialPanel = new();
    private readonly TableLayoutPanel potionRecipePanel = new();
    private readonly Label potionRecipeTotal = new();
    private readonly TextBox potionSearchText = new();
    private readonly RadioButton potionFlaskRadio = new() { Text = "烧瓶", Checked = true, AutoSize = true };
    private readonly RadioButton potionPouchRadio = new() { Text = "袋子", AutoSize = true };
    private readonly RadioButton potionSpawnRadio = new() { Text = "生成在玩家脚下", Checked = true, AutoSize = true };
    private readonly RadioButton potionGiveRadio = new() { Text = "直接获得", AutoSize = true };
    private readonly NumericUpDown potionCapacity = new() { Minimum = 1, Maximum = 100, Value = 100, Width = 90 };

    private string pendingCommandId = "";
    private string lastReportedCommandResult = "";
    private string pendingWandSnapshotId = "";
    private string pendingCharacterSnapshotId = "";
    private string lastReportedCommandId = "";
    private bool syncingDisplayOptions;
    private bool syncingBackgroundSetting;
    private bool backgroundSettingPending;
    private bool desiredAllowBackgroundRunning;
    private bool syncingQuickActionSettings;
    private bool desiredCollectGold;
    private DateTime lastCollectGoldSyncAttemptUtc = DateTime.MinValue;
    private bool desiredGhostVision;
    private DateTime lastGhostVisionSyncAttemptUtc = DateTime.MinValue;

    public MainForm()
    {
        Text = "Noita 即时修改器 4.9.3";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 760);
        Size = new Size(1440, 1100);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(246, 246, 242);

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // The executable still carries the icon even if shell extraction is unavailable.
        }

        BuildCatalogButtonImages();
        BuildInterface();
        PopulateCatalogs();
        LoadBackgroundRunningSetting();
        LoadQuickActionSettings();

        var detected = GameLocator.FindNoitaRoot();
        if (detected is not null)
        {
            gamePathText.Text = detected;
            bridge.GameRoot = detected;
            RefreshNextSeedStatus();
            AppendLog($"已检测到 Noita：{detected}");
        }
        else
        {
            AppendLog("未自动找到 Noita，请手动选择游戏目录。");
        }

        statusTimer.Tick += (_, _) => PollStatus();
        statusTimer.Start();

        // 接收“重复启动”广播：第二个实例启动时把本窗口弹到前台
        showInstanceMessageId = User32.RegisterWindowMessage("CodexNoitaTrainer_ShowWindow");
        instanceMessageWindow = new InstanceMessageWindow(showInstanceMessageId, ShowFromOtherInstance);

        FormClosed += (_, _) =>
        {
            statusTimer.Dispose();
            quickActionToolTip.Dispose();
            foreach (var image in catalogButtonImages)
                image.Dispose();
            catalogButtonFont.Dispose();
            iconAtlas.Dispose();
            materialPreviewAtlas.Dispose();
            itemIconAtlas.Dispose();
        };
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var title = new Label
        {
            Text = "Noita 即时修改器",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(title, 0, 0);

        root.Controls.Add(BuildConnectionPanel(), 0, 1);
        root.Controls.Add(BuildTabs(), 0, 2);

        logBox.Dock = DockStyle.Fill;
        logBox.ReadOnly = true;
        logBox.BackColor = Color.FromArgb(34, 36, 38);
        logBox.ForeColor = Color.Gainsboro;
        logBox.BorderStyle = BorderStyle.FixedSingle;
        logBox.Font = new Font("Consolas", 9F);
        root.Controls.Add(logBox, 0, 3);

        var warning = new Label
        {
            Text = "提示：配套模组会让本局被标记为 Modded，原版成就/进度统计可能不会更新。建议先备份重要长周目。",
            ForeColor = Color.FromArgb(120, 70, 20),
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0)
        };
        root.Controls.Add(warning, 0, 4);
    }

    private Control BuildConnectionPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 5,
            Padding = new Padding(8),
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label { Text = "游戏目录", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        gamePathText.Dock = DockStyle.Fill;
        gamePathText.Margin = new Padding(8, 3, 8, 3);
        gamePathText.Leave += (_, _) => ApplyGamePath();
        panel.Controls.Add(gamePathText, 1, 0);

        var browse = Button("浏览…", (_, _) => BrowseForGame());
        var install = Button("安装/修复配套模组", (_, _) => InstallBridge());
        var launch = Button("启动 Noita", (_, _) => LaunchGame());
        panel.Controls.Add(browse, 2, 0);
        panel.Controls.Add(install, 3, 0);
        panel.Controls.Add(launch, 4, 0);

        connectionLabel.Text = "● 未连接";
        connectionLabel.ForeColor = Color.Firebrick;
        connectionLabel.Font = new Font(Font, FontStyle.Bold);
        connectionLabel.AutoSize = true;
        connectionLabel.Anchor = AnchorStyles.Left;
        connectionLabel.Margin = new Padding(0, 8, 16, 2);
        panel.Controls.Add(connectionLabel, 0, 1);

        liveStatsLabel.Text = "进入一局游戏并启用配套模组后，信息显示页会实时更新本局数据。";
        liveStatsLabel.AutoSize = true;
        liveStatsLabel.Anchor = AnchorStyles.Left;
        liveStatsLabel.Margin = new Padding(8, 8, 0, 2);
        panel.SetColumnSpan(liveStatsLabel, 4);
        panel.Controls.Add(liveStatsLabel, 1, 1);
        return panel;
    }

    private Control BuildTabs()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildInformationTab());
        tabs.TabPages.Add(BuildTeleportTab());
        tabs.TabPages.Add(BuildQuickActionsTab());
        tabs.TabPages.Add(BuildPerkTab());
        tabs.TabPages.Add(BuildStatusTab());
        tabs.TabPages.Add(BuildSpellTab());
        tabs.TabPages.Add(BuildEventTab());
        tabs.TabPages.Add(BuildPotionTab());
        tabs.TabPages.Add(BuildItemTab());
        tabs.TabPages.Add(BuildWandManagementTab());
        tabs.TabPages.Add(BuildCharacterManagementTab());
        tabs.TabPages.Add(BuildHelpTab());
        return tabs;
    }

    private TabPage BuildInformationTab()
    {
        var page = Page("信息显示");
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            AutoScroll = true
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(outer);

        var statisticsGroup = new GroupBox
        {
            Text = "实时信息",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 14)
        };
        var statistics = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 7,
            Padding = new Padding(4)
        };
        statistics.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        statistics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statistics.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        statistics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < statistics.RowCount; row++)
            statistics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        AddInformationPair(statistics, 0, "玩家位置坐标", playerPositionValue, "镜头位置坐标", cameraPositionValue);
        AddInformationPair(statistics, 1, "当前种子", worldSeedValue, "游戏时间", gameTimeValue);
        AddInformationPair(statistics, 2, "杀敌数", enemiesKilledValue, "承受伤害数", damageTakenValue);
        AddInformationPair(statistics, 3, "恢复生命数", healedValue, "射击次数", projectilesShotValue);
        AddInformationPair(statistics, 4, "总黄金数", goldAllValue, "探索地点数", placesVisitedValue);
        AddInformationPair(statistics, 5, "拾取物品数", itemsValue, "拾取魔杖数", wandsPickedValue);
        AddInformationPair(statistics, 6, "拾取额外生命数", heartContainersValue, "踢击次数", kicksValue);
        statisticsGroup.Controls.Add(statistics);
        outer.Controls.Add(statisticsGroup, 0, 0);

        var seedGroup = new GroupBox
        {
            Text = "修改种子",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 14)
        };
        var seedLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(4)
        };
        seedLayout.Controls.Add(new Label { Text = "下一局种子", AutoSize = true, Margin = new Padding(3, 7, 8, 3) });
        seedLayout.Controls.Add(nextSeedText);
        seedLayout.Controls.Add(Button("修改", (_, _) => SetNextWorldSeed()));
        seedLayout.Controls.Add(Button("取消预设", (_, _) => ClearNextWorldSeed()));
        nextSeedStatus.Margin = new Padding(12, 7, 3, 3);
        nextSeedStatus.Text = "尚未预设下一局种子。";
        seedLayout.Controls.Add(nextSeedStatus);
        seedGroup.Controls.Add(seedLayout);
        outer.Controls.Add(seedGroup, 0, 1);

        var overlayGroup = new GroupBox
        {
            Text = "游戏内显示",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            Margin = new Padding(0)
        };
        var overlayLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4)
        };
        showEnemyHpCheckBox.Margin = new Padding(3, 4, 3, 8);
        showPlayerCoordinatesCheckBox.Margin = new Padding(3, 4, 3, 8);
        showEnemyHpCheckBox.CheckedChanged += (_, _) => SendOverlayOption(
            "SET_SHOW_ENEMY_HP", "显示怪物血量", showEnemyHpCheckBox.Checked);
        showPlayerCoordinatesCheckBox.CheckedChanged += (_, _) => SendOverlayOption(
            "SET_SHOW_PLAYER_COORDS", "显示玩家坐标", showPlayerCoordinatesCheckBox.Checked);
        overlayLayout.Controls.Add(showEnemyHpCheckBox);
        overlayLayout.Controls.Add(showPlayerCoordinatesCheckBox);
        overlayGroup.Controls.Add(overlayLayout);
        outer.Controls.Add(overlayGroup, 0, 2);
        return page;
    }

    private TabPage BuildQuickActionsTab()
    {
        var page = Page("一键操作");
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(12),
            AutoScroll = true
        };
        for (var row = 0; row < outer.RowCount; row++)
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(outer);

        outer.Controls.Add(new Label
        {
            Text = "把鼠标悬停在勾选框或按钮上可以查看相应的详细功能。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(4, 2, 4, 10)
        }, 0, 0);

        var automaticGroup = QuickActionGroup("持续功能", out var automaticLayout);
        collectGoldCheckBox.Font = new Font(Font, FontStyle.Regular);
        collectGoldCheckBox.Margin = new Padding(4, 5, 18, 5);
        collectGoldCheckBox.CheckedChanged += (_, _) => ChangeCollectGoldSetting();
        automaticLayout.Controls.Add(collectGoldCheckBox);
        quickActionToolTip.SetToolTip(collectGoldCheckBox,
            "勾选后立即把所有已加载的普通金块与血金移动到玩家处，并持续收集后来生成或加载的金块。该勾选会被记住，直到手动取消。");
        ghostVisionCheckBox.Font = new Font(Font, FontStyle.Regular);
        ghostVisionCheckBox.Margin = new Padding(4, 5, 18, 5);
        ghostVisionCheckBox.CheckedChanged += (_, _) => ChangeGhostVisionSetting();
        automaticLayout.Controls.Add(ghostVisionCheckBox);
        quickActionToolTip.SetToolTip(ghostVisionCheckBox,
            "勾选后持续启用原版 magic_eye 显形组件，使本来需要手持邪王真眼才能看见的幽灵类生物自动显形；不会生成邪王真眼，也不会显示其激光或隐藏平台。该勾选会被记住，直到手动取消。");
        outer.Controls.Add(automaticGroup, 0, 1);

        var restoreGroup = QuickActionGroup("玩家操作", out var restoreLayout);
        restoreLayout.Controls.Add(QuickActionButton("回满血量", "将当前生命恢复到现有最大生命。",
            (_, _) => SendCommand("FULL_HEAL", "回满血量")));
        restoreLayout.Controls.Add(QuickActionButton("刷新法术", "恢复玩家法术栏及四根随身法杖里所有限次法术的最大使用次数。",
            (_, _) => SendCommand("REFRESH_SPELL_USES", "刷新法术使用次数")));
        restoreLayout.Controls.Add(QuickActionButton("一键获得各种免疫", "直接获得火焰、电击、爆炸、近战和毒性五种免疫天赋，跳过已拥有的天赋。",
            (_, _) => SendCommand("GIVE_ALL_IMMUNITIES", "一键获得各种免疫")));
        outer.Controls.Add(restoreGroup, 0, 2);

        var endingGroup = QuickActionGroup("结局效果模拟", out var endingLayout);
        endingLayout.Controls.Add(QuickActionButton("黄金世界", "把当前及以后加载的世界材质转换为黄金，不触发正式结局或永久进度。",
            (_, _) => SendCommand("SIMULATE_GOLD_WORLD", "启用黄金世界模拟")));
        endingLayout.Controls.Add(QuickActionButton("毒金世界", "把当前及以后加载的世界材质转换为毒金，不触发正式结局或永久进度。",
            (_, _) => SendCommand("SIMULATE_TOXIC_GOLD_WORLD", "启用毒金世界模拟")));
        endingLayout.Controls.Add(QuickActionButton("和平", "令当前与以后生成的生物对玩家保持非敌对，不重建世界。",
            (_, _) => SendCommand("SIMULATE_PEACE", "启用和平模拟")));
        endingLayout.Controls.Add(QuickActionButton("和平+无敌", "在和平基础上获得本局永久全伤害防护与变形免疫，不改变可见最大生命。",
            (_, _) => SendCommand("SIMULATE_PEACE_INVINCIBLE", "启用和平+无敌模拟")));
        outer.Controls.Add(endingGroup, 0, 3);

        var creaturesGroup = QuickActionGroup("生物操作", out var creaturesLayout);
        creaturesLayout.Controls.Add(QuickActionButton("消灭所有生物", "只对同时具有生物阵营与 AI/生物控制组件的敌对、中立、友好生物和 Boss 施加正常致死伤害；不会再处理 TNT、爆炸水晶、管道、巢穴、假人、投射物或机关。",
            (_, _) => SendCommand("KILL_ALL_CREATURES", "消灭所有已加载生物")));
        creaturesLayout.Controls.Add(QuickActionButton("消灭所有敌对生物", "只对带有 enemy 标签，并同时具有生物阵营与 AI/生物控制组件的已加载生物施加正常致死伤害。",
            (_, _) => SendCommand("KILL_ENEMIES", "消灭所有已加载敌对生物")));
        creaturesLayout.Controls.Add(QuickActionButton("魅惑所有生物", "只永久魅惑具有 AI/生物控制组件的真正生物，并同步为玩家阵营以阻止其继续把玩家当作猎物；不会再给 TNT、机关、假人、投射物或可破坏物体添加爱心。",
            (_, _) => SendCommand("CHARM_ALL_CREATURES", "魅惑所有已加载生物")));
        creaturesLayout.Controls.Add(QuickActionButton("冻结所有生物", "只永久冻结具有 AI/生物控制组件的真正生物，使其被冰封而无法移动或行动；对冻结免疫的生物不生效。",
            (_, _) => SendCommand("FREEZE_ALL_CREATURES", "冻结所有已加载生物")));
        creaturesLayout.Controls.Add(QuickActionButton("消灭所有实体", "沿用旧版“消灭所有生物”的无差别范围：摧毁所有已加载、带 mortal 标签且具有生命组件的非玩家实体，因此可能引爆 TNT、爆炸物并触发机关。",
            (_, _) => SendCommand("KILL_ALL_ENTITIES", "消灭所有已加载实体")));
        creaturesLayout.Controls.Add(QuickActionButton("爆尸所有生物", "先给所有已加载的真正生物附加原版“爆炸尸体”效果，再施加正常致死伤害；爆炸会伤害其他实体、破坏地形并可能连锁，同时临时给予玩家原版爆炸免疫。",
            (_, _) => SendCommand("EXPLODE_ALL_CREATURES", "爆尸所有已加载生物")));
        creaturesLayout.Controls.Add(QuickActionButton("爆尸所有敌对生物", "只让带有 enemy 标签的已加载真正生物按原版“爆炸尸体”效果死亡；爆炸会伤害其他实体、破坏地形并可能连锁，但不会伤害玩家。",
            (_, _) => SendCommand("EXPLODE_ENEMIES", "爆尸所有已加载敌对生物")));
        creaturesLayout.Controls.Add(QuickActionButton("引爆所有金块", "按原版“爆炸金块”天赋的价值缩放参数引爆所有已加载普通金块与血金；爆炸会破坏地形并可能连锁，不会伤害玩家，金块会直接消失且不会获得。",
            (_, _) => SendCommand("EXPLODE_GOLD", "引爆所有已加载金块")));
        creaturesLayout.Controls.Add(QuickActionButton("4*更多友爱", "在当前阵营关系修正值上增加 100，等效于叠加四次原版“更多友爱”天赋的关系效果；不会添加天赋图标或写入永久进度。",
            (_, _) => SendCommand("GENOME_MORE_LOVE_X4", "叠加 4 层更多友爱效果")));
        creaturesLayout.Controls.Add(QuickActionButton("4*更多仇恨", "在当前阵营关系修正值上减少 100，等效于叠加四次原版“更多仇恨”天赋的关系效果；不会添加天赋图标或写入永久进度。",
            (_, _) => SendCommand("GENOME_MORE_HATRED_X4", "叠加 4 层更多仇恨效果")));
        creaturesLayout.MinimumSize = new Size(0, 94);
        outer.Controls.Add(creaturesGroup, 0, 4);

        var dangerGroup = QuickActionGroup("危险操作", out var dangerLayout);
        dangerLayout.Controls.Add(QuickActionButton("自杀", "触发玩家的正常死亡和游戏结束流程；点击后还会要求再次确认。",
            (_, _) => ConfirmSuicide()));
        dangerLayout.Controls.Add(QuickActionButton("角色永久变羊", "使当前角色变成不会自动恢复的羊，并关闭本工具的无敌防护；点击后需要确认。",
            (_, _) => ConfirmPermanentSheep()));
        outer.Controls.Add(dangerGroup, 0, 5);
        return page;
    }

    private TabPage BuildTeleportTab()
    {
        var page = Page("空间传送");
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            AutoScroll = true
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(outer);

        var liveGroup = new GroupBox
        {
            Text = "当前位置",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 14)
        };
        var liveLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        liveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        liveLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddInformationRow(liveLayout, 0, "玩家实时坐标", teleportPositionValue);
        liveGroup.Controls.Add(liveLayout);
        outer.Controls.Add(liveGroup, 0, 0);

        var customGroup = new GroupBox
        {
            Text = "坐标传送",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 14)
        };
        var customLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 5 };
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        customLayout.Controls.Add(Label("X"), 0, 0);
        teleportXText.Dock = DockStyle.Fill;
        customLayout.Controls.Add(teleportXText, 1, 0);
        customLayout.Controls.Add(Label("Y"), 2, 0);
        teleportYText.Dock = DockStyle.Fill;
        customLayout.Controls.Add(teleportYText, 3, 0);
        customLayout.Controls.Add(Button("传送", (_, _) => TeleportCustom()), 4, 0);
        customGroup.Controls.Add(customLayout);
        outer.Controls.Add(customGroup, 0, 1);

        var locationsGroup = new GroupBox
        {
            Text = "预设传送",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            Margin = new Padding(0)
        };
        var locationsOuter = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 7 };
        for (var row = 0; row < 7; row++)
            locationsOuter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        allowBackgroundRunningCheckBox.Margin = new Padding(5, 4, 5, 10);
        allowBackgroundRunningCheckBox.CheckedChanged += (_, _) => ChangeBackgroundRunningSetting();
        locationsOuter.Controls.Add(allowBackgroundRunningCheckBox, 0, 0);

        // 主线区域
        locationsOuter.Controls.Add(BuildTeleportGroup("主线区域", new (string, decimal, decimal)[]
        {
            ("出生点", 227, -100),
            ("第一圣山", -380, 1380),
            ("煤矿坑", 190, 1500),
            ("积雪深渊", 190, 3000),
            ("席西基地", 190, 5000),
            ("地下丛林", 190, 6500),
            ("避难所", 190, 8500),
            ("艺之神殿", 190, 10700),
            ("伟大之作", 6241, 15130)
        }), 0, 1);

        // Boss
        locationsOuter.Controls.Add(BuildTeleportGroup("Boss", new (string, decimal, decimal)[]
        {
            ("Boss：三眼之足（金字塔）", 9870, -800),
            ("Boss：魔杖鉴赏家（岩浆湖）", 3948, 848),
            ("Boss：奇石（天空）", 7480, -5020),
            ("Boss：森灵使者（落雪荒原）", -13510, 163),
            ("Boss：古代炼金术士（雪山）", -4870, 890),
            ("Boss：“小龙虾”（湖底）", -13955, 9975),
            ("Boss：被遗忘者（落雪荒原深处）", -11555, 13185),
            ("Boss：龙（地下丛林龙巢）", 2347, 7300),
            ("Boss：三眼之心", 6759, 8456),
            ("Boss：门神（艺之神殿）", 2669, 11604),
            ("Boss：三眼之瞳", 13976, 10997),
            ("Boss：法师领主（实验室）", 12542, 15174),
            ("Boss：小小蛆（东侧头骨）", 14600, 16250),
            ("Boss：三眼（最终Boss）", 3500, 13060)
        }), 0, 2);

        // 魔球室
        locationsOuter.Controls.Add(BuildTeleportGroup("魔球室", new (string, decimal, decimal)[]
        {
            ("魔球室（月神透特：地震）", 10000, -1300),
            ("魔球室（卷一：岩浆之海）", 775, -1074),
            ("魔球室（卷二：召唤触手）", -10010, 2827),
            ("魔球室（卷三：核弹）", 3470, 1820),
            ("魔球室（卷四：死灵之术）", 9955, 2819),
            ("魔球室（卷五：神圣炸弹）", -4375, 3867),
            ("魔球室（卷六：螺旋魔弹）", -3840, 10000),
            ("魔球室（卷七：雷云）", 4354, 763),
            ("魔球室（卷八：烟火）", -255, 16147),
            ("魔球室（卷九：召唤鹿诱饵）", -8957, 14609),
            ("魔球室（卷十：水泥）", 10476, 16148)
        }), 0, 3);

        // 精粹与精粹吞噬者
        locationsOuter.Controls.Add(BuildTeleportGroup("精粹与精粹吞噬者", new (string, decimal, decimal)[]
        {
            ("土之精粹", 16130, -1780),
            ("水之精粹", -5370, 16650),
            ("气之精粹", -13060, -5360),
            ("火之精粹（湖心岛）", -14060, 360),
            ("酒之精粹（湖底）", -14080, 13580),
            ("落雪荒原精粹吞噬者", -6880, -165),
            ("沙漠精粹吞噬者", 12575, 0)
        }), 0, 4);

        // 重要物品与设施
        locationsOuter.Controls.Add(BuildTeleportGroup("重要物品与设施", new (string, decimal, decimal)[]
        {
            ("音乐机器（巨树）", -1907, -1500),
            ("音乐机器（森林池塘）", 2792, 220),
            ("音乐机器（雪原）", -12187, -480),
            ("音乐机器（沙漠）", 14702, -150),
            ("陶笛", -10000, -6490),
            ("贫瘠神庙", -5453, -5483),
            ("拟态神庙", -1382, -5558),
            ("不详神庙", 2815, -5059),
            ("奇石神庙", 7792, -4975),
            ("地狱拟态神庙", -1900, 19000),
            ("地狱不详神庙", 2900, 19500),
            ("珊瑚宝箱", 11496, -4920),
            ("黑暗宝箱", 3840, 15590),
            ("金矿1", 14891, -3256),
            ("金矿2", -14064, 16489),
            ("席西铁砧（席西基地）", 1516, 6026),
            ("天空法术商店", 3350, -13100),
            ("地狱法术商店", 3300, 36000),
            ("魔塔法杖区域", 9980, 4340),
            ("加特林法杖", 16130, 10000),
            ("探月雷达", 16130, 3345),
            ("天赋祭坛", 14050, 7550),
            ("贪婪诅咒天赋", -1395, -463),
            ("成就石柱", -1470, -1300),
            ("雪地邪王真眼", -2440, -210),
            ("音之石", -3330, 3350),
            ("湖边小屋", -12548, 126),
            ("湖边碉堡", -12953, 540),
            ("提神葫芦房间", -15928, -6403)
        }), 0, 5);

        // 隐藏与秘密区域
        locationsOuter.Controls.Add(BuildTeleportGroup("隐藏与秘密区域", new (string, decimal, decimal)[]
        {
            ("岩浆湖（矿场东侧）", 2370, 768),
            ("森林池塘", 2541, -80),
            ("康特勒琴", -1675, -780),
            ("雪山祭坛（悬浮岛）", 774, -1197),
            ("木头人（湖边）", -14070, 90),
            ("湖泊", -14824, 163),
            ("彩虹尾迹祭坛（湖心岛上空）", -14059, -2851),
            ("云景（西）", -13229, -4817),
            ("云景（东）", 10266, -4784),
            ("沙漠巨型颅骨", 7239, -67),
            ("沙漠真菌基座", 5902, -133),
            ("沙漠天平", 13000, -67),
            ("沙漠户外厕所", 9034, -1848),
            ("沙漠瞭望塔", 13703, -69),
            ("金字塔", 8900, -320),
            ("冻结避难所", -10000, 360),
            ("古代实验室", -3150, 860),
            ("魔法神殿（积雪深渊左侧）", -2600, 3730),
            ("蜘蛛巢穴", -3665, 7823),
            ("巫师巢穴", 9706, 12603),
            ("繁茂洞穴（瞭望塔下方）", 13188, 4286),
            ("发电站", 12350, 8170),
            ("魔法神殿（艺之神殿右侧）", 3000, 11600),
            ("血肉地狱（西侧）", 6044, 7179),
            ("血肉地狱（东侧）", 14179, 13364),
            ("魔塔", 9740, 9170),
            ("月球", 260, -26110),
            ("暗月", 258, 37666),
            ("伟大之作（天空）", -200, -8500),
            ("伟大之作（地狱）", -500, 36000)
        }), 0, 6);
        locationsGroup.Controls.Add(locationsOuter);
        outer.Controls.Add(locationsGroup, 0, 2);
        return page;
    }

    private TabPage BuildHealthTab()
    {
        var page = Page("生命");
        var layout = FormLayout();
        page.Controls.Add(layout);

        layout.Controls.Add(Label("当前生命（游戏画面数值）"), 0, 0);
        layout.Controls.Add(hpText, 1, 0);
        layout.Controls.Add(Button("设置当前生命", (_, _) => SendHp()), 2, 0);

        layout.Controls.Add(Label("最大生命（游戏画面数值）"), 0, 1);
        layout.Controls.Add(maxHpText, 1, 1);
        layout.Controls.Add(Button("设置最大生命", (_, _) => SendMaxHp()), 2, 1);
        layout.Controls.Add(healWithMaxCheck, 1, 2);

        var heal = Button("一键回满", (_, _) => SendCommand("FULL_HEAL", "回满生命"));
        heal.BackColor = Color.FromArgb(210, 241, 216);
        layout.Controls.Add(heal, 2, 2);

        var explanation = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Text = "这里直接使用游戏 HUD 上的生命值。例如输入 100 就是 100 HP；修改器会在桥接层自动换算 Noita 内部的 1/25 存储单位。若当前生命高于最大生命，设置当前生命时会同步抬高最大生命。",
            ForeColor = Color.DimGray,
            Margin = new Padding(3, 18, 3, 3)
        };
        layout.SetColumnSpan(explanation, 3);
        layout.Controls.Add(explanation, 0, 3);
        return page;
    }

    private TabPage BuildSpellTab()
    {
        var page = Page("添加法术");
        page.Controls.Add(BuildCatalogButtonPage(
            catalog.Spells,
            "法术",
            spellSpawnMode,
            spellGiveMode,
            spellCount,
            item => ExecuteSpell(item.Id)));
        return page;
    }

    private TabPage BuildItemTab()
    {
        var page = Page("添加物品");
        page.Controls.Add(BuildCatalogButtonPage(
            catalog.Items,
            "物品",
            itemSpawnMode,
            itemGiveMode,
            itemCount,
            item => ExecuteItem(item.Id)));
        return page;
    }

    private TabPage BuildWandTab()
    {
        var page = Page("法杖");
        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(outer);

        var preset = new GroupBox { Text = "原版程序生成预设", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        var presetLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4 };
        presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        presetLayout.Controls.Add(Label("法杖预设"), 0, 0);
        wandCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        wandCombo.Width = 390;
        wandCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        presetLayout.Controls.Add(wandCombo, 1, 0);
        presetLayout.Controls.Add(Label("数量"), 2, 0);
        presetLayout.Controls.Add(wandCount, 3, 0);
        var presetButton = Button("在脚下生成预设法杖", (_, _) => SpawnPresetWand());
        presetButton.Margin = new Padding(3, 10, 3, 3);
        presetLayout.SetColumnSpan(presetButton, 4);
        presetLayout.Controls.Add(presetButton, 0, 1);
        preset.Controls.Add(presetLayout);
        outer.Controls.Add(preset, 0, 0);

        var custom = new GroupBox { Text = "自定义法杖", Dock = DockStyle.Fill, Padding = new Padding(10), Margin = new Padding(0, 10, 0, 0) };
        var customLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5 };
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        customLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var shuffle = new CheckBox { Text = "乱序法杖", Checked = false, AutoSize = true };
        var capacity = NumberText("10");
        var castCount = NumberText("1");
        var castDelay = NumberText("0.10");
        var recharge = NumberText("0.50");
        var manaMax = NumberText("500");
        var manaCharge = NumberText("200");
        var spread = NumberText("0");

        AddPair(customLayout, 0, "容量", capacity, "每次施放数", castCount);
        AddPair(customLayout, 1, "施放延迟（秒）", castDelay, "充能时间（秒）", recharge);
        AddPair(customLayout, 2, "最大法力", manaMax, "法力回复/秒", manaCharge);
        AddPair(customLayout, 3, "散射角度", spread, "", shuffle);

        var create = Button("生成自定义法杖", (_, _) => SpawnCustomWand(shuffle, capacity, castCount, castDelay, recharge, manaMax, manaCharge, spread));
        create.BackColor = Color.FromArgb(232, 222, 250);
        create.Margin = new Padding(3, 12, 3, 3);
        customLayout.SetColumnSpan(create, 4);
        customLayout.Controls.Add(create, 0, 4);
        custom.Controls.Add(customLayout);
        outer.Controls.Add(custom, 0, 1);
        return page;
    }

    private TabPage BuildPerkTab()
    {
        var page = Page("添加天赋");
        page.Controls.Add(BuildCatalogButtonPage(
            catalog.Perks,
            "天赋",
            perkSpawnMode,
            perkGiveMode,
            perkCount,
            item => ExecutePerk(item.Id)));
        return page;
    }

    private TabPage BuildStatusTab()
    {
        var page = Page("添加状态");
        page.Controls.Add(BuildCatalogButtonPage(
            catalog.Statuses,
            "状态",
            statusTemporaryMode,
            statusPermanentMode,
            statusDuration,
            ExecuteStatus,
            optionsLabel: "持续方式",
            countLabel: "时长(秒)/沾染程度(%)"));
        var clearBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(12, 10, 12, 0)
        };
        var clearButton = Button("清除全部状态", (_, _) => ConfirmClearEffects());
        clearButton.MinimumSize = new Size(120, 32);
        clearBar.Controls.Add(clearButton);
        clearBar.Controls.Add(new Label
        {
            Text = "移除玩家身上的状态效果（含永久效果），清除沾染与摄取状态，并重置幻觉/醉酒等视觉扭曲；天赋不受影响。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(10, 7, 4, 7)
        });
        page.Controls.Add(clearBar);
        return page;
    }

    private TabPage BuildEventTab()
    {
        var page = Page("添加事件");
        page.Controls.Add(BuildCatalogButtonPage(
            catalog.Events,
            "事件",
            null,
            null,
            null,
            ExecuteEvent,
            showIcons: false));
        return page;
    }

    private static readonly (string Key, string Label)[] PotionCategories =
    {
        ("spark", "火花"),
        ("liquid", "液体"),
        ("powder", "粉末/砂"),
        ("solid", "固体"),
        ("gas", "气体"),
        ("box2d", "Box2D")
    };

    private static string CategoryLabel(string category) => category switch
    {
        "spark" => "火花",
        "liquid" => "液体",
        "powder" => "粉末/砂",
        "solid" => "固体",
        "gas" => "气体",
        "box2d" => "Box2D",
        _ => category
    };

    private TabPage BuildPotionTab()
    {
        var page = Page("添加药水");
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(outer);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        outer.Controls.Add(main, 0, 0);

        // ---- 左侧：分类 + 搜索 + 材质列表 ----
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 0, 8, 0) };
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(left, 0, 0);

        var categoryBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 6),
            BackColor = Color.White
        };
        foreach (var (key, label) in PotionCategories)
        {
            var captured = key;
            var radio = new RadioButton
            {
                Text = label,
                AutoSize = true,
                Checked = key == currentPotionCategory,
                Margin = new Padding(2, 4, 16, 4),
                BackColor = Color.White
            };
            radio.CheckedChanged += (_, _) =>
            {
                if (!radio.Checked)
                    return;
                currentPotionCategory = captured;
                RebuildPotionBrowser();
            };
            categoryBar.Controls.Add(radio);
        }
        left.Controls.Add(categoryBar, 0, 0);

        potionSearchText.Dock = DockStyle.Fill;
        potionSearchText.PlaceholderText = "搜索材质中文名 / 英文名 / ID…";
        potionSearchText.Margin = new Padding(0, 0, 0, 6);
        potionSearchText.TextChanged += (_, _) => RebuildPotionBrowser();
        left.Controls.Add(potionSearchText, 0, 1);

        var browserHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        potionMaterialPanel.Dock = DockStyle.Top;
        potionMaterialPanel.AutoSize = true;
        potionMaterialPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        potionMaterialPanel.BackColor = Color.White;
        potionMaterialPanel.ColumnCount = 3;
        potionMaterialPanel.Padding = new Padding(4);
        potionMaterialPanel.Margin = new Padding(0);
        for (var c = 0; c < 3; c++)
            potionMaterialPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3F));
        browserHost.Controls.Add(potionMaterialPanel);
        left.Controls.Add(browserHost, 0, 2);

        // ---- 右侧：配方 ----
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8, 0, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.Controls.Add(right, 1, 0);

        right.Controls.Add(new Label
        {
            Text = "配方（点击左侧材质加入；每项数量按容器总容量的百分比计算）",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(4, 2, 4, 6)
        }, 0, 0);

        var recipeHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        potionRecipePanel.Dock = DockStyle.Top;
        potionRecipePanel.AutoSize = true;
        potionRecipePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        potionRecipePanel.BackColor = Color.White;
        potionRecipePanel.ColumnCount = 1;
        potionRecipePanel.Padding = new Padding(4);
        potionRecipePanel.Margin = new Padding(0);
        potionRecipePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        recipeHost.Controls.Add(potionRecipePanel);
        right.Controls.Add(recipeHost, 0, 1);

        potionRecipeTotal.AutoSize = true;
        potionRecipeTotal.ForeColor = Color.DimGray;
        potionRecipeTotal.Margin = new Padding(4, 6, 4, 2);
        right.Controls.Add(potionRecipeTotal, 0, 2);

        // ---- 底部控制 ----
        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.White,
            Padding = new Padding(4),
            Margin = new Padding(0, 8, 0, 0)
        };

        var containerFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 8, 0)
        };
        containerFlow.Controls.Add(Label("容器"));
        potionFlaskRadio.Margin = new Padding(4, 5, 16, 3);
        potionPouchRadio.Margin = new Padding(0, 5, 8, 3);
        containerFlow.Controls.Add(potionFlaskRadio);
        containerFlow.Controls.Add(potionPouchRadio);
        controls.Controls.Add(containerFlow);

        var capacityFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 8, 0)
        };
        capacityFlow.Controls.Add(Label("数量（%）"));
        potionCapacity.Margin = new Padding(3, 5, 8, 3);
        potionCapacity.ValueChanged += (_, _) => UpdatePotionRecipeTotal();
        capacityFlow.Controls.Add(potionCapacity);
        controls.Controls.Add(capacityFlow);

        var modeFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 8, 0)
        };
        potionSpawnRadio.Margin = new Padding(0, 5, 16, 3);
        potionGiveRadio.Margin = new Padding(0, 5, 8, 3);
        modeFlow.Controls.Add(potionSpawnRadio);
        modeFlow.Controls.Add(potionGiveRadio);
        controls.Controls.Add(modeFlow);

        var generate = Button("生成", (_, _) => GeneratePotion());
        generate.Height = 32;
        controls.Controls.Add(generate);
        outer.Controls.Add(controls, 0, 1);

        RebuildPotionBrowser();
        RebuildPotionRecipePanel();
        return page;
    }

    private void RebuildPotionBrowser()
    {
        potionMaterialPanel.SuspendLayout();
        potionMaterialPanel.Controls.Clear();

        var query = potionSearchText.Text.Trim();
        var items = catalog.Materials
            .Where(m => m.Category == currentPotionCategory)
            .Where(m => string.IsNullOrEmpty(query) ||
                        m.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        m.Zh.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                        m.En.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        const int columns = 3;
        potionMaterialPanel.RowCount = Math.Max(1, (items.Count + columns - 1) / columns);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var button = new MaterialButton(item, materialPreviewAtlas) { Dock = DockStyle.Fill };
            button.Click += (_, _) => AddMaterialToRecipe(item);
            quickActionToolTip.SetToolTip(button,
                $"{item.DisplayName}\n分类：{CategoryLabel(item.Category)}");
            potionMaterialPanel.Controls.Add(button, i % columns, i / columns);
        }
        potionMaterialPanel.ResumeLayout(true);
    }

    private Control BuildRecipeRow(PotionRecipeEntry entry)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 5,
            Margin = new Padding(0, 2, 0, 2),
            BackColor = Color.White
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));

        var preview = new Panel
        {
            Size = new Size(48, 48),
            BackColor = Color.White,
            Margin = new Padding(2),
            Anchor = AnchorStyles.Left
        };
        preview.Paint += (_, e) => DrawMaterialPreview(e.Graphics,
            new Rectangle(0, 0, preview.ClientSize.Width, preview.ClientSize.Height),
            entry.Material.PreviewIndex);
        row.Controls.Add(preview, 0, 0);

        var name = new Label
        {
            Text = entry.Material.DisplayName,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(6, 15, 4, 2)
        };
        row.Controls.Add(name, 1, 0);

        row.Controls.Add(new Label
        {
            Text = "数量（%）",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(2, 15, 2, 2)
        }, 2, 0);

        entry.Ratio.Minimum = 0;
        entry.Ratio.Maximum = 100;
        entry.Ratio.Width = 72;
        entry.Ratio.Anchor = AnchorStyles.Left;
        entry.Ratio.Margin = new Padding(2, 11, 2, 2);
        entry.Ratio.ValueChanged += (_, _) => UpdatePotionRecipeTotal();
        row.Controls.Add(entry.Ratio, 3, 0);

        var remove = Button("移除", (_, _) => RemoveMaterialFromRecipe(entry));
        remove.AutoSize = false;
        remove.Width = 68;
        remove.Height = 32;
        remove.Margin = new Padding(2, 9, 2, 2);
        row.Controls.Add(remove, 4, 0);
        return row;
    }

    private void RebuildPotionRecipePanel()
    {
        potionRecipePanel.SuspendLayout();
        potionRecipePanel.Controls.Clear();
        potionRecipePanel.RowCount = Math.Max(1, potionRecipe.Count);
        for (var i = 0; i < potionRecipe.Count; i++)
            potionRecipePanel.Controls.Add(BuildRecipeRow(potionRecipe[i]), 0, i);
        potionRecipePanel.ResumeLayout(true);
        UpdatePotionRecipeTotal();
    }

    private void UpdatePotionRecipeTotal()
    {
        if (potionRecipe.Count == 0)
        {
            potionRecipeTotal.Text = "配方为空：点击左侧材质加入。";
            potionRecipeTotal.ForeColor = Color.DimGray;
            return;
        }
        var total = potionRecipe.Sum(e => (int)e.Ratio.Value);
        var limit = (int)potionCapacity.Value;
        var exceeded = PotionRecipeExceedsCapacity(total, limit);
        potionRecipeTotal.Text = exceeded
            ? $"配方合计：{total}% / 数量上限：{limit}%（已超过上限，无法生成）"
            : $"配方合计：{total}% / 数量上限：{limit}%";
        potionRecipeTotal.ForeColor = exceeded ? Color.Firebrick : Color.DimGray;
    }

    private void AddMaterialToRecipe(MaterialItem material)
    {
        if (potionRecipe.Any(e => e.Material.Id == material.Id))
            return;
        potionRecipe.Add(new PotionRecipeEntry
        {
            Material = material,
            Ratio = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 100, Width = 72 }
        });
        RebuildPotionRecipePanel();
    }

    private void RemoveMaterialFromRecipe(PotionRecipeEntry entry)
    {
        potionRecipe.Remove(entry);
        RebuildPotionRecipePanel();
    }

    private void GeneratePotion()
    {
        if (potionRecipe.Count == 0)
        {
            MessageBox.Show(this, "请先在左侧选择至少一种材质加入配方。", "配方为空",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var activeEntries = potionRecipe.Where(entry => entry.Ratio.Value > 0).ToList();
        var total = activeEntries.Sum(entry => (int)entry.Ratio.Value);
        if (total <= 0)
        {
            MessageBox.Show(this, "配方比例合计必须大于 0。", "比例无效",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var capacityPercent = (int)potionCapacity.Value;
        if (PotionRecipeExceedsCapacity(total, capacityPercent))
        {
            MessageBox.Show(this,
                $"配方中所有材质的数量合计为 {total}%，超过了当前设置的数量上限 {capacityPercent}%。\n\n请降低配方数量或提高数量上限后再生成。",
                "配方超过数量上限", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var container = potionPouchRadio.Checked ? "pouch" : "flask";
        var containerCapacity = container == "pouch" ? 1500 : 1000;
        var mode = potionGiveRadio.Checked ? "give" : "spawn";

        var arguments = new List<object> { container, mode, activeEntries.Count };
        foreach (var entry in activeEntries)
        {
            var units = (int)(entry.Ratio.Value * containerCapacity / 100m);
            arguments.Add(entry.Material.Id);
            arguments.Add(units);
        }

        var description = $"{(mode == "give" ? "直接获得" : "生成")}药水容器（{activeEntries.Count} 种材质，实际装入 {total}%）";
        SendCommand("SPAWN_MATERIAL_CONTAINER", description, arguments.ToArray());
    }

    private static bool PotionRecipeExceedsCapacity(int recipeTotal, int capacityLimit) =>
        recipeTotal > capacityLimit;

    private sealed class PotionRecipeEntry
    {
        public MaterialItem Material { get; init; } = null!;
        public NumericUpDown Ratio { get; init; } = null!;
    }

    private void DrawMaterialPreview(Graphics graphics, Rectangle target, int previewIndex) =>
        DrawMaterialPreview(graphics, target, materialPreviewAtlas, previewIndex);

    private static void DrawMaterialPreview(
        Graphics graphics, Rectangle target, Bitmap atlas, int previewIndex)
    {
        if (previewIndex < 0)
            return;
        var source = new Rectangle(
            previewIndex % CatalogService.MaterialPreviewAtlasColumns * CatalogService.MaterialPreviewCellSize,
            previewIndex / CatalogService.MaterialPreviewAtlasColumns * CatalogService.MaterialPreviewCellSize,
            CatalogService.MaterialPreviewCellSize,
            CatalogService.MaterialPreviewCellSize);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.DrawImage(atlas, target, source, GraphicsUnit.Pixel);
    }

    private sealed class MaterialButton : Button
    {
        private readonly Bitmap previewAtlas;
        private readonly int previewIndex;
        private bool pointerInside;

        public MaterialButton(MaterialItem material, Bitmap atlas)
        {
            previewAtlas = atlas;
            previewIndex = material.PreviewIndex;
            Text = material.DisplayName;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UseVisualStyleBackColor = false;
            Height = 58;
            MinimumSize = new Size(140, 58);
            Margin = new Padding(2);
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            pointerInside = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            pointerInside = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var background = pointerInside ? Color.FromArgb(240, 246, 252) : Color.White;
            using (var brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, ClientRectangle);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                pointerInside ? Color.FromArgb(86, 135, 185) : Color.FromArgb(175, 175, 175),
                ButtonBorderStyle.Solid);

            const int previewSize = 50;
            var previewRect = new Rectangle(5, Math.Max(0, (ClientSize.Height - previewSize) / 2), previewSize, previewSize);
            DrawMaterialPreview(e.Graphics, previewRect, previewAtlas, previewIndex);

            var textBounds = new Rectangle(
                previewRect.Right + 8,
                1,
                Math.Max(0, ClientSize.Width - previewRect.Right - 12),
                Math.Max(0, ClientSize.Height - 2));
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds,
                Enabled ? ForeColor : SystemColors.GrayText,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }
    }

    private TabPage BuildWandManagementTab()
    {
        var page = Page("魔杖管理");
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(outer);

        var readButton = Button("读取当前魔杖栏", (_, _) => ReadCurrentWands());
        readButton.AutoSize = true;
        readButton.Margin = new Padding(3, 3, 3, 10);
        outer.Controls.Add(readButton, 0, 0);

        wandManagementContent.Dock = DockStyle.Fill;
        wandManagementContent.ColumnCount = 1;
        wandManagementContent.RowCount = 4;
        wandManagementContent.Visible = false;
        wandManagementContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wandManagementContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        wandManagementContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wandManagementContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.Controls.Add(wandManagementContent, 0, 1);

        wandManagementStatus.AutoSize = true;
        wandManagementStatus.ForeColor = Color.DimGray;
        wandManagementStatus.Margin = new Padding(3, 0, 3, 6);
        wandManagementContent.Controls.Add(wandManagementStatus, 0, 0);

        ConfigureManagedWandGrid();
        wandManagementContent.Controls.Add(managedWandGrid, 0, 1);

        var editor = new GroupBox
        {
            Text = "修改所选魔杖属性",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Margin = new Padding(0, 8, 0, 8)
        };
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, RowCount = 5 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddPair(fields, 0, "单次施放数", managedWandActions, "容量", managedWandCapacity);
        AddPair(fields, 1, "施放延迟（秒）", managedWandCastDelay, "充能时间（秒）", managedWandRecharge);
        AddPair(fields, 2, "法力上限", managedWandManaMax, "法力恢复速度", managedWandManaCharge);
        AddPair(fields, 3, "散射（度）", managedWandSpread, "速度加成", managedWandSpeed);
        managedWandShuffle.Margin = new Padding(3, 9, 3, 3);
        fields.Controls.Add(managedWandShuffle, 0, 4);
        fields.SetColumnSpan(managedWandShuffle, 2);
        var modify = Button("修改", (_, _) => UpdateSelectedWand());
        modify.Margin = new Padding(3, 7, 3, 3);
        fields.Controls.Add(modify, 2, 4);
        fields.SetColumnSpan(modify, 2);
        editor.Controls.Add(fields);
        wandManagementContent.Controls.Add(editor, 0, 2);

        var alwaysCast = new GroupBox
        {
            Text = "为所选魔杖添加始终释放法术",
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            Margin = new Padding(0)
        };
        alwaysCast.Controls.Add(BuildCatalogButtonPage(
            catalog.Spells,
            "法术",
            null,
            null,
            null,
            AddAlwaysCastToSelectedWand));
        wandManagementContent.Controls.Add(alwaysCast, 0, 3);
        return page;
    }

    private void ConfigureManagedWandGrid()
    {
        managedWandGrid.Dock = DockStyle.Fill;
        managedWandGrid.ReadOnly = true;
        managedWandGrid.AllowUserToAddRows = false;
        managedWandGrid.AllowUserToDeleteRows = false;
        managedWandGrid.AllowUserToResizeRows = false;
        managedWandGrid.MultiSelect = false;
        managedWandGrid.RowHeadersVisible = false;
        managedWandGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        managedWandGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        managedWandGrid.BackgroundColor = Color.White;
        managedWandGrid.BorderStyle = BorderStyle.FixedSingle;
        managedWandGrid.Columns.Add("wand", "魔杖栏");
        managedWandGrid.Columns.Add("shuffle", "乱序");
        managedWandGrid.Columns.Add("actions", "单次施放");
        managedWandGrid.Columns.Add("castDelay", "施放延迟");
        managedWandGrid.Columns.Add("recharge", "充能时间");
        managedWandGrid.Columns.Add("manaMax", "法力上限");
        managedWandGrid.Columns.Add("manaCharge", "法力恢复");
        managedWandGrid.Columns.Add("capacity", "容量");
        managedWandGrid.Columns.Add("spread", "散射");
        managedWandGrid.Columns.Add("speed", "速度加成");
        managedWandGrid.SelectionChanged += (_, _) => PopulateSelectedWandFields();
    }

    private TabPage BuildCharacterManagementTab()
    {
        var page = Page("角色管理");
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(outer);

        var readButton = Button("读取当前角色信息", (_, _) => ReadCurrentCharacter());
        readButton.AutoSize = true;
        readButton.Margin = new Padding(3, 3, 3, 10);
        outer.Controls.Add(readButton, 0, 0);

        characterManagementContent.Dock = DockStyle.Fill;
        characterManagementContent.ColumnCount = 1;
        characterManagementContent.RowCount = 2;
        characterManagementContent.Visible = false;
        characterManagementContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        characterManagementContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.Controls.Add(characterManagementContent, 0, 1);

        characterManagementStatus.AutoSize = true;
        characterManagementStatus.ForeColor = Color.DimGray;
        characterManagementStatus.Margin = new Padding(3, 0, 3, 6);
        characterManagementContent.Controls.Add(characterManagementStatus, 0, 0);

        var editor = new GroupBox
        {
            Text = "当前角色属性",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Margin = new Padding(0, 8, 0, 0)
        };
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, RowCount = 8 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddPair(fields, 0, "金钱", characterMoney, "当前血量", characterHp);
        AddPair(fields, 1, "最大血量", characterMaxHp, "肺活量（秒）", characterLungCapacity);
        AddPair(fields, 2, "当前肺活量（秒）", characterCurrentAir, "最大浮空能量（秒）", characterFlyTimeMax);
        AddPair(fields, 3, "当前浮空能量（秒）", characterFlyTimeLeft, "空中浮空恢复速度", characterFlyRecharge);
        AddPair(fields, 4, "地面浮空恢复速度", characterFlyRechargeGround, "奔跑速度", characterRunVelocity);
        AddPair(fields, 5, "水平飞行速度", characterFlyVelocityX, "跳跃速度（向上为负）", characterJumpVelocityY);
        fields.Controls.Add(new Label { Text = "重力", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 8, 3) }, 0, 6);
        characterPixelGravity.Dock = DockStyle.Fill;
        characterPixelGravity.Margin = new Padding(3, 3, 16, 3);
        fields.Controls.Add(characterPixelGravity, 1, 6);
        var modify = Button("修改", (_, _) => UpdateCurrentCharacter());
        modify.Margin = new Padding(3, 9, 3, 3);
        fields.Controls.Add(modify, 2, 7);
        fields.SetColumnSpan(modify, 2);
        editor.Controls.Add(fields);
        characterManagementContent.Controls.Add(editor, 0, 1);
        return page;
    }

    private TabPage BuildHelpTab()
    {
        var page = Page("使用说明");
        var text = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Text = "1. 确认上方游戏目录正确，点击“安装/修复配套模组”。\r\n\r\n" +
                   "2. 启动 Noita，进入 Mods 菜单，允许 Unsafe mods，并启用“Codex Noita Trainer Bridge”。首次启用后需要重新开始或重新载入游戏。\r\n\r\n" +
                   "3. 进入一局游戏。修改器顶部变成绿色“已连接”后，“信息显示”页会实时更新本局坐标、种子、累计回血与原版统计数据；该页也可以为下一次普通新游戏预设一次性种子。\r\n\r\n" +
                   "4. 勾选游戏内显示选项后，配套模组会使用原版圣山价格数字字体绘制怪物血量或玩家坐标。\r\n\r\n" +
                   "5. “空间传送”页支持输入任意坐标，也可以点击地点按钮一键传送。\r\n\r\n" +
                   "6. “一键操作”页包含自动收集金块、幽灵透视、恢复、结局效果模拟、原版爆尸/爆金效果及当前已加载生物操作。“黄金世界”“毒金世界”“和平”“和平+无敌”都不会调用正式结局，也不会写入相关永久进度。\r\n\r\n" +
                   "7. “添加天赋”页把全部天赋按进展顺序排列为图标按钮；先选择生成或直接获得，再选择数量并点击目标天赋。\r\n\r\n" +
                   "8. “添加法术”页操作相同，可把法术生成在玩家脚下或直接送入物品栏。\r\n\r\n" +
                   "9. “添加事件”页可以搜索并直接触发原版 Twitch Integration 事件；鼠标悬停在事件按钮上可查看中文效果说明。\r\n\r\n" +
                   "10. “添加药水”页可把固体、液体、气体或火花按容量百分比装入空烧瓶或空袋；烧瓶容量为 1000，袋子为 1500。配方合计超过数量上限时不会生成，容器内仍保留原版炼金反应。\r\n\r\n" +
                   "11. “魔杖管理”页会读取当前魔杖栏中最多四根魔杖；选择表格中的魔杖后，可以修改属性或从法术按钮区添加始终释放法术。\r\n\r\n" +
                   "12. “角色管理”页会读取金钱、血量、肺活量、浮空能量及恢复速度、移动/跳跃速度和重力等 13 项属性；编辑后点击“修改”即可一次生效。血量显示为游戏实际数值。\r\n\r\n" +
                   "13. 勾选“允许游戏后台运行”会关闭 Noita 的失焦暂停。若游戏已经运行，更改会在退出后再次写入，并从下次启动开始生效。\r\n\r\n" +
                   "本程序不注入 DLL、不扫描内存、不联网。EXE 只向配套模组目录写入命令文本；实际修改由 Noita 自带的 Lua 实体 API 完成。"
        };
        page.Controls.Add(text);
        return page;
    }

    private void PopulateCatalogs()
    {
        wandCombo.DataSource = catalog.Wands;
    }

    private void BrowseForGame()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "请选择包含 noita.exe 和 mods 文件夹的 Noita 游戏目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(gamePathText.Text) ? gamePathText.Text : ""
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            gamePathText.Text = dialog.SelectedPath;
            ApplyGamePath();
            RefreshNextSeedStatus();
        }
    }

    private void ApplyGamePath()
    {
        bridge.GameRoot = gamePathText.Text.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!GameLocator.IsNoitaRoot(bridge.GameRoot))
            SetConnection("● 游戏目录无效", Color.Firebrick);
    }

    private void SetNextWorldSeed()
    {
        ApplyGamePath();
        if (!uint.TryParse(nextSeedText.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed) || seed == 0)
        {
            MessageBox.Show(this, "请输入 1 到 4294967295 之间的整数种子。", "种子无效",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            bridge.WriteNextSeed(seed);
            nextSeedStatus.Text = $"已预设：{seed}（下一次普通新游戏一次性生效）";
            AppendLog($"✓ 已预设下一局种子 {seed}");
        }
        catch (Exception ex)
        {
            ShowError("预设种子失败", ex);
        }
    }

    private void ClearNextWorldSeed()
    {
        ApplyGamePath();
        try
        {
            bridge.ClearNextSeed();
            nextSeedStatus.Text = "尚未预设下一局种子。";
            AppendLog("✓ 已取消下一局种子预设");
        }
        catch (Exception ex)
        {
            ShowError("取消种子预设失败", ex);
        }
    }

    private void RefreshNextSeedStatus()
    {
        try
        {
            var seed = bridge.ReadNextSeed();
            nextSeedStatus.Text = seed.HasValue
                ? $"已预设：{seed.Value}（下一次普通新游戏一次性生效）"
                : "尚未预设下一局种子。";
        }
        catch
        {
            nextSeedStatus.Text = "无法读取种子预设状态。";
        }
    }

    private void InstallBridge()
    {
        try
        {
            ApplyGamePath();
            bridge.InstallOrRepair();
            AppendLog($"配套模组已安装：{bridge.ModRoot}");
            MessageBox.Show(this,
                "配套模组安装完成。\n\n请在 Noita 的 Mods 菜单中允许 Unsafe mods，然后启用“Codex Noita Trainer Bridge”。如果游戏已经启动，请重新启动或重新载入本局。",
                "安装完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError("安装失败", ex);
        }
    }

    private void LaunchGame()
    {
        try
        {
            NoitaConfigService.WriteAllowBackgroundRunning(desiredAllowBackgroundRunning);
            backgroundSettingPending = false;
        }
        catch (Exception ex)
        {
            AppendLog($"✗ 启动前应用后台运行设置失败：{ex.Message}");
        }

        try { bridge.LaunchNoita(); }
        catch (Exception ex) { ShowError("无法启动 Noita", ex); }
    }

    private void SendHp()
    {
        if (!TryNumber(hpText, "当前生命", 0.04m, 1_000_000_000m, out var hp)) return;
        SendCommand("SET_HP", $"设置当前生命为 {hp}", hp);
    }

    private void SendMaxHp()
    {
        if (!TryNumber(maxHpText, "最大生命", 1m, 1_000_000_000m, out var hp)) return;
        SendCommand("SET_MAX_HP", $"设置最大生命为 {hp}", hp, healWithMaxCheck.Checked);
    }

    private void ExecuteSpell(string id)
    {
        var count = (int)spellCount.Value;
        var giveDirectly = spellGiveMode.Checked;
        SendCommand(giveDirectly ? "GIVE_SPELL" : "SPAWN_SPELL",
            giveDirectly ? $"直接获得法术 {id} x{count}" : $"生成法术 {id} x{count}", id, count);
    }

    private void ExecuteItem(string id)
    {
        var count = (int)itemCount.Value;
        var giveDirectly = itemGiveMode.Checked;
        SendCommand(giveDirectly ? "GIVE_ITEM" : "SPAWN_ITEM",
            giveDirectly ? $"直接获得物品 {id} x{count}" : $"生成物品 {id} x{count}", id, count);
    }

    private void SpawnPresetWand()
    {
        if (wandCombo.SelectedItem is not CatalogItem wand)
        {
            MessageBox.Show(this, "请选择一个法杖预设。", "缺少法杖", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SendCommand("SPAWN_WAND", $"生成法杖 {wand.Id} x{wandCount.Value}", wand.Id, (int)wandCount.Value);
    }

    private void SpawnCustomWand(CheckBox shuffle, params TextBox[] fields)
    {
        var labels = new[] { "容量", "每次施放数", "施放延迟", "充能时间", "最大法力", "法力回复", "散射角度" };
        var minimums = new[] { 1m, 1m, -10m, -10m, 0m, 0m, -360m };
        var maximums = new[] { 100m, 100m, 1000m, 1000m, 1_000_000_000m, 1_000_000_000m, 360m };
        var values = new decimal[fields.Length];
        for (var i = 0; i < fields.Length; i++)
        {
            if (!TryNumber(fields[i], labels[i], minimums[i], maximums[i], out values[i])) return;
        }

        SendCommand("SPAWN_CUSTOM_WAND", "生成自定义法杖",
            shuffle.Checked, values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
    }

    private void ExecutePerk(string id)
    {
        var count = (int)perkCount.Value;
        var giveDirectly = perkGiveMode.Checked;
        SendCommand(giveDirectly ? "GIVE_PERK" : "SPAWN_PERK",
            giveDirectly ? $"直接获得天赋 {id} x{count}" : $"生成天赋 {id} x{count}", id, count);
    }

    private void ExecuteEvent(CatalogItem item)
    {
        SendCommand("RUN_EVENT", $"触发事件 {item.Zh} [{item.Id}]", item.Id);
    }

    private void ExecuteStatus(CatalogItem item)
    {
        if (item.Satiation is int satiation)
        {
            if (satiation < 0)
            {
                var result = MessageBox.Show(this,
                    "确定要模拟“又撑又胀”吗？\n\n将立即对当前角色造成其当前生命值两倍的伤害，通常情况下会致命。",
                    "确认又撑又胀",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return;
            }
            SendCommand("SET_SATIATION",
                satiation >= 0 ? $"设置饱食度：{item.Zh}" : $"触发 {item.Zh}", satiation);
            return;
        }
        if (item.StainMaterial is string stainMaterial)
        {
            var percent = Math.Clamp((int)statusDuration.Value, 1, 100);
            SendCommand("APPLY_STAIN", $"施加沾染 {item.Zh}（约 {percent}%）", stainMaterial, percent, item.Zh);
            return;
        }
        if (item.IngestMaterial is string ingestMaterial)
        {
            var ingestSeconds = statusPermanentMode.Checked ? 3600 : (int)statusDuration.Value;
            var cells = Math.Max(1, (int)Math.Round(ingestSeconds / (item.IngestSecondsPerCell ?? 0.5)));
            var ingestNote = statusPermanentMode.Checked ? "（永久按 3600 秒计）" : "";
            SendCommand("INGEST_STATUS", $"施加状态 {item.Zh}（约 {ingestSeconds} 秒{ingestNote}）",
                ingestMaterial, cells, ingestSeconds, item.Zh);
            return;
        }
        var permanent = statusPermanentMode.Checked;
        var seconds = (int)statusDuration.Value;
        SendCommand("GIVE_EFFECT",
            permanent ? $"施加状态 {item.Zh}（永久）" : $"施加状态 {item.Zh}（{seconds} 秒）",
            item.Path ?? item.Id, permanent ? 1 : 0, seconds,
            item.UiIcon ?? "", item.Zh, item.Description);
    }

    private void ConfirmClearEffects()
    {
        var result = MessageBox.Show(this,
            "确定要清除当前角色身上的全部状态效果吗？\n\n将移除所有通过本工具或游戏内途径获得的状态（含永久状态），清除沾染与摄取状态并重置幻觉类视觉扭曲。天赋不受影响。",
            "确认清除全部状态",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result == DialogResult.Yes)
            SendCommand("CLEAR_EFFECTS", "清除全部状态");
    }

    private void ReadCurrentWands()
    {
        SendWandSnapshotCommand("READ_WANDS", "读取当前魔杖栏");
    }

    private void ReadCurrentCharacter()
    {
        SendCharacterSnapshotCommand("READ_CHARACTER", "读取当前角色信息");
    }

    private void UpdateSelectedWand()
    {
        var wand = SelectedManagedWand();
        if (wand is null)
        {
            MessageBox.Show(this, "请先读取魔杖栏并在表格中选择一根魔杖。", "未选择魔杖",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryNumber(managedWandActions, "单次施放数", 1, 1000, out var actions) ||
            !TryNumber(managedWandCastDelay, "施放延迟", -1000, 1000, out var castDelay) ||
            !TryNumber(managedWandRecharge, "充能时间", -1000, 1000, out var recharge) ||
            !TryNumber(managedWandManaMax, "法力上限", 0, 1_000_000_000, out var manaMax) ||
            !TryNumber(managedWandManaCharge, "法力恢复速度", 0, 1_000_000_000, out var manaCharge) ||
            !TryNumber(managedWandCapacity, "容量", 0, 1000, out var capacity) ||
            !TryNumber(managedWandSpread, "散射", -3600, 3600, out var spread) ||
            !TryNumber(managedWandSpeed, "速度加成", 0, 1000, out var speed))
            return;
        if (decimal.Truncate(actions) != actions || decimal.Truncate(capacity) != capacity)
        {
            MessageBox.Show(this, "单次施放数和容量必须是整数。", "数值无效",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SendWandSnapshotCommand("UPDATE_WAND", $"修改魔杖栏第 {wand.Slot + 1} 根魔杖",
            wand.EntityId, managedWandShuffle.Checked, (int)actions, castDelay, recharge,
            manaMax, manaCharge, (int)capacity, spread, speed);
    }

    private void AddAlwaysCastToSelectedWand(CatalogItem item)
    {
        var wand = SelectedManagedWand();
        if (wand is null)
        {
            MessageBox.Show(this, "请先读取魔杖栏并在表格中选择一根魔杖。", "未选择魔杖",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SendWandSnapshotCommand("ADD_WAND_ALWAYS_CAST",
            $"为魔杖栏第 {wand.Slot + 1} 根魔杖添加始终释放：{item.Zh} [{item.Id}]",
            wand.EntityId, item.Id);
    }

    private void SendWandSnapshotCommand(string command, string description, params object[] arguments)
    {
        try
        {
            ApplyGamePath();
            var id = bridge.Send(command, arguments);
            pendingCommandId = id;
            pendingWandSnapshotId = id;
            AppendLog($"→ {description}");
        }
        catch (Exception ex)
        {
            ShowError("魔杖命令发送失败", ex);
        }
    }

    private void LoadManagedWands(string requestId)
    {
        try
        {
            var wands = bridge.ReadWandSnapshot(requestId);
            managedWandGrid.Rows.Clear();
            foreach (var wand in wands.OrderBy(item => item.Slot))
            {
                var rowIndex = managedWandGrid.Rows.Add(
                    $"第 {wand.Slot + 1} 格",
                    wand.Shuffle ? "是" : "否",
                    wand.ActionsPerRound,
                    $"{wand.CastDelaySeconds.ToString("0.00", CultureInfo.CurrentCulture)} 秒",
                    $"{wand.RechargeTimeSeconds.ToString("0.00", CultureInfo.CurrentCulture)} 秒",
                    wand.ManaMax.ToString("0.##", CultureInfo.CurrentCulture),
                    wand.ManaChargeSpeed.ToString("0.##", CultureInfo.CurrentCulture),
                    wand.Capacity,
                    wand.SpreadDegrees.ToString("0.##", CultureInfo.CurrentCulture),
                    wand.SpeedMultiplier.ToString("0.###", CultureInfo.CurrentCulture));
                managedWandGrid.Rows[rowIndex].Tag = wand;
            }

            wandManagementContent.Visible = true;
            wandManagementStatus.Text = wands.Count == 0
                ? "当前魔杖栏没有魔杖。"
                : $"已读取 {wands.Count} 根魔杖；请选择一行进行修改或添加始终释放法术。";
            if (managedWandGrid.Rows.Count > 0)
            {
                managedWandGrid.CurrentCell = managedWandGrid.Rows[0].Cells[0];
                managedWandGrid.Rows[0].Selected = true;
                PopulateSelectedWandFields();
            }
        }
        catch (Exception ex)
        {
            ShowError("读取魔杖栏结果失败", ex);
        }
    }

    private void UpdateCurrentCharacter()
    {
        const decimal maximumExactInteger = 9_007_199_254_740_991m;
        if (!TryNumber(characterMoney, "金钱", 0, maximumExactInteger, out var money) ||
            !TryNumber(characterHp, "当前血量", 0.04m, 1_000_000_000m, out var hp) ||
            !TryNumber(characterMaxHp, "最大血量", 0.04m, 1_000_000_000m, out var maxHp) ||
            !TryNumber(characterLungCapacity, "肺活量", 0, 1_000_000_000m, out var lungCapacity) ||
            !TryNumber(characterFlyTimeMax, "最大浮空能量", 0, 1_000_000_000m, out var flyTimeMax) ||
            !TryNumber(characterFlyTimeLeft, "当前浮空能量", 0, 1_000_000_000m, out var flyTimeLeft) ||
            !TryNumber(characterFlyRecharge, "空中浮空恢复速度", 0, 1_000_000_000m, out var flyRecharge) ||
            !TryNumber(characterFlyRechargeGround, "地面浮空恢复速度", 0, 1_000_000_000m, out var flyRechargeGround) ||
            !TryNumber(characterCurrentAir, "当前肺活量", 0, 1_000_000_000m, out var currentAir) ||
            !TryNumber(characterRunVelocity, "奔跑速度", 0, 1_000_000_000m, out var runVelocity) ||
            !TryNumber(characterFlyVelocityX, "水平飞行速度", 0, 1_000_000_000m, out var flyVelocityX) ||
            !TryNumber(characterJumpVelocityY, "跳跃速度", -1_000_000_000m, 1_000_000_000m, out var jumpVelocityY) ||
            !TryNumber(characterPixelGravity, "重力", -1_000_000_000m, 1_000_000_000m, out var pixelGravity))
            return;
        if (decimal.Truncate(money) != money)
        {
            MessageBox.Show(this, "金钱必须是整数。", "数值无效",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (hp > maxHp || flyTimeLeft > flyTimeMax || currentAir > lungCapacity)
        {
            MessageBox.Show(this,
                "当前血量不能大于最大血量；当前浮空能量不能大于最大浮空能量；当前肺活量不能大于肺活量上限。",
                "数值无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SendCharacterSnapshotCommand("UPDATE_CHARACTER", "修改当前角色属性",
            money, hp, maxHp, lungCapacity, flyTimeMax, flyTimeLeft, flyRecharge,
            flyRechargeGround, currentAir, runVelocity, flyVelocityX, jumpVelocityY, pixelGravity);
    }

    private void SendCharacterSnapshotCommand(string command, string description, params object[] arguments)
    {
        try
        {
            ApplyGamePath();
            var id = bridge.Send(command, arguments);
            pendingCommandId = id;
            pendingCharacterSnapshotId = id;
            AppendLog($"→ {description}");
        }
        catch (Exception ex)
        {
            ShowError("角色命令发送失败", ex);
        }
    }

    private void LoadManagedCharacter(string requestId)
    {
        try
        {
            var character = bridge.ReadCharacterSnapshot(requestId);
            characterMoney.Text = character.Money.ToString("0", CultureInfo.InvariantCulture);
            characterHp.Text = character.Hp.ToString("0.########", CultureInfo.InvariantCulture);
            characterMaxHp.Text = character.MaxHp.ToString("0.########", CultureInfo.InvariantCulture);
            characterLungCapacity.Text = character.LungCapacitySeconds.ToString("0.########", CultureInfo.InvariantCulture);
            characterFlyTimeMax.Text = character.FlyTimeMaxSeconds.ToString("0.########", CultureInfo.InvariantCulture);
            characterFlyTimeLeft.Text = character.FlyTimeLeftSeconds.ToString("0.########", CultureInfo.InvariantCulture);
            characterFlyRecharge.Text = character.FlyRechargeSpeed.ToString("0.########", CultureInfo.InvariantCulture);
            characterFlyRechargeGround.Text = character.FlyRechargeGroundSpeed.ToString("0.########", CultureInfo.InvariantCulture);
            characterCurrentAir.Text = character.CurrentAirSeconds.ToString("0.########", CultureInfo.InvariantCulture);
            characterRunVelocity.Text = character.RunVelocity.ToString("0.########", CultureInfo.InvariantCulture);
            characterFlyVelocityX.Text = character.FlyVelocityX.ToString("0.########", CultureInfo.InvariantCulture);
            characterJumpVelocityY.Text = character.JumpVelocityY.ToString("0.########", CultureInfo.InvariantCulture);
            characterPixelGravity.Text = character.PixelGravity.ToString("0.########", CultureInfo.InvariantCulture);
            characterManagementStatus.Text = "已读取当前角色信息；编辑下方数值后点击“修改”即可生效。";
            characterManagementContent.Visible = true;
        }
        catch (Exception ex)
        {
            ShowError("读取角色信息失败", ex);
        }
    }

    private WandSnapshot? SelectedManagedWand() =>
        managedWandGrid.SelectedRows.Count > 0
            ? managedWandGrid.SelectedRows[0].Tag as WandSnapshot
            : managedWandGrid.CurrentRow?.Tag as WandSnapshot;

    private void PopulateSelectedWandFields()
    {
        var wand = SelectedManagedWand();
        if (wand is null)
            return;
        managedWandShuffle.Checked = wand.Shuffle;
        managedWandActions.Text = wand.ActionsPerRound.ToString(CultureInfo.InvariantCulture);
        managedWandCastDelay.Text = wand.CastDelaySeconds.ToString("0.00", CultureInfo.InvariantCulture);
        managedWandRecharge.Text = wand.RechargeTimeSeconds.ToString("0.00", CultureInfo.InvariantCulture);
        managedWandManaMax.Text = wand.ManaMax.ToString("0.########", CultureInfo.InvariantCulture);
        managedWandManaCharge.Text = wand.ManaChargeSpeed.ToString("0.########", CultureInfo.InvariantCulture);
        managedWandCapacity.Text = wand.Capacity.ToString(CultureInfo.InvariantCulture);
        managedWandSpread.Text = wand.SpreadDegrees.ToString("0.########", CultureInfo.InvariantCulture);
        managedWandSpeed.Text = wand.SpeedMultiplier.ToString("0.########", CultureInfo.InvariantCulture);
    }

    private void SendCommand(string command, string description, params object[] arguments)
    {
        try
        {
            ApplyGamePath();
            pendingCommandId = bridge.Send(command, arguments);
            AppendLog($"→ {description}");
        }
        catch (Exception ex)
        {
            ShowError("命令发送失败", ex);
        }
    }

    private void SendOverlayOption(string command, string label, bool enabled)
    {
        if (syncingDisplayOptions)
            return;
        SendCommand(command, $"{(enabled ? "开启" : "关闭")}{label}", enabled);
    }

    private void LoadQuickActionSettings()
    {
        try
        {
            desiredCollectGold = TrainerSettingsService.ReadCollectGold();
            desiredGhostVision = TrainerSettingsService.ReadGhostVision();
            syncingQuickActionSettings = true;
            collectGoldCheckBox.Checked = desiredCollectGold;
            ghostVisionCheckBox.Checked = desiredGhostVision;
        }
        catch (Exception ex)
        {
            AppendLog($"✗ 无法读取一键操作设置：{ex.Message}");
        }
        finally
        {
            syncingQuickActionSettings = false;
        }
    }

    private void ChangeCollectGoldSetting()
    {
        if (syncingQuickActionSettings)
            return;

        desiredCollectGold = collectGoldCheckBox.Checked;
        try
        {
            TrainerSettingsService.WriteCollectGold(desiredCollectGold);
        }
        catch (Exception ex)
        {
            AppendLog($"✗ 无法保存收集金块设置：{ex.Message}");
        }

        SendCollectGoldSetting();
    }

    private void SendCollectGoldSetting()
    {
        lastCollectGoldSyncAttemptUtc = DateTime.UtcNow;
        SendCommand("SET_COLLECT_GOLD",
            desiredCollectGold ? "开启持续收集金块" : "关闭持续收集金块",
            desiredCollectGold);
    }

    private void ChangeGhostVisionSetting()
    {
        if (syncingQuickActionSettings)
            return;

        desiredGhostVision = ghostVisionCheckBox.Checked;
        try
        {
            TrainerSettingsService.WriteGhostVision(desiredGhostVision);
        }
        catch (Exception ex)
        {
            AppendLog($"✗ 无法保存幽灵透视设置：{ex.Message}");
        }

        SendGhostVisionSetting();
    }

    private void SendGhostVisionSetting()
    {
        lastGhostVisionSyncAttemptUtc = DateTime.UtcNow;
        SendCommand("SET_GHOST_VISION",
            desiredGhostVision ? "开启幽灵透视" : "关闭幽灵透视",
            desiredGhostVision);
    }

    private void ConfirmPermanentSheep()
    {
        var result = MessageBox.Show(this,
            "确定要让当前角色永久变羊吗？\n\n变羊后不会随时间自动恢复，也无法正常使用法杖。本工具的无敌防护将关闭，和平效果保留。此按钮不提供还原功能。",
            "确认永久变羊",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result == DialogResult.Yes)
            SendCommand("PERMANENT_SHEEP", "角色永久变羊");
    }

    private void ConfirmSuicide()
    {
        var result = MessageBox.Show(this,
            "确定要让当前玩家死亡并结束本局吗？\n\n该操作会触发正常的死亡与游戏结束流程。",
            "确认自杀",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (result == DialogResult.Yes)
            SendCommand("SUICIDE", "自杀");
    }

    private void TeleportCustom()
    {
        if (!TryNumber(teleportXText, "X 坐标", -1_000_000_000m, 1_000_000_000m, out var x)) return;
        if (!TryNumber(teleportYText, "Y 坐标", -1_000_000_000m, 1_000_000_000m, out var y)) return;
        TeleportTo("自定义坐标", x, y, false);
    }

    private void AddTeleportLocation(TableLayoutPanel layout, int index, string name, decimal x, decimal y)
    {
        var button = new Button
        {
            Text = name,
            Dock = DockStyle.Fill,
            Height = 42,
            MinimumSize = new Size(150, 42),
            Margin = new Padding(2),
            Padding = new Padding(0, 2, 0, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        button.Click += (_, _) => TeleportTo(name, x, y, true);
        layout.Controls.Add(button, index % 5, index / 5);
    }

    private Control BuildTeleportGroup(string title, (string Name, decimal X, decimal Y)[] entries)
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 4)
        };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Margin = new Padding(4, 10, 4, 6)
        };
        host.Controls.Add(header, 0, 0);

        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 5 };
        for (var column = 0; column < 5; column++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        grid.RowCount = Math.Max(1, (entries.Length + 4) / 5);
        for (var index = 0; index < entries.Length; index++)
            AddTeleportLocation(grid, index, entries[index].Name, entries[index].X, entries[index].Y);
        host.Controls.Add(grid, 0, 1);
        return host;
    }

    private void TeleportTo(string name, decimal x, decimal y, bool copyToInputs)
    {
        if (copyToInputs)
        {
            teleportXText.Text = x.ToString(CultureInfo.InvariantCulture);
            teleportYText.Text = y.ToString(CultureInfo.InvariantCulture);
        }
        SendCommand("TELEPORT_PLAYER", $"传送到{name} ({x}, {y})", x, y);
    }

    private void LoadBackgroundRunningSetting()
    {
        try
        {
            desiredAllowBackgroundRunning = NoitaConfigService.ReadAllowBackgroundRunning() ?? false;
            syncingBackgroundSetting = true;
            allowBackgroundRunningCheckBox.Checked = desiredAllowBackgroundRunning;
        }
        catch (Exception ex)
        {
            AppendLog($"✗ 无法读取后台运行设置：{ex.Message}");
        }
        finally
        {
            syncingBackgroundSetting = false;
        }
    }

    private void ChangeBackgroundRunningSetting()
    {
        if (syncingBackgroundSetting)
            return;

        var previous = desiredAllowBackgroundRunning;
        desiredAllowBackgroundRunning = allowBackgroundRunningCheckBox.Checked;
        try
        {
            NoitaConfigService.WriteAllowBackgroundRunning(desiredAllowBackgroundRunning);
            backgroundSettingPending = IsNoitaRunning();
            AppendLog(backgroundSettingPending
                ? $"✓ 已保存后台运行设置：{(desiredAllowBackgroundRunning ? "允许" : "禁止")}；当前 Noita 退出后会再次应用，下次启动生效。"
                : $"✓ 已设置：{(desiredAllowBackgroundRunning ? "允许" : "禁止")}游戏后台运行。下次启动 Noita 时生效。");
        }
        catch (Exception ex)
        {
            desiredAllowBackgroundRunning = previous;
            syncingBackgroundSetting = true;
            allowBackgroundRunningCheckBox.Checked = previous;
            syncingBackgroundSetting = false;
            ShowError("后台运行设置失败", ex);
        }
    }

    private void ApplyPendingBackgroundSetting()
    {
        if (!backgroundSettingPending || IsNoitaRunning())
            return;
        try
        {
            NoitaConfigService.WriteAllowBackgroundRunning(desiredAllowBackgroundRunning);
            backgroundSettingPending = false;
            AppendLog("✓ Noita 已退出，后台运行设置已重新写入并将在下次启动时生效。");
        }
        catch (Exception ex)
        {
            backgroundSettingPending = false;
            AppendLog($"✗ 重新应用后台运行设置失败：{ex.Message}");
        }
    }

    private static bool IsNoitaRunning()
    {
        var processes = Process.GetProcessesByName("noita");
        try { return processes.Any(process => !process.HasExited); }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    private void PollStatus()
    {
        ApplyPendingBackgroundSetting();
        if (string.IsNullOrWhiteSpace(bridge.GameRoot))
            return;

        var status = bridge.ReadStatus();
        if (status is null || !status.IsFresh)
        {
            var installed = bridge.IsInstalled;
            SetConnection(installed ? "● 未连接" : "● 配套模组未安装", Color.Firebrick);
            liveStatsLabel.Text = installed
                ? "请启用配套模组并进入一局游戏；若刚安装，请重新启动 Noita。"
                : "先点击“安装/修复配套模组”。";
            ResetInformationValues();
            return;
        }

        if (status.Protocol < 21)
        {
            SetConnection("● 配套模组需更新", Color.Firebrick);
            liveStatsLabel.Text = "请点击“安装/修复配套模组”，然后重新启动或重新载入 Noita。";
            ResetInformationValues();
            return;
        }

        if (status.CollectGold == desiredCollectGold)
        {
            lastCollectGoldSyncAttemptUtc = DateTime.MinValue;
        }
        else if (DateTime.UtcNow - lastCollectGoldSyncAttemptUtc > TimeSpan.FromSeconds(1))
        {
            SendCollectGoldSetting();
        }

        if (status.GhostVision == desiredGhostVision)
        {
            lastGhostVisionSyncAttemptUtc = DateTime.MinValue;
        }
        else if (DateTime.UtcNow - lastGhostVisionSyncAttemptUtc > TimeSpan.FromSeconds(1))
        {
            SendGhostVisionSetting();
        }

        if (!status.PlayerPresent)
        {
            SetConnection("● 桥接已加载", Color.DarkOrange);
            liveStatsLabel.Text = "桥接已加载，但当前没有检测到玩家。请进入或继续一局游戏。";
            ResetInformationValues();
        }
        else
        {
            SetConnection("● 已连接", Color.ForestGreen);
            liveStatsLabel.Text = "信息显示已连接，正在实时读取本局坐标与统计数据。";
            playerPositionValue.Text = Coordinate(status.X, status.Y);
            teleportPositionValue.Text = playerPositionValue.Text;
            cameraPositionValue.Text = Coordinate(status.CameraX, status.CameraY);
            worldSeedValue.Text = FormatWhole(status.WorldSeed);
            gameTimeValue.Text = FormatDuration(status.Playtime);
            enemiesKilledValue.Text = FormatWhole(status.EnemiesKilled);
            damageTakenValue.Text = Format(status.DamageTaken);
            healedValue.Text = Format(status.HealedCustom);
            projectilesShotValue.Text = FormatWhole(status.ProjectilesShot);
            goldAllValue.Text = FormatWhole(status.GoldAll);
            placesVisitedValue.Text = FormatWhole(status.PlacesVisited);
            itemsValue.Text = FormatWhole(status.Items);
            wandsPickedValue.Text = FormatWhole(status.WandsPicked);
            heartContainersValue.Text = FormatWhole(status.HeartContainers);
            kicksValue.Text = FormatWhole(status.Kicks);
        }

        syncingDisplayOptions = true;
        try
        {
            showEnemyHpCheckBox.Checked = status.ShowEnemyHp;
            showPlayerCoordinatesCheckBox.Checked = status.ShowPlayerCoordinates;
        }
        finally
        {
            syncingDisplayOptions = false;
        }

        if (!string.IsNullOrWhiteSpace(status.LastId) &&
            (status.LastId != lastReportedCommandId ||
             lastReportedCommandResult != $"{status.LastOk}:{status.LastMessage}") &&
            status.LastId == pendingCommandId)
        {
            lastReportedCommandId = status.LastId;
            lastReportedCommandResult = $"{status.LastOk}:{status.LastMessage}";
            AppendLog(status.LastOk ? $"✓ {status.LastMessage}" : $"✗ {status.LastMessage}");
            if (status.LastId == pendingWandSnapshotId)
            {
                if (status.LastOk)
                    LoadManagedWands(status.LastId);
                pendingWandSnapshotId = "";
            }
            if (status.LastId == pendingCharacterSnapshotId)
            {
                if (status.LastOk)
                    LoadManagedCharacter(status.LastId);
                pendingCharacterSnapshotId = "";
            }
        }
    }

    private static bool TryCatalogId(ComboBox combo, IReadOnlyCollection<CatalogItem> items, string label, out string id)
    {
        if (combo.SelectedItem is CatalogItem selected)
        {
            id = selected.Id;
            return true;
        }

        var text = combo.Text.Trim();
        var bracket = IdAtEndRegex().Match(text);
        if (bracket.Success)
            text = bracket.Groups[1].Value;
        text = text.ToUpperInvariant();

        var exact = items.FirstOrDefault(item =>
            item.Id.Equals(text, StringComparison.OrdinalIgnoreCase) ||
            item.Zh.Equals(combo.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) ||
            item.En.Equals(combo.Text.Trim(), StringComparison.CurrentCultureIgnoreCase));
        id = exact?.Id ?? text;

        if (string.IsNullOrWhiteSpace(id) || !ValidIdRegex().IsMatch(id))
        {
            MessageBox.Show($"请输入有效的{label}名称或内部 ID。", $"{label}无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    private static bool TryNumber(TextBox input, string label, decimal minimum, decimal maximum, out decimal value)
    {
        var text = input.Text.Trim();
        var ok = decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                 decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        if (!ok || value < minimum || value > maximum)
        {
            MessageBox.Show($"{label}必须是 {minimum} 到 {maximum} 之间的数字。", "数值无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            input.Focus();
            input.SelectAll();
            return false;
        }
        return true;
    }

    private void SetConnection(string text, Color color)
    {
        connectionLabel.Text = text;
        connectionLabel.ForeColor = color;
    }

    private void AppendLog(string message)
    {
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
    }

    private void ShowError(string title, Exception exception)
    {
        AppendLog($"✗ {title}: {exception.Message}");
        MessageBox.Show(this, exception.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static string Format(double? value) => value?.ToString("0.##", CultureInfo.CurrentCulture) ?? "—";

    private static string FormatWhole(double? value) => value?.ToString("0", CultureInfo.CurrentCulture) ?? "—";

    private static string Coordinate(double? x, double? y) =>
        x is null || y is null ? "—" : $"({Format(x)}, {Format(y)})";

    private static string FormatDuration(double? seconds)
    {
        if (seconds is null || seconds < 0)
            return "—";
        var duration = TimeSpan.FromSeconds(seconds.Value);
        return duration.TotalDays >= 1
            ? $"{(int)duration.TotalDays} 天 {duration:hh\\:mm\\:ss}"
            : duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private void ResetInformationValues()
    {
        playerPositionValue.Text = "—";
        teleportPositionValue.Text = "—";
        cameraPositionValue.Text = "—";
        worldSeedValue.Text = "—";
        gameTimeValue.Text = "—";
        enemiesKilledValue.Text = "—";
        damageTakenValue.Text = "—";
        healedValue.Text = "—";
        projectilesShotValue.Text = "—";
        goldAllValue.Text = "—";
        placesVisitedValue.Text = "—";
        itemsValue.Text = "—";
        wandsPickedValue.Text = "—";
        heartContainersValue.Text = "—";
        kicksValue.Text = "—";
    }

    private static TextBox InformationTextBox() => new()
    {
        Text = "—",
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(35, 75, 120),
        Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = new Padding(3, 7, 3, 7),
        TabStop = false
    };

    private void ShowFromOtherInstance()
    {
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Show();
        Activate();
        TopMost = true;
        TopMost = false;
        User32.SetForegroundWindow(Handle);
    }

    private sealed class InstanceMessageWindow : NativeWindow
    {
        private readonly int messageId;
        private readonly Action showAction;

        public InstanceMessageWindow(int messageId, Action showAction)
        {
            this.messageId = messageId;
            this.showAction = showAction;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == messageId)
                showAction();
            base.WndProc(ref m);
        }
    }

    private static void AddInformationRow(TableLayoutPanel layout, int row, string name, TextBox value)
    {
        layout.Controls.Add(new Label
        {
            Text = name,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 9, 18, 9)
        }, 0, row);
        layout.Controls.Add(value, 1, row);
    }

    private static void AddInformationPair(TableLayoutPanel layout, int row,
        string firstName, TextBox firstValue, string secondName, TextBox secondValue)
    {
        layout.Controls.Add(new Label
        {
            Text = firstName,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 9, 18, 9)
        }, 0, row);
        layout.Controls.Add(firstValue, 1, row);
        layout.Controls.Add(new Label
        {
            Text = secondName,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 9, 18, 9)
        }, 2, row);
        layout.Controls.Add(secondValue, 3, row);
    }

    private static TabPage Page(string text) => new(text) { BackColor = Color.White, Padding = new Padding(8) };

    private static TableLayoutPanel FormLayout()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(18), AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return layout;
    }

    private Control BuildCatalogButtonPage(
        IReadOnlyList<CatalogItem> items,
        string itemType,
        RadioButton? spawnMode,
        RadioButton? giveMode,
        NumericUpDown? countInput,
        Action<CatalogItem> execute,
        bool showIcons = true,
        string optionsLabel = "操作方式",
        string countLabel = "数量")
    {
        var showOptions = spawnMode is not null && giveMode is not null && countInput is not null;
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = showOptions ? 4 : 3,
            Padding = new Padding(12)
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        if (showOptions)
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var searchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Margin = new Padding(0, 0, 0, 7),
            BackColor = Color.White
        };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchLayout.Controls.Add(Label("搜索"), 0, 0);
        var searchText = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = $"输入{itemType}的中文名、英文名或内部 ID…",
            Margin = new Padding(3, 4, 8, 4)
        };
        searchLayout.Controls.Add(searchText, 1, 0);
        var resultCount = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.DimGray,
            Margin = new Padding(4, 7, 8, 7)
        };
        searchLayout.Controls.Add(resultCount, 2, 0);
        var clearSearch = Button("清空", (_, _) => searchText.Clear());
        clearSearch.MinimumSize = new Size(68, 30);
        searchLayout.Controls.Add(clearSearch, 3, 0);
        outer.Controls.Add(searchLayout, 0, 0);

        outer.Controls.Add(new Label
        {
            Text = $"把鼠标悬停在{itemType}按钮上可以查看相应的详细描述。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(4, 2, 4, 8)
        }, 0, 1);

        if (showOptions)
        {
            var options = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(4),
                Margin = new Padding(0, 0, 0, 10)
            };
            options.Controls.Add(Label(optionsLabel));
            spawnMode!.Margin = new Padding(4, 8, 16, 7);
            giveMode!.Margin = new Padding(4, 8, 24, 7);
            spawnMode.BackColor = Color.White;
            giveMode.BackColor = Color.White;
            options.Controls.Add(spawnMode);
            options.Controls.Add(giveMode);
            options.Controls.Add(Label(countLabel));
            countInput!.Margin = new Padding(3, 5, 3, 3);
            options.Controls.Add(countInput);
            outer.Controls.Add(options, 0, 2);
        }

        var scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            Margin = new Padding(0)
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = CatalogGridColumns,
            RowCount = 1,
            Padding = new Padding(0, 0, 4, 4),
            Margin = new Padding(0)
        };
        for (var column = 0; column < CatalogGridColumns; column++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / CatalogGridColumns));

        var catalogButtons = new List<(CatalogItem Item, CatalogButton Button)>(items.Count);
        foreach (var item in items)
        {
            var image = showIcons && item.IconIndex >= 0 && item.IconIndex < catalogButtonImages.Count
                ? catalogButtonImages[item.IconIndex]
                : null;
            var button = new CatalogButton(image)
            {
                Text = item.DisplayName,
                Font = catalogButtonFont,
                Dock = DockStyle.Fill,
                Height = 62,
                MinimumSize = new Size(250, 62),
                Margin = new Padding(2),
                BackColor = Color.White
            };
            button.Click += (_, _) => execute(item);
            quickActionToolTip.SetToolTip(button,
                string.IsNullOrWhiteSpace(item.Description) ? "暂无描述" : item.Description);
            catalogButtons.Add((item, button));
        }

        void ApplySearchFilter()
        {
            var query = searchText.Text.Trim();
            var matches = string.IsNullOrEmpty(query)
                ? catalogButtons
                : catalogButtons.Where(entry =>
                    entry.Item.Zh.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    entry.Item.En.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    entry.Item.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            grid.SuspendLayout();
            grid.Controls.Clear();
            grid.RowCount = Math.Max(1, (matches.Count + CatalogGridColumns - 1) / CatalogGridColumns);
            for (var index = 0; index < matches.Count; index++)
                grid.Controls.Add(matches[index].Button, index % CatalogGridColumns, index / CatalogGridColumns);
            grid.ResumeLayout(true);
            resultCount.Text = $"{matches.Count} / {items.Count}";
            scrollHost.AutoScrollPosition = Point.Empty;
        }

        searchText.TextChanged += (_, _) => ApplySearchFilter();
        searchText.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Escape)
                return;
            searchText.Clear();
            e.SuppressKeyPress = true;
        };
        ApplySearchFilter();

        scrollHost.Controls.Add(grid);
        outer.Controls.Add(scrollHost, 0, showOptions ? 3 : 2);
        return outer;
    }

    private void BuildCatalogButtonImages()
    {
        var maxIconIndex = catalog.Spells.Concat(catalog.Perks).Concat(catalog.Statuses)
            .Select(item => item.IconIndex)
            .DefaultIfEmpty(-1)
            .Max();
        for (var index = 0; index <= maxIconIndex; index++)
        {
            var image = new Bitmap(CatalogButtonIconSize, CatalogButtonIconSize);
            using var graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Transparent);
            DrawCatalogIcon(graphics, index,
                new Rectangle(0, 0, CatalogButtonIconSize, CatalogButtonIconSize));
            catalogButtonImages.Add(image);
        }

        // 物品使用独立的图标图集，追加到按钮图列表末尾并重定向索引
        var itemBaseIndex = catalogButtonImages.Count;
        for (var index = 0; index < catalog.Items.Count; index++)
        {
            var item = catalog.Items[index];
            var image = new Bitmap(CatalogButtonIconSize, CatalogButtonIconSize);
            using var graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Transparent);
            if (item.IconIndex >= 0)
                DrawCatalogIcon(graphics, item.IconIndex,
                    new Rectangle(0, 0, CatalogButtonIconSize, CatalogButtonIconSize), itemIconAtlas);
            catalogButtonImages.Add(image);
            item.IconIndex = itemBaseIndex + index;
        }
    }

    private static Label Label(string text) => new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 10, 7) };

    private static Button Button(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(0, 30),
            Margin = new Padding(4),
            Padding = new Padding(0, 2, 0, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        button.Click += onClick;
        return button;
    }

    private static GroupBox QuickActionGroup(string text, out FlowLayoutPanel layout)
    {
        var group = new GroupBox
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 8)
        };
        layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(3)
        };
        group.Controls.Add(layout);
        return group;
    }

    private Control QuickActionButton(string text, string description, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Standard,
            Width = 245,
            Height = 34,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular),
            Margin = new Padding(4, 3, 10, 6)
        };
        button.Click += onClick;
        quickActionToolTip.SetToolTip(button, description);
        return button;
    }

    private static NumericUpDown CountInput(int maximum) => new()
    {
        Minimum = 1,
        Maximum = maximum,
        Value = 1,
        Width = 70,
        Anchor = AnchorStyles.Left
    };

    private static TextBox NumberText(string text) => new() { Text = text, Width = 130, Anchor = AnchorStyles.Left | AnchorStyles.Right };

    private void ConfigureCatalogCombo(ComboBox combo, int iconSize)
    {
        catalogIconSizes[combo] = iconSize;
        combo.DropDownStyle = ComboBoxStyle.DropDown;
        combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = 30;
        combo.Dock = DockStyle.Fill;
        combo.DropDownWidth = 560;
        combo.IntegralHeight = true;
        combo.MaxDropDownItems = 14;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Margin = new Padding(0);
        combo.DrawItem += DrawCatalogItem;
    }

    private Control BuildCatalogPicker(ComboBox combo)
    {
        var iconSize = catalogIconSizes.GetValueOrDefault(combo, 24);
        var host = new Panel
        {
            Height = 32,
            MinimumSize = new Size(300, 32),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3)
        };
        var iconPanel = new Panel
        {
            Width = 30,
            Dock = DockStyle.Left,
            BackColor = Color.FromArgb(42, 44, 47),
            Cursor = Cursors.IBeam,
            Margin = new Padding(0)
        };
        var arrowPanel = new Panel
        {
            Width = 30,
            Height = host.ClientSize.Height,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
            Margin = new Padding(0)
        };

        iconPanel.Paint += (_, e) =>
        {
            if (combo.SelectedItem is CatalogItem item && item.IconIndex >= 0)
            {
                var x = (iconPanel.ClientSize.Width - iconSize) / 2;
                var y = (iconPanel.ClientSize.Height - iconSize) / 2;
                DrawCatalogIcon(e.Graphics, item.IconIndex, new Rectangle(x, y, iconSize, iconSize));
            }
        };
        iconPanel.Click += (_, _) => combo.Focus();
        combo.SelectedIndexChanged += (_, _) => iconPanel.Invalidate();
        combo.TextChanged += (_, _) => iconPanel.Invalidate();

        arrowPanel.Paint += (_, e) =>
        {
            var centerX = arrowPanel.ClientSize.Width / 2;
            var centerY = arrowPanel.ClientSize.Height / 2 - 1;
            var points = new[]
            {
                new Point(centerX - 4, centerY - 2),
                new Point(centerX + 4, centerY - 2),
                new Point(centerX, centerY + 2)
            };
            using var brush = new SolidBrush(Color.FromArgb(55, 55, 55));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPolygon(brush, points);
        };
        arrowPanel.MouseEnter += (_, _) => arrowPanel.BackColor = Color.FromArgb(242, 242, 242);
        arrowPanel.MouseLeave += (_, _) => arrowPanel.BackColor = Color.White;
        arrowPanel.Click += (_, _) =>
        {
            combo.Focus();
            combo.DroppedDown = !combo.DroppedDown;
        };

        void PositionArrow()
        {
            arrowPanel.Location = new Point(host.ClientSize.Width - arrowPanel.Width, 0);
            arrowPanel.Height = host.ClientSize.Height;
        }
        host.Resize += (_, _) => PositionArrow();

        host.Controls.Add(combo);
        host.Controls.Add(iconPanel);
        host.Controls.Add(arrowPanel);
        arrowPanel.BringToFront();
        PositionArrow();
        return host;
    }

    private void DrawCatalogItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo || e.Index < 0 || e.Index >= combo.Items.Count)
            return;

        e.DrawBackground();
        if (combo.Items[e.Index] is not CatalogItem item)
            return;

        var textX = e.Bounds.Left + 34;
        if (item.IconIndex >= 0)
        {
            var displaySize = catalogIconSizes.GetValueOrDefault(combo, 24);
            var destination = new Rectangle(
                e.Bounds.Left + (30 - displaySize) / 2,
                e.Bounds.Top + (e.Bounds.Height - displaySize) / 2,
                displaySize,
                displaySize);
            DrawCatalogIcon(e.Graphics, item.IconIndex, destination);
        }

        var textBounds = new Rectangle(textX, e.Bounds.Top, Math.Max(0, e.Bounds.Right - textX - 4), e.Bounds.Height);
        var color = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : combo.ForeColor;
        TextRenderer.DrawText(e.Graphics, item.DisplayName, combo.Font, textBounds, color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }

    private void DrawCatalogIcon(Graphics graphics, int iconIndex, Rectangle destination, Bitmap? atlas = null)
    {
        var cell = CatalogService.IconCellSize;
        var source = new Rectangle(
            (iconIndex % CatalogService.IconAtlasColumns) * cell,
            (iconIndex / CatalogService.IconAtlasColumns) * cell,
            cell,
            cell);
        using var tileBrush = new SolidBrush(Color.FromArgb(42, 44, 47));
        using var tileBorder = new Pen(Color.FromArgb(82, 85, 89));
        graphics.FillRectangle(tileBrush, destination);
        graphics.DrawRectangle(tileBorder, destination.X, destination.Y, destination.Width - 1, destination.Height - 1);
        var oldInterpolation = graphics.InterpolationMode;
        var oldPixelOffset = graphics.PixelOffsetMode;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(atlas ?? iconAtlas, destination, source, GraphicsUnit.Pixel);
        graphics.InterpolationMode = oldInterpolation;
        graphics.PixelOffsetMode = oldPixelOffset;
    }

    private static void AddPair(TableLayoutPanel layout, int row, string leftLabel, Control left, string rightLabel, Control right)
    {
        layout.Controls.Add(Label(leftLabel), 0, row);
        layout.Controls.Add(left, 1, row);
        if (!string.IsNullOrEmpty(rightLabel))
            layout.Controls.Add(Label(rightLabel), 2, row);
        layout.Controls.Add(right, 3, row);
    }

    private sealed class CatalogButton : Button
    {
        private readonly Bitmap? catalogIcon;
        private bool pointerInside;
        private bool pointerDown;

        public CatalogButton(Bitmap? icon)
        {
            catalogIcon = icon;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UseVisualStyleBackColor = false;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            pointerInside = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            pointerInside = false;
            pointerDown = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pointerDown = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pointerDown = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var background = !Enabled
                ? SystemColors.Control
                : pointerDown
                    ? Color.FromArgb(224, 234, 245)
                    : pointerInside
                        ? Color.FromArgb(240, 246, 252)
                        : Color.White;
            using (var brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, ClientRectangle);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
                pointerInside ? Color.FromArgb(86, 135, 185) : Color.FromArgb(175, 175, 175),
                ButtonBorderStyle.Solid);

            const int leftPadding = 7;
            var textLeft = leftPadding;
            if (catalogIcon is not null)
            {
                var iconTop = Math.Max(0, (ClientSize.Height - CatalogButtonIconSize) / 2);
                e.Graphics.DrawImageUnscaled(catalogIcon, leftPadding, iconTop);
                textLeft += CatalogButtonIconSize + 8;
            }

            var textBounds = new Rectangle(
                textLeft,
                1,
                Math.Max(0, ClientSize.Width - textLeft - 7),
                Math.Max(0, ClientSize.Height - 2));
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds,
                Enabled ? ForeColor : SystemColors.GrayText,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            if (Focused && ShowFocusCues)
            {
                var focus = Rectangle.Inflate(ClientRectangle, -3, -3);
                ControlPaint.DrawFocusRectangle(e.Graphics, focus);
            }
        }
    }

    [GeneratedRegex("\\[([A-Z0-9_]+)\\]$")]
    private static partial Regex IdAtEndRegex();

    [GeneratedRegex("^[A-Z0-9_]+$")]
    private static partial Regex ValidIdRegex();
}
