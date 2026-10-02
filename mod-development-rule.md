# Graveyard Keeper 2 Mod AI 开发规则

> 适用于所有 Graveyard Keeper 2（守墓人 2，Steam App 4358690）Mod 开发任务；AI 必须严格遵守，确保一次开发完成即可用。
> 本规则只针对本作自身技术体系（C# 程序集 + Harmony 补丁 + 官方 Mod 目录），不套用任何外部游戏的 Mod 概念与文件格式。

## 规则零：环境自检与加载链确认

先确认环境真实存在、插件真能被加载，再写代码；**未确认加载链前不得声称"已可用"**。探测项（只读）：

- 游戏根目录 `D:\Game\Graveyard Keeper 2`；版本看 `Player.log` 的 `Starting game, ver.`
- 官方内容 Mod 目录 `%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Mods`（另有 exe 同级 `Mods`）
- 代码 Mod 链路：`winhttp.dll` + `doorstop_config.ini` + `BepInEx\core`（**2026-09-24 已装官方 BepInEx 5.4.23.5，实测可用**）；`GraveyardKeeper2.runtime\GK2Loader.dll` / `GK2Bridge.dll`（第三方注入器，不加载插件）
- 工具：`F:\tools\dnSpy\dnSpy.Console.exe`、`dotnet --list-sdks`

