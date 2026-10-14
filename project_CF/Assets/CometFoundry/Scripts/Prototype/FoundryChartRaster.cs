using System;
using System.Collections.Generic;
using CometFoundry.Simulation;
using UnityEngine;

namespace CometFoundry.Prototype
{
    // Plot in texture coordinates, never by rotating the screen-space IMGUI matrix.
    public static class FoundryChartRaster
    {
        public static readonly Color32 Rise = new Color32(245, 84, 91, 255);
        public static readonly Color32 Fall = new Color32(76, 155, 255, 255);
        public static Texture2D Create(IReadOnlyList<FoundryHistoryPoint> points, int width, int height,
            double min, double max, double reference, Color color, bool smooth = false)
        {
            if (width < 2 || height < 2 || max <= min) throw new ArgumentException("Invalid chart bounds");
            if (smooth && points.Count > 1) points = SmoothPoints(points, width);
            var pixels = new Color32[width * height];
            if (points.Count > 0)
            {
                double start = points[0].day, end = Math.Max(start + 1, points[points.Count - 1].day);
                Vector2 previous = Vector2.zero;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 p = new Vector2((float)((points[i].day - start) / (end - start)) * (width - 1),
                        Mathf.Clamp01((float)((points[i].value - min) / (max - min))) * (height - 1));
                    if (i > 0)
                    {
                        double a = points[i - 1].value - reference, b = points[i].value - reference;
                        if (reference > 0 && a * b < 0)
                        {
                            Vector2 cross = Vector2.Lerp(previous, p, (float)(-a / (b - a)));
                            Line(pixels, width, height, previous, cross, a >= 0 ? Rise : Fall);
                            Line(pixels, width, height, cross, p, b >= 0 ? Rise : Fall);
                        }
                        else Line(pixels, width, height, previous, p, reference > 0 ? (a + b >= 0 ? Rise : Fall) : (Color32)color);
                    }
                    if (i == points.Count - 1) Dot(pixels, width, height, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), 3,
                        reference > 0 ? (points[i].value >= reference ? Rise : Fall) : (Color32)color);
                    previous = p;
                }
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(); return texture;
        }
        // Smoothstep joins monthly samples with a continuous tangent and never
        // overshoots their values, preserving the market's price limits.
        private static IReadOnlyList<FoundryHistoryPoint> SmoothPoints(IReadOnlyList<FoundryHistoryPoint> points, int width)
        {
            var curve = new List<FoundryHistoryPoint>();
            double span = Math.Max(1, points[points.Count - 1].day - points[0].day);
            curve.Add(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                var from = points[i - 1]; var to = points[i];
                int steps = Math.Max(1, (int)Math.Ceiling((to.day - from.day) / span * (width - 1)));
                for (int step = 1; step <= steps; step++)
                {
                    double t = step / (double)steps, blend = t * t * (3 - 2 * t);
                    curve.Add(new FoundryHistoryPoint(from.day + (to.day - from.day) * t,
                        from.value + (to.value - from.value) * blend));
                }
            }
            return curve;
        }
        private static void Line(Color32[] pixels, int width, int height, Vector2 a, Vector2 b, Color32 color)
        {
            int steps = Math.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y))));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
                Dot(pixels, width, height, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), 1, color);
            }
        }
        private static void Dot(Color32[] pixels, int width, int height, int x, int y, int radius, Color32 color)
        {
            for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                if (x + dx >= 0 && x + dx < width && y + dy >= 0 && y + dy < height)
                    pixels[(y + dy) * width + x + dx] = color;
        }
    }
}
