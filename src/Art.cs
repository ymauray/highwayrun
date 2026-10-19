using System.Numerics;
using Raylib_cs;
using static HighwayRun.U;

namespace HighwayRun;

sealed class SpriteDef
{
    public Texture2D Tex, Mask;
    public float WorldW;            // largeur en unités du monde
    public bool Collide;
    public float Hit = 0.4f;        // fraction de la largeur qui provoque une collision
    public int Spacing = 6;         // espacement (en segments) quand le décor est posé en rangée
    public bool Truck;
    public SpriteDef? Mirror;       // variante utilisée côté droit de la route
    public bool Visible => Tex.Id != 0;
}

/// <summary>Tous les graphismes sont générés procéduralement au démarrage : aucun fichier externe.</summary>
static class Art
{
    public static SpriteDef Palm = null!, PalmBent = null!, Bush = null!, Shrub = null!, Rock = null!, BigRock = null!,
        RedRock = null!, Cactus = null!, Pine = null!, SnowPine = null!, DeadTree = null!, Tree = null!, Acacia = null!,
        Chalet = null!, Pagoda = null!, Lantern = null!, Cherry = null!, Windmill = null!, HayBale = null!,
        Poplar = null!, House = null!, HouseBlue = null!, Lamp = null!, Lighthouse = null!, Vine = null!,
        Cypress = null!, Skull = null!, Column = null!, Birch = null!, Umbrella = null!,
        BillboardCola = null!, BillboardOil = null!, BillboardRadio = null!, Chevron = null!, ChevronRight = null!, ForkSign = null!,
        HighwaySign = null!, Arch = null!, GateStart = null!, GateCheckpoint = null!, GateGoal = null!,
        Crowd = null!, Pillar = null!;

    public static SpriteDef PlayerStraight = null!, PlayerLeft = null!, PlayerRight = null!;
    public static readonly List<SpriteDef> Traffic = [];
    static readonly Dictionary<(string, bool), SpriteDef> destSigns = [];

    static readonly Color Ink = Hex(0x181018);

    static SpriteDef Make(Canvas c, float worldW, bool collide = true, float hit = 0.4f, int spacing = 6, bool outline = true)
    {
        if (outline) c.Outline(Ink);
        var (tex, mask) = c.ToTextures();
        return new SpriteDef { Tex = tex, Mask = mask, WorldW = worldW, Collide = collide, Hit = hit, Spacing = spacing };
    }

    static Vector2 V(float x, float y) => new(x, y);