| 链路 | 判据 | Mod 放哪 | 日志看哪 |
|---|---|---|---|
| A 官方内容 Mod | `...\Mods\` 含 README.txt / workshop.json | `Mods\<类别>\<名>\` 或 exe 同级 `Mods\` | Player.log 的 `[Mods]` / `[LanguageMod]` |
| B BepInEx | winhttp + doorstop + `BepInEx\core`（**2026-09-24 已装 5.4.23.5，实测 ✅**） | `BepInEx\plugins\`，配置 `BepInEx\config\` | `BepInEx\LogOutput.log` + Player.log |
| C 第三方注入器 | `GraveyardKeeper2.runtime\GK2Loader.dll`（原生 PE）+ `GK2Bridge.dll` | **不加载第三方插件**（仅初五助手 Trainer）→ 代码 Mod 不可用 | 不产生本 Mod 日志 |

**实测结论（2026-09-24）**：

- **硬前置 = 官方 `BepInEx_win_x64_5.4.23.5`**：安装后链路成立（`winhttp.dll` + `doorstop_config.ini` → `BepInEx\core\BepInEx.Preloader.dll`；`BepInEx.dll` = 5.4.23.5、`0Harmony.dll` = 2.9.0.0），插件**稳定加载**（Player.log / LogOutput.log 均见 `Loading [Keeper Cheat Menu 0.11.2]` + GUID 横幅；`BepInEx\config\` 生成 `BepInEx.cfg` / `Narodum.gk2.keepercheatmenu.cfg` / `KeeperCheatMenu.locations.json`）；**不安装则不能运行**。
- **只装 `GraveyardKeeper2.runtime\GK2Loader.dll` 不够** —— 它是原生 PE 注入器（VMProtect，非 .NET），只 Hook 进程并拉起初五助手 Trainer（`GK2Bridge.dll`），**不扫描 `BepInEx\plugins`、不解析 `BaseUnityPlugin`、无 Chainloader**，插件不会加载。
- **跨版本引用实测可用**：插件引用 `BepInEx 5.4.20.0` + `0Harmony 2.3.3.0`，宿主提供 `5.4.23.5` + `2.9.0.0` —— Mono 按程序集名宽松绑定，**该组合稳定运行**；但不得默认其它组合可用。

判定链路"是否真的可用"必须给出**三证据**：① 游戏运行时进程模块出现 doorstop 原生模块（本作 `WINHTTP.dll`；**托管程序集不会出现在 OS 模块列表**，查不到 `BepInEx.dll` / 插件 dll 属正常，勿据此判未加载）；② 加载器产物（`BepInEx\config\`、`LogOutput.log` 等）被生成；③ 日志出现加载行与本 Mod 的 GUID 横幅。**缺一即判"未加载"**，禁止以"文件已经放在那里"当作"已生效"。

探测结论（链路、部署路径、日志路径、游戏版本）必须写入交付说明；BepInEx 被卸载、宿主换版本或游戏更新后重新探测。

## 规则一：基于真实游戏数据源

禁止捏造类名 / 方法 / 字段 / 枚举 / 控件名 / 本地化 key / 数值，必须从源码、真实程序集或官方文档取得。

- 源码 `D:\Projects\Graveyard Keeper 2\Game-source-code\Assembly-CSharp`（2002 个 .cs，可全局搜索）
- **`LazyBearTechnology.dll` 必须用 dnSpy 查**：`LLBase` 本地化、`LazyWindow` / `LazyWidgetDataBase` UI、Mod 与语言包加载都在其中且**未反编译**；禁止因源码搜不到就判定功能不存在
- 符号事实以 `GraveyardKeeper2_Data\Managed\*.dll` 为准（源码仅供参考）
- 官方文档：`...\Mods\README.txt`、`StreamingAssets\ModdingTools\VoiceOvers\README.txt`
- 资源在 `StreamingAssets\aa\`（Unity Addressables：`catalog.bin` + `StandaloneWindows64\*.bundle`），不是平铺 bundle

版本基线（看 Player.log）：游戏 `1.004.2`、Unity `6000.3.9f1`、Mono `6.13.0`、App `4358690`、build `25467846`；交付说明须写明所依据版本。

本地化：文案走 `LLBase.L(key)` / `LoadLanguageResource(lang)`（调用点见 `GameSettings.cs`），手柄图标交给既有 `LocaleReplacementRuleSet`；语言 id：en / de / fr / pt-br / es / ru / pl / ja / zh_cn / ko / tr；**禁止硬编码界面文案**。

禁止行为：猜测符号；按用户编造信息开发；跳过验证直接写补丁；**引入本作不存在的配置体系**（本作不读外部 XML / CSV 配置，"杜撰的配置文件名"不会被加载）；把"源码搜不到"当"功能不存在"。

## 规则二：编译与部署

环境：只有 dotnet（SDK 8.0.425 / MSBuild 17.11），**未安装 Visual Studio** → 必须命令行构建。

- 工程 `KeeperCheatMenu\KeeperCheatMenu.csproj` 已是 **SDK 风格工程**（`netstandard2.1`、`OutputPath bin\Release\`、`AppendTargetFrameworkToOutputPath=false`、`GenerateAssemblyInfo=false`），保持该形态
- 引用现状（2026-09-24 已修正并复核，16 项全部有 `HintPath`）：`0Harmony` → `GraveyardKeeper2.runtime\0Harmony.dll`（2.3.3.0）；`BepInEx` → `.nuget\packages\bepinex.baselib\5.4.20\lib\netstandard2.0\BepInEx.dll`（5.4.20.0）；其余 14 项（`Assembly-CSharp`、`LazyBearTechnology`、`Newtonsoft.Json`、`Sirenix.Serialization`、`Unity.TextMeshPro`、`UnityEngine` 及 8 个模块）→ `GraveyardKeeper2_Data\Managed\`
- **引用版本与运行时版本的兼容必须实测（硬性）**：本插件引用 `BepInEx 5.4.20.0` + `0Harmony 2.3.3.0`，宿主 `BepInEx 5.4.23.5` 提供 `5.4.23.5` + `0Harmony 2.9.0.0`；**实测该跨版本组合稳定加载运行**（Mono 按程序集名宽松绑定），但不得默认其它组合可用 —— 换宿主版本后必须重跑三证据验证并在交付说明记录"已验证组合"；出现 `FileLoadException` / 类型解析失败时先对齐引用与宿主版本再重编译
- 游戏程序集加 `<Private>false</Private>`，禁止随 Mod 分发游戏 DLL；工程内不得存在非 GK2 的名称 / 路径残留

```powershell
dotnet build "D:\Projects\Graveyard Keeper 2\KeeperCheatMenu\KeeperCheatMenu.csproj" -c Release
Copy-Item "D:\Projects\Graveyard Keeper 2\KeeperCheatMenu\bin\Release\KeeperCheatMenu.dll" "D:\Game\Graveyard Keeper 2\BepInEx\plugins\" -Force
Get-FileHash "D:\Game\Graveyard Keeper 2\BepInEx\plugins\KeeperCheatMenu.dll" -Algorithm SHA256
```

部署前备份游戏内原文件；禁止写入 `GraveyardKeeper2_Data\Managed\`。

身份与版本：`[BepInPlugin(guid, name, version)]`（示例 `Narodum.gk2.keepercheatmenu` / 0.11.2），GUID 发布后不可改；版本三处一致（插件特性、`AssemblyInfo.cs`、README）；本作没有通用 Mod 清单文件，元数据只用加载器要求的格式或官方 `mod.json`。

自检：build 0 Error；HintPath 全部落在 GK2 且文件存在；产物已部署且 SHA256 一致。

## 规则三：Harmony 补丁与代码规范

- 优先 Prefix / Postfix，其次 Transpiler（须注释 IL 意图），反射为最后手段；补丁写 `[HarmonyPatch(typeof(T), nameof(T.M))]`，禁止裸字符串方法名；记录目标所在程序集
- `Awake()` 只做读配置、注册 Harmony、打日志横幅；**禁止此时访问游戏数据**（场景 / 存档未就绪），运行期初始化放 `MainGame.Start` 之后或惰性初始化
- 补丁入口 `try/catch` + `Logger.LogWarning`，不允许异常打断游戏主循环
- 批量执行 / 模拟玩家操作必须有重入抑制（先例：`AdjacentCropHarvestPatch.Suppress` 配合 `try/finally` 复位）
- 反射成员必须缓存；热路径禁止字符串拼接 / LINQ / 临时对象；UI 文本先比较再赋值；`Texture2D` / `Sprite` / `GameObject` 及时释放
- 一功能一文件：`XxxPatch.cs`、`XxxData.cs`、`XxxOverlay.cs`；**禁止把反编译产物当源码**（现有 174 KB 单文件主类含 `// Token:` 痕迹，新代码不得含反编译注释或 `text` / `num` 变量名）
- UI：基于 `LazyBearTechnology` 的 `LazyWindow` / `LazyWidgetDataBase`；新窗口做独立 MonoBehaviour 叠层，不改原生窗口绑定；文案走 `LLBase`，图标用游戏既有 atlas / `ControllerIconLibrary`；热键可自定义并检测与游戏按键冲突

