# AdvancedPlanting

![Version](https://img.shields.io/badge/version-0.4.6-ffcc00?style=for-the-badge&labelColor=555555) ![Valheim](https://img.shields.io/badge/valheim-1.0.17-f07c34?style=for-the-badge&labelColor=555555) ![Platform](https://img.shields.io/badge/platform-client%20%7C%20server%20%28optional%29-0a7bc2?style=for-the-badge&labelColor=555555) ![Languages](https://img.shields.io/badge/languages-EN%20%7C%20RU%20%7C%20UA%20%7C%20DE%20%7C%20FR%20%7C%20ES%20%7C%20PT%20%7C%20PL%20%7C%20ZH%20%7C%20JA-2ea44f?style=for-the-badge&labelColor=555555) [![Donate](https://img.shields.io/badge/boosty-donate-f15f2c?style=for-the-badge&logo=boosty&logoColor=white&labelColor=555555)](https://boosty.to/kepqa/donate)

<!-- repo-only -->
Made by **kepqa** with **Claude Code**.
<!-- /repo-only -->

Plant a whole bed in one click: rows along lines, arcs, circles, free curves and closed shapes, with a live preview. Harvest it all back with `Shift` + `E`.

![A planted multi-row circle](images/preview-1-planted-circle.jpg)

## Quick start

1. Equip the **Cultivator** and pick a seed. Figure mode is on by default.
2. **Click** to place points. The preview follows the cursor: ghosts where plants go, rings for their grow radius.
3. Line, arc and circle finish by themselves. A curve or shape finishes when you click its last or first point again, or press `Z`.
4. **Mouse wheel** adds or removes rows. `G` fills a circle or shape to the centre.
5. **Click** once more: the whole figure is planted.
6. When it is ripe: hold `Shift` and press `E` on one crop, and every ripe crop of that kind within 5 m is picked.

## Shapes

| Shape | Clicks | Finishes |
|---|---|---|
| Line | start, end | by itself |
| Arc | start, middle, end | by itself |
| Circle | centre, edge | by itself |
| Curve | any number of points | click the last point again, or `Z` |
| Shape | 3 or more points | click the first point, or `Z` |

![Circle, 4 rows, hollow centre](images/preview-2-circle-hollow.jpg)

![Curve, 3 rows](images/preview-3-curve.jpg)

![Two nested curves](images/preview-5-two-curves.jpg)

## Controls

Cultivator in hand, seed selected:

| Key | Action |
|---|---|
| `LMB` | Place a point / plant the figure |
| `Mouse wheel` | Rows: more / fewer |
| `Z` | Finish a curve, close a shape |
| `G` | Fill a circle or shape: on / off |
| `Y` | Move the figure under the cursor / repeat the last figure |
| `Backspace` | Clear the figure |
| `B` | Planting menu: shape, side of extra rows, fill, undo point, settings |

Anywhere:

| Key | Action |
|---|---|
| `Shift` + `E` on a ripe crop | Pick every ripe crop of that kind within 5 m |

The keys for the current step are shown in game, bottom right.

![Planting menu](images/preview-6-settings-wheel.jpg)

![Settings window](images/preview-4-settings-window.jpg)

## Costs

- Every plant costs seeds, tool durability and gives skill, as in vanilla.
- Stamina per plant is half the vanilla cost.
- Short of stamina or seeds: the nearest plants go in first, the rest stay in the preview. Click again later, or turn on auto-continue in the settings window.

## Settings

File: `BepInEx/config/kepqa.AdvancedPlanting.cfg`. All keys above can be changed there too.

| Setting | Default | Meaning |
|---|---|---|
| `MaxPlants` | 500 | Plants per figure |
| `MaxFigureSize` | 80 m | Largest figure |
| `StaminaCostFactor` | 0.5 | Stamina per plant: 1 = vanilla, 0 = free |
| `[3. Harvest] Enabled` | on | Area harvest on / off |
| `[3. Harvest] Modifier` | LeftShift | Key to hold while picking |
| `[3. Harvest] Radius` | 5 m | Area around the picked crop |
| `[3. Harvest] SameTypeOnly` | on | Off also picks berries, mushrooms, flint and other pickables |
| `[4. Server] EnforceOnClients` | on | On a server: players use its stamina, size and harvest settings |

## Multiplayer

- Works client-side. Installing it on the server is optional and makes the server's settings apply to every player.
- Incompatible with other planting mods.

## Translations

- English, Russian, Ukrainian, German, French, Spanish, Portuguese (Brazil), Polish, Chinese (Simplified), Japanese.
- Files: `Translations/<Language>.txt`, `key = value`. New language: copy `English.txt` under the game's language name.
