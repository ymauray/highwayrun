using Raylib_cs;

namespace HighwayRun;

sealed class SpriteInst(SpriteDef def, float offset, bool center = false, int branch = -1)
{
    public readonly SpriteDef Def = def;
    public float Offset = offset;       // position latérale, en demi-largeurs de route
    public readonly bool Center = center;
    public readonly int Branch = branch; // côté de la fourche auquel l'objet appartient (-1 : aucun)
}

sealed class Car
{
    public required SpriteDef Def;
    public float Z, Lane, Speed, Phase, X;
    public bool PreferLeft;
    public Segment? Seg;
}

struct Proj
{
    public float X, Y, W, Scale, CamZ;
}

sealed class Segment
{
    public int Index;
    public float Y1, Y2, Curve;
    public int Level, Branch;
    public StageTheme Theme = null!;
    public Color Grass, Rumble, Road, Lane;
    public bool Dashes;

    // Centres des routes A et B (fourche) en début et fin de segment, en demi-largeurs
    public bool Dual;
    public float A1, A2, B1, B2;
    public int Chosen = -1;
    public float OtherFade;

    public bool Decision, Checkpoint, Goal, Traffic, NoScenery;
    public readonly List<SpriteInst> Sprites = [];
    public readonly List<Car> Cars = [];

    public Proj P1, P2;
    public float Fog;

    public float Z1 => Index * Cfg.SegmentLength;

    public float CenterA(float pct) => U.Lerp(A1, A2, pct);
    public float CenterB(float pct) => Dual ? U.Lerp(B1, B2, pct) : CenterA(pct);

    /// <summary>Centre de la route la plus proche de x.</summary>
    public float Nearest(float x, float pct)
    {
        float a = CenterA(pct), b = CenterB(pct);
        if (Dual && Chosen >= 0) return Chosen == 0 ? a : b;
        return MathF.Abs(x - a) <= MathF.Abs(x - b) ? a : b;
    }
}

/// <summary>La route : une liste de segments qui s'allonge au fil des choix de fourche.</summary>
sealed class Track
{
    public readonly List<Segment> Segs = [];
    public readonly List<Car> Cars = [];
    public int Count => Segs.Count;
    public float Length => Segs.Count * Cfg.SegmentLength;

    Random rng = new(1);
    StageTheme theme = null!, from = null!;
    int level, branch, stageStart;

    public Segment Find(float z) => Segs[Math.Clamp((int)(z / Cfg.SegmentLength), 0, Segs.Count - 1)];

    float LastY => Segs.Count == 0 ? 0 : Segs[^1].Y2;

    Segment Add(float curve, float y)
    {
        int n = Segs.Count;
        var s = new Segment { Index = n, Y1 = LastY, Y2 = y, Curve = curve, Level = level, Branch = branch, Theme = theme };
        // fondu de palette au début d'une étape (la route change progressivement de couleur)
        float t = ReferenceEquals(from, theme) ? 1 : U.Smooth((n - stageStart) / 120f);
        bool light = (n / Cfg.RumbleLength) % 2 == 0;
        s.Grass = U.Mix(light ? from.Grass1 : from.Grass2, light ? theme.Grass1 : theme.Grass2, t);
        s.Rumble = U.Mix(light ? from.Rumble1 : from.Rumble2, light ? theme.Rumble1 : theme.Rumble2, t);
        s.Road = U.Mix(light ? from.Road1 : from.Road2, light ? theme.Road1 : theme.Road2, t);
        s.Lane = U.Mix(from.Lane, theme.Lane, t);
        s.Dashes = light;
        Segs.Add(s);
        return s;
    }

    void AddRoad(int enter, int hold, int leave, float curve, float height)
    {
        float startY = LastY, endY = startY + height * Cfg.SegmentLength;
        float total = enter + hold + leave;
        for (int i = 0; i < enter; i++) Add(U.EaseIn(0, curve, i / (float)enter), U.EaseInOut(startY, endY, (i + 1) / total));
        for (int i = 0; i < hold; i++) Add(curve, U.EaseInOut(startY, endY, (enter + i + 1) / total));
        for (int i = 0; i < leave; i++) Add(U.EaseInOut(curve, 0, i / (float)leave), U.EaseInOut(startY, endY, (enter + hold + i + 1) / total));
    }

