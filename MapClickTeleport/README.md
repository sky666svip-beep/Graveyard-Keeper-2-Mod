# Map Click Teleport

Standalone Graveyard Keeper 2 plugin: **open the world map, double-click any spot, and the player is teleported there** — across scenes, with the correct ground height.

It has no menu, no UI, no hotkey requirement and **no dependency on Keeper Cheat Menu**. Installing the cheat menu is not needed.

## How to use

1. Load a save.
2. Open the in-game map (the **Map** page of the character window; the `UIMapWindow` copy used by the milestone flow works too).
3. **Double-click** the spot you want to travel to (left mouse button, two clicks within 0.4 s and 12 px by default).
4. The map window closes and the player is moved there.

Optional extra triggers, off by default: `Ctrl + left click`, `middle click`, and a configurable hotkey.

Clicking outside the drawn map (letterbox, tab strip, list area) does nothing. A click in the world while the map is closed does nothing at all.

## Requirements

- Graveyard Keeper 2 Steam build 25467846 (game 1.004.2, Unity 6000.3.9f1).
- **BepInEx 5 — verified with `BepInEx_win_x64_5.4.23.5`.** Without BepInEx the plugin does not load at all. The game's own `GK2Loader.dll` / `GK2Bridge.dll` under `GraveyardKeeper2.runtime` only host the 初五助手 trainer and never scan `BepInEx\plugins`.
- Verified combination (2026-09-26): this build references `BepInEx 5.4.20.0` and runs on host `BepInEx 5.4.23.5` (Mono binds by assembly name). Re-verify with the three-evidence check after changing the host version.

## Installation / 安装使用

1. Put `MapClickTeleport.dll` into `BepInEx/plugins/`.
2. Start the game and load a save — no key to press, the plugin is always armed.
3. Config file: `BepInEx/config/Narodum.gk2.mapclickteleport.cfg`.
4. Uninstall: delete the DLL (and the cfg if you want a clean slate).

Log evidence that it loaded: `BepInEx/LogOutput.log` and `Player.log` contain `Loading [Map Click Teleport 1.0.0]` and the banner `Map Click Teleport 1.0.0 loaded.`.