    public static void Load()
    {
        Palm = Make(DrawPalm(false), 1150, hit: 0.22f, spacing: 5);
        PalmBent = Make(DrawPalm(true), 1150, hit: 0.22f, spacing: 5);
        Bush = Make(DrawBush(Hex(0x2E7A2E), Hex(0x3E9038), Hex(0x62BC56)), 650, hit: 0.7f, spacing: 3);
        Shrub = Make(DrawBush(Hex(0x7A7A34), Hex(0x8E8C40), Hex(0xB0AA5A)), 550, hit: 0.7f, spacing: 3);
        Rock = Make(DrawRock(1f, Hex(0x8C8A86)), 700, hit: 0.8f);
        BigRock = Make(DrawRock(2.1f, Hex(0x9A8E80)), 1700, hit: 0.85f, spacing: 10);
        RedRock = Make(DrawRedRock(), 1400, hit: 0.75f, spacing: 9);
        Cactus = Make(DrawCactus(), 600, hit: 0.35f, spacing: 6);
        Pine = Make(DrawPine(false), 950, hit: 0.3f, spacing: 4);
        SnowPine = Make(DrawPine(true), 950, hit: 0.3f, spacing: 4);
        DeadTree = Make(DrawDeadTree(), 1000, hit: 0.2f, spacing: 7);
        Tree = Make(DrawRoundTree(false, Hex(0x3C8A34), Hex(0x58A848)), 1350, hit: 0.2f, spacing: 6);
        Acacia = Make(DrawRoundTree(true, Hex(0x6E8A30), Hex(0x8CA840)), 1500, hit: 0.15f, spacing: 9);
        Chalet = Make(DrawChalet(), 2000, hit: 0.9f, spacing: 14);
        Pagoda = Make(DrawPagoda(), 2300, hit: 0.8f, spacing: 16);
        Lantern = Make(DrawLantern(), 330, hit: 0.8f, spacing: 4);
        Cherry = Make(DrawCherry(), 1450, hit: 0.2f, spacing: 5);
        Windmill = Make(DrawWindmill(), 2300, hit: 0.4f, spacing: 18);
        HayBale = Make(DrawHayBale(), 520, hit: 0.9f, spacing: 5);
        Poplar = Make(DrawPoplar(), 640, hit: 0.3f, spacing: 3);
        House = Make(DrawHouse(Hex(0xF2E8D6), Hex(0xC2582E)), 2100, hit: 0.95f, spacing: 10);
        HouseBlue = Make(DrawHouse(Hex(0xE4F0F8), Hex(0x3A6EC0)), 2100, hit: 0.95f, spacing: 10);
        Lamp = Make(DrawLamp(false), 420, hit: 0.15f, spacing: 6);
        Lamp.Mirror = Make(DrawLamp(true), 420, hit: 0.15f, spacing: 6);
        Lighthouse = Make(DrawLighthouse(), 900, hit: 0.6f, spacing: 30);
        Vine = Make(DrawVine(), 760, hit: 0.9f, spacing: 3);
        Cypress = Make(DrawCypress(), 480, hit: 0.5f, spacing: 4);
        Skull = Make(DrawSkull(), 420, collide: false);
        Column = Make(DrawColumn(), 520, hit: 0.7f, spacing: 6);
        Birch = Make(DrawBirch(), 850, hit: 0.2f, spacing: 4);
        Umbrella = Make(DrawUmbrella(), 760, hit: 0.15f, spacing: 6);
        BillboardCola = Make(DrawBillboard("SUN COLA", Hex(0xD82828), Color.White), 2200, hit: 0.9f, spacing: 20);
        BillboardOil = Make(DrawBillboard("TURBO OIL", Hex(0xF0C020), Hex(0x202020)), 2200, hit: 0.9f, spacing: 20);
        BillboardRadio = Make(DrawBillboard("FM HIGHWAY", Hex(0x2058C0), Hex(0xFFE060)), 2200, hit: 0.9f, spacing: 20);
        Chevron = Make(DrawChevron(false), 600, hit: 0.3f);
        ChevronRight = Make(DrawChevron(true), 600, hit: 0.3f);
        ForkSign = Make(DrawForkSign(), 1000, hit: 0.9f);
        HighwaySign = Make(DrawPanelSign("AUSFAHRT", Hex(0x2050B0), false), 1900, hit: 0.9f, spacing: 30);
        Arch = Make(DrawArch(), 2 * 1.45f * Cfg.RoadWidth, collide: false);
        GateStart = Make(DrawGate("START", Hex(0x2048B8), Hex(0xE8E8F0)), 2 * 1.45f * Cfg.RoadWidth, collide: false);
        GateCheckpoint = Make(DrawGate("CHECKPOINT", Hex(0xE03020), Hex(0xF4F4F4)), 2 * 1.45f * Cfg.RoadWidth, collide: false);
        GateGoal = Make(DrawGate("GOAL", Hex(0x101010), Hex(0xF4D030)), 2 * 1.45f * Cfg.RoadWidth, collide: false);
        Crowd = Make(DrawCrowd(), 1500, hit: 0.9f, spacing: 3);
        Pillar = new SpriteDef { WorldW = 0.4f * Cfg.RoadWidth, Collide = true, Hit = 1f };

        PlayerStraight = Make(DrawPlayer(0), 104f / (Cfg.CameraDepth / Cfg.PlayerZ * Cfg.Width / 2f), collide: false);
        PlayerLeft = Make(DrawPlayer(-1), PlayerStraight.WorldW, collide: false);
        PlayerRight = Make(DrawPlayer(1), PlayerStraight.WorldW, collide: false);

        AddCar(CarKind.Beetle, Hex(0xF0C828), 380);
        AddCar(CarKind.Beetle, Hex(0x3C78D8), 380);
        AddCar(CarKind.Beetle, Hex(0xF0F0E8), 380);
        AddCar(CarKind.Sedan, Hex(0x3A9A50), 430);
        AddCar(CarKind.Sedan, Hex(0xE8E8E8), 430);
        AddCar(CarKind.Sedan, Hex(0x8A8A98), 430);
        AddCar(CarKind.Van, Hex(0xF4F4F0), 440);
        AddCar(CarKind.Van, Hex(0x60B0E0), 440);
        AddCar(CarKind.Truck, Hex(0xE88A20), 540);
        AddCar(CarKind.Truck, Hex(0x2E6EC8), 540);
        AddCar(CarKind.Sport, Hex(0xF4F4F4), 460);
        AddCar(CarKind.Sport, Hex(0x202028), 460);
        AddCar(CarKind.Sport, Hex(0xF0D820), 460);
    }

    /// <summary>Panneau directionnel (vert) portant le nom de l'étape suivante.</summary>
    public static SpriteDef DestSign(string name, bool left)
    {
        if (!destSigns.TryGetValue((name, left), out var def))
        {
            def = Make(DrawPanelSign(name, Hex(0x1E7A3A), left), 2000, hit: 0.9f);
            destSigns[(name, left)] = def;
        }
        return def;
    }

    // ---------------------------------------------------------------- végétation

    static Canvas DrawPalm(bool bent)
    {
        var c = new Canvas(64, 104);
        Color tr = Hex(0x9C7442), trd = Hex(0x6E4E28);
        float bx = bent ? 22 : 32, tx = bent ? 44 : 33, ty = 24;
        for (int k = 0; k <= 32; k++)
        {
            float t = k / 32f;
            float x = Lerp(bx, tx, t) + (bent ? -MathF.Sin(t * MathF.PI) * 5 : MathF.Sin(t * 5) * 1.2f);
            float y = Lerp(103, ty, t);
            c.Ellipse(x, y, Lerp(4.2f, 2.6f, t), 2.2f, k % 3 == 0 ? trd : tr);
        }
        Color g1 = Hex(0x2E8C34), g2 = Hex(0x1E6A26), g3 = Hex(0x48AC44);
        (float a, float len, float droop)[] fronds =
            [(195, 28, 16), (225, 24, 12), (255, 18, 8), (285, 18, 8), (315, 24, 12), (345, 28, 16), (165, 22, 18), (15, 22, 18)];
        foreach (var (a, len, droop) in fronds) Leaf(c, tx, ty, a * MathF.PI / 180f, len, droop, 5f, g1, g2, g3);
        c.Circle(tx - 3, ty + 3, 2.6f, Hex(0x5A3A1A));
        c.Circle(tx + 2, ty + 4, 2.6f, Hex(0x6A4420));
        return c;
    }

