using Raylib_cs;
using static HighwayRun.U;

namespace HighwayRun;

enum Backdrop { Sea, Hills, Canyon, Dunes, Snowy, Mountains, City, Town, Lake }

/// <summary>Palette, décor et caractère d'une étape.</summary>
sealed class StageTheme
{
    public required string Name;
    public Color SkyTop, SkyBottom, Fog;
    public Color Grass1, Grass2;
    public Color Rumble1 = Hex(0xF2F2F2), Rumble2 = Hex(0xD02828);
    public Color Road1 = Hex(0x8E8E96), Road2 = Hex(0x88888F);
    public Color Lane = Hex(0xF4F4F4);
    public Color Far, Near, Water = Hex(0x2E7FD0);
    public Color CloudTint = Color.White;
    public Backdrop Backdrop;
    public float FogDensity = 3f;
    public bool Sun, Clouds = true;
    public float Curviness = 0.5f, Hilliness = 0.5f, Traffic = 0.6f, Scatter = 0.12f;
    public (SpriteDef def, float weight)[] Rows = [];
    public (SpriteDef def, float weight)[] Props = [];
    public SpriteDef[] Billboards = [];
    public bool Arches;

    public Texture2D FarTex, NearTex;
}

static class Themes
{
    public static StageTheme[][] Levels = null!;
    public static StageTheme Get(int level, int branch) => Levels[level][branch];
    public static IEnumerable<StageTheme> All => Levels.SelectMany(l => l);

