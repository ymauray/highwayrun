using System.Numerics;
using Raylib_cs;
using static HighwayRun.U;

namespace HighwayRun;

readonly record struct Look(Color Shirt, Color Pants, Color Hair, Color Skin, bool LongHair = false, bool Dress = false, bool Swimsuit = false);

/// <summary>Les cinq scènes de fin (A à E), animées en primitives.</summary>
sealed class EndingScene(int kind, StageTheme theme)
{
    const int W = Cfg.Width, H = Cfg.Height;
    const float Ground = 206;
    public readonly int Kind = kind;
    public float T;
    public float Duration => 12f;
    public bool Done => T >= Duration;

    static readonly Color Skin = Hex(0xF0C49C);
    static readonly Look Driver = new(Hex(0xF4F4F4), Hex(0x3050A0), Hex(0x3A2410), Skin);
    static readonly Look Passenger = new(Hex(0xF06890), Hex(0xF06890), Hex(0xF4C850), Skin, LongHair: true, Dress: true);
    static readonly Look Bikini = new(Hex(0xE02828), Hex(0xE8B080), Hex(0x2A1A10), Hex(0xE8B080), LongHair: true, Swimsuit: true);
    static readonly Look Genie = new(Hex(0x7A3AA0), Hex(0x7A3AA0), Hex(0xF4F4F4), Hex(0xD8A878), Dress: true);
    static readonly Look Official = new(Hex(0x202838), Hex(0x202838), Hex(0x888888), Skin);
    static readonly Look[] Harem =
    [
        new(Hex(0xF080C0), Hex(0xF080C0), Hex(0x201010), Hex(0xE8B080), true, true),
        new(Hex(0x60C0E0), Hex(0x60C0E0), Hex(0x402010), Skin, true, true),
        new(Hex(0xF0D040), Hex(0xF0D040), Hex(0x101010), Hex(0xC89060), true, true),
        new(Hex(0xA060E0), Hex(0xA060E0), Hex(0x602010), Skin, true, true),
    ];
    static readonly Look[] Men =
    [
        new(Hex(0xE03030), Hex(0x404040), Hex(0x201810), Skin), new(Hex(0x3070D0), Hex(0x303030), Hex(0x503020), Hex(0xD8A070)),
        new(Hex(0x30A050), Hex(0x504030), Hex(0x101010), Skin), new(Hex(0xF0B030), Hex(0x303060), Hex(0x302010), Hex(0x9A6A44)),
        new(Hex(0xF0F0F0), Hex(0x404050), Hex(0x705030), Skin), new(Hex(0xA040A0), Hex(0x303030), Hex(0x201010), Hex(0xD8A070)),
    ];

    public void Update(float dt) => T += dt;

    public void Draw(Renderer r)
    {
        r.Background(theme, theme, 1);
        Raylib.DrawRectangle(0, H / 2 + 1, W, H / 2, theme.Grass1);
        Raylib.DrawRectangle(0, (int)Ground - 10, W, 34, theme.Road1);
        Raylib.DrawRectangle(0, (int)Ground - 12, W, 2, theme.Lane);
        Raylib.DrawRectangle(0, (int)Ground + 24, W, 2, theme.Lane);
        for (int x = 0; x < W; x += 40) Raylib.DrawRectangle(x, (int)Ground + 7, 20, 2, theme.Lane);

        switch (Kind)
        {
            case 0: SceneA(); break;
            case 1: SceneB(); break;
            case 2: SceneC(); break;
            case 3: SceneD(); break;
            default: SceneE(); break;
        }

        float fadeIn = 1 - Clamp01(T / 0.8f), fadeOut = Clamp01((T - Duration + 1f) / 1f);
        float f = MathF.Max(fadeIn, fadeOut);
        if (f > 0) Raylib.DrawRectangle(0, 0, W, H, Fade(Color.Black, f));
    }

