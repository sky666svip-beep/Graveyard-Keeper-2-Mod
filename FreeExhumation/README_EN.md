# Free Exhumation

**Graveyard Keeper 2** BepInEx plugin: **exhume bodies without first removing the tombstone/fence, without an exhume certificate, and without consuming one.**

## Features

1. **Ignore decoration requirement**: exhume even while the tombstone/fence is still in place.
2. **Ignore exhume certificate**: exhume even with zero certificates in your inventory.
3. **Don't consume certificate** (configurable): by default nothing is deducted after exhuming.
4. **Auto-remove decorations** (configurable, on by default): while exhuming, returns the tombstone/fence to your inventory; if your inventory is full, the decoration stays on the grave — **it is never destroyed**.
5. **Optional skip confirmation dialog** (off by default): exhume immediately on click.

## Installation

1. Make sure the game has BepInEx installed (`BepInEx\core\BepInEx.Preloader.dll` exists).
2. Copy `FreeExhumation.dll` into `BepInEx\plugins\`.
3. Launch the game; the log should show:

```
[Free Exhumation] loaded. GUID: Narodum.gk2.freeexhumation v1.0.0
```

Uninstall: delete `BepInEx\plugins\FreeExhumation.dll` (the config file can be deleted too, see below).

## Configuration

On first launch the file `BepInEx\config\Narodum.gk2.freeexhumation.cfg` is generated. Changes take effect after restarting the game.

| Section | Key | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Master switch; `false` fully restores vanilla rules |
| Rules | `IgnoreDecorationRequirement` | `true` | Ignore "remove tombstone/fence first" |
| Rules | `RequireExhumeCertificate` | `false` | Set to `true` to restore the vanilla rule "an exhume certificate is required" |
| Rules | `ConsumeExhumeCertificate` | `false` | Whether to consume 1 certificate on exhume (only if one is present in inventory) |
| Rules | `AutoRemoveDecorations` | `true` | Return the tombstone/fence to inventory while exhuming |
| Rules | `SkipConfirmationDialog` | `false` | Skip the confirmation dialog and exhume immediately on click |
| Debug | `VerboseLogging` | `false` | Log every exhumation |