    static void Leaf(Canvas c, float x0, float y0, float a, float len, float droop, float width, Color c1, Color c2, Color hi)
    {
        int n = (int)(len * 1.5f);
        for (int k = 0; k <= n; k++)
        {
            float t = k / (float)n;
            float x = x0 + MathF.Cos(a) * len * t, y = y0 + MathF.Sin(a) * len * t + droop * t * t;
            float w = width * (1 - t * 0.75f) * (0.55f + 0.45f * MathF.Sin(t * MathF.PI));
            c.Ellipse(x, y + 1, w * 0.6f + 0.6f, w * 0.5f + 0.6f, c2);
            c.Ellipse(x, y, w * 0.5f + 0.5f, w * 0.35f + 0.5f, k % 5 == 0 ? hi : c1);
            if (k % 3 == 0 && t > 0.15f) c.Line(x, y, x - MathF.Cos(a) * 1.5f, y + w * 1.4f + 1.5f, 1.2f, c2);
        }
    }

    static Canvas DrawBush(Color dark, Color mid, Color light)
    {
        var c = new Canvas(48, 30);
        c.Ellipse(24, 21, 22, 9, dark);
        c.Ellipse(14, 16, 10, 9, mid);
        c.Ellipse(32, 14, 12, 10, mid);
        c.Ellipse(23, 11, 9, 8, mid);
        c.Ellipse(20, 8, 4, 3, light);
        c.Ellipse(33, 9, 4, 3, light);
        c.Ellipse(11, 13, 3, 2.5f, light);
        return c;
    }

    static Canvas DrawRock(float s, Color baseCol)
    {
        int w = (int)(44 * s), h = (int)(30 * s);
        var c = new Canvas(w, h);
        Vector2 P(float x, float y) => V(x * s, y * s);
        c.Poly(baseCol, P(1, 30), P(4, 16), P(12, 7), P(24, 2), P(35, 7), P(42, 17), P(43.5f, 30));
        c.Poly(Shade(baseCol, 1.2f), P(7, 17), P(13, 9), P(24, 4), P(21, 13));
        c.Poly(Shade(baseCol, 0.7f), P(1, 30), P(43.5f, 30), P(42, 22), P(24, 25), P(4, 24));
        c.Line(26 * s, 8 * s, 30 * s, 20 * s, s, Shade(baseCol, 0.75f));
        return c;
    }

    static Canvas DrawRedRock()
    {
        var c = new Canvas(56, 92);
        Color r = Hex(0xB85A34);
        c.Poly(r, V(3, 92), V(9, 62), V(13, 32), V(19, 12), V(29, 5), V(39, 13), V(44, 36), V(48, 64), V(54, 92));
        c.Poly(Hex(0xD8784A), V(15, 32), V(20, 14), V(28, 8), V(25, 40), V(19, 70), V(12, 72));
        c.Map((x, y, p) => y % 11 < 2 ? Shade(p, 0.78f) : p);
        return c;
    }

    static Canvas DrawCactus()
    {
        var c = new Canvas(36, 66);
        Color g = Hex(0x3C8C3C), gl = Hex(0x62B456), gd = Hex(0x2A6A2A);
        c.Rect(14, 8, 9, 58, g); c.Ellipse(18.5f, 8.5f, 4.5f, 4.5f, g);
        c.Rect(6, 33, 9, 6, g); c.Rect(5, 18, 6, 21, g); c.Ellipse(8, 18, 3, 3, g);
        c.Rect(22, 26, 8, 6, g); c.Rect(27, 12, 6, 20, g); c.Ellipse(30, 12, 3, 3, g);
        c.Rect(16, 7, 1, 59, gl); c.Rect(20, 7, 1, 59, gd); c.Rect(7, 17, 1, 20, gl); c.Rect(29, 11, 1, 19, gl);
        return c;
    }

    static Canvas DrawPine(bool snow)
    {
        var c = new Canvas(44, 96);
        c.Rect(19, 78, 6, 18, Hex(0x5A3A1E));
        Color d = Hex(0x1E5A30), l = Hex(0x2E7A40);
        for (int k = 0; k < 4; k++)
        {
            float top = 2 + k * 16, bottom = 36 + k * 15, half = 8 + k * 5.5f;
            c.Poly(d, V(22, top), V(22 + half, bottom), V(22 - half, bottom));
            c.Poly(l, V(22, top + 2), V(22, bottom - 3), V(22 - half + 3, bottom - 3));
            if (snow) c.Poly(Hex(0xF4F8FF), V(22, top), V(22 + half * 0.35f, top + (bottom - top) * 0.3f), V(22 - half * 0.35f, top + (bottom - top) * 0.3f));
        }
        return c;
    }

    static Canvas DrawDeadTree()
    {
        var c = new Canvas(56, 80);
        Color b = Hex(0x76604C);
        c.Line(28, 79, 28, 40, 5, b);
        c.Line(28, 54, 14, 30, 3, b); c.Line(14, 30, 8, 18, 2, b); c.Line(14, 32, 4, 30, 1.5f, b);
        c.Line(28, 46, 42, 26, 3, b); c.Line(42, 26, 48, 12, 2, b); c.Line(42, 28, 52, 30, 1.5f, b);
        c.Line(28, 40, 30, 10, 3, b); c.Line(30, 22, 22, 8, 2, b); c.Line(29, 18, 36, 6, 1.5f, b);
        return c;
    }