    void Straight(int n) { float y = LastY; for (int i = 0; i < n; i++) Add(0, y); }

    void Put(int index, SpriteDef def, float offset, bool center = false, int branch = -1)
    {
        if (index >= 0 && index < Segs.Count) Segs[index].Sprites.Add(new SpriteInst(def, offset, center, branch));
    }

    void PutGate(int index, SpriteDef gate)
    {
        Put(index, gate, 0, center: true);
        Put(index, Art.Pillar, -1.27f, center: true);
        Put(index, Art.Pillar, 1.27f, center: true);
        for (int i = index - 4; i <= index + 4; i++) if (i >= 0 && i < Segs.Count) Segs[i].NoScenery = true;
    }

    /// <summary>Construit une étape complète : départ ou checkpoint, corps, puis fourche ou arrivée.</summary>
    public void BuildStage(int level, int branch, StageTheme? previous)
    {
        this.level = level;
        this.branch = branch;
        theme = Themes.Get(level, branch);
        from = previous ?? theme;
        stageStart = Segs.Count;
        rng = new Random(level * 31 + branch * 7 + 12345);

        int start = Segs.Count;
        Straight(60);
        if (level == 0)
        {
            PutGate(start + 12, Art.GateStart);
            for (int i = start + 5; i < start + 22; i += 3) { Put(i, Art.Crowd, -1.25f); Put(i, Art.Crowd, 1.25f); }
        }
        else
        {
            Segs[start + 12].Checkpoint = true;
            PutGate(start + 12, Art.GateCheckpoint);
        }

        int bodyStart = Segs.Count;
        BuildBody(1650 + level * 70);
        AddRoad(25, 30, 25, 0, -LastY / Cfg.SegmentLength * 0.6f);  // on redescend vers le niveau de la mer
        int bodyEnd = Segs.Count;

        for (int i = bodyStart + 10; i < bodyEnd - 30; i++) Segs[i].Traffic = true;
        Decorate(level == 0 ? start + 24 : start, bodyEnd);

        if (level < 4) BuildFork(); else BuildGoal();
    }

    void BuildBody(int target)
    {
        int start = Segs.Count;
        float cv = theme.Curviness, hl = theme.Hilliness;
        float[] mags = [2, 3, 4, 5, 6];
        while (Segs.Count - start < target)
        {
            double r = rng.NextDouble();
            float mag = mags[Math.Min(4, (int)(rng.NextDouble() * (1.6 + cv * 4.4)))];
            float dir = rng.Next(2) == 0 ? -1 : 1;
            float Height() => rng.NextDouble() < 0.35 ? 0 : (float)(rng.NextDouble() * 2 - 1) * 55 * hl - LastY / Cfg.SegmentLength * 0.35f;
            if (r < 0.2)
                AddRoad(15 + rng.Next(25), 20 + rng.Next(50), 15 + rng.Next(25), 0, Height());
            else if (r < 0.75)
                AddRoad(20 + rng.Next(25), 25 + rng.Next(70), 20 + rng.Next(25), dir * mag, Height());
            else
            {
                AddRoad(20 + rng.Next(15), 20 + rng.Next(30), 20 + rng.Next(15), dir * mag, Height());
                AddRoad(20 + rng.Next(15), 20 + rng.Next(30), 20 + rng.Next(15), -dir * mag, Height());
            }
        }
    }

    void Decorate(int from, int to)
    {
        int i = from;
        while (i < to)
        {
            int run = 50 + rng.Next(90);
            var row = U.Pick(rng, theme.Rows);
            int sides = rng.Next(4); // 0 gauche, 1 droite, 2-3 les deux
            float off = 1.22f + (float)rng.NextDouble() * 0.35f;
            for (int j = i; j < Math.Min(i + run, to); j += row.Spacing)
            {
                if (Segs[j].NoScenery) continue;
                if (sides != 1) Put(j, row, -off);
                if (sides != 0) Put(j, row, off);
            }
            i += run;
        }

        for (int j = from; j < to; j++)
        {
            var s = Segs[j];
            if (s.NoScenery) continue;
            if (rng.NextDouble() < theme.Scatter && theme.Props.Length > 0)
            {
                var p = U.Pick(rng, theme.Props);
                float side = rng.Next(2) == 0 ? -1 : 1;
                Put(j, p, side * (1.7f + (float)rng.NextDouble() * 2.5f));
            }
            // chevrons à l'extérieur des virages serrés
            if (MathF.Abs(s.Curve) >= 3.5f && j % 8 == 0)
                Put(j, s.Curve > 0 ? Art.ChevronRight : Art.Chevron, s.Curve > 0 ? -1.12f : 1.12f);
            if (theme.Billboards.Length > 0 && j % 230 == 115 && MathF.Abs(s.Curve) < 1)
                Put(j, theme.Billboards[rng.Next(theme.Billboards.Length)], (rng.Next(2) == 0 ? -1 : 1) * 1.3f);
            if (theme.Arches && j % 280 == 140 && MathF.Abs(s.Curve) < 0.5f)
            {
                Put(j, Art.Arch, 0, center: true);
                Put(j, Art.Pillar, -1.27f, center: true);
                Put(j, Art.Pillar, 1.27f, center: true);
            }
        }
    }

