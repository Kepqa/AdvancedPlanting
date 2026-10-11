# Changelog

## 0.4.6

- New setting StaminaCostFactor: stamina per plant in figure mode, default 0.5 (half of vanilla).
- MaxPlants default raised from 300 to 500. Existing config files keep their value.
- README lists both settings.
- Server settings: with the mod on the server, players use its StaminaCostFactor, MaxPlants and MaxFigureSize (EnforceOnClients, default on). No extra dependency.
- Repeat figure (Y): moves a finished figure under the cursor, or places the last planted or cleared figure again.
- Area harvest: hold Shift and pick a ripe crop to pick every crop of the same kind within 5 m. Configurable or off in [3. Harvest]; the server can enforce Enabled and Radius.
- Feedback: bug reports and ideas on GitHub Issues, linked from the mod page.
- The seed counter no longer turns red: it only sees the inventory, and seeds may come from containers.
- Planting works with mods that take seeds from nearby containers: seeds are checked and taken through the game's own requirement calls, and the mod pays for every plant of a figure itself.

## 0.4.5

- Mod page and package description cover only the mod.

## 0.4.4

- Translations: Ukrainian, German, French, Spanish, Portuguese (Brazil), Polish, Chinese (Simplified), Japanese.
- Russian: the "ready" message now agrees with every path name.
- README: badges and a donate link.

## 0.4.3

- Declared incompatible with other planting mods: BepInEx skips AdvancedPlanting when one is installed and logs the reason.

## 0.4.2

- Shorter README and package description.
- Depends on BepInExPack_Valheim 5.4.2351.

## 0.4.1

- Fix: the 0.4.0 package kept the DLLs in a nested plugins/ folder, so the mod was not installed where BepInEx looks and did not load. DLLs are back at the package root.

## 0.4.0

- New settings window and status panel design: dark wood and bronze artwork drawn by the mod, the game's bold Averia font, high-contrast text, icon buttons for path types, custom sliders and checkboxes.
- Figure hints now appear above the game's own key hints at the bottom right, in the same style.
- Quick controls: mouse wheel changes the number of rows, Z finishes a curve or closes a shape, G toggles fill, Backspace clears the figure. All configurable.
- No more stall the first time the planting menu opens: icons and window artwork are prepared on a background thread at start-up.
- Wheel icons are slightly smaller; the fill icon is clearer.
- Status panel, key hints and preview update less often internally, reducing per-frame work.
- Fixes found while debugging: key hints no longer depend on the internal layout of the vanilla hint list (they did not show in 0.3.0); wheel icons are recreated if the game unloads them between worlds; a planting click no longer validates the whole figure twice; Thunderstore manifest is written without a byte order mark.
- Plugin GUID is now kepqa.AdvancedPlanting (config file kepqa.AdvancedPlanting.cfg).

## 0.3.0

- Settings window rebuilt from the game's own UI (inventory panel background, buttons, toggles and sliders), so it matches Valheim and scales with the game's UI.
- Figure hints in the standard key-hint list at the bottom right while building; the status panel keeps them only when key hints are off in the game settings.
- Circle is drawn with two clicks: centre, then edge.
- MaxFigureSize setting (default 80 m): figures larger than this are not laid out at all, are shown as a red outline, and clicks that would exceed it are refused, so a stray click can no longer freeze the game.
- Much faster preview: offset rows use a spatial grid and a coarse curve (worst case 180 ms -> 10 ms per rebuild), fill rows stop at the plant cap, rebuilds are throttled to 80 ms, game-rule checks are spread over frames, ghosts are created gradually, and the preview is redrawn only when something changed.
- Uses the game's localization API directly for the language and key names.

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

## 0.1.0

- First version: curve planting along line, arc, circle, spline and shape fill with offset rows, jitter and thinning.