## Configuration (`[General]`)

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. |
| `Double click` | `true` | Teleport when the map is double-clicked. |
| `Double click window` | `0.4` | Max seconds between the two clicks (0.15–1). |
| `Double click distance` | `12` | Max pixels between the two clicks (2–40). |
| `Ctrl + left click` | `false` | Extra trigger for players who dislike double clicks. |
| `Middle click` | `false` | Extra trigger on middle mouse button. |
| `Hotkey` | `None` | Optional extra trigger: while the map is open, this key teleports to the spot under the mouse. |
| `Cooldown` | `0.6` | Seconds after a teleport during which further triggers are ignored (0–5), so a triple click cannot travel twice. |
| `Close map window` | `true` | Closes the map window before teleporting, exactly like the game does for its own map milestones. |
| `Correct ground height` | `true` | Resolves the real ground elevation of the clicked spot. **Keep this on** — with it off, a teleport between different heights leaves the player floating and unable to move. |
| `Free the player if stuck` | `true` | After arriving, asks the game to move the player to a nearby free spot when they ended up inside something (uses the game's own helper, which only acts when the player actually overlaps something). |
| `Validate landing spot` | `true` | When the clicked spot is not inside any world zone (solid rock, cliff face, water), the plugin lands on the **ground surface under the cursor** (found by casting a ray down the map column), or on the nearest walkable navigation point when that column has no ground. **Keep this on** — without it, such a click drops the player out of the map. |
| `Snap distance` | `30` | How far the nearest walkable navigation point may be when the clicked spot is not walkable (5–200). If nothing is close enough, the teleport is refused and logged instead. |
| `Recover if the player falls out` | `true` | If the player ends up falling well below the landing height, they are teleported back to where they stood before the map teleport. |
| `Verbose logging` | `false` | Also logs the raw map rectangle and cursor numbers of every attempt. |

## Why the height needs its own step (and why it used to float)

The world is an oblique projection: `VisualConsts.ProjectGroundPointToElevation` shifts `z` by `-0.75` per unit of elevation, while the map's vertical axis is `z + y * tan(tilt)` — the two cancel out at `tan = 0.75`. A map point therefore stacks a whole **elevation column**, and different ground heights overlap on the map.

So a click only gives `x` and the projected depth. The plugin then:

1. converts the clicked column into the zone's ground frame with `WorldZone.GroundPlaneY`,
2. finds the zone that owns the spot (`WorldZoneData.wholeZoneRect`) and asks it for the real ground height (`WorldZone.TryGetBuildElevationY`),
3. builds the exact landing point with `VisualConsts.ProjectGroundPointToElevation`,

which is the same ground lookup `BuildController` uses. If the target scene is **not loaded yet** (a long cross-scene jump), its elevation data does not exist. The scene is then picked from the navigation points of **every** scene in the save (they are known without loading anything): the tightest per-scene point bounds that contain the click wins, and the height comes from the nearest navigation point **of that scene** rather than a global nearest point that may belong to a different one. Right after arrival the height is corrected once more (`Ground snap (map arrival): moved from y … to y …`).

## Log lines / troubleshooting

```
Map click teleport (double-click): going to scene 'Graveyard' at (-100.0, 0.4, -179.9), height from zone data.
Map click teleport: closed the character window.
Ground snap (map arrival): moved from y 5.00 to y 0.37 in scene 'Graveyard' via zone 'graveyard'.
Map click teleport: asked the game to free the player if they are stuck.
```

- No `Map click teleport` line at all → the trigger never fired (map not open, or the click was outside the map).
- `no map page is visible right now` → the map was not open when the trigger fired.
- `height from` tells you how the landing height was resolved: `zone data` (exact, the scene is loaded), `scene navigation points` (cross-scene, resolved from the scene's own navigation points) or `global approximation` (last resort).
- `mapRect was not readable, falling back to …` → the game's map layout changed and the plugin used a fallback rect; the mod still works but please report it.
- `none of the MapPageWidget map fields … could be read` → the game UI changed enough that the plugin needs an update.

## Changelog

**1.3.2**
- A surface hit that sits far below every known ground height is rejected as a bogus collider. Real logs showed the ray hitting y -32.9 while the scene's zones are around y 1-6, after which the player fell straight through; such a column now falls back to navigation point snapping instead.

**1.3.1**
- The post-arrival ground correction no longer pulls the player down to a zone's **ground plane** when no elevation area covers the spot. Real logs showed zone `forest_post` reporting a ground plane of 0 while the terrain there is at 1.2, so the correction dropped the player inside the terrain and they fell out of the map right after a perfectly good landing. Only a real elevation area is used now.
- Navigation points are verified before being used: a point whose elevation column has no ground (some transit links sit in mid air) is skipped, and the next nearest one is tried.

**1.3.0**
- **Precision**: a clicked spot that is outside every world zone no longer jumps to the nearest navigation point. The plugin now casts a ray straight down the map column the cursor points at and lands on the ground surface it hits, which is the exact spot under the cursor. Navigation point snapping is only used when that column has no ground at all (void or water).
- Every teleport now logs how far the landing spot is from the click (`N units from the click`), so precision problems are measurable instead of a matter of opinion.

**1.2.3**
- Snapping now prefers navigation points that are **inside a world zone**. The navigation data also holds transit and town link points outside every zone, and real logs showed one of those with a height of 0 which dropped the player out of the map (the recovery caught it, but the landing was useless). Points inside a zone always resolved a real ground height.

**1.2.2**
- The ground correction now waits for the player to really be at the landing spot (plus a short settle delay) instead of running on a 1.2 s timeout. Real logs showed the same landing spot resolving a zone in one run and failing in another, which was the correction running before the arrival scene's zone triggers were up.
- The post-teleport window was extended and now logs a warning when the landing spot was never reached.

**1.2.1**
- Fixed a flag mix-up that made the landing validation skip itself whenever the approximate path produced a height: clicks outside every world zone were still teleported into solid rock. Real logs showed the fall-out recovery working while validation never fired. `height from zone data` now means "the spot is inside a walkable zone"; anything else is validated.

**1.2.0**
- **Landing validation**: a clicked spot that is not inside any world zone (solid rock, cliff, water) no longer teleports the player into it. The plugin lands on the nearest walkable navigation point of that scene, or refuses the teleport when nothing is within `Snap distance`.
- **Fall-out recovery**: if the player still ends up falling well below the landing height (the camera follows them down until the background turns black), the teleport is undone and they are returned to where they stood before.
- Both are configurable; the log says which one acted.

**1.1.1**
- Zone lookup now uses the zone collider's world bounds instead of `WorldZoneData.wholeZoneRect`, which the game builds from the collider's local size and therefore misses rotated or scaled zones. Real game logs showed a player standing inside the `village` zone that the old test could not find, which cost the exact ground height on those teleports.
- The post-arrival ground snap now asks the game which zone the player is in (zone membership comes from physics triggers) and resolves the height from the player's own world position, instead of re-deriving the map column. It also logs the zone it used.
- Requires the `UnityEngine.PhysicsModule` reference (BoxCollider / Bounds).

**1.1.0**
- Free the player if stuck: after arriving, the game's own free-place helper is called, so landing inside geometry or on a roof no longer leaves the player trapped.
- Extra triggers: `Ctrl + left click` and `middle click` (both off by default).
- `Cooldown` so a triple click cannot teleport twice.
- Map layout fallbacks: if `MapPageWidget.mapRect` cannot be read, `milestonesRect` and then the scroll view content are used instead.
- Cross-scene accuracy: the target scene is now resolved from the navigation point bounds of every scene in the save, and the landing height comes from that scene's own nearest navigation point instead of a global one.

**1.0.0**
- First release.

## Known limitations / conflicts

- **Keeper Cheat Menu**: if that plugin is installed, it detects this one and gives up its own double-click trigger (it keeps only its `MAP TELEPORT` hotkey), so a double-click never teleports twice. Nothing to configure.
- If the cheat menu's own overlay is on screen, clicks are ignored (the cursor would not be over the map). That check is name based (`KeeperCheatMenuCanvas`), so it silently does nothing when the cheat menu is absent.
- On the `UIMapWindow` copy of the map (milestone flow, where milestone icons are clickable) a double-click **on a milestone icon** fires the game's own milestone travel first and then this plugin. The player-facing map inside the character window has non-clickable milestones, so it is unaffected.
- Landing spots are not validated for walkability: clicking a roof, water or inside geometry can leave the player somewhere awkward. `Correct ground height` puts them on the ground of that column, which resolves the common cases.
- Back up your save before using teleport cheats (`%USERPROFILE%\AppData\LocalLow\Lazy Bear Games\Graveyard Keeper 2\Steam_1.dat`).