    /// <summary>La fourche : la route se dédouble, le joueur choisit sa branche.</summary>
    void BuildFork()
    {
        int f0 = Segs.Count;
        float S = Cfg.ForkSplit;
        Straight(40);
        var left = Themes.Get(level + 1, branch);
        var right = Themes.Get(level + 1, branch + 1);
        Put(f0 + 6, Art.ForkSign, -1.3f); Put(f0 + 6, Art.ForkSign, 1.3f);
        Put(f0 + 22, Art.DestSign(left.Name, true), -1.3f);
        Put(f0 + 22, Art.DestSign(right.Name, false), 1.3f);
        Put(f0 + 34, Art.ForkSign, -1.3f); Put(f0 + 34, Art.ForkSign, 1.3f);

        bool barrier = false;
        float y = LastY;
        const int grow = 110;
        for (int k = 0; k < grow; k++)
        {
            float s1 = U.EaseInOut(0, S, k / (float)grow), s2 = U.EaseInOut(0, S, (k + 1) / (float)grow);
            var seg = Add(0, y);
            seg.Dual = true;
            seg.A1 = -s1; seg.A2 = -s2; seg.B1 = s1; seg.B2 = s2;
            if (!barrier && s1 > 1.25f) { barrier = true; Put(seg.Index, Art.ForkSign, 0, center: true); }
            else if (barrier && k % 6 == 0) Put(seg.Index, Art.Bush, 0, center: true);
        }
        Segs[^1].Decision = true;

        var rowL = U.Pick(rng, theme.Rows);
        for (int k = 0; k < 270; k++)
        {
            var seg = Add(0, y);
            seg.Dual = true;
            seg.A1 = seg.A2 = -S; seg.B1 = seg.B2 = S;
            if (k % rowL.Spacing == 0)
            {
                Put(seg.Index, rowL, -S - 1.25f, branch: 0);
                Put(seg.Index, rowL, S + 1.25f, branch: 1);
            }
            if (k % 9 == 4) Put(seg.Index, Art.Bush, 0, center: true);
        }
    }

    void BuildGoal()
    {
        int g0 = Segs.Count;
        Straight(420);
        Segs[g0 + 30].Goal = true;
        PutGate(g0 + 30, Art.GateGoal);
        for (int i = g0 + 20; i < g0 + 50; i += 3) { Put(i, Art.Crowd, -1.25f); Put(i, Art.Crowd, 1.25f); }
    }

    /// <summary>Le joueur s'est engagé sur une branche : on recentre le repère sur elle
    /// (sans changement visible) puis on ajoute l'étape suivante.</summary>
    public float Choose(Segment decision, int side)
    {
        float delta = side == 0 ? decision.A2 : decision.B2;
        int from = Math.Max(0, decision.Index - 60);
        for (int i = from; i < Segs.Count; i++)
        {
            var s = Segs[i];
            s.A1 -= delta; s.A2 -= delta; s.B1 -= delta; s.B2 -= delta;
            foreach (var sp in s.Sprites) sp.Offset -= delta;
        }
        int end = Segs.Count;
        for (int i = decision.Index + 1; i < end; i++)
        {
            Segs[i].Chosen = side;
            Segs[i].OtherFade = U.Smooth((i - decision.Index - 20) / (float)(end - decision.Index - 40));
        }
        BuildStage(decision.Level + 1, decision.Branch + side, decision.Theme);
        return delta;
    }
}