    // A : porté en triomphe... puis lâché pour une fille en bikini
    void SceneA()
    {
        SideCar(330, Ground, 0);
        Person(300, Ground, Passenger, 0, false, 0);
        float girlX = T < 6 ? 520 : MathF.Max(268, 520 - (T - 6) * 150);
        bool distracted = T > 6.4f;
        for (int i = 0; i < 6; i++)
        {
            float mx = 120 + i * 14 + (distracted ? MathF.Min(50, (T - 6.4f) * 40) : 0);
            float up = distracted ? 0 : 0.7f + 0.3f * MathF.Sin(T * 6 + i);
            Person(mx, Ground + (i % 2) * 4, Men[i], up, distracted, distracted ? T * 9 : 0);
        }
        float h;
        if (T < 6.4f) h = 20 + MathF.Abs(MathF.Sin((T - 0.5f) * MathF.PI)) * 55;
        else
        {
            float ft = T - 6.4f;
            float h0 = 20 + MathF.Abs(MathF.Sin(5.9f * MathF.PI)) * 55;
            h = MathF.Max(0, h0 - 160 * ft * ft);
        }
        if (h > 0 || T < 6.4f) Person(155, Ground - 18 - h, Driver, 1, true, 0);
        else
        {
            PersonLying(140, Ground + 2, Driver);
            for (int k = 0; k < 3; k++)
            {
                float a = T * 4 + k * 2.1f;
                Star(140 + MathF.Cos(a) * 12, Ground - 12 + MathF.Sin(a) * 4);
            }
        }
        Person(girlX, Ground, Bikini, T > 7.8f ? 0.5f + 0.5f * MathF.Sin(T * 4) : 0, false, girlX > 268 ? T * 9 : 0);
    }

    // B : la voiture tombe en morceaux
    void SceneB()
    {
        float carX = T < 2 ? -130 + 290 * Smooth(T / 2) : 160;
        float breakT = Clamp01((T - 3.5f) / 1.5f);
        SideCar(carX, Ground, breakT);
        if (T < 2.6f)
        {
            Raylib.DrawCircle((int)carX + 52, (int)Ground - 34, 4, Driver.Hair);
            Raylib.DrawCircle((int)carX + 70, (int)Ground - 33, 4, Passenger.Hair);
        }
        else
        {
            float up = T > 5.5f ? 0.5f + 0.5f * MathF.Sin(T * 5) : 0.2f;
            Person(130, Ground, Driver, up, true, 0);
            Person(320, Ground, Passenger, T > 5.5f ? 0.5f + 0.5f * MathF.Sin(T * 5 + 1.5f) : 0.2f, false, 0);
            if (T > 6) { SweatDrop(138, Ground - 30); SweatDrop(328, Ground - 30); }
        }
        if (breakT > 0 && breakT < 1)
            for (int k = 0; k < 6; k++)
                Raylib.DrawCircleV(new Vector2(carX + 20 + k * 18, Ground - 20 - breakT * 30 - k % 2 * 8), 6 + breakT * 8, Fade(Hex(0xD8D8D8), 1 - breakT));
    }

    // C : une lampe magique, et un harem apparaît
    void SceneC()
    {
        SideCar(300, Ground, 0);
        float genieX = T < 3 ? -20 + 175 * Smooth(T / 3) : T < 5 ? 155 : 155 - (T - 5) * 90;
        if (genieX > -30) Person(genieX, Ground, Genie, T > 2.6f && T < 3.4f ? 0.4f : 0, T < 5, T < 3 || T > 5 ? T * 8 : 0);
        Person(200, Ground, Driver, T > 3.4f && T < 5.6f ? 0.4f : 0, false, 0);
        Vector2 lamp = T < 3.2f ? new(genieX + 8, Ground - 14) : new(196 + (T > 4 && T < 5.5f ? MathF.Sin(T * 30) * 2 : 0), Ground - 15);
        if (T < 5.6f) Lamp(lamp);
        if (T > 5.4f && T < 7)
        {
            float k = (T - 5.4f) / 1.6f;
            for (int i = 0; i < 5; i++)
                Raylib.DrawCircleV(new Vector2(200 + MathF.Cos(i * 1.3f) * 20 * k, Ground - 30 - k * 20 + MathF.Sin(i * 1.3f) * 10), 10 + 10 * k, Fade(Hex(0xE8D8F0), 1 - k));
        }
        if (T > 6)
        {
            float a = Clamp01((T - 6) / 0.6f);
            float[] xs = [168, 182, 220, 234];
            for (int i = 0; i < 4; i++)
                if (a > 0.3f) Person(xs[i], Ground + (i % 2) * 3, Harem[i], 0.5f + 0.5f * MathF.Sin(T * 4 + i), i >= 2, 0);
        }
        bool jump = T > 6.6f;
        Person(262, Ground - (jump ? MathF.Abs(MathF.Sin(T * 7)) * 8 : 0), Passenger, jump ? 0.6f : 0, false, 0);
        if (jump) Bubble(262, Ground - 46, "!");
    }

