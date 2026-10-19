using Raylib_cs;

namespace HighwayRun;

/// <summary>Police Russo One, embarquée dans le binaire et rastérisée à la taille exacte de chaque usage.</summary>
static class Fonts
{
    static byte[] data = [];
    static readonly Dictionary<int, Font> cache = [];

    public static void Load()
    {
        using var stream = typeof(Fonts).Assembly.GetManifestResourceStream("RussoOne.ttf")
                           ?? throw new InvalidOperationException("Police RussoOne.ttf introuvable dans les ressources");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        data = ms.ToArray();
    }

    /// <summary>La police rastérisée pour une hauteur donnée en pixels réels.</summary>
    public static Font Get(int pixelSize)
    {
        if (!cache.TryGetValue(pixelSize, out var font))
        {
            font = Raylib.LoadFontFromMemory(".ttf", data, pixelSize, null!, 0);
            Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
            cache[pixelSize] = font;
        }
        return font;
    }

    public static float Spacing(float size) => size / 20f;

    public static void Unload()
    {
        foreach (var f in cache.Values) Raylib.UnloadFont(f);
        cache.Clear();
    }
}