    static Canvas DrawRoundTree(bool acacia, Color cc, Color light)
    {
        var c = new Canvas(64, 80);
        Color trunk = Hex(0x6A4A2A);
        if (acacia)
        {
            c.Line(32, 79, 32, 40, 4, trunk); c.Line(32, 50, 18, 28, 2.5f, trunk); c.Line(32, 46, 46, 28, 2.5f, trunk);
            c.Ellipse(32, 26, 31, 9, Shade(cc, 0.8f)); c.Ellipse(32, 22, 26, 8, cc); c.Ellipse(26, 19, 10, 4, light);
        }
        else
        {
            c.Rect(29, 46, 6, 34, trunk);
            c.Ellipse(32, 34, 24, 18, Shade(cc, 0.8f)); c.Ellipse(21, 27, 13, 12, cc); c.Ellipse(43, 25, 14, 13, cc);
            c.Ellipse(32, 17, 15, 13, cc); c.Ellipse(27, 13, 6, 5, light); c.Ellipse(16, 24, 5, 4, light); c.Ellipse(41, 19, 5, 4, light);
        }
        return c;
    }

    static Canvas DrawCherry()
    {
        var c = new Canvas(72, 64);
        Color trunk = Hex(0x4A2E22);
        c.Line(36, 63, 36, 36, 4, trunk); c.Line(36, 42, 22, 28, 2.5f, trunk); c.Line(36, 40, 52, 26, 2.5f, trunk);
        Color p1 = Hex(0xE88AB0), p2 = Hex(0xF4A8C8), p3 = Hex(0xFFD0E2);
        c.Ellipse(36, 26, 32, 14, p1); c.Ellipse(22, 22, 14, 12, p2); c.Ellipse(50, 20, 15, 12, p2);
        c.Ellipse(36, 14, 16, 11, p2); c.Ellipse(30, 11, 6, 4, p3); c.Ellipse(48, 15, 5, 4, p3); c.Ellipse(16, 20, 5, 4, p3);
        return c;
    }

    static Canvas DrawPoplar()
    {
        var c = new Canvas(26, 100);
        c.Rect(12, 84, 3, 16, Hex(0x5A3A1E));
        c.Ellipse(13, 48, 11, 42, Hex(0x2E6A2C)); c.Ellipse(10, 44, 6, 34, Hex(0x3E8238)); c.Ellipse(9, 30, 2.5f, 12, Hex(0x58A04C));
        return c;
    }

    static Canvas DrawCypress()
    {
        var c = new Canvas(22, 86);
        c.Rect(10, 78, 3, 8, Hex(0x5A3A1E));
        c.Poly(Hex(0x24502A), V(11, 0), V(17, 18), V(20, 48), V(17, 74), V(11, 80), V(5, 74), V(2, 48), V(5, 18));
        c.Poly(Hex(0x346A36), V(11, 4), V(8, 20), V(6, 48), V(8, 70), V(11, 74));
        return c;
    }

    static Canvas DrawBirch()
    {
        var c = new Canvas(36, 92);
        c.Rect(16, 30, 5, 62, Hex(0xF0F0EA));
        for (int y = 34; y < 90; y += 7) c.Rect(16 + (y % 3), y, 3, 2, Hex(0x303030));
        c.Ellipse(18, 26, 16, 22, Hex(0x6EA83C)); c.Ellipse(13, 20, 9, 12, Hex(0x8CC04C)); c.Ellipse(24, 16, 7, 9, Hex(0x9CD05C));
        return c;
    }

    static Canvas DrawVine()
    {
        var c = new Canvas(48, 30);
        c.Rect(2, 6, 3, 24, Hex(0x6A4A2A)); c.Rect(43, 6, 3, 24, Hex(0x6A4A2A));
        for (int x = 4; x < 46; x += 6) c.Ellipse(x, 14, 6, 7, x % 12 == 4 ? Hex(0x3E8A30) : Hex(0x4E9C3A));
        for (int x = 8; x < 44; x += 9) { c.Circle(x, 22, 2, Hex(0x5A2A6A)); c.Circle(x + 2, 23, 2, Hex(0x6A3480)); c.Circle(x + 1, 25, 1.5f, Hex(0x5A2A6A)); }
        return c;
    }

    // ---------------------------------------------------------------- bâtiments et objets

    static Canvas DrawChalet()
    {
        var c = new Canvas(80, 66);
        c.Rect(8, 30, 64, 36, Hex(0xF0E6D0));
        c.Rect(8, 50, 64, 16, Hex(0xA06A3A));
        c.Poly(Hex(0x6A3A1E), V(0, 36), V(40, 4), V(80, 36));
        c.Poly(Hex(0xB07840), V(12, 34), V(40, 12), V(68, 34));
        c.Rect(16, 38, 10, 9, Hex(0x2A3A5A)); c.Rect(54, 38, 10, 9, Hex(0x2A3A5A));
        c.Rect(13, 38, 3, 9, Hex(0x2E7A40)); c.Rect(26, 38, 3, 9, Hex(0x2E7A40)); c.Rect(51, 38, 3, 9, Hex(0x2E7A40)); c.Rect(64, 38, 3, 9, Hex(0x2E7A40));
        c.Rect(34, 52, 12, 14, Hex(0x4A2A14));
        c.Rect(36, 22, 8, 7, Hex(0x2A3A5A));
        c.Rect(8, 48, 64, 2, Hex(0x5A3418));
        return c;
    }

    static Canvas DrawPagoda()
    {
        var c = new Canvas(72, 112);
        c.Rect(35, 0, 2, 18, Hex(0xD0A030));
        for (int k = 0; k < 3; k++)
        {
            float y = 16 + k * 30, wall = 11 + k * 4, w = 22 + k * 8;
            c.Rect(36 - wall, y + 8, wall * 2, 22, Hex(0xC0382A));
            c.Rect(36 - wall + 3, y + 13, wall * 2 - 6, 10, Hex(0xF0E8D8));
            for (float x = 36 - wall + 6; x < 36 + wall - 4; x += 6) c.Rect(x, y + 13, 1, 10, Hex(0x8A2A20));
            c.Poly(Hex(0x3A4250), V(36 - w - 5, y + 11), V(36 - w + 6, y + 3), V(36 - 6, y - 2), V(36 + 6, y - 2), V(36 + w - 6, y + 3), V(36 + w + 5, y + 11), V(36 + w - 4, y + 9), V(36 - w + 4, y + 9));
            c.Rect(36 - w + 4, y + 8, (w - 4) * 2, 2, Hex(0x5A6474));
        }
        c.Rect(12, 104, 48, 8, Hex(0x9A9A92));
        return c;
    }

