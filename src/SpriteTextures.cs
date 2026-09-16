using System;
using UnityEngine;

namespace ClearMinimap;

internal sealed class SpriteTextures
{
    private readonly Sprite?[] sources = new Sprite?[128];
    private readonly RenderTexture?[] textures = new RenderTexture?[128];
    private int next_icon = 1;

    public Texture get(Sprite sprite, bool map)
    {
        for (int i = map ? 0 : 1; i < (map ? 1 : textures.Length); i++)
            if (sources[i] == sprite && textures[i] != null) return textures[i]!;
        int index = map ? 0 : next_icon;
        Rect crop = sprite.textureRect;
        Texture source = sprite.texture;
        var (width, height) = MapProjection.texture_size(crop.width, crop.height, map);
        var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            useMipMap = true,
            autoGenerateMips = false,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 1
        };
        RenderTexture previous = RenderTexture.active;
        try
        {
            if (!texture.Create()) throw new InvalidOperationException("Unable to allocate filtered minimap sprite.");
            Graphics.Blit(source, texture, new Vector2(crop.width / source.width, crop.height / source.height),
                new Vector2(crop.x / source.width, crop.y / source.height));
            texture.GenerateMips();
            if (textures[index] != null)
            {
                textures[index]!.Release();
                UnityEngine.Object.Destroy(textures[index]);
            }
            sources[index] = sprite;
            textures[index] = texture;
            if (!map) next_icon = next_icon == textures.Length - 1 ? 1 : next_icon + 1;
            return texture;
        }
        catch
        {
            texture.Release();
            UnityEngine.Object.Destroy(texture);
            throw;
        }
        finally { RenderTexture.active = previous; }
    }

    public void reset()
    {
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] != null)
            {
                textures[i]!.Release();
                UnityEngine.Object.Destroy(textures[i]);
            }
            textures[i] = null;
            sources[i] = null;
        }
        next_icon = 1;
    }
}
