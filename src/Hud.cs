using System.Numerics;
using Raylib_cs;

namespace HighwayRun;

/// <summary>Texte avec la police bitmap intégrée de raylib et carte du parcours.</summary>
static class Hud
{
    static Font Font => Raylib.GetFontDefault();

    public static float Measure(string s, int size) => Raylib.MeasureTextEx(Font, s, size, size / 10f).X;

    public static void Text(string s, float x, float y, int size, Color c, bool shadow = true)
    {
        float sp = size / 10f;
        if (shadow) Raylib.DrawTextEx(Font, s, new Vector2(MathF.Round(x) + size / 10, MathF.Round(y) + size / 10), size, sp, U.Fade(Color.Black, c.A / 255f * 0.7f));
        Raylib.DrawTextEx(Font, s, new Vector2(MathF.Round(x), MathF.Round(y)), size, sp, c);
    }

    public static void TextC(string s, float cx, float y, int size, Color c, bool shadow = true) => Text(s, cx - Measure(s, size) / 2, y, size, c, shadow);
    public static void TextR(string s, float rx, float y, int size, Color c, bool shadow = true) => Text(s, rx - Measure(s, size), y, size, c, shadow);

    /// <summary>Texte « arcade » : contour sombre épais puis remplissage.</summary>
    public static void Bold(string s, float cx, float y, int size, Color fill, Color outline)
    {
        float x = cx - Measure(s, size) / 2;
        int o = Math.Max(1, size / 10);
        for (int dx = -o; dx <= o; dx += o)
            for (int dy = -o; dy <= o; dy += o)
                if (dx != 0 || dy != 0) Text(s, x + dx, y + dy, size, outline, false);
        Text(s, x, y, size, fill, false);
    }

    public static Vector2 Node(float x0, float y0, float colW, float rowH, int level, int idx) =>
        new(x0 + level * colW, y0 + (idx - level / 2f) * rowH);

    /// <summary>La pyramide des 15 étapes, avec le chemin parcouru en surbrillance.</summary>
    public static void CourseMap(float x0, float y0, float colW, float rowH, IReadOnlyList<int> path, bool names, float t, float r = 4)
    {
        var dim = U.Hex(0x6A6A80);
        for (int l = 0; l < 4; l++)
            for (int i = 0; i <= l; i++)
            {
                var a = Node(x0, y0, colW, rowH, l, i);
                Raylib.DrawLineEx(a, Node(x0, y0, colW, rowH, l + 1, i), 1, dim);
                Raylib.DrawLineEx(a, Node(x0, y0, colW, rowH, l + 1, i + 1), 1, dim);
            }
        for (int l = 0; l + 1 < path.Count; l++)
            Raylib.DrawLineEx(Node(x0, y0, colW, rowH, l, path[l]), Node(x0, y0, colW, rowH, l + 1, path[l + 1]), r > 3 ? 3 : 2, U.Hex(0xFFD830));

        for (int l = 0; l < 5; l++)
            for (int i = 0; i <= l; i++)
            {
                var p = Node(x0, y0, colW, rowH, l, i);
                var th = Themes.Get(l, i);
                bool visited = l < path.Count && path[l] == i;
                bool current = visited && l == path.Count - 1;
                float rr = current ? r + (MathF.Sin(t * 8) > 0 ? 1 : 0) : r;
                Raylib.DrawCircleV(p, rr + 1, Color.Black);
                Raylib.DrawCircleV(p, rr, visited ? th.Grass1 : U.Mix(th.Grass1, U.Hex(0x303040), 0.6f));
                if (names)
                    TextC(th.Name, p.X, p.Y + r + 3, 10, visited ? Color.White : U.Hex(0x9A9AB0));
                if (l == 4 && names)
                    Text(((char)('A' + i)).ToString(), p.X + r + 6, p.Y - 4, 10, visited ? U.Hex(0xFFD830) : dim);
            }
    }
}
