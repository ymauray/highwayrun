using System.Globalization;

namespace HighwayRun;

/// <summary>Réglages conservés d'une partie à l'autre (à côté du meilleur score).</summary>
static class Settings
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HighwayRun", "settings.txt");

    public static float Music = 0.8f, Sfx = 0.8f;

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var kv = line.Split('=', 2);
                if (kv.Length != 2 || !float.TryParse(kv[1], CultureInfo.InvariantCulture, out float v)) continue;
                v = Math.Clamp(v, 0f, 1f);
                if (kv[0] == "music") Music = v;
                else if (kv[0] == "sfx") Sfx = v;
            }
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllLines(FilePath,
            [
                "music=" + Music.ToString(CultureInfo.InvariantCulture),
                "sfx=" + Sfx.ToString(CultureInfo.InvariantCulture),
            ]);
        }
        catch { }
    }
}
