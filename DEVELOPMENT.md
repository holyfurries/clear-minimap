# Development

Targets Schedule I 0.4.6f13 IL2CPP and MelonLoader 0.7.3.
See [BUILDING.md](BUILDING.md) for local build instructions.

## Rendering

The native MapPositionUtility supplies map coordinates. The map image and POI
container's local transforms convert them to sprite positions, stopping at their
common ancestor. Hierarchy traversal is capped at 32 levels.

A ScreenSpaceOverlay canvas renders the north-up map, symbols and labels in
LateUpdate. It has no camera or raycaster. Pools are capped at 144 images and
16 labels; unused elements are disabled. The 16-pixel player arrow follows the
camera's horizontal view direction and keeps its previous heading when looking
straight up or down.

The display is 280 reference pixels wide by default, adjustable from 200 to 360,
with a 16-pixel top/right margin. Sizes scale with screen height. The visible
area is 220 metres across. The clock appears below the map; objective symbols
have no text labels.

SpriteTextures creates cropped, mipmapped GPU copies with trilinear filtering.
The native phone textures are never modified. One map slot is capped at
2048x2048; 127 reusable icon slots are capped at 256x256. Their maximum combined
allocation is approximately 64 MiB including mip chains. Scene unload releases
these textures and destroys the canvas.

## Markers and movement

POI enable/disable hooks maintain a 256-slot registry. Discovery runs at 2 Hz,
bounded to 16 players, 32 contracts, 32 owned properties, 32 quests with up to
16 entries each, and 96 resulting markers. Dealer-assigned contracts and generic
NPC POIs are omitted. Native visibility and UI initialization affect availability.

QuestPoI and DeaddropPoI prefabs count as objectives, including pickup markers
not owned by a tracked QuestEntry. Foreground selection skips Shade, Shadow,
Background and Outline layers. Native foreground sprites preserve tint and
aspect ratio. Generated symbols are fallbacks where no usable sprite exists.

Teammates, deals and objectives remain at the map edge when out of range.
Low-priority crowded symbols are omitted. Up to twelve teammate/deal labels
use overlap checks; symbols render above labels.

MapMotion follows the map centre and marker positions with a 45 ms time
constant, using at most 97 identity-keyed histories and one sample per frame.
Teleports over 30 metres, gaps over 250 ms, and scene changes reset history.
The map crop and markers share the same followed centre. This introduces a
small positional lag and never changes gameplay transforms.

## Settings and lifecycle

Size and visibility use MelonPreferences with a 0.75-second save delay.
An IMGUI settings card appears with the minimap preview while Settings is open.
The gameplay overlay otherwise hides during pause, phone use and death.

Known competing minimap mods disable this overlay. API failures hide it and
log once per scene. Objective-count changes produce diagnostic logs.
The mod adds no network messages or save-game data.

## Verification

    dotnet run --project tests/Tests.csproj
    dotnet run --project verification/InteropCheck.csproj -p:MelonLoaderDir="$MELONLOADER_DIR" -- bin/Release/net6.0/ClearMinimap.dll "$MELONLOADER_DIR/Il2CppAssemblies"

The standalone tests cover projection, heading, edge clipping, texture sizes,
foreground selection, movement smoothing and bounded marker churn. The interop
check detects known throwing wrappers and invalid reference-wrapper initialization.
It also rejects immediate-mode drawing calls in the gameplay renderer.

In-game checks remain necessary:

- Compare landmarks with the phone map before and after opening it.
- Walk, strafe and turn in place; check map and marker stability.
- Verify cardinal arrow directions, vehicle free-look and vertical camera views.
- Check starting cash pickups, tracked objectives, deals and teammates at the edge.
- Test completing/untracking objectives, dealer assignment and property purchases.
- Compare four-player positions and markers, including joining and leaving.
- Test Settings, saved size, phone/pause/death visibility and scene reload.
- Check 720p, 1080p, 1440p and ultrawide layouts against other HUD elements.

Successful compilation and automated checks do not establish native rendering
or multiplayer behavior. The screen-space renderer still requires runtime validation.
Game assets are referenced at runtime and are not distributed.

## 0.1.12

Marker labels use a 10-point font instead of 12. The clock and preview retain 12 points.
Font size is set on every pooled label draw so reused labels cannot inherit the wrong size.

## 0.1.13 daily marker

The local player code and TimeManager.ElapsedDays deterministically choose a duck on
one percent of in-game days. Reloading that day preserves the outcome; the next day
uses a new score. The generated glyph faces upward at zero heading and rotates with
the existing camera-heading transform. Its forward beak supplies the direction cue.
It uses the existing bounded texture pool and cleanup, and never changes map projection
or world movement. Labels retain the smaller font from 0.1.12. Pure checks cover stable
selection and probability over 10,000 days; visual clarity and native day rollover require
game testing. Marker choice is personal, not shared across all players.
