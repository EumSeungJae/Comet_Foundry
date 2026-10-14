using System.Collections.Generic;
using CometFoundry.Data;
using UnityEngine;

namespace CometFoundry.Prototype
{
    // Code-native top-down sprites: ivory panels, orange markings, graphite machinery.
    public sealed class FoundryBuildingArt
    {
        private readonly Dictionary<FacilityType, Texture2D> textures = new Dictionary<FacilityType, Texture2D>();
        private static readonly Color White = new Color32(239, 242, 235, 255);
        private static readonly Color Shade = new Color32(176, 190, 191, 255);
        private static readonly Color Dark = new Color32(37, 52, 61, 255);
        private static readonly Color Orange = new Color32(255, 135, 46, 255);
        public Texture2D Get(FacilityType type)
        {
            if (textures.TryGetValue(type, out var existing)) return existing;
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Foundry_" + type };
            var pixels = new Color[128 * 128];
            Paint(pixels, 8, 9, 112, 108, Dark);
            Paint(pixels, 12, 14, 104, 98, Shade);
            Paint(pixels, 12, 23, 104, 89, White);
            Paint(pixels, 12, 23, 9, 89, Orange);
            Paint(pixels, 25, 99, 72, 4, Orange);
            Paint(pixels, 31, 13, 66, 13, Dark);
            for (int x = 32; x < 96; x += 12) Paint(pixels, x, 15, 6, 4, Orange);
            switch (type)
            {
                case FacilityType.Mine:
                    Circle(pixels, 68, 64, 35, Dark); Circle(pixels, 68, 67, 27, Shade);
                    Circle(pixels, 68, 67, 19, White); Circle(pixels, 68, 67, 9, Orange);
                    Paint(pixels, 62, 32, 12, 18, Dark); break;
                case FacilityType.Refinery:
                    Circle(pixels, 66, 65, 33, Shade); Circle(pixels, 66, 69, 29, White);
                    Circle(pixels, 66, 69, 15, Dark); Circle(pixels, 66, 69, 9, Orange);
                    Paint(pixels, 95, 34, 12, 47, Dark); Paint(pixels, 98, 36, 6, 42, Orange); break;
                case FacilityType.Factory:
                    Paint(pixels, 31, 37, 74, 48, Dark); Paint(pixels, 36, 43, 30, 37, Shade);
                    Circle(pixels, 51, 62, 12, White); Circle(pixels, 51, 62, 5, Orange);
                    for (int y = 46; y < 81; y += 8) Paint(pixels, 76, y, 22, 3, White); break;
                case FacilityType.Assembly:
                    Paint(pixels, 35, 33, 61, 54, Shade); Paint(pixels, 40, 38, 51, 44, Dark);
                    Paint(pixels, 52, 50, 27, 21, Orange); Paint(pixels, 31, 55, 20, 8, White);
                    Paint(pixels, 80, 62, 24, 8, White); Paint(pixels, 100, 42, 6, 35, Orange); break;
                case FacilityType.Warehouse:
                    for (int y = 35; y < 89; y += 18)
                    {
                        Paint(pixels, 30, y, 75, 14, Shade); Paint(pixels, 30, y + 4, 75, 10, White);
                        Paint(pixels, 42, y, 4, 14, Orange); Paint(pixels, 88, y, 4, 14, Orange);
                    } break;
                case FacilityType.MarketTerminal:
                    Paint(pixels, 30, 33, 74, 55, Dark); Paint(pixels, 36, 41, 62, 40, Shade);
                    Paint(pixels, 46, 52, 44, 12, Orange); Paint(pixels, 80, 44, 9, 28, Orange); break;
                case FacilityType.PowerPlant:
                    Paint(pixels, 28, 30, 78, 63, Dark);
                    for (int x = 33; x < 100; x += 22)
                    { Paint(pixels, x, 37, 16, 47, Shade); Paint(pixels, x, 72, 16, 12, White); Paint(pixels, x + 5, 41, 6, 25, Orange); }
                    break;
                case FacilityType.LogisticsHub:
                    Paint(pixels, 30, 32, 76, 58, Dark);
                    Paint(pixels, 36, 38, 64, 45, Shade);
                    Paint(pixels, 58, 45, 20, 31, White);
                    Paint(pixels, 47, 56, 42, 9, White);
                    Paint(pixels, 63, 49, 10, 22, Orange);
                    for (int x = 35; x < 105; x += 17) Paint(pixels, x, 86, 9, 5, Orange);
                    break;
                case FacilityType.DroneHangar:
                    Paint(pixels, 29, 32, 78, 58, Dark); Paint(pixels, 34, 37, 68, 48, Shade);
                    Paint(pixels, 61, 43, 13, 34, White); Paint(pixels, 48, 55, 39, 10, White);
                    Circle(pixels, 44, 44, 8, Dark); Circle(pixels, 90, 76, 8, Dark);
                    Circle(pixels, 44, 76, 8, Dark); Circle(pixels, 90, 44, 8, Dark);
                    Circle(pixels, 67, 60, 6, Orange); break;
            }
            for (int x = 27; x < 111; x += 78) for (int y = 28; y < 100; y += 67) Paint(pixels, x, y, 3, 3, Dark);
            texture.SetPixels(pixels); texture.Apply(); textures.Add(type, texture); return texture;
        }
        private static void Paint(Color[] pixels, int x, int y, int w, int h, Color color)
        {
            for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) pixels[yy * 128 + xx] = color;
        }
        private static void Circle(Color[] pixels, int x, int y, int radius, Color color)
        {
            for (int yy = y - radius; yy <= y + radius; yy++) for (int xx = x - radius; xx <= x + radius; xx++)
                if ((xx - x) * (xx - x) + (yy - y) * (yy - y) <= radius * radius) pixels[yy * 128 + xx] = color;
        }
        public void Dispose()
        {
            foreach (var texture in textures.Values)
                if (Application.isPlaying) Object.Destroy(texture); else Object.DestroyImmediate(texture);
            textures.Clear();
        }
    }
}