    static Canvas DrawLantern()
    {
        var c = new Canvas(20, 46);
        Color st = Hex(0xA8A49C);
        c.Rect(3, 40, 14, 6, st); c.Rect(7, 24, 6, 16, st); c.Rect(4, 14, 12, 10, st);
        c.Rect(7, 16, 6, 6, Hex(0xFFD860));
        c.Poly(Shade(st, 0.8f), V(0, 15), V(10, 6), V(20, 15));
        c.Circle(10, 5, 2, st);
        return c;
    }

    static Canvas DrawWindmill()
    {
        var c = new Canvas(88, 120);
        c.Poly(Hex(0xB8A080), V(30, 120), V(58, 120), V(52, 44), V(36, 44));
        c.Rect(40, 104, 8, 16, Hex(0x5A3A20)); c.Rect(41, 70, 6, 8, Hex(0x2A3A5A));
        c.Poly(Hex(0x6A4A30), V(33, 47), V(44, 33), V(55, 47));
        Color sail = Hex(0xF0E8D8), lat = Hex(0x9A8A70);
        for (int k = 0; k < 4; k++)
        {
            float a = (45 + 90 * k) * MathF.PI / 180f;
            float ex = 44 + MathF.Cos(a) * 42, ey = 40 + MathF.Sin(a) * 42;
            c.Line(44, 40, ex, ey, 1.5f, lat);
            float px = -MathF.Sin(a) * 4, py = MathF.Cos(a) * 4;
            float sx = 44 + MathF.Cos(a) * 10, sy = 40 + MathF.Sin(a) * 10;
            c.Poly(sail, V(sx, sy), V(ex, ey), V(ex + px * 1.6f, ey + py * 1.6f), V(sx + px * 1.6f, sy + py * 1.6f));
            for (int s = 1; s < 5; s++)
            {
                float t = 0.25f + s * 0.17f;
                float lx = 44 + MathF.Cos(a) * 42 * t, ly = 40 + MathF.Sin(a) * 42 * t;
                c.Line(lx, ly, lx + px * 1.6f, ly + py * 1.6f, 1, lat);
            }
        }
        c.Circle(44, 40, 3, Hex(0x3A2A1A));
        return c;
    }

    static Canvas DrawHayBale()
    {
        var c = new Canvas(36, 28);
        Color h = Hex(0xD8B050);
        c.Rect(10, 8, 22, 19, Shade(h, 0.85f)); c.Ellipse(31, 17.5f, 4, 9.5f, Shade(h, 0.85f));
        c.Ellipse(10, 17.5f, 8, 9.5f, h);
        c.Ellipse(10, 17.5f, 5, 6, Shade(h, 0.8f)); c.Ellipse(10, 17.5f, 3, 3.5f, h); c.Ellipse(10, 17.5f, 1, 1.5f, Shade(h, 0.8f));
        for (int x = 14; x < 32; x += 4) c.Rect(x, 9, 1, 17, Shade(h, 0.72f));
        return c;
    }

    static Canvas DrawHouse(Color wall, Color roof)
    {
        var c = new Canvas(80, 64);
        c.Rect(6, 24, 68, 40, wall);
        c.Poly(roof, V(0, 26), V(14, 6), V(66, 6), V(80, 26));
        c.Rect(0, 24, 80, 3, Shade(roof, 0.75f));
        for (int k = 0; k < 3; k++)
        {
            float x = 13 + k * 20;
            c.Rect(x - 1, 33, 12, 11, Color.White); c.Rect(x, 34, 10, 9, Hex(0x2A4A7A)); c.Rect(x + 4, 34, 1, 9, Color.White);
        }
        c.Rect(54, 48, 10, 16, Hex(0x6A3A20));
        c.Rect(12, 50, 30, 6, Hex(0x48A040));
        return c;
    }

    static Canvas DrawLamp(bool mirror)
    {
        var c = new Canvas(28, 90);
        Color pole = Hex(0x8890A0);
        int px = mirror ? 20 : 6;
        int dir = mirror ? -1 : 1;
        c.Rect(px, 10, 3, 80, pole);
        c.Rect(px - 2, 84, 7, 6, Shade(pole, 0.8f));
        c.Line(px + 1, 10, px + 1 + dir * 14, 6, 2, pole);
        float hx = px + 1 + dir * 14;
        c.Rect(hx - 5, 4, 10, 4, Shade(pole, 0.7f));
        c.Rect(hx - 4, 8, 8, 2, Hex(0xFFF0A0));
        return c;
    }

    static Canvas DrawLighthouse()
    {
        var c = new Canvas(36, 120);
        c.Poly(Color.White, V(7, 120), V(29, 120), V(24, 24), V(12, 24));
        c.Map((x, y, p) => y >= 24 && (y - 24) % 24 < 12 ? Hex(0xD03028) : p);
        c.Rect(7, 18, 22, 5, Hex(0x2A2A30));
        c.Rect(12, 8, 12, 10, Hex(0xFFE070));
        c.Poly(Hex(0xC02820), V(10, 9), V(18, 0), V(26, 9));
        c.Rect(15, 100, 6, 20, Hex(0x3A2A1A));
        return c;
    }

