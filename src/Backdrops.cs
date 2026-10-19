using Raylib_cs;
using static HighwayRun.U;

namespace HighwayRun;

/// <summary>Arrière-plans en parallaxe (lointain, proche, nuages), générés pour chaque étape.
/// Les silhouettes sont des sommes de sinus de fréquences entières : elles bouclent sans couture.</summary>
static class Backdrops
{
    public const int LayerW = Cfg.Width * 3, FarH = 110, NearH = 70, CloudH = 100;
    public static Texture2D Clouds;

    public static void Load()
    {
        Clouds = BuildClouds();
        int seed = 1;
        foreach (var th in Themes.All)
        {
            var rng = new Random(seed++ * 977);
            th.FarTex = BuildFar(th, rng).ToTexture();
            th.NearTex = BuildNear(th, rng).ToTexture();
        }
    }

    const int S = Cfg.ArtScale;

    /// <summary>Hauteurs (en unités logiques) pour chaque colonne de pixels réels.</summary>
    static float[] Ridge(Random rng, float baseH, params (int k, float amp)[] harm)
    {
        var phases = harm.Select(_ => (float)rng.NextDouble() * MathF.Tau).ToArray();
        var h = new float[LayerW * S];
        for (int px = 0; px < h.Length; px++)
        {
            float x = px / (float)S, v = baseH;
            for (int i = 0; i < harm.Length; i++) v += harm[i].amp * MathF.Sin(MathF.Tau * harm[i].k * x / LayerW + phases[i]);
            h[px] = v;
        }
        return h;
    }

    /// <summary>Hauteur à la colonne logique x.</summary>
    static float At(float[] h, int x) => h[(x * S % h.Length + h.Length) % h.Length];
    static bool Rising(float[] h, int x) => At(h, x + 1) > At(h, x);

    /// <summary>Remplit sous la silhouette. color reçoit des coordonnées logiques et la profondeur sous la crête.</summary>
    static void Fill(Canvas c, float[] h, Func<int, int, float, Color> color)
    {
        for (int px = 0; px < c.W; px++)
        {
            int top = (int)MathF.Round(c.H - Math.Clamp(h[px] * S, 0, c.H));
            for (int py = Math.Max(0, top); py < c.H; py++) c.Plot(px, py, color(px / S, py / S, (py - top) / (float)S));
        }
    }

    static Canvas BuildFar(StageTheme th, Random rng)
    {
        var c = new Canvas(LayerW, FarH);
        Color far = Mix(th.Far, th.SkyBottom, 0.2f), farHi = Mix(far, Color.White, 0.18f);
        switch (th.Backdrop)
        {
            case Backdrop.Sea:
            case Backdrop.Town:
            {
                var isl = Ridge(rng, -14, (3, 14), (7, 8), (11, 5));
                Fill(c, isl.Select(v => v + 12).ToArray(), (x, y, d) => d < 2 ? farHi : far);
                Water(c, th, 12);
                break;
            }
            case Backdrop.Canyon:
            {
                var raw = Ridge(rng, 0, (3, 1), (5, 0.6f), (9, 0.35f));
                var h = raw.Select(v => 22 + 52 * Smooth((v + 0.1f) * 2.2f)).ToArray();
                Fill(c, h, (x, y, d) => (y % 9 < 2) ? Shade(far, 0.85f) : d < 2 ? farHi : far);
                break;
            }
            case Backdrop.Dunes:
            {
                var h = Ridge(rng, 26, (2, 12), (5, 6), (9, 2));
                Fill(c, h, (x, y, d) => Rising(h, x) ? farHi : far);
                break;
            }
            case Backdrop.Snowy:
            case Backdrop.Lake:
            {
                var h = Peaks(rng, 34, 46);
                float max = h.Max();
                Fill(c, h, (x, y, d) => (c.LH - y) > max - 22 + (x * 7 % 5) ? Hex(0xF4F8FF) : d < 1 ? farHi : far);
                break;
            }
            case Backdrop.Mountains:
            {
                var h = Peaks(rng, 30, 52);
                Fill(c, h, (x, y, d) => Rising(h, x) ? farHi : far);
                break;
            }
            default:
            {
                var h = Ridge(rng, 36, (2, 14), (4, 9), (7, 5), (13, 2));
                Fill(c, h, (x, y, d) => d < 2 ? farHi : far);
                break;
            }
        }
        return c;
    }