    public static void Init()
    {
        var bb = new[] { Art.BillboardCola, Art.BillboardOil, Art.BillboardRadio };

        var coconut = new StageTheme
        {
            Name = "COCONUT BEACH", SkyTop = Hex(0x1C6FE8), SkyBottom = Hex(0xA6DDFF), Fog = Hex(0xC4E8FF),
            Grass1 = Hex(0xF0DCA4), Grass2 = Hex(0xE6CF92), Far = Hex(0x5A8CB8), Near = Hex(0xD8C088), Water = Hex(0x2C86D8),
            Backdrop = Backdrop.Sea, FogDensity = 2.5f, Curviness = 0.35f, Hilliness = 0.25f, Traffic = 0.55f,
            Rows = [(Art.Palm, 4), (Art.PalmBent, 3)],
            Props = [(Art.Bush, 2), (Art.Umbrella, 2), (Art.Palm, 1)], Billboards = bb,
        };
        var gateway = new StageTheme
        {
            Name = "GATEWAY", SkyTop = Hex(0x2F86E8), SkyBottom = Hex(0xBDE3FF), Fog = Hex(0xCDEAFF),
            Grass1 = Hex(0x6BB04A), Grass2 = Hex(0x62A443), Far = Hex(0x6A9AB8), Near = Hex(0x4A8A3C),
            Backdrop = Backdrop.Hills, Curviness = 0.5f, Hilliness = 0.45f, Traffic = 0.6f, Arches = true,
            Rows = [(Art.Tree, 3), (Art.Bush, 2), (Art.Palm, 1)],
            Props = [(Art.Rock, 2), (Art.Bush, 2), (Art.BigRock, 1)], Billboards = bb,
        };
        var canyon = new StageTheme
        {
            Name = "DEVIL'S CANYON", SkyTop = Hex(0x3A7FD0), SkyBottom = Hex(0xF0C890), Fog = Hex(0xE8C49C),
            Grass1 = Hex(0xB4603A), Grass2 = Hex(0xA85634), Rumble2 = Hex(0x7A3020), Far = Hex(0x9A4A2E), Near = Hex(0x7A3420),
            Backdrop = Backdrop.Canyon, Curviness = 0.75f, Hilliness = 0.6f, Traffic = 0.5f,
            Rows = [(Art.RedRock, 3), (Art.BigRock, 2)],
            Props = [(Art.Rock, 3), (Art.DeadTree, 1), (Art.Cactus, 0.5f)],
        };
        var desert = new StageTheme
        {
            Name = "DESERT", SkyTop = Hex(0x3C8EE8), SkyBottom = Hex(0xFCE6B0), Fog = Hex(0xF8E4BC), Sun = true,
            Grass1 = Hex(0xE8C47A), Grass2 = Hex(0xDDB86E), Far = Hex(0xD9A860), Near = Hex(0xC89048),
            Backdrop = Backdrop.Dunes, Curviness = 0.3f, Hilliness = 0.5f, Traffic = 0.45f, Clouds = false,
            Rows = [(Art.Cactus, 4), (Art.Palm, 1)],
            Props = [(Art.Rock, 2), (Art.Skull, 1), (Art.DeadTree, 1), (Art.Shrub, 2)], Billboards = [Art.BillboardCola],
        };
        var alps = new StageTheme
        {
            Name = "ALPS", SkyTop = Hex(0x5AA0F0), SkyBottom = Hex(0xE6F4FF), Fog = Hex(0xE8F4FF),
            Grass1 = Hex(0x5FAE52), Grass2 = Hex(0x55A048), Far = Hex(0x8FA6C8), Near = Hex(0x3E7F48),
            Backdrop = Backdrop.Snowy, Curviness = 0.6f, Hilliness = 0.85f, Traffic = 0.5f,
            Rows = [(Art.Pine, 4), (Art.SnowPine, 2)],
            Props = [(Art.Chalet, 1), (Art.Bush, 2), (Art.Rock, 1)],
        };
        var cloudy = new StageTheme
        {
            Name = "CLOUDY MOUNTAIN", SkyTop = Hex(0x7E8C9C), SkyBottom = Hex(0xCAD2DA), Fog = Hex(0xC4CCD4), FogDensity = 6.5f,
            Grass1 = Hex(0x4F7A4A), Grass2 = Hex(0x47703F), Far = Hex(0x6F7F8F), Near = Hex(0x4A5E52), CloudTint = Hex(0xB8C0C8),
            Backdrop = Backdrop.Mountains, Curviness = 0.95f, Hilliness = 0.7f, Traffic = 0.45f,
            Rows = [(Art.Pine, 5)],
            Props = [(Art.Rock, 2), (Art.BigRock, 1), (Art.Bush, 1)],
        };
        var wilderness = new StageTheme
        {
            Name = "WILDERNESS", SkyTop = Hex(0x4A90E0), SkyBottom = Hex(0xF8E0A8), Fog = Hex(0xF0DCB0), Sun = true,
            Grass1 = Hex(0xA8A048), Grass2 = Hex(0x9C9440), Far = Hex(0x9A8458), Near = Hex(0x6E7038),
            Backdrop = Backdrop.Canyon, Curviness = 0.55f, Hilliness = 0.55f, Traffic = 0.5f,
            Rows = [(Art.Acacia, 2), (Art.DeadTree, 2)],
            Props = [(Art.Rock, 2), (Art.Shrub, 3), (Art.BigRock, 1)], Billboards = [Art.BillboardOil],
        };
        var capital = new StageTheme
        {
            Name = "OLD CAPITAL", SkyTop = Hex(0x4C7CD8), SkyBottom = Hex(0xFFD2BC), Fog = Hex(0xF8D8CC),
            Grass1 = Hex(0x6EA850), Grass2 = Hex(0x64A048), Rumble2 = Hex(0x404048), Far = Hex(0x7C6C8C), Near = Hex(0x4C5C4C),
            Backdrop = Backdrop.City, Curviness = 0.6f, Hilliness = 0.35f, Traffic = 0.6f,
            Rows = [(Art.Lantern, 3), (Art.Cherry, 3)],
            Props = [(Art.Pagoda, 1), (Art.Pine, 1), (Art.Cherry, 1)],
        };
        var wheat = new StageTheme
        {
            Name = "WHEAT FIELD", SkyTop = Hex(0x4A9AF0), SkyBottom = Hex(0xFFF0C0), Fog = Hex(0xFFF0D0),
            Grass1 = Hex(0xE0C050), Grass2 = Hex(0xD4B448), Far = Hex(0x8AAA60), Near = Hex(0xB09A40),
            Backdrop = Backdrop.Hills, Curviness = 0.35f, Hilliness = 0.5f, Traffic = 0.55f,
            Rows = [(Art.Poplar, 3), (Art.HayBale, 2)],
            Props = [(Art.Windmill, 1), (Art.HayBale, 2), (Art.Bush, 1)], Billboards = bb,
        };
        var seaside = new StageTheme
        {
            Name = "SEASIDE TOWN", SkyTop = Hex(0x2A80F0), SkyBottom = Hex(0xB0E0FF), Fog = Hex(0xC8E8FF),
            Grass1 = Hex(0x78B060), Grass2 = Hex(0x70A858), Far = Hex(0x6090B8), Near = Hex(0xE8D8C0), Water = Hex(0x2A80D0),
            Backdrop = Backdrop.Town, Curviness = 0.55f, Hilliness = 0.4f, Traffic = 0.7f,
            Rows = [(Art.Lamp, 3), (Art.Palm, 2)],
            Props = [(Art.House, 2), (Art.HouseBlue, 2), (Art.Lighthouse, 0.3f), (Art.Bush, 1)], Billboards = bb,
        };
        var vineyard = new StageTheme
        {
            Name = "VINEYARD", SkyTop = Hex(0x4C90E8), SkyBottom = Hex(0xFFE6B8), Fog = Hex(0xF8E4C4),
            Grass1 = Hex(0x7AA84A), Grass2 = Hex(0x70A040), Far = Hex(0x8C9C6C), Near = Hex(0x5C7C3C),
            Backdrop = Backdrop.Hills, Curviness = 0.55f, Hilliness = 0.55f, Traffic = 0.55f,
            Rows = [(Art.Vine, 4), (Art.Cypress, 3)],
            Props = [(Art.House, 1), (Art.Cypress, 1), (Art.Bush, 1)],
        };
        var deathValley = new StageTheme
        {
            Name = "DEATH VALLEY", SkyTop = Hex(0xE07840), SkyBottom = Hex(0xFFC870), Fog = Hex(0xF8C088), Sun = true,
            Grass1 = Hex(0xA87850), Grass2 = Hex(0x9C6E48), Far = Hex(0x8C5838), Near = Hex(0x6C4028), Clouds = false,
            Backdrop = Backdrop.Canyon, Curviness = 0.6f, Hilliness = 0.6f, Traffic = 0.4f,
            Rows = [(Art.Cactus, 3), (Art.RedRock, 1)],
            Props = [(Art.Rock, 3), (Art.DeadTree, 2), (Art.Skull, 1)],
        };
        var desolation = new StageTheme
        {
            Name = "DESOLATION HILL", SkyTop = Hex(0x50607A), SkyBottom = Hex(0xB8A898), Fog = Hex(0xB0A49C), FogDensity = 4.5f,
            Grass1 = Hex(0x7C786C), Grass2 = Hex(0x747066), Far = Hex(0x5C5C6C), Near = Hex(0x4C4A48), CloudTint = Hex(0xA0A0A8),
            Backdrop = Backdrop.Mountains, Curviness = 0.7f, Hilliness = 0.8f, Traffic = 0.4f,
            Rows = [(Art.Column, 3), (Art.DeadTree, 2)],
            Props = [(Art.Rock, 3), (Art.BigRock, 1), (Art.DeadTree, 1)],
        };
        var autobahn = new StageTheme
        {
            Name = "AUTOBAHN", SkyTop = Hex(0x3A78D8), SkyBottom = Hex(0xD0E8FF), Fog = Hex(0xD8ECFF),
            Grass1 = Hex(0x5C9C4C), Grass2 = Hex(0x549444), Road1 = Hex(0x74747C), Road2 = Hex(0x6E6E76), Rumble2 = Hex(0x2050C0),
            Far = Hex(0x7090A8), Near = Hex(0x4C7C44),
            Backdrop = Backdrop.Hills, Curviness = 0.2f, Hilliness = 0.3f, Traffic = 0.9f,
            Rows = [(Art.Poplar, 3), (Art.Lamp, 2)],
            Props = [(Art.HighwaySign, 1), (Art.Pine, 2), (Art.Windmill, 0.4f)], Billboards = bb,
        };
        var lakeside = new StageTheme
        {
            Name = "LAKESIDE", SkyTop = Hex(0x4A98F0), SkyBottom = Hex(0xD8F0FF), Fog = Hex(0xDCF0FF),
            Grass1 = Hex(0x5CB060), Grass2 = Hex(0x54A458), Far = Hex(0x7A94B8), Near = Hex(0x3E7A48), Water = Hex(0x4A9AD8),
            Backdrop = Backdrop.Lake, Curviness = 0.5f, Hilliness = 0.45f, Traffic = 0.5f,
            Rows = [(Art.Birch, 3), (Art.Pine, 2)],
            Props = [(Art.Bush, 2), (Art.House, 0.5f), (Art.Rock, 1)],
        };

        Levels =
        [
            [coconut],
            [gateway, canyon],
            [desert, alps, cloudy],
            [wilderness, capital, wheat, seaside],
            [vineyard, deathValley, desolation, autobahn, lakeside],
        ];
    }
}