    static Canvas DrawSkull()
    {
        var c = new Canvas(36, 22);
        Color b = Hex(0xEEE6D0);
        c.Line(14, 7, 3, 3, 3, b); c.Line(3, 3, 1, 9, 2, b);
        c.Line(22, 7, 33, 3, 3, b); c.Line(33, 3, 35, 9, 2, b);
        c.Ellipse(18, 10, 7, 6, b); c.Ellipse(18, 17, 4, 5, b);
        c.Circle(15, 10, 1.6f, Hex(0x3A2A20)); c.Circle(21, 10, 1.6f, Hex(0x3A2A20));
        return c;
    }

    static Canvas DrawColumn()
    {
        var c = new Canvas(26, 78);
        Color m = Hex(0xD8D0C0);
        c.Rect(2, 70, 22, 8, Shade(m, 0.9f)); c.Rect(4, 66, 18, 4, m);
        c.Poly(m, V(5, 66), V(5, 16), V(9, 10), V(13, 15), V(17, 6), V(21, 12), V(21, 66));
        for (int x = 7; x < 21; x += 4) c.Rect(x, 16, 1, 50, Shade(m, 0.8f));
        return c;
    }

    static Canvas DrawUmbrella()
    {
        var c = new Canvas(44, 50);
        c.Rect(21, 14, 2, 36, Hex(0xE8E8E0));
        for (int k = 0; k < 6; k++)
        {
            float x0 = 1 + k * 7, x1 = x0 + 7;
            c.Poly(k % 2 == 0 ? Hex(0xE03030) : Color.White, V(22, 3), V(x0, 18), V(x1, 18));
            c.Ellipse((x0 + x1) / 2, 18, 3.5f, 2, k % 2 == 0 ? Hex(0xE03030) : Color.White);
        }
        c.Rect(8, 44, 28, 3, Hex(0x4AA0E0));
        return c;
    }

    static Canvas DrawBillboard(string text, Color panel, Color ink)
    {
        var c = new Canvas(124, 70);
        c.Rect(22, 40, 6, 30, Hex(0x5A5A60)); c.Rect(96, 40, 6, 30, Hex(0x5A5A60));
        c.Rect(2, 2, 120, 42, Color.White);
        c.Rect(5, 5, 114, 36, panel);
        c.Rect(5, 32, 114, 3, Shade(panel, 0.8f));
        c.TextCentered(text, 62, 12, 20, ink);
        return c;
    }

    static Canvas DrawChevron(bool right)
    {
        var c = new Canvas(30, 38);
        c.Rect(13, 20, 4, 18, Hex(0x8890A0));
        c.Rect(1, 1, 28, 21, Hex(0xF8D020));
        c.Rect(1, 1, 28, 2, Hex(0x202020)); c.Rect(1, 20, 28, 2, Hex(0x202020));
        Vector2[] pts = [V(8, 11.5f), V(17, 4), V(22, 4), V(13, 11.5f), V(22, 19), V(17, 19)];
        if (right) pts = pts.Select(p => V(30 - p.X, p.Y)).ToArray();
        c.Poly(Hex(0x202020), pts);
        return c;
    }

    static Canvas DrawForkSign()
    {
        var c = new Canvas(52, 46);
        c.Rect(10, 30, 4, 16, Hex(0x8890A0)); c.Rect(38, 30, 4, 16, Hex(0x8890A0));
        c.Rect(1, 1, 50, 31, Color.White);
        c.Rect(3, 3, 46, 27, Hex(0x2050B0));
        c.Line(26, 27, 26, 17, 4, Color.White);
        c.Line(26, 17, 14, 8, 4, Color.White);
        c.Line(26, 17, 38, 8, 4, Color.White);
        c.Poly(Color.White, V(8, 4), V(17, 4), V(10, 12));
        c.Poly(Color.White, V(44, 4), V(35, 4), V(42, 12));
        return c;
    }

    static Canvas DrawPanelSign(string text, Color panel, bool left)
    {
        var c = new Canvas(132, 62);
        c.Rect(30, 40, 5, 22, Hex(0x8890A0)); c.Rect(97, 40, 5, 22, Hex(0x8890A0));
        c.Rect(1, 1, 130, 40, Color.White);
        c.Rect(3, 3, 126, 36, panel);
        c.TextCentered(text, 66, 8, 10, Color.White);
        float y = 28;
        if (left) { c.Line(46, y, 86, y, 3, Color.White); c.Poly(Color.White, V(38, y), V(48, y - 6), V(48, y + 6)); }
        else { c.Line(46, y, 86, y, 3, Color.White); c.Poly(Color.White, V(94, y), V(84, y - 6), V(84, y + 6)); }
        return c;
    }

    static Canvas DrawArch()
    {
        var c = new Canvas(240, 130);
        Color s = Hex(0xB8A088);
        c.Rect(0, 26, 32, 104, s); c.Rect(208, 26, 32, 104, s);
        c.Rect(0, 0, 240, 40, s);
        c.EraseEllipse(120, 42, 90, 16);
        c.Rect(0, 0, 240, 4, Shade(s, 1.15f));
        c.Map((x, y, p) => (y % 10 == 0 || ((x + (y / 10 % 2) * 8) % 16 == 0)) ? Shade(p, 0.8f) : p);
        c.Rect(32, 26, 3, 104, Shade(s, 0.7f)); c.Rect(205, 26, 3, 104, Shade(s, 0.7f));
        return c;
    }