    static float[] Peaks(Random rng, float baseH, float amp)
    {
        int[] ks = [3, 5, 8, 13];
        float[] amps = [1f, 0.6f, 0.35f, 0.2f];
        var ph = ks.Select(_ => (float)rng.NextDouble() * MathF.Tau).ToArray();
        var h = new float[LayerW * S];
        for (int px = 0; px < h.Length; px++)
        {
            float x = px / (float)S, v = 0;
            for (int i = 0; i < ks.Length; i++) v += amps[i] * (1 - MathF.Abs(MathF.Sin(MathF.PI * ks[i] * x / LayerW + ph[i])));
            h[px] = baseH + (v - 1f) * amp * 0.75f;
        }
        return h;
    }

    static void Water(Canvas c, StageTheme th, int height)
    {
        for (int py = c.H - height * S; py < c.H; py++)
            for (int px = 0; px < c.W; px++)
            {
                int x = px / S, y = py / S;
                bool glint = ((x * 13 + y * 7) % 23 == 0) && y > c.LH - height + 2 && px % S == 0;
                c.Plot(px, py, glint ? Mix(th.Water, Color.White, 0.6f) : Mix(th.Water, th.SkyBottom, (c.H - py) / (float)(height * S) * 0.35f));
            }
    }

    static Canvas BuildNear(StageTheme th, Random rng)
    {
        var c = new Canvas(LayerW, NearH);
        Color near = th.Near, hi = Shade(near, 1.15f), dk = Shade(near, 0.85f);
        switch (th.Backdrop)
        {
            case Backdrop.Sea:
            {
                // dunes de sable sur une partie de l'horizon seulement : la mer reste visible
                var gate = Ridge(rng, 0, (1, 1), (2, 0.5f));
                var h = gate.Select((g, px) => g > 0.2f ? 6 + 10 * Smooth((g - 0.2f) * 3) + 3 * MathF.Sin(px / (float)S * 0.05f) : 0).ToArray();
                Fill(c, h, (x, y, d) => d < 1 ? hi : near);
                break;
            }
            case Backdrop.Town:
            {
                for (int x = 0; x < LayerW;)
                {
                    int w = 14 + rng.Next(18), hh = 10 + rng.Next(18);
                    if (rng.Next(5) == 0) { x += w * 2; continue; }
                    Color wall = Mix(new[] { Hex(0xF0E6D6), Hex(0xE8F0F8), Hex(0xF4E0C0), Hex(0xE0E8D0) }[rng.Next(4)], th.SkyBottom, 0.2f);
                    Color roof = Mix(rng.Next(2) == 0 ? Hex(0xC2582E) : Hex(0x3A6EC0), th.SkyBottom, 0.2f);
                    c.Rect(x, NearH - hh, w, hh, wall);
                    c.Poly(roof, new(x - 1, NearH - hh), new(x + w / 2f, NearH - hh - 6), new(x + w + 1, NearH - hh));
                    for (int wy = NearH - hh + 3; wy < NearH - 3; wy += 5)
                        for (int wx = x + 2; wx < x + w - 2; wx += 4) c.Rect(wx, wy, 2, 2, Mix(Hex(0x2A4A7A), th.SkyBottom, 0.3f));
                    x += w + rng.Next(4);
                }
                break;
            }
            case Backdrop.City:
            {
                var h = Ridge(rng, 8, (3, 2), (6, 1));
                Fill(c, h, (x, y, d) => near);
                for (int x = 20; x < LayerW; x += 60 + rng.Next(90))
                {
                    int tiers = 2 + rng.Next(3);
                    float bx = x, by = NearH - 8;
                    for (int t = 0; t < tiers; t++)
                    {
                        float w = 18 - t * 3;
                        c.Rect(bx - w * 0.55f, by - 7, w * 1.1f, 7, dk);
                        c.Poly(near, new(bx - w - 3, by - 6), new(bx - w + 2, by - 10), new(bx + w - 2, by - 10), new(bx + w + 3, by - 6));
                        by -= 10;
                    }
                    c.Rect(bx - 0.5f, by - 6, 1, 6, near);
                }
                break;
            }
            case Backdrop.Lake:
            {
                var h = Ridge(rng, 16, (4, 3), (9, 2));
                Fill(c, h, (x, y, d) => (d < 4 && (x % 5 < 3)) ? dk : near);
                for (int x = 0; x < LayerW; x += 4 + rng.Next(5))
                {
                    float top = NearH - At(h, x);
                    c.Poly(dk, new(x, top - 7 - rng.Next(4)), new(x + 3, top + 1), new(x - 3, top + 1));
                }
                Water(c, th, 9);
                break;
            }
            case Backdrop.Canyon:
            {
                var raw = Ridge(rng, 0, (4, 1), (7, 0.5f));
                var h = raw.Select(v => 8 + 30 * Smooth((v - 0.2f) * 2.5f)).ToArray();
                Fill(c, h, (x, y, d) => (y % 7 < 2) ? dk : d < 1 ? hi : near);
                break;
            }
            case Backdrop.Dunes:
            {
                var h = Ridge(rng, 12, (3, 6), (7, 3));
                Fill(c, h, (x, y, d) => Rising(h, x) ? hi : near);
                break;
            }
            case Backdrop.Snowy:
            case Backdrop.Mountains:
            {
                var h = Ridge(rng, 18, (3, 6), (7, 4), (12, 2));
                Fill(c, h, (x, y, d) => near);
                for (int x = 0; x < LayerW; x += 3 + rng.Next(4))
                {
                    float top = NearH - At(h, x);
                    float th2 = 6 + rng.Next(6);
                    c.Poly(dk, new(x, top - th2), new(x + 3, top + 2), new(x - 3, top + 2));
                }
                break;
            }
            default:
            {
                var h = Ridge(rng, 16, (3, 7), (5, 4), (11, 2));
                Fill(c, h, (x, y, d) => d < 1 ? hi : near);
                for (int x = 0; x < LayerW; x += 5 + rng.Next(9))
                {
                    if (rng.Next(3) == 0) continue;
                    float top = NearH - At(h, x);
                    c.Ellipse(x, top, 3 + rng.Next(3), 3, dk);
                }
                break;
            }
        }
        return c;
    }

    static Texture2D BuildClouds()
    {
        var c = new Canvas(LayerW, CloudH);
        var rng = new Random(99);
        Color shade = Hex(0xDCE6F0), white = Hex(0xFFFFFF);
        for (int k = 0; k < 16; k++)
        {
            float cx = rng.Next(LayerW), cy = 14 + rng.Next(56);
            int puffs = 4 + rng.Next(5);
            float scale = 0.6f + (float)rng.NextDouble() * 0.9f;
            for (int pass = 0; pass < 2; pass++)
                for (int p = 0; p < puffs; p++)
                {
                    var r2 = new Random(k * 31 + p);
                    float px = cx + (p - puffs / 2f) * 9 * scale, py = cy - r2.Next(6) * scale;
                    float rx = (8 + r2.Next(8)) * scale, ry = (5 + r2.Next(5)) * scale;
                    foreach (float off in new float[] { 0, -LayerW, LayerW })
                        if (pass == 0) c.Ellipse(px + off, py + 2, rx, ry, shade);
                        else c.Ellipse(px + off, py - 1, rx * 0.85f, ry * 0.8f, white);
                }
        }
        return c.ToTexture();
    }
}
