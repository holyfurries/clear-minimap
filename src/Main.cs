using System;
using HarmonyLib;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.MainMenu;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Map;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ClearMinimap.Main), "Clear Minimap", "0.3.0", "holyfurries")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClearMinimap;

public sealed class Main : MelonMod
{
    private readonly MinimapView view = new();
    private MelonPreferences_Entry<float> map_size = null!;
    private MelonPreferences_Entry<bool> enabled = null!;
    private MelonPreferences_Entry<bool> show_npcs = null!;
    private MelonPreferences_Entry<bool> show_dealers = null!;
    private MelonPreferences_Entry<bool> show_vehicles = null!;
    private MelonPreferences_Entry<bool> show_teammates = null!;
    private MelonPreferences_Entry<bool> show_properties = null!;
    private MelonPreferences_Entry<bool> show_deals = null!;
    private MelonPreferences_Entry<bool> show_objectives = null!;
    private MelonPreferences_Entry<bool> show_places = null!;
    private MelonPreferences_Entry<bool> show_labels = null!;
    private MelonPreferences_Entry<float> zoom = null!;
    private MelonPreferences_Entry<MapCorner> corner = null!;
    private MelonPreferences_Entry<int> horizontal_offset_percent = null!;
    private MelonPreferences_Entry<int> vertical_offset_percent = null!;
    private MelonPreferences_Entry<KeyCode> zoom_in_key = null!;
    private MelonPreferences_Entry<KeyCode> zoom_out_key = null!;
    private bool running;
    private bool failed;
    private bool conflict;
    private float refresh_seconds;

    public override void OnInitializeMelon()
    {
        MelonPreferences_Category preferences = MelonPreferences.CreateCategory("ClearMinimap");
        map_size = preferences.CreateEntry<float>("size", 280f, "Minimap size");
        enabled = preferences.CreateEntry<bool>("enabled", true, "Show minimap");
        show_npcs = preferences.CreateEntry<bool>("show_npcs", true, "Show phone map NPCs");
        show_dealers = preferences.CreateEntry("show_dealers", true, "Show recruited dealers");
        show_vehicles = preferences.CreateEntry("show_vehicles", true, "Show owned vehicles");
        show_teammates = preferences.CreateEntry("show_teammates", true, "Show teammates");
        show_properties = preferences.CreateEntry("show_properties", true, "Show properties");
        show_deals = preferences.CreateEntry("show_deals", true, "Show deals");
        show_objectives = preferences.CreateEntry("show_objectives", true, "Show objectives");
        show_places = preferences.CreateEntry("show_places", true, "Show places");
        show_labels = preferences.CreateEntry("show_labels", true, "Show name and distance labels");
        show_npcs.OnEntryValueChanged.Subscribe((_, value) => refresh_seconds = 0);
        zoom = preferences.CreateEntry<float>("zoom", 1f, "Zoom");
        zoom_in_key = preferences.CreateEntry("zoom_in_key", KeyCode.PageUp, "Zoom in shortcut");
        zoom_out_key = preferences.CreateEntry("zoom_out_key", KeyCode.PageDown, "Zoom out shortcut");
        corner = preferences.CreateEntry("corner", MapCorner.TopRight, "Screen corner");
        horizontal_offset_percent = preferences.CreateEntry("horizontal_offset_percent", 1, "Horizontal offset (%)");
        vertical_offset_percent = preferences.CreateEntry("vertical_offset_percent", 1, "Vertical offset (%)");
        ModSettings.Settings.toggle("Clear Minimap", "Show minimap", enabled);
        ModSettings.Settings.slider("Clear Minimap", "Minimap size", map_size, minimum: 200f, maximum: 360f, whole_numbers: true);
        ModSettings.Settings.toggle("Clear Minimap", "Show teammates", show_teammates);
        ModSettings.Settings.toggle("Clear Minimap", "Show properties", show_properties);
        ModSettings.Settings.toggle("Clear Minimap", "Show deals", show_deals);
        ModSettings.Settings.toggle("Clear Minimap", "Show objectives", show_objectives);
        ModSettings.Settings.toggle("Clear Minimap", "Show places", show_places);
        ModSettings.Settings.toggle("Clear Minimap", "Show phone map NPCs", show_npcs);
        ModSettings.Settings.toggle("Clear Minimap", "Show recruited dealers", show_dealers);
        ModSettings.Settings.toggle("Clear Minimap", "Show owned vehicles", show_vehicles);
        ModSettings.Settings.toggle("Clear Minimap", "Show name and distance labels", show_labels);
        ModSettings.Settings.slider("Clear Minimap", "Zoom", zoom, minimum: 0.5f, maximum: 4f);
        ModSettings.Settings.keybind("Clear Minimap", "Zoom in shortcut", zoom_in_key);
        ModSettings.Settings.keybind("Clear Minimap", "Zoom out shortcut", zoom_out_key);
        ModSettings.Settings.dropdown("Clear Minimap", "Screen corner", corner);
        ModSettings.Settings.slider("Clear Minimap", "Horizontal offset (%)", horizontal_offset_percent, minimum: 0, maximum: 100);
        ModSettings.Settings.slider("Clear Minimap", "Vertical offset (%)", vertical_offset_percent, minimum: 0, maximum: 100);
        HarmonyInstance.Patch(AccessTools.Method(typeof(POI), nameof(POI.OnEnable)),
            postfix: new HarmonyMethod(typeof(Main), nameof(register_point)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(POI), nameof(POI.OnDisable)),
            postfix: new HarmonyMethod(typeof(Main), nameof(unregister_point)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(NPCPoI), nameof(NPCPoI.InitializeUI)),
            postfix: new HarmonyMethod(typeof(Main), nameof(register_point)));
        foreach (MelonMod mod in RegisteredMelons)
        {
            if (mod == this) continue;
            string name = mod.Info.Name;
            if (name.Equals("MinimapMod", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("SimpleMinimap", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Expanded_Minimap", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Expanded Minimap", StringComparison.OrdinalIgnoreCase)) conflict = true;
        }
        if (conflict) LoggerInstance.Warning("Clear Minimap: Disable the other minimap mod, then relaunch. This copy will stay hidden to avoid duplicate maps.");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (sceneName != "Main") return;
        running = true;
        failed = false;
        refresh_seconds = 0;
    }

    public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main") return;
        running = false;
        failed = false;
        view.reset();
        MapMarkers.reset();
    }