## 规则四：官方内容 Mod 优先

能不改代码就别改：语言包放 `Mods\Languages\`；语音包放 `Mods\VoiceOvers\<lang>\`，其 `mod.json`（`{"schemaVersion":1,"language":"ru","name":"...","enabled":true}`）目录名等于语言、音频文件名等于行 id、格式按 .wav → .ogg → .mp3 查找、缺失回退官方语音。扫描目录：LocalLow `Mods\`、exe 同级 `Mods\`、已订阅创意工坊；游戏内 `Shift+F10` 重载、`Shift+F11` 工坊窗口；目录名以 `~` 开头即忽略（用此方式临时禁用，不要删目录）。**禁止修改游戏目录内原文件**（含 `voiceover_lines.json`）。

## 规则五：安全与兼容

- **只新增不改本体**：禁止修改 exe / `UnityPlayer.dll` / `GraveyardKeeper2_Data\**`；只允许写入 `BepInEx\plugins\`、`BepInEx\config\`、官方 `Mods\` 与本项目目录；确有必要改游戏文件时，先停下向用户说明风险并取得同意
- **存档**：位于 LocalLow（`Steam_1.dat` + backup，路径来自 `SaveSystem.SaveFolder`）；涉及数值 / 物品 / 任务 / 僵尸的功能必须提示备份；写入前校验取值范围与 ID 存在性；避免在存档加载期间做重活（日志出现 `Serialization depth limit` 视为 Bug）
- **冲突**：本机存在第三方修改器（`守墓人2_初五助手`，功能与本类插件大量重叠），README 必须写明冲突与互相覆盖，测试时逐项单独验证；GUID 唯一、配置键名不冲突、禁止无恢复路径地改全局状态（如 `Time.timeScale`）
- **配置**：写到 `BepInEx\config\`；代码内禁止硬编码本机绝对路径（只允许出现在文档 / 脚本 / HintPath）；配置键发布后保持向后兼容（缺键用默认值）

## 规则六：日志与排障

主日志 `%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Player.log`（上一轮为 `Player-prev.log`）；BepInEx 链路另有 `BepInEx\LogOutput.log`（随 BepInEx 5.4.23.5 安装生成）。插件启动时必须用 `Logger.LogInfo` 打印含 GUID + 版本的横幅，作为"是否被加载"的关键凭据（配合规则零三证据法）。

关键字：`Chainloader` / `BepInEx` / `Plugin`（加载失败）、`Harmony` / `Patching`（补丁失败）、`MissingMethodException` / `TypeLoadException` / `AmbiguousMatchException`（目标不存在或签名变更）、`NullReferenceException`、`FileNotFoundException` / `Could not load file or assembly`、`at <你的命名空间>`；本作特有：`Starting game, ver.`、`[Mods] Reloaded.`、`[LanguageMod] Loaded ...`、`[SteamWorkshop]`、`No data for object [ItemDef] with id = "..."`（ID 错误）、`[DLCEngine]`、`Serialization depth limit`、tag `#shutdown#` / `#aud#` / `#icon#` / `#sprites#`。

```powershell
Select-String "$env:USERPROFILE\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Player.log" -Pattern 'KeeperCheatMenu|Narodum\.gk2|BepInEx|Harmony|Exception|No data for object' | Select-Object -Last 80 -ExpandProperty Line
```

流程：先看日志 → 判类型（未加载 / 补丁失败 / 运行期异常 / 数据告警）→ 用 dnSpy 核对真源 → 改完让用户重测并复查日志 → 日志中无本 Mod 痕迹时按规则零重查加载链。禁止不看日志猜原因、禁止未重测就宣称已修复、禁止忽视 Warning。