    static Canvas DrawGate(string text, Color banner, Color ink)
    {
        var c = new Canvas(240, 124);
        Color pil = Hex(0xE8E8EC);
        c.Rect(0, 18, 30, 106, pil); c.Rect(210, 18, 30, 106, pil);
        c.Map((x, y, p) => y >= 18 && (y / 8) % 2 == 0 ? Hex(0xD02828) : p);
        c.Rect(6, 4, 228, 34, Hex(0x303038));
        c.Rect(9, 7, 222, 28, banner);
        if (text == "GOAL")
            for (int x = 9; x < 231; x += 7)
                for (int y = 7; y < 35; y += 7)
                    if (((x - 9) / 7 + (y - 7) / 7) % 2 == 0) c.Rect(x, y, 7, 7, Hex(0xF4F4F4));
        if (text == "START")
            for (int k = 0; k < 3; k++) c.Circle(30 + k * 12, 21, 4, Hex(0xE02020));
        if (text == "GOAL") c.Rect(84, 9, 72, 24, Hex(0x101010));
        c.TextCentered(text, 120, 11, 20, ink);
        return c;
    }

    static Canvas DrawCrowd()
    {
        var c = new Canvas(80, 40);
        var r = new Random(4);
        Color[] shirts = [Hex(0xE03030), Hex(0x3060D0), Hex(0xF0D030), Hex(0x30A050), Hex(0xF0F0F0), Hex(0xE070B0)];
        Color[] skins = [Hex(0xF0C8A0), Hex(0xD8A070), Hex(0x9A6A44)];
        for (int row = 0; row < 2; row++)
            for (int k = 0; k < 7; k++)
            {
                float x = 6 + k * 11 + row * 5 + r.Next(3), y = 22 + row * 6;
                Color sh = shirts[r.Next(shirts.Length)], sk = skins[r.Next(skins.Length)];
                c.Rect(x - 4, y, 8, 40 - y, sh);
                c.Circle(x, y - 3, 3.5f, sk);
                c.Ellipse(x, y - 5.5f, 3.5f, 1.8f, Hex(0x3A2418));
                if (r.Next(2) == 0) { c.Line(x - 4, y + 1, x - 7, y - 9, 2, sk); c.Line(x + 4, y + 1, x + 7, y - 9, 2, sk); }
            }
        return c;
    }

    // ---------------------------------------------------------------- véhicules

    /// <summary>Le cabriolet rouge du joueur, vu de dos. turn = -1 (gauche), 0, 1 (droite).</summary>
    static Canvas DrawPlayer(int turn)
    {
        var c = new Canvas(104, 58);
        int sh = turn * 3;
        Color red = Hex(0xE01818), redD = Hex(0xA00C0C), redL = Hex(0xFF5848), black = Hex(0x141414), dark = Hex(0x2C2C30);

        c.Ellipse(52, 54, 51, 4, Hex(0x000000, 110));
        // pneus
        c.Rect(6 - turn, 38, 18, 18, black); c.Rect(80 - turn, 38, 18, 18, black);
        for (int y = 40; y < 56; y += 3) { c.Rect(7 - turn, y, 16, 1, dark); c.Rect(81 - turn, y, 16, 1, dark); }
        // pare-brise
        c.Poly(Hex(0xA8D8F4, 210), V(22 + sh, 1), V(82 + sh, 1), V(89 + sh, 14), V(15 + sh, 14));
        c.Rect(22 + sh, 1, 60, 1, Hex(0x606870));
        // conducteur (cheveux bruns, chemise blanche)
        c.Ellipse(36 + sh, 16, 11, 5, Hex(0xF4F4F4));
        c.Rect(34 + sh, 11, 4, 4, Hex(0xE8B088));
        c.Circle(36 + sh, 7.5f, 6, Hex(0x3A2410));
        c.Ellipse(36 + sh, 4.5f, 5, 2.5f, Hex(0x50341C));
        // passagère (longs cheveux blonds au vent)
        c.Ellipse(68 + sh, 16, 10, 5, Hex(0xF06890));
        c.Ellipse(68 + sh - turn * 2, 10, 7.5f, 8, Hex(0xF4C850));
        c.Ellipse(70 + sh - turn * 3, 14, 6, 5, Hex(0xF4C850));
        c.Ellipse(66 + sh, 6, 3, 2, Hex(0xFFE8A0));
        // capot arrière
        c.Poly(redL, V(12 + sh, 14), V(92 + sh, 14), V(99, 23), V(5, 23));
        c.Rect(14 + sh, 15, 76, 1, Hex(0xFF8070));
        // flanc visible en virage, avec les ouïes latérales
        if (turn != 0)
        {
            int sx = turn < 0 ? 95 : 0;
            c.Rect(sx, 21, 9, 24, redD);
            for (int k = 0; k < 5; k++) c.Rect(sx + 1, 23 + k * 4, 7, 2, Hex(0x6A0606));
        }
        // carrosserie
        c.Poly(red, V(4, 26), V(9, 21), V(95, 21), V(100, 26), V(100, 42), V(96, 46), V(8, 46), V(4, 42));
        // grille arrière et feux
        c.Rect(8, 26, 88, 11, black);
        for (int y = 27; y < 37; y += 2) c.Rect(9, y, 86, 1, dark);
        c.Rect(10, 27, 18, 8, Hex(0xFF3020)); c.Rect(76, 27, 18, 8, Hex(0xFF3020));
        c.Rect(12, 28, 6, 3, Hex(0xFFA060)); c.Rect(86, 28, 6, 3, Hex(0xFFA060));
        c.Rect(42, 38, 20, 6, Hex(0xF0F0E0)); c.Rect(44, 40, 16, 1, Hex(0x606060));
        c.Rect(6, 43, 92, 3, redD);
        c.Rect(15, 46, 6, 3, dark); c.Rect(83, 46, 6, 3, dark);
        c.Rect(2 + sh, 15, 5, 4, black); c.Rect(97 + sh, 15, 5, 4, black);
        return c;
    }

