# Keeper Body & Zombie Editor (守墓人2 尸体与僵尸属性编辑器)

[English](#english) | [中文](#中文)

---

<a name="english"></a>
## English

An in-game native overlay and standalone management mod for **Graveyard Keeper 2** (Steam App 4358690) that allows customizing White Skulls and Red Skulls on bodies and zombie workers, as well as freely adding and modifying Technology Points (Red points, Green points, Blue points) for zombie workers individually or in bulk.

### Key Features

1. **Smart Contextual Adaptation & Global Hotkey (Press F8)**:
   - **Global Standalone Management Window**: Press **F8** at any time in-game to toggle the editor window on/off. Supports dragging the title bar to any position on your screen.
   - **In-Game Hotkey Customization**: Directly click the **[Hotkey: F8]** button on the header bar and press any keyboard key to rebind it instantly. The new key is immediately saved to config and active.
   - **Non-Intrusive Design (Never Auto-Pops Up)**: The editor will **never** automatically pop up during normal gameplay (such as interacting with the Autopsy Table or inspecting zombie stations), keeping your game completely clean and immersion intact.
   - **Context-Aware Targeting on Hotkey**: When you press **F8** to open the editor, it intelligently detects your current context:
     - **Autopsy Table Active**: Automatically targets and syncs the corpse currently on the autopsy table.
     - **Zombie Worker Window Active**: Automatically targets and syncs the inspected zombie worker's skulls and tech points.
     - **Carried Target Active**: If you are carrying a body or zombie on your shoulders, automatically targets the carried object.
     - **World Exploration**: Seamlessly browses all active zombie workers in your compound with `[< Prev]` and `[Next >]` buttons.

2. **Body & Zombie Skulls Modifier (White Skulls & Red Skulls)**:
   - Enter desired values for White Skulls and Red Skulls (e.g. 26 White, 0 Red) and click **Apply Skulls** (`ui_apply`) to take effect instantly.
   - **Full Game Logic Synchronization**: Changes are recognized by grave quality calculations (`GetTotalQualityGrave`), the Resurrection Table, and zombie workstations.
   - **Safe Restoration**: Click **Restore Default** (`hint_reset`) at any time to revert back to natural organ calculations.

3. **Zombie Technology Points Modifier (Red / Green / Blue Points)**:
   - **Instant Single Modification**: Freely enter desired values for Red points (`tech_red`), Green points (`tech_green`), and Blue points (`tech_blue`). Click **Apply Current Tech** to grant them immediately to the active zombie.
   - **One-Click Batch Application**: Click **Apply to All Zombies** to distribute the specified technology points to all zombie workers across your compound in one click.
   - **Native Save Synchronization**: Technology points are directly handled through native `techRed` / `techGreen` / `techBlue` fields and events, fully persistent in game saves.

4. **Dynamic Game Language Adaptation & Native Game Terminology**:
   - **Native Sprite Icons Integration**: Uses genuine in-game font sprites for White Skulls (`<sprite name="skull-zombie_window">` / `<sprite name="skull">`) and Red Skulls (`<sprite name="rskull-zombie_window">` / `<sprite name="rskull">`), as well as official Technology Points sprites (`<sprite name="tech_red">`, `<sprite name="tech_green">`, `<sprite name="tech_blue">`).
   - **Real-Time Language Response**: Actively listens to `GameSettings.OnLanguageChanged`. When switching languages in game settings (English, Chinese, Russian, German, French, Spanish, Japanese, Korean, etc.), all labels, titles, buttons, and status messages update immediately.
   - **Official Terminology First**: Buttons and titles directly invoke official localization terms such as `LLBase.L("ui_apply")` (Apply), `LLBase.L("hint_reset")` (Reset), `LLBase.L("body_zombie")` (Zombie), and `LLBase.L("ui_grave_corpse_widget_header")` (Body).

### Controls & Hotkeys

| Action | Control |
|---|---|
| Toggle Editor Window | **F8** (Click **[Hotkey: F8]** on window header to rebind on-the-fly, or edit `BepInEx/config/Narodum.gk2.keeperbodyzombieeditor.cfg`) |
| Customize Hotkey In-Game | Click **[Hotkey: F8]** on the header bar, press any keyboard key to instantly bind and save (Press **ESC** to cancel) |
| Move Window | Left-Click & Drag the Header Bar |
| Close Window | Press **ESC** or Click `[X]` |
| Switch World Zombies | Click `[< Prev]` or `[Next >]` |

### Requirements

- **Game Version**: Graveyard Keeper 2 (Steam App 4358690)
- **Mod Loader**: BepInEx 5.4.23.5 x64 (verified with `winhttp.dll` + `doorstop_config.ini` + `BepInEx\core`)

### Installation & Uninstallation

#### Installation
1. Obtain `KeeperBodyZombieEditor.dll`.
2. Place `KeeperBodyZombieEditor.dll` into your game root's `BepInEx\plugins\` folder.
3. Launch the game and load your save. Press **F8** in-game to start editing.

#### Uninstallation
- Delete `KeeperBodyZombieEditor.dll` from `BepInEx\plugins\`.
- Custom skull values are saved in `BepInEx\config\KeeperBodyZombieEditor.data.json`, which can also be deleted if desired.

### Save Game Safety Notice

- Custom skull data is safely isolated in `BepInEx\config\KeeperBodyZombieEditor.data.json` and applied through Harmony runtime patches. It **does not alter the binary layout of original save files**, ensuring your save remains 100% clean and uncorrupted after uninstalling.
- Technology points utilize native game mechanics and fields.
- Creating regular backups of your save files (`%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\`) is always recommended.

---

<a name="中文"></a>
## 中文

针对《守墓人2》(Graveyard Keeper 2) 开发的原生高层级界面注入式 Mod，支持自由修改尸体/僵尸工人的白骷髅与红骷髅数量，并支持为僵尸工人自由修改与批量添加三种科技点（红点、绿点、蓝点）。

### 主要功能

1. **情境智能自适应识别与全局热键 (按 F8 即开即用)**：
   - **全局独立管理窗口**：在游戏内随时按下 **F8** 键呼出/关闭编辑器窗口，支持按住标题栏自由拖拽移动至屏幕任意位置。
   - **窗口内即时自定义按键**：直接点击标题栏右上角的【热键: F8】按钮，按下任意想要绑定的键盘按键即可即刻生效，并自动保存到配置文件，再也不用手动改配置文本！
   - **纯粹无打扰设计（绝不主动自动弹窗）**：在游戏正常游玩（如查看解剖台、检视僵尸工作台）期间，编辑器**绝不会**自动弹窗打扰玩家，保持纯净的原版游戏交互沉浸感。
   - **呼出时情境自适应识别**：当按下 **F8** 呼出编辑器时，会根据您当前的游玩状态智能识别优先目标：
     - **正打开解剖台**：呼出时自动识别并同步解剖台上尸体的红白骷髅属性；
     - **正查看僵尸工人**：呼出时自动识别并同步当前僵尸工人的骷髅属性与三种科技点；
     - **肩上正搬运尸体/僵尸**：呼出时自动优先锁定当前搬运的目标；
     - **自由探索状态**：一键切换至世界僵尸工人一览，提供 `[< 上一只]` 和 `[下一只 >]` 按钮远程浏览领地内所有僵尸。

2. **尸体 / 僵尸 骷髅修改（白骷髅 & 红骷髅）**：
   - 自由填写目标白骷髅与红骷髅数量（如 26 白骷髅、0 红骷髅），点击【应用骷髅】即刻生效。
   - **全面同步**：墓地埋葬评分、复活台、工作效率计算均全面生效。
   - **安全还原**：支持一键【还原器官值】，随时撤销修改，恢复为身体器官自然计算数值。

3. **僵尸工人三种科技点自由修改（红/绿/蓝）**：
   - **单体即时修改**：自由输入红点、绿点、蓝点数值，点击【应用当前科技点】直接赋予当前僵尸。
   - **全局一键批量应用**：点击【应用至所有僵尸】，一键将输入的科技点数量赋予领地内所有的僵尸工人，省去逐个调整的繁琐操作。
   - **原生存档同步**：科技点直接写入游戏存档系统，安全可靠。

4. **动态游戏语言响应与原生专有名词**：
   - **原生字体图标集成**：骷髅采用原版白骷髅与红骷髅图文字符（`<sprite name="skull-zombie_window">`、`<sprite name="rskull-zombie_window">`），科技点采用游戏原版图文字符（`<sprite name="tech_red">`、`<sprite name="tech_green">`、`<sprite name="tech_blue">`），杜绝杜撰名词。
   - **多语言即时响应**：深度绑定 `GameSettings.OnLanguageChanged`，当在游戏设置中切换任意语言（中/英/俄/德/法/西/日/韩等）时，编辑器所有文案、标题和按钮文字瞬间自适应切换。
   - **官方词条优先**：核心操作直接调用原版 `LLBase.L("ui_apply")`（应用）、`LLBase.L("hint_reset")`（重置）、`LLBase.L("body_zombie")`（僵尸）、`LLBase.L("ui_grave_corpse_widget_header")`（尸体）。

### 按键与操作指南

| 功能操作 | 按键 / 交互 |
|---|---|
| 呼出 / 关闭编辑器窗口 | **F8**（可点击窗口右上角【热键: F8】按钮直接按键修改，或在 `BepInEx/config/Narodum.gk2.keeperbodyzombieeditor.cfg` 自定义） |
| 窗口内自定义热键 | 点击窗口右上角【热键: F8】按钮，按下任意键盘按键即刻绑定并自动保存（按 **ESC** 取消） |
| 移动窗口位置 | 鼠标左键按住顶部标题栏拖拽 |
| 关闭窗口 | 按 **ESC** 键或点击右上角 `[X]` |
| 切换全图僵尸工人 | 点击 `[< 上一只]` / `[下一只 >]` 按钮 |

### 运行环境与前置需求

- **游戏版本**：Graveyard Keeper 2 (Steam App 4358690)
- **加载器**：BepInEx 5.4.23.5 x64 (实测通过)

### 安装与卸载

#### 安装方法
1. 编译或下载获取 `KeeperBodyZombieEditor.dll`。
2. 将 `KeeperBodyZombieEditor.dll` 放置于游戏根目录下的 `BepInEx\plugins\` 文件夹内。
3. 启动游戏进入存档，按 **F8** 即可呼出修改器。

#### 卸载方法
- 直接从 `BepInEx\plugins\` 中删除 `KeeperBodyZombieEditor.dll` 即可。
- 骷髅自定义数据保存在 `BepInEx\config\KeeperBodyZombieEditor.data.json` 中，如需清除也可一并删除。

### 存档安全性提示

- 本 Mod 的骷髅修改机制通过外部持久化配置文件与 Harmony 动态拦截实现，**不会破坏原版游戏存档二进制结构**，卸载后存档依然安全完好。
- 僵尸科技点直接利用游戏原生 `techRed/techGreen/techBlue` 字段与事件，属于游戏原生支持的功能。
- 建议在大规模操作前备份好游戏存档（位于 `%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\`）。