    public override void OnUpdate()
    {
        try
        {
            if (!ready()) return;
            if (enabled.Value && Player.Local.Health.IsAlive && !settings_open() &&
                !(PauseMenu.InstanceExists && PauseMenu.Instance.IsPaused) && !(Phone.InstanceExists && Phone.Instance.IsOpen))
            {
                int direction = (zoom_in_key.Value != KeyCode.None && Input.GetKeyDown(zoom_in_key.Value) ? 1 : 0) -
                    (zoom_out_key.Value != KeyCode.None && Input.GetKeyDown(zoom_out_key.Value) ? 1 : 0);
                if (direction != 0)
                {
                    float value = MapProjection.zoom(MapProjection.zoom(zoom.Value) + direction * 0.25f);
                    if (value != zoom.Value)
                    {
                        zoom.Value = value;
                        try { MelonPreferences.Save(); }
                        catch (Exception error) { LoggerInstance.Warning($"Clear Minimap: Could not save zoom: {error.Message}"); }
                    }
                }
            }
            if (Time.unscaledTime < refresh_seconds) return;
            refresh_seconds = Time.unscaledTime + 0.5f;
            MapMarkers.refresh(show_npcs.Value);
        }
        catch (Exception error) { fail(error); }
    }

    public override void OnLateUpdate()
    {
        if (failed || conflict) { view.hide(); return; }
        try
        {
            bool settings = settings_open();
            bool available = ready();
            if (!settings && (!available || !enabled.Value || (PauseMenu.InstanceExists && PauseMenu.Instance.IsPaused) ||
                (Phone.InstanceExists && Phone.Instance.IsOpen) || !Player.Local.Health.IsAlive))
            {
                view.hide();
                return;
            }
            float scale = Math.Clamp(Screen.height / 1080f, 0.65f, 2f);
            float size = MapProjection.size(map_size.Value);
            System.Numerics.Vector2 position = MapProjection.panel_position(new(Screen.width / scale, Screen.height / scale), size,
                corner.Value, new(horizontal_offset_percent.Value, vertical_offset_percent.Value));
            view.draw(new Rect(position.X, position.Y, size, size), available, scale, MapProjection.zoom(zoom.Value), marker_visibility(), show_labels.Value);
        }
        catch (Exception error) { fail(error); }
    }

    private MarkerVisibility marker_visibility() => new(
        places: show_places.Value, properties: show_properties.Value, deals: show_deals.Value,
        teammates: show_teammates.Value, objectives: show_objectives.Value, npcs: show_npcs.Value, dealers: show_dealers.Value, vehicles: show_vehicles.Value);

    private static bool settings_open()
    {
        MenuScreen screen = MenuScreen.Current;
        return screen != null && screen.IsOpen && screen.TryCast<SettingsScreen>() != null;
    }

    private bool ready()
    {
        return running && !failed && !conflict && LoadManager.InstanceExists && LoadManager.Instance.IsGameLoaded &&
            Player.Local != null && Player.Local.Health != null && MapApp.InstanceExists && MapPositionUtility.InstanceExists &&
            MapApp.Instance.BackgroundImage != null && MapApp.Instance.PoIContainer != null;
    }

    private static void register_point(POI __instance) => MapMarkers.register_point(__instance);
    private static void unregister_point(POI __instance) => MapMarkers.unregister_point(__instance);

    private void fail(Exception error)
    {
        if (failed) return;
        failed = true;
        view.hide();
        LoggerInstance.Error($"Clear Minimap: Hidden after a game API failure: {error}");
    }
}
