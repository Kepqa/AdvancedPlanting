# AdvancedPlanting

![Version](https://img.shields.io/badge/version-0.4.6-ffcc00?style=for-the-badge&labelColor=555555) ![Valheim](https://img.shields.io/badge/valheim-1.0.17-f07c34?style=for-the-badge&labelColor=555555) ![Platform](https://img.shields.io/badge/platform-client-0a7bc2?style=for-the-badge&labelColor=555555) ![Languages](https://img.shields.io/badge/languages-EN%20%7C%20RU%20%7C%20UA%20%7C%20DE%20%7C%20FR%20%7C%20ES%20%7C%20PT%20%7C%20PL%20%7C%20ZH%20%7C%20JA-2ea44f?style=for-the-badge&labelColor=555555) [![Donate](https://img.shields.io/badge/boosty-donate-f15f2c?style=for-the-badge&logo=boosty&logoColor=white&labelColor=555555)](https://boosty.to/kepqa/donate)

<!-- repo-only -->
Made by **kepqa** with **Claude Code**.
<!-- /repo-only -->

Plant along lines, arcs, circles, curves and filled shapes.

![A planted multi-row circle](images/preview-1-planted-circle.jpg)

## Features

- Five path types: line, arc, circle, curve, shape.
- Parallel rows, fill to the centre for closed shapes.
- Jitter and thinning for a natural look.
- Live preview: ghosts and grow-radius rings before planting.
- Vanilla costs: seeds, durability, skill; stamina at half the vanilla cost by default.
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

## Settings

Edit `BepInEx/config/kepqa.AdvancedPlanting.cfg`:
- `MaxPlants`: plants per figure, default 500.
- `StaminaCostFactor`: stamina per plant in figure mode, default 0.5 (half of vanilla, 1 = vanilla, 0 = free).
- Keys and display options.

![Planting menu](images/preview-6-settings-wheel.jpg)

![Settings window](images/preview-4-settings-window.jpg)

## Translations

- Files: `Translations/<Language>.txt`, `key = value`.
- Included: English, Russian, Ukrainian, German, French, Spanish, Portuguese (Brazil), Polish, Chinese (Simplified), Japanese.
- New language: copy `English.txt`, rename to the game's language name.
