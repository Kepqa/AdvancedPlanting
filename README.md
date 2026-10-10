# AdvancedPlanting

Plant along lines, arcs, circles, free curves and filled shapes instead of a fixed grid. Every figure becomes a set of curved parallel rows, with optional natural jitter and thinning so fields do not look ruler-straight. A live preview shows every plant before you spend a single seed.

By **kepqa**. Client-side BepInEx plugin, works in single player and on servers without being installed on the server or by other players.

## How it works

Pick a seed or sapling with the cultivator. The mod starts in figure mode.

1. **Click** to place points. The cursor acts as the next point, so the figure, its rows and every future plant follow the mouse before you commit.
2. **Finish** the figure. A line (2 points), arc (start, middle, end) and circle (centre, then edge) finish on their own. A curve finishes when you click its last point again, and a shape closes when you click its first point. The point that would finish the figure is highlighted in blue.
3. **Click** again to plant.

Every plant costs exactly what vanilla charges: seeds, stamina, tool durability, and it gives farming skill. When stamina or seeds run out, the nearest plants are planted and the rest of the figure stays. Rest and click again to finish it, or turn on **auto-continue**, which plants the rest one by one as stamina recovers.

## Controls

| Action | Default |
|---|---|
| Place point, plant the finished figure | Left mouse button |
| Finish a curve or close a shape | `Z`, or click its last or first point |
| Number of rows | Mouse wheel |
| Fill a circle or shape to the centre | `G` |
| Clear the figure | `Backspace` |
| Planting menu | `B` |

The keys are free in build mode: the game does not use `B` or `Z`, and `G` (the radial menu key) does nothing while building. All keys can be changed or disabled in the config. The current actions are listed with the game's own key hints at the bottom right.

## Planting menu

A radial menu in the style of the emote wheel:

- ordinary single planting, which turns figure mode off;
- path type: line, arc, circle, curve, shape;
- side of the extra rows (both, left or right; inside or outside for closed shapes);
- fill on or off;
- undo point, clear figure;
- reshuffle the random pattern;
- settings window: path, fill, row side, spacing, rows, jitter, thinning, auto-continue, and the seed and stamina cost of the figure.

## Preview

- A ghost plant stands wherever planting is possible.
- Each plant has a ring showing its grow radius: green can be planted, red cannot (the reason is shown in the status panel), grey is still being checked.
- Plants never get closer than their grow radius plus a margin, so they never block each other's growth. Jitter only moves plants within that limit, so it shows once spacing is larger than the minimum.
- Places where the plant would wither are refused even if vanilla allows planting: wrong biome, Ashlands heat, mountain cold.
- A figure larger than `MaxFigureSize` (80 m by default) is shown as a red outline and is not laid out, so a stray click far away cannot freeze the game.

## Configuration

`BepInEx/config/kepqa.AdvancedPlanting.cfg`, also editable with a config manager.

| Setting | Default | Meaning |
|---|---|---|
| PlantingWheel, FinishFigure, ToggleFill, ClearFigure | B, Z, G, Backspace | Keys; `None` disables one |
| RowsOnMouseWheel | true | Mouse wheel changes rows in figure mode |
| EnabledByDefault | true | Start in figure mode |
| DefaultSpacingFactor | 2.0 | Initial spacing = grow radius × factor |
| MinDistanceMargin | 0.35 | Minimum distance = grow radius + margin |
| MaxPlants | 300 | Cap on plants per figure |
| MaxFigureSize | 80 | Largest figure in metres |
| RandomRotation | true | Random yaw for each plant |
| AutoContinueInterval | 0.6 | Seconds between auto-continue plants |
| ShowHud, ShowCurve, ShowGrowRadius | true | Status panel and preview overlays |

## Translations

All texts come from `plugins/Translations/<Language>.txt` next to the DLL, in `key = value` lines. The file name is the language name from the game settings: `English`, `Russian`, `German`, and so on. English and Russian are included. To add a language, copy `English.txt`, rename it and translate the values; it is picked up with the game's language setting.

## Compatibility

- Client-side only; other players and the server do not need it.
- Do not combine with other mods that change how planting works (for example PlantEasily): both would handle the same click.

## Building from source

Requires the .NET SDK and a Valheim install with BepInEx. Create `src/AdvancedPlanting/Local.props`:

```xml
<Project>
  <PropertyGroup>
    <ValheimPath>D:\Steam\steamapps\common\Valheim</ValheimPath>
    <CopyToGame>true</CopyToGame>
  </PropertyGroup>
</Project>
```

Then `dotnet build src/AdvancedPlanting -c Release`. A Release build also writes the Thunderstore package to `dist/`. Geometry tests: `dotnet test tests/AdvancedPlanting.Core.Tests`.
