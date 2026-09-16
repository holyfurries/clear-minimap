using System;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Map;
using UnityEngine;
using Vector = System.Numerics.Vector2;

namespace ClearMinimap;

internal sealed class MinimapView
{
    private readonly SpriteTextures sprite_textures = new();
    private readonly MapMotion motion = new();
    private readonly Texture2D?[] icons = new Texture2D?[5];
    private readonly MapOverlay overlay = new();
    private readonly Rect[] labels = new Rect[12];
    private readonly Vector2[] placed_markers = new Vector2[96];
    private readonly MapMarker[] visible_markers = new MapMarker[96];
    private readonly Color[] marker_colors = new Color[96];
    private float heading_degrees;
    private readonly Color background = new(0.055f, 0.07f, 0.085f, 0.97f);

    public void reset()
    {
        sprite_textures.reset();
        motion.reset();
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] != null) UnityEngine.Object.Destroy(icons[i]);
            icons[i] = null;
        }
        Array.Clear(visible_markers, 0, visible_markers.Length);
        overlay.reset();
        heading_degrees = 0;
    }

    public void hide() => overlay.hide();

    public void draw(Rect panel, bool map_available, float scale)
    {
        overlay.begin(scale);
        try { draw_content(panel, map_available); }
        finally { overlay.end(); }
    }

    private void draw_content(Rect panel, bool map_available)
    {
        for (int i = 0; i < icons.Length; i++) icons[i] ??= create_icon(i);
        overlay.box(new Rect(panel.x - 3, panel.y - 3, panel.width + 6, panel.height + 26), background);
        overlay.box(new Rect(panel.x - 1, panel.y - 1, panel.width + 2, panel.height + 2), new Color(0.3f, 0.37f, 0.4f, 1));
        overlay.box(panel, new Color(0.09f, 0.12f, 0.14f, 1));
        if (!map_available)
        {
            overlay.label(panel, "Map preview", Color.white, true);
            draw_arrow(panel.center, 0, 16, Color.white);
            return;
        }
        Player player = Player.Local;
        MapApp app = MapApp.Instance;
        Sprite sprite = app.BackgroundImage.sprite != null ? app.BackgroundImage.sprite : app.MainMapSprite;
        if (sprite == null || sprite.texture == null) return;
        RectTransform container = app.PoIContainer;
        Rect image_rect = app.BackgroundImage.rectTransform.rect;
        Matrix4x4 container_to_image = map_transform(container, app.BackgroundImage.rectTransform);
        Vector3 anchor = new((0.5f - container.pivot.x) * container.rect.width,
            (0.5f - container.pivot.y) * container.rect.height, 0);
        Vector origin_uv = map_position(Vector3.zero, container_to_image, anchor, image_rect);
        Vector axis_x = (map_position(Vector3.right * 100f, container_to_image, anchor, image_rect) - origin_uv) / 100f;
        Vector axis_z = (map_position(Vector3.forward * 100f, container_to_image, anchor, image_rect) - origin_uv) / 100f;
        int frame = Time.frameCount;
        float seconds = Time.unscaledTime;
        Vector player_position = motion.sample(long.MinValue,
            new Vector(player.transform.position.x, player.transform.position.z), frame, seconds);
        Vector center = MapProjection.world_position(player_position, origin_uv, axis_x, axis_z);
        Vector span = new(220f * MathF.Sqrt(axis_x.X * axis_x.X + axis_z.X * axis_z.X),
            220f * MathF.Sqrt(axis_x.Y * axis_x.Y + axis_z.Y * axis_z.Y));
        if (!float.IsFinite(span.LengthSquared()) || span.X < 0.00001f || span.Y < 0.00001f) return;
        if (MapProjection.clip(center, span, out Vector minimum, out Vector maximum))
        {
            Vector origin = center - span / 2;
            Rect target = new(panel.x + (minimum.X - origin.X) / span.X * panel.width,
                panel.y + (origin.Y + span.Y - maximum.Y) / span.Y * panel.height,
                (maximum.X - minimum.X) / span.X * panel.width, (maximum.Y - minimum.Y) / span.Y * panel.height);
            Rect texture_uv = new(minimum.X, minimum.Y, maximum.X - minimum.X, maximum.Y - minimum.Y);
            overlay.texture(target, sprite_textures.get(sprite, true), texture_uv, Color.white);
        }
        int label_count = 0;
        int placed_count = 0;
        for (int i = 0; i < MapMarkers.count; i++)
        {
            MapMarker marker = MapMarkers.markers[i];
            bool important = marker.kind is MarkerKind.Teammate or MarkerKind.Deal or MarkerKind.Objective;
            Vector3 position = marker.target != null ? marker.target.position : marker.position;
            Vector marker_position = motion.sample(marker.source.ToInt64(), new Vector(position.x, position.z), frame, seconds);
            if (!MapProjection.try_offset(MapProjection.world_position(marker_position, origin_uv, axis_x, axis_z), center, span, panel.width,
                important, out Vector offset, out _)) continue;
            Vector2 point = new(panel.center.x + offset.X, panel.center.y + offset.Y);
            bool crowded = false;
            for (int j = 0; j < placed_count; j++)
                if ((placed_markers[j] - point).sqrMagnitude < 100f) crowded = true;
            if (crowded && !important) continue;

            Color color = marker.kind switch
            {
                MarkerKind.Teammate => new Color(0.35f, 0.8f, 1f, 1),
                MarkerKind.Deal => new Color(0.35f, 1f, 0.55f, 1),
                MarkerKind.Objective => new Color(1f, 0.86f, 0.3f, 1),
                MarkerKind.Home => new Color(1f, 0.86f, 0.5f, 1),
                _ => Color.white
            };
            placed_markers[placed_count] = point;
            visible_markers[placed_count] = marker;
            marker_colors[placed_count] = color;
            placed_count++;
            if (marker.kind == MarkerKind.Objective || !important || label_count >= labels.Length) continue;
            int distance = (int)Math.Min(9999f, Vector3.Distance(player.transform.position, position));
            string text = $"{marker.label} · {distance}m";
            Rect label = new(Math.Clamp(point.x + 11, panel.x + 2, panel.xMax - 132),
                Math.Clamp(point.y - 8, panel.y + 2, panel.yMax - 18), 130, 16);
            bool overlap = false;
            for (int j = 0; j < label_count; j++) if (labels[j].Overlaps(label)) overlap = true;
            if (overlap) continue;
            labels[label_count++] = label;
            overlay.box(label, new Color(0.03f, 0.045f, 0.06f, 0.8f));
            overlay.label(label, text, color);
        }
        for (int i = 0; i < placed_count; i++)
        {
            draw_marker(placed_markers[i], visible_markers[i], marker_colors[i]);
            visible_markers[i] = default;
        }
        Vector3 forward = PlayerCamera.InstanceExists && PlayerCamera.Instance.Camera != null
            ? PlayerCamera.Instance.Camera.transform.forward : player.transform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.001f)
        {
            Vector direction = axis_x * forward.x + axis_z * forward.z;
            heading_degrees = MapProjection.heading(new Vector(direction.X / span.X, direction.Y / span.Y));
        }
        draw_arrow(panel.center, heading_degrees, 16, Color.white);
        string time = TimeManager.InstanceExists ? $"{TimeManager.Instance.CurrentTime / 100:00}:{TimeManager.Instance.CurrentTime % 100:00}" : "";
        overlay.label(new Rect(panel.x + 6, panel.yMax + 3, panel.width - 12, 18), time, Color.white);
    }

    private static Matrix4x4 map_transform(Transform container, Transform image)
    {
        Transform ancestor = image;
        Matrix4x4 image_to_ancestor = Matrix4x4.identity;
        for (int i = 0; i < 32 && ancestor != null; i++)
        {
            if (container == ancestor || container.IsChildOf(ancestor)) break;
            image_to_ancestor = Matrix4x4.TRS(ancestor.localPosition, ancestor.localRotation, ancestor.localScale) * image_to_ancestor;
            ancestor = ancestor.parent;
        }
        if (ancestor == null || (container != ancestor && !container.IsChildOf(ancestor)))
            throw new InvalidOperationException("Map image and marker container have no shared ancestor within 32 levels.");
        Matrix4x4 container_to_ancestor = Matrix4x4.identity;
        Transform current = container;
        for (int i = 0; i < 32 && current != ancestor && current != null; i++)
        {
            container_to_ancestor = Matrix4x4.TRS(current.localPosition, current.localRotation, current.localScale) * container_to_ancestor;
            current = current.parent;
        }
        if (current != ancestor) throw new InvalidOperationException("Map marker hierarchy exceeds 32 levels.");
        return image_to_ancestor.inverse * container_to_ancestor;
    }

    private static Vector map_position(Vector3 position, Matrix4x4 container_to_image, Vector3 anchor, Rect rect)
    {
        Vector2 mapped = MapPositionUtility.Instance.GetMapPosition(position);
        Vector3 local = container_to_image.MultiplyPoint3x4(new Vector3(mapped.x, mapped.y, 0) + anchor);
        if (rect.width <= 0 || rect.height <= 0) return new Vector(float.NaN);
        return new Vector((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
    }

    private void draw_marker(Vector2 point, MapMarker marker, Color color)
    {
        Rect rect = new(point.x - 8, point.y - 8, 16, 16);
        if (marker.sprite != null && marker.sprite.texture != null &&
            (!marker.sprite.packed || marker.sprite.packingMode == SpritePackingMode.Rectangle))
        {
            Texture texture = sprite_textures.get(marker.sprite, false);
            float aspect = (float)texture.width / texture.height;
            if (aspect > 1) { rect.height /= aspect; rect.y = point.y - rect.height / 2; }
            else { rect.width *= aspect; rect.x = point.x - rect.width / 2; }
            overlay.texture(rect, texture, new Rect(0, 0, 1, 1), marker.tint);
        }
        else overlay.texture(rect, icons[marker.kind == MarkerKind.Home ? 1 : marker.kind == MarkerKind.Deal ? 2 : marker.kind == MarkerKind.Objective ? 4 : 3], new Rect(0, 0, 1, 1), color);
    }

    private void draw_arrow(Vector2 point, float angle, float size, Color color)
    {
        overlay.texture(new Rect(point.x - size / 2, point.y - size / 2, size, size),
            icons[0], new Rect(0, 0, 1, 1), color, angle);
    }

    private static Texture2D create_icon(int kind)
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                int covered = 0;
                for (int sample = 0; sample < 16; sample++)
                {
                    float horizontal = (x + (sample % 4 + 0.5f) / 4f - 16f) / 16f;
                    float vertical = (y + (sample / 4 + 0.5f) / 4f - 16f) / 16f;
                    bool inside = kind switch
                    {
                        0 => vertical > -0.7f && vertical < 0.85f && Math.Abs(horizontal) < (0.85f - vertical) * 0.48f &&
                            (vertical > -0.15f || Math.Abs(horizontal) > (-0.15f - vertical) * 0.4f),
                        1 => (Math.Abs(horizontal) < 0.65f && vertical > -0.7f && vertical < 0.2f &&
                            !(Math.Abs(horizontal) < 0.18f && vertical < -0.1f)) ||
                            (vertical >= 0.2f && vertical < 0.85f && Math.Abs(horizontal) < 0.85f - vertical),
                        2 => (Math.Abs(horizontal) < 0.75f && vertical > -0.55f && vertical < 0.4f) ||
                            (Math.Abs(horizontal) < 0.35f && vertical >= 0.4f && vertical < 0.65f),
                        4 => Math.Abs(horizontal) < 0.18f &&
                            ((vertical > -0.15f && vertical < 0.8f) || (vertical > -0.75f && vertical < -0.4f)),
                        _ => horizontal * horizontal + vertical * vertical < 0.45f
                    };
                    if (inside) covered++;
                }
                texture.SetPixel(x, y, new Color(1, 1, 1, covered / 16f));
            }
        }
        texture.Apply(false, true);
        return texture;
    }
}
