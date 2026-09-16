using System;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Quests;
using UnityEngine;
using UnityEngine.UI;

namespace ClearMinimap;

internal enum MarkerKind { Place, Home, Deal, Teammate, Objective }
internal readonly record struct MapMarker(Vector3 position, string label, MarkerKind kind, IntPtr source, Sprite? sprite, Transform? target, Color tint);

internal static class MapMarkers
{
    private static readonly POI?[] points = new POI?[256];
    public static readonly MapMarker[] markers = new MapMarker[96];
    public static int count { get; private set; }

    public static void reset()
    {
        Array.Clear(points, 0, points.Length);
        Array.Clear(markers, 0, markers.Length);
        count = 0;
    }

    public static void register_point(POI point)
    {
        if (point == null) return;
        int empty = -1;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == point) return;
            if (points[i] == null && empty < 0) empty = i;
        }
        if (empty >= 0) points[empty] = point;
    }

    public static void unregister_point(POI point)
    {
        for (int i = 0; i < points.Length; i++)
            if (points[i] == point) points[i] = null;
    }

    public static void refresh()
    {
        Array.Clear(markers, 0, count);
        count = 0;
        Player local = Player.Local;
        int limit = Math.Min(Player.PlayerList.Count, 16);
        for (int i = 0; i < limit; i++)
        {
            Player player = Player.PlayerList[i];
            if (player == null || player == local) continue;
            string label = player.PlayerName;
            if (player.Health != null && !player.Health.IsAlive) label += " (down)";
            Image? image = foreground_image(player.PoI);
            add(new MapMarker(player.transform.position, label, MarkerKind.Teammate,
                player.PoI != null ? player.PoI.Pointer : player.Pointer, image?.sprite, player.transform, image?.color ?? Color.white));
        }
        limit = Math.Min(Contract.Contracts.Count, 32);
        for (int i = 0; i < limit; i++)
        {
            Contract contract = Contract.Contracts[i];
            if (contract == null || contract.State != EQuestState.Active || contract.Dealer != null || contract.DeliveryLocation == null) continue;
            Transform location = contract.DeliveryLocation.CustomerStandPoint;
            if (location == null) continue;
            POI? point = contract.Entries != null && contract.Entries.Count > 0 && contract.Entries[0] != null
                ? contract.Entries[0].PoI : null;
            Image? image = foreground_image(point);
            add(new MapMarker(location.position, "Deal", MarkerKind.Deal, point != null ? point.Pointer : contract.Pointer,
                image?.sprite, location, image?.color ?? Color.white));
        }
        limit = Math.Min(Quest.ActiveQuests.Count, 32);
        for (int i = 0; i < limit && count < markers.Length; i++)
        {
            Quest quest = Quest.ActiveQuests[i];
            if (quest == null || quest.TryCast<Contract>() != null || quest.State != EQuestState.Active ||
                !quest.IsTracked || quest.Entries == null) continue;
            int entry_count = Math.Min(quest.Entries.Count, 16);
            for (int j = 0; j < entry_count && count < markers.Length; j++)
            {
                QuestEntry entry = quest.Entries[j];
                if (entry == null || entry.State != EQuestState.Active) continue;
                POI point = entry.PoI;
                Transform location = point != null ? point.transform : entry.PoILocation;
                if (location == null) continue;
                Image? image = foreground_image(point);
                add(new MapMarker(location.position, entry.Title, MarkerKind.Objective,
                    point != null ? point.Pointer : entry.Pointer, image?.sprite, location, image?.color ?? Color.white));
            }
        }
        limit = Math.Min(Property.OwnedProperties.Count, 32);
        for (int i = 0; i < limit; i++)
        {
            Property property = Property.OwnedProperties[i];
            if (property == null) continue;
            Vector3 position = property.PoI != null ? property.PoI.transform.position : property.transform.position;
            Image? image = foreground_image(property.PoI);
            add(new MapMarker(position, property.propertyName, MarkerKind.Home,
                property.PoI != null ? property.PoI.Pointer : property.Pointer, image?.sprite, property.PoI != null ? property.PoI.transform : property.transform, image?.color ?? Color.white));
        }
        for (int i = 0; i < points.Length && count < markers.Length; i++)
        {
            POI? point = points[i];
            if (point == null || !point.isActiveAndEnabled || point.TryCast<NPCPoI>() != null ||
                !point.UISetup || point.UI == null || !point.UI.gameObject.activeSelf ||
                (local != null && local.PoI == point)) continue;
            string label = point.MainText;
            if (string.IsNullOrWhiteSpace(label)) continue;
            Image? image = foreground_image(point);
            string prefab_name = point.UIPrefab != null ? point.UIPrefab.name : point.UI.name;
            MarkerKind kind = MapProjection.objective_icon(prefab_name) ? MarkerKind.Objective : MarkerKind.Place;
            add(new MapMarker(point.transform.position, label, kind, point.Pointer, image != null ? image.sprite : null, point.transform, image != null ? image.color : Color.white));
        }
    }

    private static Image? foreground_image(POI? point)
    {
        if (point == null || point.IconContainer == null) return null;
        var images = point.IconContainer.GetComponentsInChildren<Image>(true);
        int count = Math.Min(images.Length, 32);
        for (int i = count - 1; i >= 0; i--)
        {
            Image image = images[i];
            if (image == null || !image.enabled || !image.gameObject.activeSelf || image.color.a <= 0 ||
                image.sprite == null || !MapProjection.foreground_icon(image.gameObject.name)) continue;
            return image;
        }
        return null;
    }

    private static void add(MapMarker marker)
    {
        if (count >= markers.Length || !float.IsFinite(marker.position.sqrMagnitude)) return;
        for (int i = 0; i < count; i++) if (markers[i].source == marker.source) return;
        string label = marker.label ?? "";
        if (label.Length > 24) label = label.Substring(0, 24);
        markers[count++] = marker with { label = label.Replace('\n', ' ').Replace('\r', ' ') };
    }
}
