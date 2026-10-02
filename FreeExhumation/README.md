# Free Exhumation（自由挖掘）

Graveyard Keeper 2（守墓人 2）BepInEx 插件：**挖尸体不再需要先拆墓碑 / 栅栏，也不再需要挖掘许可，且不消耗挖掘许可。**

- GUID：`Narodum.gk2.freeexhumation`
- 版本：`1.0.0`
- 依据游戏版本：`1.004.2`（Unity `6000.3.9f1`，Mono `6.13.0`）
- 生效链路：官方 `BepInEx_win_x64_5.4.23.5`（`winhttp.dll` + `doorstop_config.ini` → `BepInEx\core`），插件放 `BepInEx\plugins\`

---

## 原版的三个门槛

游戏在 `UIGraveWindowData`（Assembly-CSharp）里拦了三层：

| 原版方法 | 作用 |
|---|---|
| `CanExhume()` | 坟上还装着墓碑（`gravetop`）或栅栏（`gravebot`）时返回 `false` → 挖掘按钮变灰，点击直接返回 |
| `HasExhumeCertificate()` | 背包里没有 `exhume_certificate`（挖掘许可）时走"条件不足"弹窗，只能关闭 |
| `ExhumeBody()` | 真正挖掘，第一行就 `RemoveItemById("exhume_certificate", 1)` 强制扣掉一张许可 |

## 本插件做了什么

| 补丁 | 目标（Harmony） | 行为 |
|---|---|---|
| Postfix | `UIGraveWindowData.CanExhume` | 无视装饰限制，恒返回 `true` |
| Postfix | `UIGraveWindowData.HasExhumeCertificate` | 无视许可限制，恒返回 `true` |
| Prefix | `UIGraveWindowData.TryExhumeBody` | 接管挖掘按钮：弹确认框（OK 一定可点）→ 由 `ExhumeService` 执行挖掘，不强制扣许可；可选自动收回装饰 |

补丁入口全部 `try/catch`，异常时自动回退到原版逻辑，不会打断游戏主循环。

## 功能

1. **无视装饰限制**：墓碑 / 栅栏还在也能直接挖。
2. **无视挖掘许可**：背包里一张许可都没有也能挖。
3. **不消耗许可**（可配置）：默认挖完不扣任何东西。
4. **自动收回装饰**（可配置，默认开）：挖的同时把墓碑 / 栅栏退回玩家背包；背包放不下时装饰留在坟上，**绝不销毁**。
5. **可选跳过确认弹窗**（默认关）：点挖掘立刻挖。

## 安装

1. 确认游戏已装 BepInEx（`BepInEx\core\BepInEx.Preloader.dll` 存在）。
2. 把 `FreeExhumation.dll` 复制到 `D:\Game\Graveyard Keeper 2\BepInEx\plugins\`。
3. 启动游戏，日志里应出现：

```
[Free Exhumation] loaded. GUID: Narodum.gk2.freeexhumation v1.0.0
```

卸载：删除 `BepInEx\plugins\FreeExhumation.dll`（配置文件可一并删除，见下）。

## 配置

首次启动后生成 `BepInEx\config\Narodum.gk2.freeexhumation.cfg`，改完重启游戏生效。

| 段 | 键 | 默认 | 说明 |
|---|---|---|---|
| General | `Enabled` | `true` | 总开关，`false` = 完全恢复原版规则 |
| Rules | `IgnoreDecorationRequirement` | `true` | 无视"先拆墓碑 / 栅栏" |
| Rules | `RequireExhumeCertificate` | `false` | 设为 `true` 则恢复"必须有挖掘许可"的原版规则 |
| Rules | `ConsumeExhumeCertificate` | `false` | 挖的时候是否扣 1 张许可（只有背包里有才扣） |
| Rules | `AutoRemoveDecorations` | `true` | 挖掘时把墓碑 / 栅栏收回背包 |
| Rules | `SkipConfirmationDialog` | `false` | 跳过确认弹窗，点挖掘直接挖 |
| Debug | `VerboseLogging` | `false` | 每次挖掘打日志 |

## 日志与排障

- BepInEx：`D:\Game\Graveyard Keeper 2\BepInEx\LogOutput.log`
- 游戏：`%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Player.log`

```powershell
Select-String "$env:USERPROFILE\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Player.log" -Pattern 'Free Exhumation|Narodum\.gk2\.freeexhumation|Harmony|Exception' | Select-Object -Last 40 -ExpandProperty Line
```

看不到横幅 → 按"规则零"复查 BepInEx 链路；看到 `patch failed` 警告 → 游戏更新后方法签名变了，插件会自动回退到原版行为。

## 已知限制

- 挖掘结果与原版一致：坟变成"已挖开"的 `grave_exhume`，尸体仍在坟里，后续拿取流程不变。
- 背包已满时装饰不会被收回，仍留在坟上（宁可留着也不销毁）。
- 只改挖掘条件，不改墓地评分、葬礼流程、僵尸相关逻辑。
- 与任何同样修改 `UIGraveWindowData` 挖掘流程的 Mod 可能互相覆盖（目前已知同机共存的 `KeeperCheatMenu`、`MapClickTeleport`、`Keeper's Relocation Service` 均不涉及此处）。

## 安全提示

- 只改内存中的判定逻辑，不修改任何游戏文件。
- 涉及存档写入（坟的状态会变），**建议先备份存档**：`%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Steam_1.dat`（及其 backup）。
