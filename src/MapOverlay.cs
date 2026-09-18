using System;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace ClearMinimap;

internal sealed class MapOverlay
{
    private readonly RawImage?[] images = new RawImage?[144];
    private readonly Text?[] labels = new Text?[16];
    private Canvas? canvas;
    private Font? font;
    private int image_count;
    private int label_count;
    private int draw_order;

    public void begin(float scale)
    {
        if (canvas == null)
        {
            var root = new GameObject("ClearMinimap", new[] { Il2CppType.Of<RectTransform>() });
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = false;
        }
        canvas.enabled = true;
        canvas.scaleFactor = scale;
        if (font == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<Font>();
            int limit = Math.Min(fonts.Length, 128);
            for (int i = 0; i < limit; i++)
                if (fonts[i] != null) { font = fonts[i]; break; }
        }
        image_count = 0;
        label_count = 0;
        draw_order = 0;
    }

    public void texture(Rect rect, Texture? texture, Rect uv, Color color, float angle = 0)
    {
        if (texture == null) return;
        if (image_count >= images.Length) throw new InvalidOperationException("Minimap image pool exhausted.");
        RawImage? image = images[image_count];
        if (image == null)
        {
            var element = new GameObject("Symbol", new[] { Il2CppType.Of<RectTransform>() });
            element.transform.SetParent(canvas!.transform, false);
            image = element.AddComponent<RawImage>();
            image.raycastTarget = false;
            images[image_count] = image;
        }
        image_count++;
        image.enabled = true;
        image.texture = texture;
        image.uvRect = uv;
        image.color = color;
        place(image.rectTransform, rect, angle);
    }

    public void label(Rect rect, string value, Color color, bool centered = false, int font_size = 12)
    {
        if (font == null) return;
        if (label_count >= labels.Length) throw new InvalidOperationException("Minimap label pool exhausted.");
        Text? text = labels[label_count];
        if (text == null)
        {
            var element = new GameObject("Label", new[] { Il2CppType.Of<RectTransform>() });
            element.transform.SetParent(canvas!.transform, false);
            text = element.AddComponent<Text>();
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            labels[label_count] = text;
        }
        label_count++;
        text.enabled = true;
        text.font = font;
        text.fontSize = font_size;
        text.text = value;
        text.color = color;
        text.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        place(text.rectTransform, rect, 0);
    }

    public void box(Rect rect, Color color) => texture(rect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), color);

    public void end()
    {
        for (int i = image_count; i < images.Length; i++) if (images[i] != null) images[i]!.enabled = false;
        for (int i = label_count; i < labels.Length; i++) if (labels[i] != null) labels[i]!.enabled = false;
    }

    public void hide()
    {
        if (canvas != null) canvas.enabled = false;
    }

    public void reset()
    {
        if (canvas != null) UnityEngine.Object.Destroy(canvas.gameObject);
        canvas = null;
        font = null;
        Array.Clear(images, 0, images.Length);
        Array.Clear(labels, 0, labels.Length);
    }

    private void place(RectTransform transform, Rect rect, float angle)
    {
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.pivot = new Vector2(0.5f, 0.5f);
        transform.anchoredPosition = new Vector2(rect.center.x, -rect.center.y);
        transform.sizeDelta = new Vector2(rect.width, rect.height);
        transform.localRotation = Quaternion.Euler(0, 0, -angle);
        if (transform.GetSiblingIndex() != draw_order) transform.SetSiblingIndex(draw_order);
        draw_order++;
    }
}
