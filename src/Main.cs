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

[assembly: MelonInfo(typeof(ClearMinimap.Main), "Clear Minimap", "0.1.13", "holyfurries")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClearMinimap;

public sealed class Main : MelonMod
{
    private readonly MinimapView view = new();
    private MelonPreferences_Category preferences = null!;
    private MelonPreferences_Entry<float> map_size = null!;
    private MelonPreferences_Entry<bool> enabled = null!;
    private bool running;
    private bool failed;
    private bool conflict;
    private bool dirty;
    private float save_seconds;
    private float refresh_seconds;
    private GUIStyle? settings_style;
    private int objective_count_previous = -1;

    public override void OnInitializeMelon()
    {
        preferences = MelonPreferences.CreateCategory("ClearMinimap");
        map_size = preferences.CreateEntry<float>("size", 280f, "Minimap size");
        enabled = preferences.CreateEntry<bool>("enabled", true, "Show minimap");
        HarmonyInstance.Patch(AccessTools.Method(typeof(POI), nameof(POI.OnEnable)),
            postfix: new HarmonyMethod(typeof(Main), nameof(register_point)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(POI), nameof(POI.OnDisable)),
            postfix: new HarmonyMethod(typeof(Main), nameof(unregister_point)));
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
        objective_count_previous = -1;
        LoggerInstance.Msg("Clear Minimap: Map enabled; screen-space overlay renderer; size controls are shown while Settings is open. No F3 menu.");
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
            if (dirty && Time.unscaledTime >= save_seconds)
            {
                dirty = false;
                preferences.SaveToFile(false);
            }
            if (!ready() || Time.unscaledTime < refresh_seconds) return;
            refresh_seconds = Time.unscaledTime + 0.5f;
            MapMarkers.refresh();
            int objective_count = 0;
            for (int i = 0; i < MapMarkers.count; i++)
                if (MapMarkers.markers[i].kind == MarkerKind.Objective) objective_count++;
            if (objective_count != objective_count_previous)
            {
                objective_count_previous = objective_count;
                LoggerInstance.Msg($"Clear Minimap: Located tracked objectives={objective_count}.");
            }
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
            view.draw(new Rect(Screen.width / scale - size - 16, 16, size, size), available, scale);
        }
        catch (Exception error) { fail(error); }
    }

    public override void OnGUI()
    {
        if (failed || conflict) return;
        Matrix4x4 matrix = GUI.matrix;
        Color color = GUI.color;
        try
        {
            if (!settings_open()) return;
            float scale = Math.Clamp(Screen.height / 1080f, 0.65f, 2f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            draw_settings(new Rect(Screen.width / scale - 376, 16, 360, 360));
        }
        catch (Exception error) { fail(error); }
        finally { GUI.matrix = matrix; GUI.color = color; }
    }

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

    private void draw_settings(Rect panel)
    {
        settings_style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, richText = false };
        if (Event.current.type == EventType.Repaint)
            Graphics.DrawTexture(new Rect(panel.x - 3, panel.yMax + 28, panel.width + 6, 94),
                Texture2D.whiteTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0,
                new Color(0.055f, 0.07f, 0.085f, 0.98f), GUI.blendMaterial, -1);
        GUI.Label(new Rect(panel.x + 10, panel.yMax + 34, panel.width - 20, 24), $"Minimap size · {MapProjection.size(map_size.Value):0}", settings_style);
        float selected = GUI.HorizontalSlider(new Rect(panel.x + 12, panel.yMax + 63, panel.width - 24, 18), MapProjection.size(map_size.Value), 200f, 360f);
        selected = MathF.Round(selected / 5f) * 5f;
        bool show = GUI.Toggle(new Rect(panel.x + 12, panel.yMax + 86, panel.width - 24, 24), enabled.Value, " Show minimap");
        if (selected == map_size.Value && show == enabled.Value) return;
        map_size.Value = selected;
        enabled.Value = show;
        dirty = true;
        save_seconds = Time.unscaledTime + 0.75f;
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