## 规则七：交付

交付物：已构建并部署的 DLL；README（功能列表、Requirements、安装与卸载、默认热键、已知限制、备份存档提示）；交付说明（依据的游戏版本、生效链路、部署与配置路径、日志路径、冲突说明、未验证项）。

自检清单：

- [ ] 符号均在真实程序集 / 官方文档核实
- [ ] `dotnet build -c Release` 0 Error，无非 GK2 残留引用
- [ ] 产物已部署且 SHA256 一致，游戏内原文件已备份
- [ ] GUID 与版本三处一致
- [ ] 无 `// Token:` 等反编译残留、无硬编码绝对路径、反射已缓存
- [ ] 批量操作具备重入抑制 + `try/catch`
- [ ] 能走官方 `Mods` 的部分已走官方
- [ ] 未修改游戏原文件，已提示备份存档
- [ ] 与其它工具的冲突已在 README 写明
- [ ] 重测后 Player.log 出现 GUID 横幅且无新增 Error / Warning
- [ ] 用户要求的每项功能已逐条验证

卸载说明：删除插件 DLL（含 `.bak`）、删除 `BepInEx\config\` 下对应配置、可选删除本 Mod 数据文件（如 `KeeperCheatMenu.locations.json`），并说明对存档的影响。

## 附录：速查与维护

**核心概念 → 查证入口**

| 概念 | 入口 |
|---|---|
| UI 窗口 / 控件 | `LazyBearTechnology.dll` 的 `LazyWindow` / `LazyWidgetDataBase`（dnSpy）|
| 文案 / 本地化 | `LLBase.L` / `LoadLanguageResource`（`GameSettings.cs`）；语言包 `Mods\Languages\` |
| 物品 / 配方 / 建筑 / 天赋 / 商人 | `ItemDef`、`CraftDef`、`BuildingDef`、`PerkDef`、`VendorDef`；数值表 `GameBalance` |
| 存档 | `SaveSystem`（`persistentDataPath`）、`GameSave` |
| 世界对象 / 地块 / 园艺 | `WorldZoneData`、`WgoData`、`PlacementBlockingArea`、`Garden*` |
| 僵尸 / AI / 寻路 | `ZombieSystem`、`WCCD_*`、`AstarPathfindingProject` |
| 输入 / 手柄图标 | `Rewired`（`Rewired_Core.dll`）、`GameKey`、`ControllerIconLibrary` |
| 资源加载 | `Unity.Addressables` + `StreamingAssets\aa\catalog.bin` |
| DLC 判定 | 日志 `[DLCEngine]`、`DLC*` 类 |
| 反编译查证 | `F:\tools\dnSpy\dnSpy.Console.exe` 读 `Managed\*.dll` |

- 游戏 `D:\Game\Graveyard Keeper 2`（v1.004.2 / Unity 6000.3.9f1 / Mono 6.13 / App 4358690）
- 源码 `D:\Projects\Graveyard Keeper 2\Game-source-code\`：Assembly-CSharp（2002 个 .cs）、Assembly-CSharp-firstpass；**LazyBearTechnology 未反编译**
- 部署 `BepInEx\plugins\`（现有 `KeeperCheatMenu.dll`，构建与部署件 SHA256 一致）；**链路 = 官方 `BepInEx_win_x64_5.4.23.5`**（winhttp + doorstop + `BepInEx\core`，**实测可用：不装则不能运行**）；`GraveyardKeeper2.runtime\` = 初五助手注入器（原生 PE + Trainer，不支持第三方插件）；编译引用 = `bepinex.baselib 5.4.20` + `runtime\0Harmony 2.3.3.0` + `Managed\`
- 官方 Mod `...\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Mods\`；日志 `...\Player.log` + `BepInEx\LogOutput.log`；存档 `Steam_1.dat`
- 工具 `F:\tools\dnSpy\dnSpy.Console.exe`；dotnet SDK 8.0.425
- 待办：① **已结案** —— GK2Loader 为原生注入器（VMProtect），仅承载初五助手 Trainer，不支持第三方插件；② **已完成** —— csproj 已改为 SDK 工程，16 项引用就位，产物与部署 SHA256 一致；③ **已完成** —— 已装官方 BepInEx 5.4.23.5，三证据验证通过、插件稳定加载（不安装则不能运行）
- 维护：三份副本正文必须一致 —— 根目录 `mod-development-rule.md` 为正文来源，`.agents\rules\mod-development-rules.md`、`.codebuddy\rules\mod-development-rules.mdc` 仅 front-matter 不同；先改主文件再同步，改完比对正文哈希
