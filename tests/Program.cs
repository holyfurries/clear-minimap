using System;
using System.Numerics;
using ClearMinimap;

internal static class Program
{
    private static void Main()
    {
        MarkerKind[] kinds = Enum.GetValues<MarkerKind>();
        MarkerVisibility all = new(true, true, true, true, true, true, true, true);
        MarkerVisibility none = new(false, false, false, false, false, false, false, false);
        MarkerVisibility[] hidden =
        {
            all with { places = false }, all with { properties = false }, all with { deals = false },
            all with { teammates = false }, all with { objectives = false }, all with { npcs = false },
            all with { dealers = false }, all with { vehicles = false }
        };
        require(kinds.Length == hidden.Length, "Every marker category has an independent toggle");
        for (int i = 0; i < kinds.Length; i++)
        {
            require(all.allows(kinds[i]), "All marker categories are visible by default");
            require(!none.allows(kinds[i]), "All marker categories can be hidden");
            for (int j = 0; j < hidden.Length; j++)
                require(hidden[j].allows(kinds[i]) == (i != j), "Each marker toggle changes only its own category");
        }

        uint first_day = MarkerDay.score("test-player", 1);
        if (first_day != MarkerDay.score("test-player", 1) || first_day == MarkerDay.score("test-player", 2))
            throw new InvalidOperationException("Daily cosmetic selection must survive reload and vary by day.");
        int rare_days = 0;
        for (int day = 0; day < 10000; day++)
            if (MarkerDay.score("test-player", day) % 100 == 0) rare_days++;
        if (rare_days < 50 || rare_days > 150) throw new InvalidOperationException("Daily cosmetic probability drift.");
        if (MarkerDay.score("test-player", -1) != uint.MaxValue) throw new InvalidOperationException("Invalid day rejected.");

        require(MapProjection.clock(930, 0) == "Monday 09:30" && MapProjection.clock(2359, 6) == "Sunday 23:59",
            "Clock shows the day of the week before the time");
        require(MapProjection.clock(1200, 7) == "12:00" && MapProjection.clock(1200, -1) == "12:00",
            "Unknown days show only the time");
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
        require(MapProjection.zoom(float.NaN) == 1 && MapProjection.zoom(float.PositiveInfinity) == 1 &&
            MapProjection.zoom(0) == 0.5f && MapProjection.zoom(10) == 4, "Zoom rejects invalid values and bounds coverage");
        require(MapProjection.try_offset(new Vector2(0.525f, 0.5f), center, span / MapProjection.zoom(2), 280, false, out offset, out _) &&
            Math.Abs(offset.X - 70) < 0.01f, "Zoom scales landmarks and map coverage together");
        require(!MapProjection.try_offset(new Vector2(0.56f, 0.5f), center, span / 2, 280, false, out _, out _),
            "Zoomed static markers disappear beyond the visible map");
        require(MapProjection.try_offset(new Vector2(0.56f, 0.5f), center, span / 2, 280, true, out offset, out edge) &&
            edge && Math.Abs(offset.X - 128) < 0.01f, "Zoom preserves useful edge pointers");
        Vector2 screen = new(1920, 1080);
        Vector2 offset_percent = new(1);
        require(Vector2.Distance(MapProjection.panel_position(screen, 280, MapCorner.TopRight, offset_percent), new Vector2(1620.66f, 10.73f)) < 0.001f,
            "Default placement is one percent from the selected corner");
        require(MapProjection.panel_position(screen, 280, MapCorner.TopLeft, Vector2.Zero) == new Vector2(3, 3),
            "Zero percent reaches the selected safe corner");
        require(MapProjection.panel_position(screen, 280, MapCorner.BottomRight, Vector2.Zero) == new Vector2(1637, 776),
            "Bottom placement reserves room for the frame and clock");
        require(MapProjection.panel_position(screen, 280, MapCorner.TopLeft, new(100)) == new Vector2(1637, 776),
            "One hundred percent reaches the opposite safe corner");
        foreach (MapCorner corner in Enum.GetValues<MapCorner>())
            require(MapProjection.panel_position(screen, 280, corner, new(50)) == new Vector2(820, 389.5f),
                "Halfway placement is centered independently of the selected corner");
        require(Vector2.Distance(MapProjection.panel_position(screen, 280, (MapCorner)99, new(float.NaN)),
            MapProjection.panel_position(screen, 280, MapCorner.TopRight, offset_percent)) < 0.001f,
            "Invalid corner and percentages use defaults");
        Vector2 wide_screen = new(2560, 1080);
        Vector2 wide_position = MapProjection.panel_position(wide_screen, 280, MapCorner.TopLeft, new(25));
        require(Math.Abs((wide_position.X - 3) / (wide_screen.X - 286) - 0.25f) < 0.0001f,
            "Horizontal percentage follows the available width on ultrawide screens");
        foreach (Vector2 viewport in new[] { new Vector2(1108, 1108), screen, new Vector2(2560, 1080) })
        {
            foreach (MapCorner corner in Enum.GetValues<MapCorner>())
            {
                Vector2 position = MapProjection.panel_position(viewport, 360, corner, new Vector2(1000, 1000));
                require(position.X >= 3 && position.X + 360 + 3 <= viewport.X, "Out-of-range percentages keep the frame inside every screen corner");
                require(position.Y >= 3 && position.Y + 360 + 24 <= viewport.Y, "Out-of-range percentages keep the clock inside the viewport");
            }
        }
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
        Console.WriteLine("Marker visibility, clock, map projection, zoom, placement, heading, edge markers, texture clipping, and size checks passed.");
    }

    private static void require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
