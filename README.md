# AdvancedPlanting

Made by **kepqa** with **Claude Code**.

Plant along lines, arcs, circles, curves and filled shapes.

## Features

- Five path types: line, arc, circle, curve, shape.
- Parallel rows, fill to the centre for closed shapes.
- Jitter and thinning for a natural look.
- Live preview: ghosts and grow-radius rings before planting.
- Vanilla costs: seeds, stamina, durability, skill.
- Partial planting when stamina runs out; optional auto-continue.
- Client-side only.

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

## Translations

- Files: `Translations/<Language>.txt`, `key = value`.
- Included: English, Russian.
- New language: copy `English.txt`, rename to the game's language name.

## Building

- Set `ValheimPath` in `src/AdvancedPlanting/Local.props`.
- `dotnet build src/AdvancedPlanting -c Release` builds the DLL and the Thunderstore package in `dist/`.