    // D : la coupe est remise à la passagère
    void SceneD()
    {
        SideCar(320, Ground, 0);
        Podium(200);
        float offX = T < 3 ? 520 - 280 * Smooth(T / 3) : T < 4.5f ? 240 : MathF.Max(178, 240 - (T - 4.5f) * 60);
        bool given = T > 6;
        Person(offX, Ground, Official, !given && T > 3 ? 0.35f : 0, false, T < 3 || (T > 4.5f && offX > 178) ? T * 8 : 0);
        if (!given) Trophy(new Vector2(offX - 7, Ground - 16), 0.8f);
        bool reach = T > 3 && T < 5;
        bool sad = T > 5.5f;
        Person(206, Ground - 10 + (sad ? 2 : 0), Driver, reach ? 0.4f : 0, false, 0, sad);
        if (sad) SweatDrop(214, Ground - 44);
        float hop = given ? MathF.Abs(MathF.Sin(T * 6)) * 6 : 0;
        Person(150, Ground - hop, Passenger, given ? 1 : 0, true, 0);
        if (given) Trophy(new Vector2(150, Ground - 40 - hop), 1);
    }

    // E : la coupe revient au conducteur... mais la passagère la veut
    void SceneE()
    {
        SideCar(320, Ground, 0);
        Podium(200);
        float offX = T < 3 ? 520 - 270 * Smooth(T / 3) : T < 4 ? 250 : 250 + (T - 4) * 110;
        bool given = T > 3.5f;
        Person(offX, Ground, Official, !given ? 0.35f : 0, given, T < 3 || T > 4 ? T * 8 : 0);
        if (!given) Trophy(new Vector2(offX - 7, Ground - 16), 0.8f);
        bool tug = T > 6.5f;
        float pullX = tug ? MathF.Sin(T * 5) * 8 : 0;
        Person(206, Ground - 10, Driver, given ? 1 : 0, tug, 0);
        float passX = tug ? 182 : T > 5.5f ? 150 + (T - 5.5f) * 32 : 150;
        Person(passX, Ground - (tug ? 0 : MathF.Abs(MathF.Sin(T * 7)) * (T > 5 ? 5 : 0)), Passenger, tug ? 1 : 0, true, T > 5.5f && !tug ? T * 9 : 0);
        if (given) Trophy(new Vector2((tug ? 194 : 206) + pullX, Ground - 50), 1);
        if (tug) { Bubble(206, Ground - 70, "!"); Bubble(passX, Ground - 58, "!"); }
    }

    // ------------------------------------------------------------------ primitives

    static void Rect(float x, float y, float w, float h, Color c) => Raylib.DrawRectangle((int)MathF.Round(x), (int)MathF.Round(y), (int)MathF.Round(w), (int)MathF.Round(h), c);