    enum CarKind { Beetle, Sedan, Van, Truck, Sport }

    static void AddCar(CarKind kind, Color body, float worldW)
    {
        var c = DrawCar(kind, body);
        var def = Make(c, worldW, collide: true, hit: 0.8f);
        def.Truck = kind == CarKind.Truck;
        Traffic.Add(def);
    }

    static Canvas DrawCar(CarKind kind, Color body)
    {
        Color dark = Shade(body, 0.72f), light = Shade(body, 1.15f), glass = Hex(0x304A6A), tire = Hex(0x141414),
              tail = Hex(0xF03020), chrome = Hex(0xC8C8D0);
        Canvas c;
        switch (kind)
        {
            case CarKind.Beetle:
                c = new Canvas(56, 44);
                c.Ellipse(28, 42, 27, 3, Hex(0x000000, 100));
                c.Rect(5, 32, 10, 11, tire); c.Rect(41, 32, 10, 11, tire);
                c.Ellipse(28, 28, 26, 13, body);
                c.Ellipse(28, 15, 17, 12, body);
                c.Ellipse(28, 13, 9, 4.5f, glass);
                c.Ellipse(28, 9, 12, 3, light);
                for (int x = 22; x <= 34; x += 3) c.Rect(x, 24, 1, 6, dark);
                c.Ellipse(8, 30, 3, 4, tail); c.Ellipse(48, 30, 3, 4, tail);
                c.Rect(4, 37, 48, 3, chrome);
                return c;
            case CarKind.Sedan:
                c = new Canvas(60, 44);
                c.Ellipse(30, 42, 29, 3, Hex(0x000000, 100));
                c.Rect(4, 32, 11, 11, tire); c.Rect(45, 32, 11, 11, tire);
                c.Poly(body, V(10, 4), V(50, 4), V(56, 20), V(4, 20));
                c.Poly(glass, V(13, 7), V(47, 7), V(51, 18), V(9, 18));
                c.Poly(body, V(1, 23), V(4, 19), V(56, 19), V(59, 23), V(59, 37), V(1, 37));
                c.Rect(1, 30, 58, 2, dark);
                c.Rect(3, 23, 11, 5, tail); c.Rect(46, 23, 11, 5, tail);
                c.Rect(23, 29, 14, 5, Hex(0xF0F0E0));
                c.Rect(1, 36, 58, 3, chrome);
                return c;
            case CarKind.Van:
                c = new Canvas(60, 58);
                c.Ellipse(30, 56, 29, 3, Hex(0x000000, 100));
                c.Rect(4, 46, 11, 11, tire); c.Rect(45, 46, 11, 11, tire);
                c.Rect(3, 3, 54, 48, body);
                c.Rect(3, 3, 54, 3, light);
                c.Rect(8, 8, 20, 14, glass); c.Rect(32, 8, 20, 14, glass);
                c.Rect(29, 6, 2, 40, dark);
                c.Rect(4, 30, 4, 12, tail); c.Rect(52, 30, 4, 12, tail);
                c.Rect(24, 40, 12, 5, Hex(0xF0F0E0));
                c.Rect(2, 48, 56, 3, Hex(0x404048));
                return c;
            case CarKind.Truck:
                c = new Canvas(72, 88);
                c.Ellipse(36, 86, 35, 3, Hex(0x000000, 110));
                c.Rect(3, 72, 14, 14, tire); c.Rect(19, 72, 6, 14, tire); c.Rect(47, 72, 6, 14, tire); c.Rect(55, 72, 14, 14, tire);
                c.Rect(1, 0, 70, 70, body);
                c.Rect(1, 0, 70, 3, light);
                c.Rect(35, 4, 2, 64, dark);
                for (int y = 10; y < 66; y += 14) { c.Rect(4, y, 29, 1, dark); c.Rect(39, y, 29, 1, dark); }
                c.Rect(30, 34, 3, 10, chrome); c.Rect(39, 34, 3, 10, chrome);
                c.Rect(1, 68, 70, 5, Hex(0x303034));
                c.Rect(3, 69, 8, 4, tail); c.Rect(61, 69, 8, 4, tail);
                c.Rect(4, 80, 8, 6, Hex(0x202020)); c.Rect(60, 80, 8, 6, Hex(0x202020));
                return c;
            default:
                c = new Canvas(64, 38);
                c.Ellipse(32, 36, 31, 3, Hex(0x000000, 100));
                c.Rect(4, 25, 13, 12, tire); c.Rect(47, 25, 13, 12, tire);
                c.Poly(body, V(16, 8), V(48, 8), V(54, 18), V(10, 18));
                c.Poly(glass, V(19, 10), V(45, 10), V(49, 17), V(15, 17));
                c.Poly(body, V(1, 21), V(6, 16), V(58, 16), V(63, 21), V(63, 32), V(1, 32));
                c.Rect(1, 21, 62, 6, Hex(0x181818));
                c.Circle(9, 24, 2.5f, tail); c.Circle(15, 24, 2.5f, tail); c.Circle(49, 24, 2.5f, tail); c.Circle(55, 24, 2.5f, tail);
                c.Rect(2, 5, 60, 3, Hex(0x181818)); c.Rect(8, 8, 3, 8, Hex(0x181818)); c.Rect(53, 8, 3, 8, Hex(0x181818));
                c.Rect(1, 31, 62, 2, dark);
                return c;
        }
    }
}
