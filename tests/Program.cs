using System;
using System.Numerics;
using ClearMinimap;

internal static class Program
{
    private static void Main()
    {
        Vector2 center = new(0.5f, 0.5f);
        Vector2 span = new(0.2f, 0.2f);
        require(MapProjection.try_offset(center, center, span, 280, false, out Vector2 offset, out bool edge) && offset == Vector2.Zero && !edge, "Player stays at center");
        require(MapProjection.try_offset(new Vector2(0.5f, 0.55f), center, span, 280, false, out offset, out _) && Math.Abs(offset.Y + 70) < 0.01f, "Map north projects upward");
        require(MapProjection.try_offset(new Vector2(0.55f, 0.5f), center, span, 280, false, out offset, out _) && Math.Abs(offset.X - 70) < 0.01f, "East projects right");
        require(!MapProjection.try_offset(Vector2.One, center, span, 280, false, out _, out _), "Off-map static markers hidden");
        require(MapProjection.try_offset(Vector2.One, center, span, 280, true, out offset, out edge) && edge && Math.Abs(offset.X - 128) < 0.01f && Math.Abs(offset.Y + 128) < 0.01f, "Useful off-map markers clamp to square edge");
        require(!MapProjection.try_offset(center, center, Vector2.Zero, 280, true, out _, out _), "Invalid map scale rejected");
        require(!MapProjection.try_offset(new Vector2(float.NaN, 0), center, span, 280, true, out _, out _), "Invalid marker rejected");
        require(MapProjection.heading(Vector2.UnitY) == 0, "North heading");
        require(MapProjection.heading(Vector2.UnitX) == 90, "East heading");
        require(MapProjection.heading(-Vector2.UnitX) == -90, "West heading");
        require(Math.Abs(MapProjection.heading(-Vector2.UnitY)) == 180, "South heading");
        require(MapProjection.size(float.NaN) == 280 && MapProjection.size(1000) == 360 && MapProjection.size(0) == 200, "Size defaults and limits");
        require(MapProjection.clip(new Vector2(0.02f, 0.5f), span, out Vector2 minimum, out Vector2 maximum) && minimum.X == 0 && Math.Abs(maximum.X - 0.12f) < 0.001f, "Texture clips at map boundary instead of wrapping");
        require(!MapProjection.clip(new Vector2(2, 2), span, out _, out _), "No map drawn outside sprite");
        Vector2 moved_center = center + new Vector2(0.025f, 0);
        require(MapProjection.clip(center, span, out Vector2 before_minimum, out _) &&
            MapProjection.clip(moved_center, span, out Vector2 after_minimum, out _) &&
            Math.Abs(after_minimum.X - before_minimum.X - 0.025f) < 0.0001f, "Texture window follows movement");
        require(MapProjection.try_offset(center, moved_center, span, 280, false, out offset, out _) &&
            Math.Abs(offset.X + 35) < 0.01f, "Stationary landmark moves left when player moves east");
        Vector2 projection_origin = new(0.3f, 0.6f);
        Vector2 axis_x = new(0.002f, 0.001f);
        Vector2 axis_z = new(-0.001f, 0.002f);
        Vector2 last = MapProjection.world_position(Vector2.Zero, projection_origin, axis_x, axis_z);
        for (int frame = 1; frame <= 60; frame++)
        {
            Vector2 next = MapProjection.world_position(new Vector2(frame * 0.1f, 0), projection_origin, axis_x, axis_z);
            require(Vector2.Distance(next - last, axis_x * 0.1f) < 0.000001f, "Live marker movement advances every frame on rotated maps");
            last = next;
        }
        string[] deaddrop_layers = { "Shade", "Outline", "Fill", "Image" };
        string selected_layer = null;
        for (int i = deaddrop_layers.Length - 1; i >= 0; i--)
        {
            if (!MapProjection.foreground_icon(deaddrop_layers[i])) continue;
            selected_layer = deaddrop_layers[i];
            break;
        }
        require(selected_layer == "Image", "Deaddrop foreground selected instead of gradient shade");
        require(!MapProjection.foreground_icon("Background") && !MapProjection.foreground_icon("Shade") &&
            !MapProjection.foreground_icon("Outline") && MapProjection.foreground_icon("Fill"), "Quest backdrop layers excluded");
        require(MapProjection.objective_icon("DeaddropPoI(Clone)") &&
            MapProjection.objective_icon("DeaddropPoI_Red") &&
            MapProjection.objective_icon("QuestPoI(Clone)") &&
            !MapProjection.objective_icon("PropertyPoI"), "Pickup POIs receive objective edge markers");
        require(MapProjection.texture_size(512, 512, false) == (256, 256), "Phone symbols use bounded filtered copies");
        require(MapProjection.texture_size(512, 288, false) == (256, 144), "Phone symbol aspect ratio is preserved");
        require(MapProjection.texture_size(64, 32, false) == (64, 32), "Small sprites are not enlarged");
        require(MapProjection.texture_size(8192, 4096, true) == (2048, 1024), "Map texture memory is bounded");
        var motion = new MapMotion();
        require(motion.sample(1, Vector2.Zero, 0, 0) == Vector2.Zero, "Initial movement sample is exact");
        Vector2 smoothed = motion.sample(1, Vector2.One, 1, 1f / 60);
        require(smoothed.X > 0 && smoothed.X < 1, "Movement steps are smoothed without overshoot");
        require(motion.sample(1, new Vector2(2), 1, 1f / 60) == smoothed, "Only one movement sample per frame");
        require(motion.sample(1, new Vector2(100), 2, 2f / 60) == new Vector2(100), "Teleports snap immediately");
        require(motion.sample(1, new Vector2(101), 3, 1) == new Vector2(101), "Resume after hiding does not drift");
        motion.reset();
        require(motion.sample(1, new Vector2(10), 0, 0) == new Vector2(10), "Scene reset discards movement history");
        foreach (int rate in new[] { 30, 60, 144 })
        {
            motion.reset();
            float previous_position = 0;
            for (int frame = 0; frame < rate; frame++)
            {
                float time = (float)frame / rate;
                float stepped_position = MathF.Floor(time * 50) / 50 * 6;
                Vector2 result = motion.sample(1, new Vector2(stepped_position, 0), frame, time);
                require(result.X >= previous_position && result.X <= stepped_position, "Physics-step movement remains monotonic");
                require(stepped_position - result.X < 0.4f, "Steady movement lag stays below 40 cm at 6 m/s");
                previous_position = result.X;
            }
        }
        motion.reset();
        for (int identity = 0; identity < 200; identity++)
            require(motion.sample(identity, new Vector2(identity, 0), identity, identity) == new Vector2(identity, 0), "Marker churn remains bounded and starts at current position");
        Console.WriteLine("Map projection, heading, edge markers, texture clipping, and size checks passed.");
    }

    private static void require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
