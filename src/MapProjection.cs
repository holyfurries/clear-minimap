using System;
using System.Numerics;

namespace ClearMinimap;

internal static class MapProjection
{
    public static float size(float value) => float.IsFinite(value) ? Math.Clamp(value, 200f, 360f) : 280f;

    public static bool try_offset(Vector2 point, Vector2 center, Vector2 span, float size_pixels,
        bool edge_marker, out Vector2 offset, out bool at_edge)
    {
        offset = default;
        at_edge = false;
        if (!float.IsFinite(point.LengthSquared()) || !float.IsFinite(center.LengthSquared()) ||
            !float.IsFinite(span.LengthSquared()) || span.X <= 0 || span.Y <= 0 ||
            !float.IsFinite(size_pixels) || size_pixels < 32f) return false;
        offset = new Vector2((point.X - center.X) / span.X, -(point.Y - center.Y) / span.Y) * size_pixels;
        if (!float.IsFinite(offset.LengthSquared())) return false;
        float limit = size_pixels / 2f - 12f;
        float distance = Math.Max(Math.Abs(offset.X), Math.Abs(offset.Y));
        at_edge = distance > limit;
        if (!at_edge) return true;
        if (!edge_marker) return false;
        offset *= limit / distance;
        return true;
    }

    public static float heading(Vector2 direction)
    {
        if (!float.IsFinite(direction.LengthSquared()) || direction.LengthSquared() < 0.00000001f) return 0f;
        return MathF.Atan2(direction.X, direction.Y) * 180f / MathF.PI;
    }

    public static (int width, int height) texture_size(float width, float height, bool map)
    {
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Sprite dimensions must be finite and positive.");
        float scale = Math.Min(1f, (map ? 2048f : 256f) / Math.Max(width, height));
        return (Math.Max(1, (int)MathF.Round(width * scale)), Math.Max(1, (int)MathF.Round(height * scale)));
    }

    public static bool foreground_icon(string name)
    {
        return !name.Equals("Shade", StringComparison.OrdinalIgnoreCase) &&
            !name.Equals("Shadow", StringComparison.OrdinalIgnoreCase) &&
            !name.Equals("Background", StringComparison.OrdinalIgnoreCase) &&
            !name.Equals("Outline", StringComparison.OrdinalIgnoreCase);
    }

    public static bool objective_icon(string name)
    {
        return name.StartsWith("QuestPoI", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("DeaddropPoI", StringComparison.OrdinalIgnoreCase);
    }

    public static Vector2 world_position(Vector2 world, Vector2 origin, Vector2 axis_x, Vector2 axis_z)
    {
        return origin + axis_x * world.X + axis_z * world.Y;
    }

    public static bool clip(Vector2 center, Vector2 span, out Vector2 minimum, out Vector2 maximum)
    {
        minimum = default;
        maximum = default;
        if (!float.IsFinite(center.LengthSquared()) || !float.IsFinite(span.LengthSquared()) || span.X <= 0 || span.Y <= 0) return false;
        minimum = Vector2.Max(Vector2.Zero, center - span / 2f);
        maximum = Vector2.Min(Vector2.One, center + span / 2f);
        return minimum.X < maximum.X && minimum.Y < maximum.Y;
    }
}
