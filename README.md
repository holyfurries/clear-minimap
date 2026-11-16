# Clear Minimap

[Holy Furries Studio](https://holyfurries.com) ·
[Thunderstore](https://thunderstore.io/c/schedule-i/p/holyfurries/ClearMinimap/)
· [Source](https://github.com/holyfurries/clear-minimap)

A square minimap with a north-up map and an arrow that follows your view. It
shows teammates with names and distances, owned properties, active deals,
tracked objectives, and locations from the game's map.

Teammates, deals, and objectives stay visible at the edge when out of range. The
map covers 220 metres across at the default zoom and hides while the phone or
pause menu is open, except for its settings preview.

## Install

**IL2CPP only.** Requires **MelonLoader 0.7.3** and **ModSettings 0.3.0**.

Disable **SimpleMinimap** or other minimap mods. Install through **r2modman** or
**Thunderstore Mod Manager**, which installs ModSettings automatically. For
manual installation, copy `ClearMinimap.dll` and `ModSettings.dll` from their
release ZIPs into the game's `Mods/` folder.

## Settings

Open **Settings → Mods → Clear Minimap** to change visibility, size, zoom,
screen corner, and horizontal/vertical placement percentages. The map is 280
reference pixels wide by default, adjustable from 200 to 360. Zoom ranges from
0.5× to 4×. **PageUp** zooms in and **PageDown** zooms out by default while
playing. Change **Zoom in shortcut** and **Zoom out shortcut** in the same menu.
Click the control and press a key, use **Backspace** to disable it, or
**Escape** to cancel. Placement defaults to 1% of the available travel from the
selected corner. Use 0% for that edge, 50% to center, or 100% for the opposite
edge. Changes apply live and are saved in `UserData/MelonPreferences.cfg`.

Toggle teammates, properties, deals, objectives, places, NPCs, recruited
dealers, and owned vehicles independently. Dealers use a gold outline, and
vehicles use a blue outline. The vehicle you are riding uses the player marker.
All marker categories are enabled by default. Hiding a category also hides its
labels and edge pointers.

Turn off **Show name and distance labels** to hide the boxes with names and
distances next to teammates and deals. Their markers and edge pointers stay
visible.

**Show phone map NPCs** includes NPC icons visible on the native phone map and
is enabled by default. NPCs use the native icon and color, disappear outside the
minimap, and give way to deals, objectives, and other existing markers when
crowded. Open the phone map once if its NPC icons have not yet initialized.

Occasionally your player marker has a duck day. It still follows your view.

## Development

See
[BUILDING.md](https://github.com/holyfurries/clear-minimap/blob/main/BUILDING.md)
for builds, tests, packaging, and in-game checks.

[Code: MIT](https://github.com/holyfurries/clear-minimap/blob/main/LICENSE).
[Icon: studio asset terms](https://github.com/holyfurries/brand-assets/blob/main/LICENSE).
