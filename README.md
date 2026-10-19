# Clear Minimap

A square minimap with a north-up map and an arrow that follows your view. It
shows teammates with names and distances, owned properties, active deals,
tracked objectives, and locations from the game's map.

Teammates, deals, and objectives stay visible at the edge when out of range. The
map covers 220 metres across and hides while the phone or pause menu is open,
except for its settings preview.

## Install

**IL2CPP only.** Requires **MelonLoader 0.7.3** and **ModSettings 0.2.0**.

Disable **SimpleMinimap** or other minimap mods. Install through **r2modman** or
**Thunderstore Mod Manager**, which installs ModSettings automatically. For
manual installation, copy `ClearMinimap.dll` and `ModSettings.dll` from their
release ZIPs into the game's `Mods/` folder.

## Settings

Open **Settings → Mods → Clear Minimap** to change visibility and size. The map
is 280 reference pixels wide by default, adjustable from 200 to 360. Changes
apply live and are saved in `UserData/MelonPreferences.cfg`.

Occasionally your player marker has a duck day. It still follows your view.

## Development

See
[BUILDING.md](https://github.com/holyfurries/clear-minimap/blob/main/BUILDING.md)
for builds, tests, packaging, and in-game checks.

[MIT license](https://github.com/holyfurries/clear-minimap/blob/main/LICENSE).
