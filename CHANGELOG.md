# Changelog

## 0.2.1

- Planting wheel moved to its own key (B by default, configurable). The vanilla radial key works as before.
- Fill toggle for circle and shape: plant concentric rows to the centre. Without it, the row count and side apply, and for closed paths the sides read as inside and outside.
- New wheel icons: drawn at 256 px with a dark outline, simpler shapes that stay readable at wheel size.
- Every text is translatable: English and Russian built in, more languages or edits through text files in the Translations folder. The language follows the game setting.
- Grow-radius rings follow the slope of the ground.
- Removed "Finish figure" from the wheel; clicking the last point does the same.

## 0.2.0

- No hotkeys of its own anymore. Left click places points and plants; the vanilla radial key (G) opens a planting wheel while a seed is selected.
- Rubber-band preview: the cursor is the next control point, so the figure, rows and plants are visible before the click.
- Curves finish by clicking the last point again, shapes close by clicking the first point. Line, arc and circle finish on their last point.
- Planting wheel: single planting, path types, row side, undo, clear, finish, reshuffle, settings.
- Settings window with live sliders for spacing, rows, jitter and thinning, plus seed and stamina cost.
- Partial planting: plants nearest first while stamina and seeds last, keeps the rest of the figure, and plants it on the next click.
- Optional auto-continue: plants the rest one by one as stamina recovers, paying for each plant.
- Clearer preview: ghosts only where planting is possible, grow-radius rings coloured by status, planted spots hidden.
- Alt + mouse wheel parameter editing removed.
- Author set to kepqa.

## 0.1.0

- First version: curve planting along line, arc, circle, spline and shape fill with offset rows, jitter and thinning.
