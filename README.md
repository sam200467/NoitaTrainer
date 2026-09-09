# Noita 即时修改器

面向 Windows 版 Noita 的中文辅助修改器。

## 运行环境

- Windows 10/11 x64
- Noita Steam 正式版
- 发布版为自包含单文件程序，无需单独安装 .NET 运行时

## 安装与使用

1. 从仓库右侧的 **Releases** 页面下载便携版 ZIP 或 `NoitaTrainer.exe`。
2. 运行 `NoitaTrainer.exe`，确认程序检测到正确的 Noita 游戏目录。
3. 点击“安装/修复配套模组”。
4. 在 Noita 的 Mods 菜单中允许 Unsafe mods，并启用 **Codex Noita Trainer Bridge**。
5. 重新启动或重新载入 Noita，进入一局游戏。修改器顶部显示绿色“已连接”后即可使用。

> 使用模组会令当前游戏被标记为 Modded，原版成就或进度统计可能不会更新。操作重要长周目前建议备份存档。

## 主要功能

- 实时显示角色、世界及本局统计信息
- 坐标传送和常用地点快捷传送
- 回满血量、刷新法术、世界转换及阵营调整
- 添加天赋、法术、Twitch 事件、药水和物品
- 读取并修改当前魔杖属性
- 读取并修改角色属性
- 自动收集金块、幽灵透视等持续功能

更完整的功能与安全说明见 [NoitaTrainer-README.txt](./NoitaTrainer-README.txt)。

## 从源码构建

需要安装 .NET 8 SDK：

```powershell
dotnet build -c Release
```

生成 Windows x64 自包含单文件程序：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 卸载

1. 在 Noita 的 Mods 菜单中停用 **Codex Noita Trainer Bridge**。
2. 退出游戏后删除 `Noita\mods\codex_noita_trainer_bridge`。
3. 删除修改器程序。如需清除偏好设置，再删除 `%LocalAppData%\CodexNoitaTrainer\settings.txt`。

## 数据与素材说明

法术、天赋及材料的中文资料整理自 Noita Wiki。Noita Wiki 页面内容采用 CC BY-NC-SA 3.0 许可；相关名称、图像与游戏内容版权归各自权利人所有。本项目与 Nolla Games 无隶属或官方合作关系。

- [Noita Wiki 中文站](https://noita.wiki.gg/zh/wiki/Noita_Wiki)
- [材料信息表](https://noita.wiki.gg/wiki/Material_Information_Table)

本项目目前尚未附加统一的软件源码许可证；除上述第三方内容的既有许可外，不代表自动授予复制、修改或再分发源码的权利。

