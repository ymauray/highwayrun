using System.Numerics;
using Raylib_cs;

namespace HighwayRun;

/// <summary>Petite surface de dessin en mémoire pour générer les sprites pixel art au démarrage.</summary>
sealed class Canvas(int w, int h)
{
    public readonly int W = w, H = h;
    public readonly Color[] Px = new Color[w * h];
    readonly List<(string text, int x, int y, int size, Color color)> texts = [];

    public void Plot(int x, int y, Color c)
    {
        if ((uint)x >= (uint)W || (uint)y >= (uint)H || c.A == 0) return;
        ref Color d = ref Px[y * W + x];
        if (c.A == 255 || d.A == 0) { d = c; return; }
        float a = c.A / 255f;
        d = new Color((byte)(d.R + (c.R - d.R) * a), (byte)(d.G + (c.G - d.G) * a), (byte)(d.B + (c.B - d.B) * a), Math.Max(d.A, c.A));
    }

    public void Rect(float x, float y, float w, float h, Color c)
    {
        int x0 = (int)MathF.Round(x), y0 = (int)MathF.Round(y), x1 = (int)MathF.Round(x + w), y1 = (int)MathF.Round(y + h);
        for (int j = y0; j < y1; j++)
            for (int i = x0; i < x1; i++) Plot(i, j, c);
    }

    public void Ellipse(float cx, float cy, float rx, float ry, Color c)
    {
        for (int j = (int)MathF.Floor(cy - ry); j <= (int)MathF.Ceiling(cy + ry); j++)
            for (int i = (int)MathF.Floor(cx - rx); i <= (int)MathF.Ceiling(cx + rx); i++)
            {
                float dx = (i + 0.5f - cx) / rx, dy = (j + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Plot(i, j, c);
            }
    }

    public void Circle(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, c);

    public void EraseEllipse(float cx, float cy, float rx, float ry)
    {
        for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float dx = (i + 0.5f - cx) / rx, dy = (j + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Px[j * W + i] = Color.Blank;
            }
    }

    public void Poly(Color c, params Vector2[] pts)
    {
        float minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        var xs = new List<float>();
        for (int j = (int)MathF.Floor(minY); j <= (int)MathF.Ceiling(maxY); j++)
        {
            float sy = j + 0.5f;
            xs.Clear();
            for (int k = 0; k < pts.Length; k++)
            {
                Vector2 a = pts[k], b = pts[(k + 1) % pts.Length];
                if ((a.Y <= sy && b.Y > sy) || (b.Y <= sy && a.Y > sy))
                    xs.Add(a.X + (sy - a.Y) / (b.Y - a.Y) * (b.X - a.X));
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
                for (int i = (int)MathF.Round(xs[k]); i < (int)MathF.Round(xs[k + 1]); i++) Plot(i, j, c);
        }
    }

    public void Line(float x0, float y0, float x1, float y1, float thick, Color c)
    {
        float len = MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0));
        int n = Math.Max(1, (int)MathF.Ceiling(len * 2));
        float r = MathF.Max(0.5f, thick / 2);
        for (int k = 0; k <= n; k++)
        {
            float t = k / (float)n;
            Ellipse(U.Lerp(x0, x1, t), U.Lerp(y0, y1, t), r, r, c);
        }
    }

    /// <summary>Applique une transformation aux pixels opaques.</summary>
    public void Map(Func<int, int, Color, Color> f)
    {
        for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                ref Color p = ref Px[j * W + i];
                if (p.A > 0) p = f(i, j, p);
            }
    }

    /// <summary>Contour d'un pixel autour des zones opaques (look arcade).</summary>
    public void Outline(Color c)
    {
        var src = (Color[])Px.Clone();
        for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                if (src[j * W + i].A >= 128) continue;
                bool edge = (i > 0 && src[j * W + i - 1].A >= 128) || (i < W - 1 && src[j * W + i + 1].A >= 128)
                         || (j > 0 && src[(j - 1) * W + i].A >= 128) || (j < H - 1 && src[(j + 1) * W + i].A >= 128);
                if (edge) Px[j * W + i] = c;
            }
    }

    public void Text(string s, int x, int y, int size, Color c) => texts.Add((s, x, y, size, c));

    public void TextCentered(string s, float cx, int y, int size, Color c)
    {
        int w = Raylib.MeasureText(s, size);
        texts.Add((s, (int)(cx - w / 2f), y, size, c));
    }

    /// <summary>Crée la texture et son masque (silhouette blanche utilisée pour le brouillard).</summary>
    public unsafe (Texture2D tex, Texture2D mask) ToTextures()
    {
        Image img = Raylib.GenImageColor(W, H, Color.Blank);
        fixed (Color* p = Px) Buffer.MemoryCopy(p, img.Data, (long)W * H * 4, (long)W * H * 4);
        foreach (var t in texts) Raylib.ImageDrawText(ref img, t.text, t.x, t.y, t.size, t.color);
        var tex = Raylib.LoadTextureFromImage(img);

        var data = (Color*)img.Data;
        for (int i = 0; i < W * H; i++) data[i] = new Color((byte)255, (byte)255, (byte)255, data[i].A);
        var mask = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        Raylib.SetTextureFilter(mask, TextureFilter.Point);
        return (tex, mask);
    }

    public unsafe Texture2D ToTexture()
    {
        Image img = Raylib.GenImageColor(W, H, Color.Blank);
        fixed (Color* p = Px) Buffer.MemoryCopy(p, img.Data, (long)W * H * 4, (long)W * H * 4);
        var tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        return tex;
    }
}