    /// <summary>Personnage d'environ 30 px. armsUp : 0 bras le long du corps, 1 bras levés.</summary>
    static void Person(float x, float feet, Look l, float armsUp, bool facingRight, float walk, bool slumped = false)
    {
        float swing = walk != 0 ? MathF.Sin(walk) * 2.5f : 0;
        if (l.Dress)
            Raylib.DrawTriangle(new Vector2(x, feet - 20), new Vector2(x - 7, feet - 2), new Vector2(x + 7, feet - 2), l.Pants);
        Rect(x - 3.5f + swing, feet - 10, 3, 10, l.Dress ? l.Skin : l.Pants);
        Rect(x + 0.5f - swing, feet - 10, 3, 10, l.Dress ? l.Skin : l.Pants);
        Rect(x - 4, feet - 20, 8, 11, l.Shirt);
        if (l.Swimsuit)
        {
            Rect(x - 4, feet - 20, 8, 11, l.Skin);
            Rect(x - 4, feet - 18, 8, 3, l.Shirt);
            Rect(x - 4, feet - 11, 8, 3, l.Shirt);
        }
        float sy = feet - 19;
        float droop = slumped ? 2 : 0;
        Vector2 hl = Vector2.Lerp(new(x - 6, feet - 10 + droop), new(x - 7, feet - 31), armsUp);
        Vector2 hr = Vector2.Lerp(new(x + 6, feet - 10 + droop), new(x + 7, feet - 31), armsUp);
        Raylib.DrawLineEx(new Vector2(x - 4, sy), hl, 2.2f, l.Skin);
        Raylib.DrawLineEx(new Vector2(x + 4, sy), hr, 2.2f, l.Skin);
        float hy = feet - 24 + (slumped ? 2 : 0);
        float face = facingRight ? 1 : -1;
        if (l.LongHair) Rect(x - 4.5f - face * 1, hy - 2, 9, 10, l.Hair);
        Raylib.DrawCircleV(new Vector2(x, hy), 4.2f, l.Skin);
        Raylib.DrawEllipse((int)(x - face * 1), (int)(hy - 2.5f), 4.6f, 2.6f, l.Hair);
        Rect(x + face * 2 - 0.5f, hy - 1, 1, 1, Hex(0x201010));
    }

    static void PersonLying(float x, float y, Look l)
    {
        Rect(x - 16, y - 6, 10, 5, l.Pants);
        Rect(x - 6, y - 7, 11, 6, l.Shirt);
        Raylib.DrawCircleV(new Vector2(x + 9, y - 4), 4, l.Skin);
        Raylib.DrawEllipse((int)x + 11, (int)y - 5, 2.5f, 4, l.Hair);
        Raylib.DrawLineEx(new Vector2(x, y - 6), new Vector2(x + 4, y - 13), 2, l.Skin);
    }

    /// <summary>Le cabriolet vu de profil. breakT > 0 : il part en morceaux.</summary>
    static void SideCar(float x, float gy, float breakT)
    {
        Color red = Hex(0xE01818), redD = Hex(0xA00C0C), black = Hex(0x141414);
        float drop = MathF.Min(9, breakT * 30);
        float body = gy - 26 + drop;
        Raylib.DrawEllipse((int)x + 60, (int)gy, 64, 4, Fade(Color.Black, 0.35f));
        Rect(x + 4, body, 116, 15, red);
        Raylib.DrawTriangle(new Vector2(x + 120, body), new Vector2(x + 120, body + 15), new Vector2(x + 128, body + 13), red);
        Rect(x + 2, body - 3, 34, 4, red);
        Rect(x + 4, body + 12, 116, 3, redD);
        for (int k = 0; k < 4; k++) Rect(x + 46, body + 3 + k * 3, 30, 1.5f, Hex(0x700808));
        if (breakT < 0.4f)
        {
            Raylib.DrawLineEx(new Vector2(x + 82, body), new Vector2(x + 70, body - 12), 2, Hex(0xA8D8F4));
            Rect(x + 40, body - 4, 30, 4, black);
        }
        else
        {
            float k = (breakT - 0.4f) / 0.6f;
            Raylib.DrawRectanglePro(new Rectangle(x + 40, body - 4 + k * 20, 30, 4), Vector2.Zero, k * 25, black);
            Raylib.DrawLineEx(new Vector2(x + 90 + k * 20, body - 2 + k * 14), new Vector2(x + 80 + k * 26, body - 12 + k * 22), 2, Hex(0xA8D8F4));
        }
        Rect(x + 116, body + 4, 6, 4, Hex(0xFFF0B0));
        Rect(x, body + 3, 4, 5, Hex(0xFF3020));
        float fx = x + 98 + breakT * 140, rx = x + 24 - breakT * 120;
        Wheel(rx, gy - 9, breakT * 12);
        Wheel(fx, gy - 9, breakT * 14);
    }

