# Keeper Cheat Menu

Cheat menu for the Windows Steam release of Graveyard Keeper 2.

## Current features

- F6 toggleable in-game menu
- Infinite energy
- Infinite combat stamina
- Player invulnerability
- One-click insanity reset
- Instant manual actions (digging, planting, building, and other player tool/craft actions complete in one action)
- Optional sleeping at full energy until a configurable manual wake-up key is pressed
- Optional sleeping without triggering the normal wake-up save
- One-click energy, stamina, and health restoration
- Add money using separate gold, silver, and copper fields (silver/copper are limited to 0-99)
- Add red, green, or blue technology points
- Searchable item catalog and item spawning (status line reports the localized item name, not the internal id)
- Item search tolerant of rich-text tags, invisible characters and spacing, with a per-character fallback and an explicit "no match" notice
- Chinese-tuned layout: larger text and a longer item-name limit when the menu language is Chinese
- Native-styled overlay modeled after Graveyard Keeper 2's pause menu
- In-game item icons in the item browser
- Missing-icon placeholders and cleaned localized item names
- Left-side Player, Items, Teleport, and Misc navigation with selected-tab highlighting
- Named teleport locations stored in `BepInEx/config/KeeperCheatMenu.locations.json` across game restarts, with visible coordinates and the scene name
- Teleport slot cap is configurable (default 24, range 1-64); the saved-location list is a scrollable dropdown with a live name/scene filter
- Rename the selected slot and overwrite the selected slot with the player's current position without retyping its name
- Quick slot hotkeys: modifier (Alt by default) + 1..9 teleports straight to that slot while the menu is closed
- World-map teleport: open the in-game map, hover any spot on it, press the assignable hotkey and the player is sent there (cross-scene, the map window is closed first just like the game's own milestone travel)
- XYZ nudge buttons with a configurable step that move the player in place, useful for unsticking from geometry
- User-provided nine-sliced button artwork across the complete menu
- User-provided header, sidebar, content background, and outer-border artwork
- Gold-bordered, darkened text-field focus, a blinking insertion caret at the current text position, and gameplay-input locking while typing
- Misc toggle that harvests connected ready crops of the same type after one is harvested manually
- Configurable 1-30 range action for instantly finishing crop growth, with an assignable hotkey that rejects keys already bound by the game
- Range action for instantly finishing active compost, oven, forge, and other machine crafts
- Range action for immediately regrowing harvested berries, mushrooms, and wild honey
- Movable, lockable nearby-fishing text HUD listing stock, fish name, and compatible bait, including during the minigame
- Fix tab with a targeted nearest-composter interaction and stuck-craft reset
- Efficiency page: craft without materials (missing ingredients no longer block a craft, and nothing is consumed when it starts), plus the display tuning section (live UI scale in 0.25 steps, pixel-perfect toggle, panel-artwork toggle, font-mode switch)
- Persistent in-game language selector for Chinese and English (defaults to Chinese)
- Separate persistent, conflict-checked hotkeys for all three range actions
- Zombies section with persistent movement-speed and work-speed multipliers
- Separate red, green, and blue zombie experience-gain multipliers

## Requirements

- Graveyard Keeper 2 Steam build 25467846
- **BepInEx 5 — verified with `BepInEx_win_x64_5.4.23.5`.** Without BepInEx installed the plugin does **not** load. The game's `GK2Loader.dll` / `GK2Bridge.dll` under `GraveyardKeeper2.runtime` only host the 初五助手 trainer: they do **not** scan `BepInEx\plugins`, do **not** parse `BaseUnityPlugin`, and contain no chainloader, so they can never load this plugin by themselves.
- Verified combination (2026-09-24): this build references `BepInEx 5.4.20.0` + `0Harmony 2.3.3.0` and runs stably on host `BepInEx 5.4.23.5` + `0Harmony 2.9.0.0` (log shows `Loading [Keeper Cheat Menu 0.12.1]`). Other host versions are not guaranteed — re-verify with the three-evidence check (doorstop module present, `BepInEx\config\` + `LogOutput.log` generated, GUID banner in log) before claiming support.

## Installation / 安装使用

1. 将 `KeeperCheatMenu.dll` 放入游戏根目录的 `BepInEx/plugins` 目录中。
   > ⚠️ **前置条件**：只把 DLL 放进目录**不会生效**，必须先安装 BepInEx 5（见 Requirements）。加载成功的**三证据**：游戏进程模块出现 `WINHTTP.dll`（doorstop 原生模块；托管 dll 不会出现在 OS 模块列表，勿据此判未加载）、`BepInEx\config\` 与 `LogOutput.log` 已生成、日志出现 `Loading [Keeper Cheat Menu ...]` 与 GUID 横幅——缺一即说明未被加载。
2. 启动游戏并载入存档，按 **F6** 键即可打开/关闭作弊菜单。
3. 默认语言已设置为**简体中文**（界面左下角可随时切换 中文 / English）。
   > 0.11.5 起仅保留中英两种语言，德语与韩语已移除；旧配置里若残留 German/Korean，启动时会**自动改为 English**。
4. 在使用任何作弊功能前，建议先备份重要存档（位于 `%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\`）。

## Teleport / 传送（0.11.7 起增强）

传送页包含四块内容：保存/管理预设点、已保存点列表（可滚动、可搜索）、快速传送、鼠标传送与坐标微调。

| 控件 | 作用 |
| --- | --- |
| `Location name...` + `Save current location` | 用当前角色坐标（场景 + X/Y/Z）保存一个具名预设；同名会覆盖 |
| `Rename selected` | 用输入框里的文字重命名当前选中的预设（重名会被拒绝） |
| `Update to here` | 把当前选中预设的坐标/场景改写为角色当前位置，名称不变 |
| `SAVED LOCATIONS (n/上限)` | 标题右侧显示已用槽位/上限；`▼` 打开可滚动列表，列表项显示"名称 - 场景 (x, z)" |
| `Search locations...` | 按名称或场景名实时过滤列表 |
| `Teleport` | 传送到当前选中预设（跨场景会自动切场景，同场景直接落点） |
| `MAP TELEPORT` 区的 `Hotkey: ...` 按钮 | 为**地图传送**绑定/解绑热键（可选，双击也能传送），与游戏按键冲突时会拒绝 |
| `X- / X+ / Y- / Y+ / Z- / Z+` | 按步长即时微调角色坐标（不换场景，卡地形时很好用） |
| 微调行左侧输入框 | 微调步长（0.1–20，默认 1） |
| `Ground` | **落地**：按当前所在位置的地面高度把角色贴回地面（悬空/卡住时一键恢复） |

**地图传送怎么用（0.12.1）**：关闭作弊菜单（F6），用游戏自己的按键打开**地图**（角色窗口的 Map 页），然后二选一——

1. **在地图上双击**想去的那个点（左键快速点两下，默认 0.4 秒 / 12 像素内算双击）；或
2. 把鼠标停在那个点上，按传送页 `MAP TELEPORT` 行绑定的热键（例如 `K`）。

触发后插件会先关闭地图所在的窗口（`CharacterWindow` 或 `UIMapWindow`），再执行传送，跨场景也可以。

- 游戏里玩家看的地图是**角色窗口里的 Map 页**（`CharacterWindowData.CharPage.Map` → `CharacterWindow.mapPageWidget`），`UIMapWindow` 是"里程碑流程"单独弹出的另一份地图。插件直接查找**当前可见的 `MapPageWidget`**，两种宿主都支持。
- 地图坐标换算复用了游戏自己的投影：`GUIElements.WorldMin/WorldMax` 的 x/z 范围 + `z + y * tan(倾斜角)`，因此落点和地图上里程碑图标的位置是同一套坐标系。
- **落点高度（0.12.0 修复）**：地图上同一点其实叠着整条"高度柱"（地图投影 `z + y*tan(倾斜角)` 与 `VisualConsts` 的 0.75 抬升系数互相抵消，所以不同高度的地面在地图上重叠）。旧版直接沿用**出发地**的 y，从高地传到低处就会悬空且无法移动。现在改为：先用 `WorldZone.GroundPlaneY` 把点击位置换算到该区域的**地面坐标系**，再用 `WorldZone.TryGetBuildElevationY` 取该点真实地面高度，最后用 `VisualConsts.ProjectGroundPointToElevation` 得到精确落点 —— 与游戏 BuildController 取地面点的方式完全一致。
- 目标场景**尚未加载**时（跨场景点击）拿不到高程数据，此时用最近的导航点高度近似，并在到达后用 `Ground snap` 再校正一次（日志 `Ground snap (map arrival): moved from y … to y …`）。
- 场景归属按"当前场景区域 → 最近的导航点 → 最近的场景原点"三级推断。
- 排查日志（每次触发都会打印）：
  - `Map teleport (double-click|hotkey K): map rect 1024x768 pivot (0.5, 0.5), local (…) -> u/v (0.42, 0.61).`
  - `Map teleport (…): spot resolved to scene 'xxx' at (…)` 以及 `closed the character window.`
  - 若一行都没有，说明触发没进函数；若有 `no map page is visible right now`，说明当时地图页没打开。落点不对时把这两行发出来即可定位。
- 鼠标停在地图绘制范围之外（黑边、标签栏）时不会传送，只在状态栏提示；在地图没打开时双击世界不会有任何反应（不会刷状态栏）。
- 已知小冲突：如果地图是 `UIMapWindow` 那一份（里程碑流程弹出的地图，里程碑可点击），在里程碑图标上双击会先触发游戏自己的里程碑传送、再触发本功能。角色窗口里的地图页里程碑不可点击，不存在该问题。
- **与独立插件 `Map Click Teleport` 的关系（0.12.1）**：地图双击传送已可独立运行（`MapClickTeleport.dll`，不依赖本菜单）。若检测到该插件已安装，本菜单会**让出双击触发**、只保留 `MAP TELEPORT` 热键，避免一次双击触发两次传送；两个都装不会互相干扰。只用菜单、不装那个插件时，双击照旧可用。
- 配置项 `[Teleport] Cursor teleport hotkey` 就是地图传送热键（键名沿用旧版以便保留你已绑定的按键，默认未绑定；不绑也能用双击）。旧版遗留的 `Cursor teleport range` 已不再使用，可以从 cfg 里删掉。

`BepInEx\config\Narodum.gk2.keepercheatmenu.cfg` 的 `[Teleport]` 段：

| 配置项 | 默认 | 说明 |
| --- | --- | --- |
| `Max saved locations` | `24` | 预设点上限（1–64）。旧版本固定为 8 |
| `Quick slot hotkeys` | `true` | 是否启用"修饰键 + 1..9"快速传送 |
| `Quick slot modifier` | `Alt` | 修饰键，可填 `Alt` / `Ctrl` / `Shift` |
| `Cursor teleport hotkey` | `None` | 地图传送热键（默认未绑定，建议在菜单里绑一个） |
| `Nudge step` | `1` | 微调按钮步长（0.1–20） |

- **快速传送**：菜单**关闭**时按 `修饰键 + 1..9` 直接传送到列表中第 1–9 个预设；槽位为空时只写日志不动作。列表中超过 9 个预设时，只有前 9 个有快速槽位。
- 鼠标传送与微调都在**当前场景**内生效；鼠标传送把落点投影到角色当前所站的地面高度平面（`WorldZone.GroundPlaneY` 语义），不会因为点到屋顶/建筑而把角色扔到别的高度。
- 微调的 `Y` 轴是**离地高度**：向上抬升后角色会因重力自然落回地面；若角色卡进地形，先用 `Y+` 抬起再用 `X/Z` 挪开，比直接朝地形里推更安全。
- 预设文件仍是 `BepInEx\config\KeeperCheatMenu.locations.json`，新增了 `sceneName` 字段（用于显示场景名）。旧文件没有该字段也能正常读取，列表会退回显示 `sceneId`。

## Efficiency page / 效率页

侧栏 **Efficiency（效率）** 页签上半部分是效率功能，下半部分是原有的显示调节（UI 缩放 / 像素对齐 / 面板贴图 / 字体模式）。

| 控件 | 说明 |
| --- | --- |
| `Craft without materials: On/Off` | 制作无视材料：材料不足不再阻止制作，开始制作时也不扣除材料。同时影响制作窗口的可用性判断（按钮不再灰显）。对应配置 `[Player] Craft without materials`。 |

实现方式（`CraftWithoutMaterialsPatch.cs`），两条路径都覆盖：

**制作（crafting）**
- `CanStartCraft` 返回 `NotEnoughResources` 时改为 `OK`（基类 + `CraftElement` + `ConveyorCraftElement`，因为它是虚方法）
- `RemoveCraftRequirements` 用 Prefix 跳过消耗（基类 + `CraftElement` + `CraftElementMix` + `CraftElementSurvey`）
- 燃料类制作的 UI 判断走 `CraftDefExtensions.CanActuallyStartCraft` / `CanActuallyStartInstantCraft` / `CanActuallyStartCraftWithNeeds`，单独补丁

**建造（building）**
- 建造流程是 `WgoBuildPointer.SetTarget()` 里两个委托：`canTakeResources`（检查）与 `takeResourcesAction`（扣材料），二者最终都走 `MultiInventory`
- 检查：`MultiInventory.HasItemsById(List<NeedItemData>, ...)` 两个重载 → Postfix 返回 true
- 消耗：`MultiInventory.RemoveItems(List<NeedItemData>, ...)` 两个重载 → Prefix 把材料清单替换为空列表（**不是**跳过整个方法，因为返回值会被调用方当作 `craftInput` 使用，跳过会导致 null 引用）

> ⚠️ **副作用**：因为挂在 `MultiInventory` 这一层公共入口上，开关开启时**所有**按配方扣材料的操作都不再扣除（建造、制作，以及任何走同一 API 的加料/投料行为）。这是为了让建造与制作共用一套逻辑；如需收窄到只影响建造与制作，需要改为挂到各自的调用点。

## Menu sharpness / 菜单清晰度

菜单是**自建 UGUI 叠加层**（独立 Canvas + TextMeshPro），不是游戏的 `LazyWindow` 原生窗口；发虚来自"像素美术 + 非整数缩放 + 像素字体"，与是否用原生界面无关。0.11.3 起可在 `BepInEx\config\Narodum.gk2.keepercheatmenu.cfg` 的 `[General]` 段调整：

| 配置项 | 默认 | 说明 |
| --- | --- | --- |
| `UI scale` | `0` | `0` = 自动：按屏幕尺寸吸附到整数倍（1x / 2x，像素美术最锐利）；填 `1.25`、`1.5` 等可强制放大（越大越不锐利）。 |
| `Pixel perfect UI` | `true` | 让菜单对齐整像素，消除文字/图片的亚像素模糊。 |
| `Font mode` | `Auto` | `Auto`：游戏字体若为非距离场（位图）或 atlas < 1024 或采样字号 < 44pt，自动改用系统高清 SDF 字体；`Game`：始终用游戏像素字体；`System`：始终用高清 SDF 字体（中文取微软雅黑/黑体，64pt / 2048 atlas）。 |
| `Panel artwork` | `true` | `false` 时去掉像素风面板贴图，改用纯色面板——任意缩放下都不会有采样损失，配合 `UI scale` 放大最清晰。 |

- 想"最大清晰度"：`UI scale` 按需填（如 `1.25`）+ `Panel artwork = false` + `Font mode = System`。
- 想"保留像素风"：`Panel artwork = true` + 自动整数缩放（`UI scale = 0`），菜单会略小但笔画完整。

**游戏内实时调节（0.11.3 新增）**：菜单侧栏新增 **Display** 页，可在不重启的情况下调整并即时看到效果——

| 控件 | 作用 |
| --- | --- |
| `−` / 数值 / `+` | 按 0.25 步进实时改变菜单缩放（立即写入配置） |
| `Auto` | 恢复按屏幕尺寸自动吸附的整数倍缩放 |
| `Pixel perfect: On/Off` | 切换整像素对齐 |
| `Panel artwork: On/Off` | 切换像素风面板贴图（切换会重建菜单） |
| `Font mode: Auto/Game/System` | 循环切换字体来源（切换会重建菜单） |

- 字体选择结果会写入日志：`Using loaded Chinese font asset: ... [...]` 或 `Game Chinese font [...] is low detail; switching to a crisp system SDF font.`
- 启动时日志会打印 `Menu canvas: <宽>x<高> screen, scale <倍数>x, pixel perfect = <bool>`，可用于确认实际缩放。

## 0.22.1 合并说明（2026-09-26）

本版本把 `backups\KeeperCheatMenu`（0.22.0 分支，51 个源文件、10 个页面）并入本工程。该分支源码是 **dnSpy 反编译产物**，不能直接编译，本次修复了以下反编译残留：

- 编译器生成的非法标识符：`<>O.<n>__Xxx`（lambda 缓存字段）、`<>c__DisplayClassNNN_0`（闭包类）、`CS$<>8__locals`、`<>4__this`、`<>9__n`、`<Method>b__n` → 全部机械重命名为合法标识符
- 缺失的闭包类定义（438 / 472 / 479 / 480）→ 重写为局部变量，去掉闭包
- `fieldof(<PrivateImplementationDetails>...)` 伪语法（CJK 字体种子字符数组）→ 还原为普通 `new char[] { ... }`
- 枚举被写成数字（`CraftStatus`、`ZombieType`、`TextAlignmentOptions`、`FontStyles`、`GlyphRenderMode`、`AtlasPopulationMode`、`CharacterValidation`、`TextWrappingModes`、`TakenControlType`、`ItemRelatedWidgetState`、`Formatting` 等）→ 加显式转换或还原枚举成员
- 丢失的 `out` 关键字（`TryGetCurrentGameScene`、`TryGetCraftIdFromPlotWgoId`、`CanTeleport`、`TryGetBuildElevationY` 等）→ 补回
- 被推断成 `object` 的类型（`WgoDelayedSpawnSystemData`、`QuestSystemData`）→ 还原真实类型
- `TMP_FontAsset.CreateFontAsset` 的重载参数、`TryAddCharacters(uint[])` 签名、`Harmony.UnpatchSelf`（0Harmony 2.3.3 无此 API）→ 分别修正

工程新增引用：`UnityEngine.PhysicsModule`（`FreeBuildPlacementPatch` 需要 `Physics` / `RaycastHit`）。
`MapClickTeleport` 独立插件与 `TeleportEnhancements.cs`（旧分支的地图传送实现）已被本版本内置的 `MapTeleportEverywherePatch` 取代，旧文件保留在 `backups\KeeperCheatMenu-mine-0.12.0-20260926\`。

**已知残留警告（无害）**：`CS0162` 无法访问代码、`CS0219` 未使用变量（反编译 switch/goto 结构）、`CS0618` `enableWordWrapping` 过时、`CS0472` 一处恒真比较（`QuestPhraseRequirement.Entity != null`，保留原行为）。

**尚未合并**（来自 0.12.0 分支的改动）：菜单清晰度增强（UI 缩放 / 像素对齐 / 字体模式 / 面板贴图）、物品搜索健壮化、中文布局字号优化、物品提示使用本地化名称。

## 0.22.2 页面滚动修复（2026-09-26）

新版（0.22.0）有 6 个页面的控件 Y 偏移超出了 Body 可视区（容器高 **694**），导致大量控件看不见且无法滚动：

| 页面 | 修复前最大 Y | Content 高度 |
|---|---|---|
| Misc | 1390 | 1430 |
| Fix | 930 | 970 |
| Player | 866 | 900 |
| World | 736 | 770 |
| Alchemy | 708 | 740 |
| Crafting | 690（本版本新增的显示设置贴到边界） | 740 |

做法：把每个页面的根节点改成 **Viewport（`Mask` + `ScrollRect`）**，内部再建 **Content**（`anchor (0,1)-(1,1)`、`pivot (0.5,1)`、固定高度），页面所有控件挂到 Content。页面的 `_xxxPage` 字段指向 **Viewport**，这样 `Show*Page()` 的 `SetActive` 能连遮罩一起控制（若指向 Content，隐藏时会留下不透明的 Mask 图形）。

Items 页原本就有 GridLayout + ScrollRect，Teleport / Zombies 在可视区内，均未改动。

## 上游更新工具链（tools/upstream）

上游源码是 dnSpy 反编译产物（不可直接编译），叠加了我们自己的优化，所以每次上游发版都要重放两层改动。已全部脚本化：

| 脚本 | 作用 |
|---|---|
| `update_from_upstream.py <上游目录>` | **一键更新**：备份 → 复制上游 → 修复 → 优化 → 编译 → 编译成功才部署并校验 SHA256 |
| `fix_decompiled.py <目录>` | A 层：反编译残留修复（非法标识符、缺失闭包类、`fieldof` 伪语法、枚举写成数字、丢失的 `out`、`object` 类型、API 签名） |
| `apply_local.py <目录>` | B 层：本地优化（12 类改动，见 `tools/upstream/README.md` 的清单），**幂等** |
| `verify_rebuild.py <上游目录>` | 闭环验证：从上游源码跑一遍两层脚本，确认能编译通过（改规则后必跑） |

**已验证**：以 `backups/KeeperCheatMenu-0.22.0-reference`（上游原始反编译源码）为输入，脚本处理后 `dotnet build` **0 错误** —— 证明规则集完整，上游再发版只需一条命令。

改动模板放在 `tools/upstream/patches/`，锚点与失效处理方式见 `tools/upstream/README.md`。

### 0.22.3 修复：菜单无法打开（反射字符串被误改）

**症状**：`Player.log` 里 `Loading [Keeper Cheat Menu 0.22.2]` 之后紧跟
`Exception: Method ... FishingPondRegenerationCapacityPatch::TargetMethod() returned an unexpected result: null`
→ Harmony 抛 `HarmonyException`，**插件加载中断**，菜单完全打不开。

**原因**：修复反编译残留时的全局正则把**字符串字面量**也重命名了。
`FishingPondRegenerationCapacityPatch` 用反射在**游戏程序集**里查找编译器生成的闭包类，而它按名字匹配的是游戏里的真实名字：

| 位置 | 被误改成 | 游戏程序集里的真实名字 |
|---|---|---|
| `GetNestedTypes(...).Name ==` | `"__Closure134_0"` | `"<>c__DisplayClass134_0"` |
| `AccessTools.Method(closure, ...)` | `"__LambdaReinitBalanceRelatedStuff_2"` | `"<ReinitBalanceRelatedStuff>b__2"` |
| `AccessTools.Field(closure, ...)` | `"__This"` | `"<>4__this"` |

名字不匹配 → 反射返回 null → Harmony 认为补丁无效 → 抛异常。

**修复**：
1. 三处反射字符串还原为游戏里的真实名字；
2. **加固**：闭包类改为**按方法查找**（`GetMethod("<ReinitBalanceRelatedStuff>b__2")`），不再依赖 `<>c__DisplayClass<数字>` 的编号，游戏小更新导致编号变化也能找到；
3. **兜底**：`TargetMethod()` 找不到目标时返回一个占位方法，而不是返回 null —— 宁可这一个补丁失效，也不能让整个插件加载中断（否则连菜单都开不了）。

**工具链同步加固**：
* `fix_decompiled.py` 新增第 8 组规则，把引号内的 `"__Closure..."` / `"__Lambda..."` / `"__This"` / `"__locals"` / `"__OCache"` / `"__f..."` **还原**为 `<>` 原始形式（标识符重命名绝不能碰字符串字面量）。
* `verify_rebuild.py` 新增 **[反射]** 检查项：扫描生成源码里是否残留被误改的反射字符串 —— 这类问题**编译期完全看不出来**，只有跑起来才炸。

### 0.22.4 修复：切中文后所有文字空白

**症状**：切到中文（Chinese Simplified）后菜单按钮文字全部消失（连英文也一起没了），只有数字和符号能显示。

**日志**：
```
Game Chinese font [chinese, render mode 0, sampling 0pt, atlas no atlas] is low detail; switching to a crisp system SDF font.
Unable to load font face for [] font asset.
[Warning] Could not create Chinese font: The CJK font was loaded, but required glyphs could not be added.
```

**原因**：清晰度增强里的"低清字体检测"生效后，走了上游那条**不可靠的系统字体回退路径**：
1. 上游中文系统字体**只试 `msyh.ttc`** —— 而 Unity 的 FontEngine **不支持 TTC 集合格式**，加载失败（`Unable to load font face for []`）；
2. 接着的名称版 `CreateFontAsset("Microsoft YaHei", "Regular", 48)` 虽然返回了资产，但**加不进中文字形** → 抛 `InvalidOperationException`；
3. 结果 `_chineseFontAsset` 为 null，而按钮文字**共用这一个字体资产** → 全部空白。

**修复**：
1. **候选列表（TTF 优先）**：中文 `msyh.ttf → simhei.ttf → msyh.ttc → simsun.ttc → simkai.ttf`；日文 `YuGothM.ttc → meiryo.ttc → msgothic.ttc → yugothic.ttf`。逐个尝试，成功即用并打日志 `Loaded CJK system font file: …`。
2. **保底回退**：系统字体仍加不了所需字形时，**返回游戏自带字体**（低清但可用）并打 Warning，而不是抛异常导致完全没有字体 —— 宁可不清晰，也不能白屏。

对应 `apply_local.py` 新增两个步骤（`CJK 字体候选列表` / `CJK 字形缺失回退`）与模板 `patches/font_candidates.txt`，已纳入闭环验证（19 项优化 / 0 错误）。

### 0.22.5 补齐：无视材料后仍无法制作/建造（可用性检查未放宽）

**症状**：开了"免费制作/建造"后材料确实不扣了，但**材料不足时仍然做不了**（按钮灰显 / 无法放置）。

**原因**：上游只放宽了"消耗"，没放宽"可用性检查"，两处缺口：

| 缺口 | 说明 |
|---|---|
| 制作 | `FreeCraftingAvailabilityPatch` 只 patch 了 `CraftElementBase.CanStartCraft`，但它是 **virtual**，实际调用的是 `CraftElement` / `ConveyorCraftElement` 的 **override** —— 虚分派绕过了补丁，`NotEnoughResources` 照样返回 |
| 建造 | 可用性检查是 `WgoBuildPointer.SetTarget` 里的 `canTakeResources` 委托（内部调 `MultiInventory.HasItemsById`）。上游只在 `UpdateSelectionCellsState` 期间临时把委托换成 `() => true`，**放置等其它路径**仍会真的检查材料 |

**修复**：新增独立文件 **`FreeCraftingAvailabilityEnhancement.cs`**（与上游文件不同名，上游更新不会覆盖），补齐三处入口：

1. `FreeCraftingOverrideAvailabilityPatch` —— patch `CanStartCraft` 的所有 override（`CraftElement` / `ConveyorCraftElement`），`NotEnoughResources` → `OK`
2. `FreeCraftingInventoryCheckPatch` —— patch `MultiInventory.HasItemsById` 两个重载 → 返回 true（制作与建造共用这一层，一并覆盖放置路径）
3. `FreeCraftingFuelUiPatch` —— patch `CraftDefExtensions.CanActuallyStartCraft` / `CanActuallyStartInstantCraft` / `CanActuallyStartCraftWithNeeds`（燃料类制作的 UI 判断不经 `CanStartCraft`）

开关沿用上游已有配置项（`FreeCraftingEnabled` / `FreeBuildingCostsEnabled`），不新增配置。补丁用**独立日志源**（不依赖主类成员），目标找不到时打 `Free-crafting patch target not found: <目标>`，便于排查。
