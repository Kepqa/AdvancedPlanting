# AdvancedPlanting

Made by **kepqa** with **Claude Code**.

Plant along lines, arcs, circles, curves and filled shapes.

![A planted multi-row circle](images/preview-1-planted-circle.jpg)

## Features

- Five path types: line, arc, circle, curve, shape.
- Parallel rows, fill to the centre for closed shapes.
- Jitter and thinning for a natural look.
- Live preview: ghosts and grow-radius rings before planting.
- Vanilla costs: seeds, stamina, durability, skill.
- Partial planting when stamina runs out; optional auto-continue.
- Client-side only.
- Incompatible with other planting mods.

![Circle, 4 rows, hollow centre](images/preview-2-circle-hollow.jpg)

![Curve, 3 rows](images/preview-3-curve.jpg)

![Two nested curves](images/preview-5-two-curves.jpg)

## Controls

| Action | Key |
|---|---|
| Place point / plant | LMB |
| Finish curve / close shape | `Z` or click last / first point |
| Rows | Mouse wheel |
| Fill on/off | `G` |
| Clear figure | `Backspace` |
| Planting menu | `B` |

Keys are configurable in `BepInEx/config/kepqa.AdvancedPlanting.cfg`.

![Planting menu](images/preview-6-settings-wheel.jpg)

![Settings window](images/preview-4-settings-window.jpg)

## Translations

- Files: `Translations/<Language>.txt`, `key = value`.
- Included: English, Russian.
- New language: copy `English.txt`, rename to the game's language name.