    static void Wheel(float x, float y, float rot)
    {
        Raylib.DrawCircleV(new Vector2(x, y), 9, Hex(0x141414));
        Raylib.DrawCircleV(new Vector2(x, y), 4.5f, Hex(0xB8B8C0));
        Raylib.DrawLineEx(new Vector2(x + MathF.Cos(rot) * 4, y + MathF.Sin(rot) * 4), new Vector2(x - MathF.Cos(rot) * 4, y - MathF.Sin(rot) * 4), 1.5f, Hex(0x505058));
    }

    static void Trophy(Vector2 p, float s)
    {
        Color gold = Hex(0xF4C830), dark = Hex(0xB08818);
        Rect(p.X - 5 * s, p.Y + 6 * s, 10 * s, 3 * s, dark);
        Rect(p.X - 1.5f * s, p.Y + 1 * s, 3 * s, 6 * s, gold);
        Raylib.DrawCircleSector(p, 6.5f * s, 0, 180, 12, gold);
        Rect(p.X - 6.5f * s, p.Y - 5 * s, 13 * s, 5 * s, gold);
        Raylib.DrawRingLines(new Vector2(p.X - 7 * s, p.Y - 2 * s), 2 * s, 3.5f * s, 90, 270, 8, dark);
        Raylib.DrawRingLines(new Vector2(p.X + 7 * s, p.Y - 2 * s), 2 * s, 3.5f * s, -90, 90, 8, dark);
        Rect(p.X - 3 * s, p.Y - 4 * s, 2 * s, 3 * s, Hex(0xFFF0A0));
    }

    static void Lamp(Vector2 p)
    {
        Color gold = Hex(0xF4C830);
        Raylib.DrawEllipse((int)p.X, (int)p.Y, 6, 3, gold);
        Raylib.DrawTriangle(new Vector2(p.X + 4, p.Y - 2), new Vector2(p.X + 4, p.Y + 1), new Vector2(p.X + 11, p.Y - 4), gold);
        Rect(p.X - 2, p.Y - 5, 4, 2, gold);
    }

    static void Podium(float x)
    {
        Rect(x - 22, Ground - 10, 44, 12, Hex(0xF4F4F4));
        Rect(x - 22, Ground - 10, 44, 2, Hex(0xD0D0D8));
        Hud.TextC("1", x, Ground - 8, 10, Hex(0xD02828), false);
    }

    static void SweatDrop(float x, float y)
    {
        Raylib.DrawCircleV(new Vector2(x, y + 3), 2, Hex(0x80C8FF));
        Raylib.DrawTriangle(new Vector2(x, y - 1), new Vector2(x - 2, y + 3), new Vector2(x + 2, y + 3), Hex(0x80C8FF));
    }

    static void Star(float x, float y)
    {
        Raylib.DrawLineEx(new Vector2(x - 3, y), new Vector2(x + 3, y), 1.5f, Hex(0xFFE040));
        Raylib.DrawLineEx(new Vector2(x, y - 3), new Vector2(x, y + 3), 1.5f, Hex(0xFFE040));
    }

    static void Bubble(float x, float y, string s)
    {
        Raylib.DrawCircleV(new Vector2(x, y), 8, Color.White);
        Raylib.DrawTriangle(new Vector2(x - 3, y + 6), new Vector2(x - 6, y + 12), new Vector2(x + 2, y + 7), Color.White);
        Hud.TextC(s, x, y - 5, 10, Hex(0xD02020), false);
    }
}
