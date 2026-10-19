using Raylib_cs;

namespace HighwayRun;

static class U
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    public static float EaseIn(float a, float b, float t) => a + (b - a) * t * t;
    public static float EaseInOut(float a, float b, float t) => a + (b - a) * (-MathF.Cos(t * MathF.PI) / 2f + 0.5f);
    public static float Clamp01(float t) => Math.Clamp(t, 0f, 1f);
    public static float Smooth(float t) { t = Clamp01(t); return t * t * (3 - 2 * t); }
    public static float Wrap(float v) => v - MathF.Floor(v);

    public static Color Hex(uint rgb, byte a = 255) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);

    public static Color Mix(Color a, Color b, float t)
    {
        t = Clamp01(t);
        return new Color((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t),
                         (byte)(a.B + (b.B - a.B) * t), (byte)(a.A + (b.A - a.A) * t));
    }

    public static Color Fade(Color c, float alpha) => new(c.R, c.G, c.B, (byte)(c.A * Clamp01(alpha)));

    public static Color Shade(Color c, float f) =>
        new((byte)Math.Clamp(c.R * f, 0, 255), (byte)Math.Clamp(c.G * f, 0, 255), (byte)Math.Clamp(c.B * f, 0, 255), c.A);

    public static bool Overlap(float x1, float w1, float x2, float w2)
    {
        float min1 = x1 - w1 / 2, max1 = x1 + w1 / 2, min2 = x2 - w2 / 2, max2 = x2 + w2 / 2;
        return !(max1 < min2 || min1 > max2);
    }

    /// <summary>Visibilité (1 = net, 0 = noyé dans le brouillard).</summary>
    public static float FogVisibility(float d, float density) => 1f / MathF.Exp(d * d * density);

    public static T Pick<T>(Random rng, (T item, float weight)[] items)
    {
        float total = 0;
        foreach (var it in items) total += it.weight;
        float r = (float)rng.NextDouble() * total;
        foreach (var it in items) { r -= it.weight; if (r <= 0) return it.item; }
        return items[^1].item;
    }

    public static string FormatLap(float t)
    {
        int m = (int)(t / 60), s = (int)(t % 60), c = (int)(t * 100 % 100);
        return $"{m}'{s:00}\"{c:00}";
    }
}
