using System.Numerics;
using Raylib_cs;
using static HighwayRun.U;

namespace HighwayRun;

enum State { Title, Radio, Countdown, Racing, TimeUp, Goal, Ending, Results }

sealed class Particle
{
    public Vector2 P, V;
    public float Life, Max, Size;
    public Color C;
}

/// <summary>Boucle de jeu : enchaînement des écrans, physique de la voiture, trafic et affichage.</summary>
sealed class Game : IDisposable
{
    const int W = Cfg.Width, H = Cfg.Height;
    const float Seg = Cfg.SegmentLength;

    readonly RenderTexture2D target;
    readonly Renderer renderer = new();
    readonly Audio audio = new();
    readonly Random rng = new();
    public bool QuitRequested;

    State state;
    float stateT, accumulator, globalT;
    bool paused;

    Track track = null!;
    bool attract;
    float position, playerX, speed, lastSteer, rev;
    bool lowGear = true, automatic = true, throttleHeld;
    float timeLeft, stageTime;
    double score, best;
    readonly List<float> laps = [];
    readonly List<int> path = [];
    StageTheme bgTheme = null!, bgPrev = null!;
    float bgBlend = 1;
    float crashT, crashDur, crashSide;
    int crashKind;
    float shake, skid;
    bool offroad;
    string banner = "", banner2 = "", stageLabel = "";
    float bannerT, stageLabelT;
    Color bannerColor = Color.White;
    float countdown;
    int lastCount;
    int radio;
    EndingScene? ending;
    bool finished;
    float spawnDist;
    float autoLane;
    int autoSide;
    bool autopilot;
    readonly List<Particle> particles = [];

    static readonly string BestFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HighwayRun", "best.txt");

    public Game()
    {
        target = Raylib.LoadRenderTexture(W * Cfg.ArtScale, H * Cfg.ArtScale);
        Raylib.SetTextureFilter(target.Texture, TextureFilter.Point);
        best = LoadBest();
        GoTitle();
    }

    // ================================================================== flux des écrans

    void GoTitle()
    {
        state = State.Title;
        stateT = 0;
        paused = false;
        attract = true;
        audio.Music.Stop();
        track = new Track();
        track.BuildStage(0, 0, null);
        position = 0;
        playerX = 0;
        speed = Cfg.MaxSpeed * 0.7f;
        bgTheme = bgPrev = Themes.Get(0, 0);
        bgBlend = 1;
        crashT = 0;
        particles.Clear();
        path.Clear();
        path.Add(0);
        laps.Clear();
        Populate();
    }

    void StartRace()
    {
        attract = false;
        track = new Track();
        track.BuildStage(0, 0, null);
        position = 0;
        playerX = 0;
        speed = 0;
        rev = 0.1f;
        lowGear = true;
        timeLeft = Cfg.StartTime;
        stageTime = 0;
        score = 0;
        laps.Clear();
        path.Clear();
        path.Add(0);
        bgTheme = bgPrev = Themes.Get(0, 0);
        bgBlend = 1;
        crashT = 0;
        finished = false;
        ending = null;
        particles.Clear();
        banner = "";
        state = State.Countdown;
        stateT = 0;
        countdown = 3f;
        lastCount = 4;
        StageLabel(Themes.Get(0, 0), 0);
        Populate();
    }

    void StartEnding()
    {
        int kind = path.Count >= 5 ? path[4] : 0;
        ending = new EndingScene(kind, Themes.Get(4, kind));
        state = State.Ending;
        stateT = 0;
    }

    void ToResults()
    {
        state = State.Results;
        stateT = 0;
        paused = false;
        if (score > best && !autopilot) { best = score; SaveBest(); }
    }

    void Banner(string a, string b, Color c, float t)
    {
        banner = a;
        banner2 = b;
        bannerColor = c;
        bannerT = t;
    }

    void StageLabel(StageTheme th, int level)
    {
        stageLabel = $"STAGE {level + 1}  -  {th.Name}";
        stageLabelT = 5;
    }

    // ================================================================== boucle

    public void Frame(float dt)
    {
        globalT += dt;
        if (Input.Fullscreen) Raylib.ToggleBorderlessWindowed();
        else HandleInput();
        if (Input.Mute) audio.Muted = !audio.Muted;

        if (!paused)
        {
            accumulator += MathF.Min(dt, 0.1f);
            while (accumulator >= Cfg.Step)
            {
                Update(Cfg.Step);
                accumulator -= Cfg.Step;
            }
        }
        UpdateAudio();
        audio.Update();
        RenderScene();
        Present();
    }

    void HandleInput()
    {
        switch (state)
        {
            case State.Title:
                if (Input.Back) QuitRequested = true;
                else if (Input.Left || Input.Right) { automatic = !automatic; audio.PlayBeep(660, 0.05f); }
                else if (Input.Confirm)
                {
                    state = State.Radio;
                    stateT = 0;
                    audio.Music.Play(Songs.All[radio]);
                    audio.PlayBeep(880, 0.08f);
                }
                break;
            case State.Radio:
                if (Input.Back) GoTitle();
                else if (Input.Left || Input.Right)
                {
                    radio = (radio + (Input.Left ? Songs.All.Length - 1 : 1)) % Songs.All.Length;
                    audio.Music.Play(Songs.All[radio]);
                }
                else if (Input.Confirm) StartRace();
                break;
            case State.Countdown:
            case State.Racing:
            case State.TimeUp:
            case State.Goal:
                if (paused)
                {
                    if (Input.Pause) paused = false;
                    else if (Input.Quit) GoTitle();
                }
                else if (Input.Pause) paused = true;
                else if (!automatic && Input.Shift && state is State.Racing or State.Countdown)
                {
                    lowGear = !lowGear;
                    audio.PlayShift();
                }
                break;
            case State.Ending:
                if (ending!.T > 2 && Input.Confirm) ToResults();
                break;
            case State.Results:
                if (stateT > 1 && (Input.Confirm || Input.Back)) GoTitle();
                break;
        }
    }

    void Update(float dt)
    {
        stateT += dt;
        bannerT -= dt;
        stageLabelT -= dt;
        bgBlend = MathF.Min(1, bgBlend + dt / 2.5f);
        shake = MathF.Max(0, shake - dt * 3);
        switch (state)
        {
            case State.Title:
            case State.Radio:
                UpdateAttract(dt);
                break;
            case State.Countdown:
                UpdateCountdown(dt);
                break;
            case State.Racing:
            case State.TimeUp:
            case State.Goal:
                UpdateRace(dt);
                break;
            case State.Ending:
                ending!.Update(dt);
                if (ending.Done) ToResults();
                break;
        }
        UpdateParticles(dt);
    }

    // ================================================================== physique

    void UpdateCountdown(float dt)
    {
        countdown -= dt;
        throttleHeld = Input.Accel;
        rev += ((throttleHeld ? 0.95f : 0.12f) - rev) * MathF.Min(1, dt * 5);
        int n = (int)MathF.Ceiling(countdown);
        if (n != lastCount)
        {
            lastCount = n;
            if (n >= 1)
            {
                audio.PlayBeep(440, 0.25f);
                Banner(n.ToString(), "", Hex(0xFFE040), 0.9f);
            }
            else
            {
                audio.PlayBeep(880, 0.6f);
                Banner("GO!", "", Hex(0x60FF60), 1.2f);
                state = State.Racing;
                stateT = 0;
            }
        }
        UpdateTraffic(dt);
    }

    float PlayerPct(float z) => z % Seg / Seg;

    void UpdateRace(float dt)
    {
        float z0 = position + Cfg.PlayerZ;
        var seg = track.Find(z0);
        float sp = speed / Cfg.MaxSpeed;
        bool canDrive = state == State.Racing && crashT <= 0;

        float steer = 0;
        bool acc = false, brk = false;
        if (canDrive && autopilot) (steer, acc, brk) = AutoPilot(seg, sp, 1f);
        else if (canDrive) { steer = Input.Steer; acc = Input.Accel; brk = Input.Brake; }
        else if (state == State.Goal)
        {
            steer = Math.Clamp((seg.Nearest(playerX, PlayerPct(z0)) - playerX) * 2, -1, 1);
            brk = stateT > 0.8f;
        }
        lastSteer = steer;
        throttleHeld = acc;

        if (automatic && crashT <= 0)
        {
            if (lowGear && sp > 0.6f) { lowGear = false; audio.PlayShift(); }
            else if (!lowGear && sp < 0.42f) lowGear = true;
        }

        // un peu de direction même à très basse vitesse, pour pouvoir se dégager d'un obstacle
        float dx = dt * 2.2f * MathF.Min(1f, sp * 1.25f + (speed > 0 || acc ? 0.2f : 0));
        float startPos = position;
        position = MathF.Min(position + speed * dt, track.Length - Seg * 30);

        if (crashT <= 0)
        {
            playerX += steer * dx;
            playerX -= dx * sp * seg.Curve * Cfg.Centrifugal;
        }

        float accel;
        if (crashT > 0) accel = -Cfg.MaxSpeed * (crashKind == 0 ? 1.6f : 0.9f);
        else if (acc)
        {
            if (lowGear) accel = sp < Cfg.LowGearTop ? Cfg.MaxSpeed / 3.2f : (Cfg.LowGearTop - sp) * Cfg.MaxSpeed * 3;
            else accel = Cfg.MaxSpeed / 6.5f * (sp < 0.35f ? 0.3f + sp : 1 - MathF.Pow(sp, 8) * 0.95f);
        }
        else if (brk) accel = -Cfg.MaxSpeed / 1.3f;
        else accel = -Cfg.MaxSpeed / 6.5f;
        speed += accel * dt;

        float z1 = position + Cfg.PlayerZ;
        var seg2 = track.Find(z1);
        float pct = PlayerPct(z1);
        float center = seg2.Nearest(playerX, pct);
        offroad = MathF.Abs(playerX - center) > 1.05f;
        if (offroad && speed > Cfg.OffRoadLimit)
        {
            speed -= Cfg.MaxSpeed / 1.8f * dt;
            shake = MathF.Max(shake, 0.35f);
        }
        speed = Math.Clamp(speed, 0, Cfg.MaxSpeed);

        if (crashT <= 0) Collide(z0, z1);

        float lo = seg2.Chosen >= 0 ? center : MathF.Min(seg2.CenterA(pct), seg2.CenterB(pct));
        float hi = seg2.Chosen >= 0 ? center : MathF.Max(seg2.CenterA(pct), seg2.CenterB(pct));
        playerX = Math.Clamp(playerX, lo - 3f, hi + 3f);

        int i0 = (int)(z0 / Seg), i1 = (int)((position + Cfg.PlayerZ) / Seg);
        for (int i = i0 + 1; i <= i1 && i < track.Count; i++) Cross(track.Segs[i]);

        Parallax(seg, (position - startPos) / Seg);

        if (state == State.Racing)
        {
            timeLeft -= dt;
            stageTime += dt;
            score += speed * dt * (offroad ? 0.5 : 2.5);
            if (timeLeft <= 0)
            {
                timeLeft = 0;
                state = State.TimeUp;
                stateT = 0;
                Banner("TIME UP", "", Hex(0xFF4040), 99);
                audio.PlayBeep(220, 0.9f);
            }
        }
        else if (state == State.TimeUp && speed <= 1 && stateT > 2.5f) ToResults();
        else if (state == State.Goal)
        {
            if (timeLeft > 0 && stateT > 1)
            {
                float k = MathF.Min(timeLeft, dt * 25);
                timeLeft -= k;
                score += k * 100000;
                if ((int)(timeLeft * 4) != (int)((timeLeft + k) * 4)) audio.PlayBeep(1320, 0.03f, 0, 0.5f);
            }
            if (speed <= 1 && stateT > 3.5f && timeLeft <= 0) StartEnding();
        }

        if (crashT > 0)
        {
            crashT -= dt;
            float c = track.Find(position + Cfg.PlayerZ).Nearest(playerX, pct);
            if (crashT < crashDur * 0.45f) playerX = Lerp(playerX, c, MathF.Min(1, dt * 3));
            if (crashT <= 0) { crashT = 0; speed = 0; playerX = c; }
        }

        float push = MathF.Abs(seg.Curve) * sp * sp;
        skid = 0;
        if (crashT > 0 && crashKind == 1) skid = 1;
        else if (crashT <= 0 && sp > 0.5f && push > 2f && MathF.Abs(steer) > 0.3f) skid = MathF.Min(1, 0.4f + (push - 2) / 2);
        else if (brk && sp > 0.45f) skid = 0.6f;

        if (offroad && sp > 0.05f && state != State.Goal)
            for (int k = -1; k <= 1; k += 2)
                Emit(new Vector2(W / 2f + k * 38, H - 10), new Vector2(k * (10 + rng.NextSingle() * 30), -20 - rng.NextSingle() * 40),
                     0.5f, 3, Shade(seg2.Grass, 0.85f));
        if (skid > 0.3f && rng.NextSingle() < skid)
            for (int k = -1; k <= 1; k += 2)
                Emit(new Vector2(W / 2f + k * 38, H - 8), new Vector2(k * rng.NextSingle() * 15, -10 - rng.NextSingle() * 20),
                     0.6f, 4, Hex(0xE8E8E8, 180));

        UpdateTraffic(dt);
    }

    void Parallax(Segment seg, float segs)
    {
        renderer.SkyOffset = Wrap(renderer.SkyOffset + 0.0006f * seg.Curve * segs);
        renderer.FarOffset = Wrap(renderer.FarOffset + 0.0011f * seg.Curve * segs);
        renderer.NearOffset = Wrap(renderer.NearOffset + 0.0018f * seg.Curve * segs);
        var th = track.Find(position + Cfg.PlayerZ).Theme;
        if (th != bgTheme)
        {
            bgPrev = bgBlend >= 1 ? bgTheme : bgPrev;
            bgTheme = th;
            bgBlend = 0;
        }
    }

    void Cross(Segment s)
    {
        if (attract) return;
        if (s.Checkpoint)
        {
            laps.Add(stageTime);
            stageTime = 0;
            path.Add(s.Branch);
            float ext = Cfg.Extension[s.Level];
            timeLeft += ext;
            Banner("CHECKPOINT", $"EXTENDED PLAY  +{ext:0}", Hex(0xFFE040), 3f);
            StageLabel(s.Theme, s.Level);
            audio.PlayJingle();
        }
        if (s.Decision)
        {
            int side = autopilot ? autoSide : MathF.Abs(playerX - s.A2) <= MathF.Abs(playerX - s.B2) ? 0 : 1;
            playerX -= track.Choose(s, side);
            autoSide = rng.Next(2);
        }
        if (s.Goal)
        {
            laps.Add(stageTime);
            finished = true;
            state = State.Goal;
            stateT = 0;
            Banner("GOAL!", "", Hex(0xFFD830), 99);
            audio.PlayJingle();
        }
    }

    void Collide(float z0, float z1)
    {
        float pw = Cfg.PlayerWidth / Cfg.RoadWidth;
        int i0 = (int)(z0 / Seg), i1 = (int)(z1 / Seg);
        for (int i = i0; i <= i1 && i < track.Count; i++)
        {
            var s = track.Segs[i];
            foreach (var sp in s.Sprites)
            {
                if (!sp.Def.Collide) continue;
                if (sp.Branch >= 0 && s.Chosen >= 0 && sp.Branch != s.Chosen && s.OtherFade > 0.5f) continue;
                float w = sp.Def.WorldW / Cfg.RoadWidth;
                float cx = sp.Center ? sp.Offset : sp.Offset + MathF.Sign(sp.Offset) * w / 2;
                if (Overlap(playerX, pw * 0.9f, cx, w * sp.Def.Hit)) { HitObject(s); return; }
            }
            foreach (var car in s.Cars)
            {
                if (car.Z < z0 - 40 || car.Z > z1 + 80) continue;
                if (speed > car.Speed && Overlap(playerX, pw * 0.85f, car.X, car.Def.WorldW / Cfg.RoadWidth * 0.8f)) { HitCar(car); return; }
            }
        }
    }

    void HitObject(Segment s)
    {
        if (speed > Cfg.MaxSpeed * 0.28f) StartCrash(0);
        else
        {
            speed = 0;
            position = MathF.Max(0, s.Z1 - Cfg.PlayerZ - 150);
            audio.PlayBump();
            shake = 0.6f;
        }
    }

    void HitCar(Car car)
    {
        bool spin = speed - car.Speed > Cfg.MaxSpeed * 0.45f;
        speed = car.Speed * (spin ? 0.5f : 0.55f);
        position = car.Z - Cfg.PlayerZ - 50;
        if (spin) StartCrash(1);
        else { audio.PlayBump(); shake = 0.8f; }
    }

    void StartCrash(int kind)
    {
        crashKind = kind;
        crashDur = crashT = kind == 0 ? 2.6f : 1.5f;
        crashSide = lastSteer >= 0 ? 1 : -1;
        audio.PlayCrash();
        shake = 1;
    }

    // ================================================================== trafic et pilote automatique

    void Populate()
    {
        foreach (var c in track.Cars) c.Seg?.Cars.Remove(c);
        track.Cars.Clear();
        var th = track.Find(position + Cfg.PlayerZ).Theme;
        for (float z = position + Seg * 40; z < position + Seg * 210; z += Seg * (14 + rng.Next(22)) / th.Traffic)
            SpawnCar(z);
        spawnDist = Seg * 10;
    }

    void SpawnCar(float z)
    {
        var s = track.Find(z);
        if (!s.Traffic) return;
        float lane = (rng.Next(3) - 1) * (2f / 3f);
        foreach (var c in track.Cars)
            if (MathF.Abs(c.Z - z) < Seg * 12 && MathF.Abs(c.Lane - lane) < 0.1f) return;
        var def = Art.Traffic[rng.Next(Art.Traffic.Count)];
        var car = new Car
        {
            Def = def, Z = z, Lane = lane, X = lane, Seg = s, Phase = rng.NextSingle() * 6, PreferLeft = rng.Next(2) == 0,
            Speed = Cfg.MaxSpeed * (def.Truck ? 0.22f + rng.NextSingle() * 0.14f : 0.3f + rng.NextSingle() * 0.26f),
        };
        s.Cars.Add(car);
        track.Cars.Add(car);
    }

    void UpdateTraffic(float dt)
    {
        for (int i = track.Cars.Count - 1; i >= 0; i--)
        {
            var car = track.Cars[i];
            car.Z += car.Speed * dt;
            car.Phase += dt;
            var s = track.Find(car.Z);
            bool onLeft = car.Lane < -0.1f || (MathF.Abs(car.Lane) < 0.1f && car.PreferLeft);
            bool lost = s.Chosen >= 0 && (onLeft ? 0 : 1) != s.Chosen && s.OtherFade > 0.6f;
            if (car.Z < position - Seg || car.Z >= track.Length - Seg * 2 || lost)
            {
                car.Seg?.Cars.Remove(car);
                track.Cars.RemoveAt(i);
                continue;
            }
            if (s != car.Seg)
            {
                car.Seg?.Cars.Remove(car);
                s.Cars.Add(car);
                car.Seg = s;
            }
            float pct = car.Z % Seg / Seg;
            car.X = (onLeft ? s.CenterA(pct) : s.CenterB(pct)) + car.Lane + (car.Def.Truck ? MathF.Sin(car.Phase * 1.3f) * 0.06f : 0);
        }

        spawnDist -= speed * dt;
        if (spawnDist <= 0)
        {
            var th = track.Find(position + Cfg.PlayerZ).Theme;
            spawnDist = Seg * (14 + rng.Next(22)) / th.Traffic;
            SpawnCar(position + Seg * Cfg.TrafficHorizon);
        }
    }

    (float steer, bool acc, bool brk) AutoPilot(Segment seg, float sp, float topSpeed)
    {
        float z = position + Cfg.PlayerZ, pct = PlayerPct(z);
        int side = seg.Chosen >= 0 ? seg.Chosen : autoSide;
        float center = seg.Dual ? (side == 0 ? seg.CenterA(pct) : seg.CenterB(pct)) : seg.CenterA(pct);
        bool blocked(float lane) => track.Cars.Any(c => c.Z > z - 60 && c.Z < z + Seg * 28 && MathF.Abs(c.X - (center + lane)) < 0.5f);
        if (blocked(autoLane))
            foreach (float l in new[] { 0f, -2f / 3f, 2f / 3f })
                if (!blocked(l)) { autoLane = l; break; }
        float steer = Math.Clamp((center + autoLane - playerX) * 2.5f + sp * seg.Curve * Cfg.Centrifugal, -1, 1);
        var ahead = track.Find(z + Seg * 30);
        float want = MathF.Max(MathF.Abs(seg.Curve), MathF.Abs(ahead.Curve)) >= 5 ? 0.74f * topSpeed : topSpeed;
        return (steer, sp < want, sp > want + 0.08f);
    }

    void UpdateAttract(float dt)
    {
        float z = position + Cfg.PlayerZ;
        var seg = track.Find(z);
        float sp = speed / Cfg.MaxSpeed;
        var (steer, _, _) = AutoPilot(seg, sp, 0.72f);
        if (seg.Dual && !track.Find(z - Seg).Dual) autoSide = rng.Next(2);
        lastSteer = steer;
        speed += (Cfg.MaxSpeed * 0.72f - speed) * MathF.Min(1, dt * 0.5f);
        float dx = dt * 2.2f * MathF.Min(1f, sp * 1.25f);
        playerX += steer * dx - dx * sp * seg.Curve * Cfg.Centrifugal;
        float startPos = position;
        position += speed * dt;
        Parallax(seg, (position - startPos) / Seg);
        offroad = false;
        skid = 0;
        if (position > track.Length - (Cfg.ForkTail + 30) * Seg)
        {
            position = 0;
            playerX = 0;
            Populate();
        }
        UpdateTraffic(dt);
    }

    // ================================================================== particules

    void Emit(Vector2 p, Vector2 v, float life, float size, Color c) =>
        particles.Add(new Particle { P = p, V = v, Life = life, Max = life, Size = size, C = c });

    void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.P += p.V * dt;
            p.Size += dt * 6;
            p.Life -= dt;
            if (p.Life <= 0) particles.RemoveAt(i);
        }
    }

    // ================================================================== son

    void UpdateAudio()
    {
        bool racing = state is State.Countdown or State.Racing or State.TimeUp or State.Goal;
        audio.Engine = racing && !paused ? 1 : 0;
        float sp = speed / Cfg.MaxSpeed;
        float rpm = state == State.Countdown ? rev : lowGear ? sp / Cfg.LowGearTop : 0.25f + sp * 0.75f;
        if (crashT > 0) rpm *= 0.6f;
        audio.Rpm = Math.Clamp(rpm, 0, 1.05f);
        audio.Throttle = throttleHeld ? 1 : 0.15f;
        audio.Skid = racing && !paused ? skid : 0;
        audio.Offroad = racing && !paused && offroad ? MathF.Min(1, sp * 2) * 0.5f : 0;
    }

    // ================================================================== affichage

    void RenderScene()
    {
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(Color.Black);
        // tout est dessiné en coordonnées logiques 480×270, agrandies vers la résolution réelle
        Raylib.BeginMode2D(new Camera2D(Vector2.Zero, Vector2.Zero, 0, Cfg.ArtScale));
        switch (state)
        {
            case State.Ending: ending!.Draw(renderer); break;
            case State.Results: DrawResults(); break;
            default:
                DrawWorld();
                DrawOverlay();
                break;
        }
        if (paused) DrawPause();
        Raylib.EndMode2D();
        Raylib.EndTextureMode();
    }

    void Present()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        float sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
        float scale = MathF.Min(sw / W, sh / H);
        float dw = W * scale, dh = H * scale;
        Raylib.DrawTexturePro(target.Texture, new Rectangle(0, 0, target.Texture.Width, -target.Texture.Height),
                              new Rectangle((sw - dw) / 2, (sh - dh) / 2, dw, dh),
                              Vector2.Zero, 0, Color.White);
        Raylib.EndDrawing();
    }

    void DrawWorld()
    {
        float z = position + Cfg.PlayerZ;
        var seg = track.Find(z);
        float py = Lerp(seg.Y1, seg.Y2, PlayerPct(z));
        float shakeY = shake > 0 ? (rng.NextSingle() - 0.5f) * shake * 50 : 0;
        renderer.Background(bgPrev, bgTheme, bgBlend);
        renderer.World(track, position, playerX, py + shakeY, seg, DrawPlayer);
        foreach (var p in particles)
            Raylib.DrawCircleV(p.P, p.Size, Fade(p.C, p.Life / p.Max * (p.C.A / 255f)));
    }

    void DrawPlayer()
    {
        var def = Art.PlayerStraight;
        if (speed > 0 && crashT <= 0) def = lastSteer < -0.3f ? Art.PlayerLeft : lastSteer > 0.3f ? Art.PlayerRight : Art.PlayerStraight;
        float rot = 0, lift = 0;
        if (crashT > 0)
        {
            float t = 1 - crashT / crashDur;
            if (crashKind == 0)
            {
                float k = Clamp01(t / 0.55f);
                rot = k * 720 * crashSide;
                lift = MathF.Sin(k * MathF.PI) * 55;
            }
            else
            {
                int f = (int)(t * 24) % 4;
                def = f == 1 ? Art.PlayerRight : f == 3 ? Art.PlayerLeft : Art.PlayerStraight;
                rot = MathF.Sin(t * 30) * 6 * (1 - t);
            }
        }
        float w = def.Tex.Width / (float)Cfg.ArtScale, h = def.Tex.Height / (float)Cfg.ArtScale;
        float bounce = speed > 30 ? ((int)(globalT * 30) % 2) * (offroad ? 2 : 1) : 0;
        float x = MathF.Round(W / 2f), y = MathF.Round(H - 6 - h / 2 - bounce - lift);
        if (lift > 2) Raylib.DrawEllipse(W / 2, H - 8, 48 - lift * 0.3f, 4, Fade(Color.Black, 0.35f));
        Raylib.DrawTexturePro(def.Tex, new Rectangle(0, 0, def.Tex.Width, def.Tex.Height), new Rectangle(x, y, w, h), new Vector2(w / 2, h / 2), rot, Color.White);
    }

    void DrawOverlay()
    {
        switch (state)
        {
            case State.Title: DrawTitle(); break;
            case State.Radio: DrawRadio(); break;
            default: DrawHud(); break;
        }
    }

    void DrawHud()
    {
        Hud.Text("TIME", 12, 6, 10, Hex(0xFFB020));
        int tl = (int)MathF.Ceiling(timeLeft);
        Color tc = timeLeft < 10 && state == State.Racing && (int)(globalT * 4) % 2 == 0 ? Hex(0xFF3030) : Hex(0xFFE040);
        Hud.Text(tl.ToString(), 12, 16, 30, tc);

        Hud.TextC("SCORE", W / 2f, 6, 10, Hex(0x60E0FF));
        Hud.TextC(((long)score).ToString("N0"), W / 2f, 17, 20, Hex(0x9CFF80));

        Hud.TextR("LAP", W - 12, 6, 10, Hex(0xFF80C0));
        Hud.TextR(FormatLap(stageTime), W - 12, 17, 10, Color.White);
        Hud.TextR($"STAGE {path.Count}", W - 12, 29, 10, Color.White);

        int kmh = (int)(speed / Cfg.MaxSpeed * Cfg.TopKmh);
        Hud.TextR(kmh.ToString(), 76, H - 34, 30, Color.White);
        Hud.Text("km/h", 80, H - 18, 10, Hex(0xFFE040));
        float rpm = audio.Rpm;
        for (int i = 0; i < 20; i++)
        {
            float t = i / 20f;
            Color c = t < 0.6f ? Hex(0x40E060) : t < 0.85f ? Hex(0xF0E040) : Hex(0xF04040);
            int bh = 3 + i / 2;
            Raylib.DrawRectangle(12 + i * 5, H - 40 - bh, 4, bh, t < rpm ? c : Fade(c, 0.22f));
        }

        Raylib.DrawRectangleRounded(new Rectangle(W - 64, H - 22, 52, 14), 0.5f, 6, lowGear ? Hex(0x2060C0) : Hex(0xC02020));
        Hud.TextC(lowGear ? "LOW" : "HIGH", W - 38, H - 19, 10, Color.White, false);
        if (automatic) Hud.TextR("AT", W - 68, H - 19, 10, Hex(0xC0C0D0));
        Hud.CourseMap(W - 60, H - 50, 11, 7, path, false, globalT, 2);

        if (bannerT > 0 && (bannerT > 0.5f || (int)(globalT * 8) % 2 == 0))
        {
            bool count = state == State.Countdown || banner == "GO!";
            Hud.Bold(banner, W / 2f, count ? 136 : 66, count ? 40 : 30, bannerColor, Hex(0x200810));
            if (banner2 != "") Hud.Bold(banner2, W / 2f, 100, 10, Color.White, Hex(0x200810));
        }
        if (stageLabelT > 0 && (bannerT <= 0 || banner == "GO!" || state == State.Countdown))
            Hud.Bold(stageLabel, W / 2f, state == State.Countdown || banner == "GO!" ? 120 : 112, 10, Hex(0xFFFFFF), Hex(0x103060));
        if (state == State.Goal && stateT > 1 && timeLeft > 0)
            Hud.Bold("TIME BONUS", W / 2f, 120, 10, Hex(0x9CFF80), Hex(0x103010));
    }

    void DrawTitle()
    {
        Raylib.DrawRectangle(0, 0, W, H, Fade(Color.Black, 0.18f));
        Hud.Bold("HIGHWAY", W / 2f, 34, 40, Hex(0xFFD030), Hex(0xA01818));
        Hud.Bold("RUN", W / 2f, 76, 40, Hex(0xFF7030), Hex(0x701010));
        Hud.TextC("A TRIBUTE TO THE ARCADE DRIVING CLASSICS", W / 2f, 122, 10, Color.White);
        if ((int)(globalT * 2) % 2 == 0) Hud.Bold("PRESS ENTER", W / 2f, 160, 20, Hex(0xFFFFFF), Hex(0x202060));
        Hud.TextC($"<   TRANSMISSION : {(automatic ? "AUTOMATIC" : "MANUAL")}   >", W / 2f, 190, 10, Hex(0xFFE040));
        Hud.TextC($"BEST SCORE  {(long)best:N0}", W / 2f, 206, 10, Hex(0x9CFF80));
        Hud.TextC("ARROWS DRIVE  -  SPACE GEAR  -  P PAUSE  -  M MUTE  -  F11 FULLSCREEN", W / 2f, H - 14, 10, Hex(0xD0D0E0));
    }

    void DrawRadio()
    {
        Raylib.DrawRectangle(0, 0, W, H, Fade(Color.Black, 0.35f));
        var panel = new Rectangle(70, 58, 340, 160);
        Raylib.DrawRectangleRounded(panel, 0.08f, 8, Hex(0x181820, 235));
        Raylib.DrawRectangleRoundedLinesEx(panel, 0.08f, 8, 2, Hex(0x606078));
        Hud.TextC("SELECT MUSIC", W / 2f, 68, 20, Hex(0xFFD030));
        // cadran de la radio
        Raylib.DrawRectangle(92, 98, 296, 14, Hex(0x302818));
        for (int x = 96; x < 386; x += 8) Raylib.DrawRectangle(x, 104, 1, x % 32 == 0 ? 8 : 4, Hex(0xC0A060));
        float needle = 120 + radio * 120;
        Raylib.DrawRectangle((int)needle, 96, 2, 18, Hex(0xFF3030));
        for (int i = 0; i < Songs.All.Length; i++)
        {
            var s = Songs.All[i];
            bool sel = i == radio;
            float y = 126 + i * 20;
            Color c = sel ? Hex(0xFFFFFF) : Hex(0x8888A0);
            Hud.Text(sel ? ">" : " ", 110, y, 10, Hex(0xFF5050));
            Hud.Text(s.Station, 124, y, 10, c);
            Hud.Text(s.Name, 200, y, 10, c);
        }
        Hud.TextC("<  >  CHANGE STATION        ENTER  START", W / 2f, 198, 10, Hex(0xC0C0D0));
    }

    void DrawPause()
    {
        Raylib.DrawRectangle(0, 0, W, H, Fade(Color.Black, 0.55f));
        Hud.Bold("PAUSE", W / 2f, 100, 30, Color.White, Hex(0x202060));
        Hud.TextC("P / ESC  RESUME      Q  QUIT TO TITLE", W / 2f, 145, 10, Hex(0xD0D0E0));
    }

    void DrawResults()
    {
        var th = bgTheme;
        Raylib.DrawRectangleGradientV(0, 0, W, H, Shade(th.SkyTop, 0.45f), Shade(th.SkyBottom, 0.3f));
        Hud.Bold(finished ? "CONGRATULATIONS!" : "GAME OVER", W / 2f, 10, 20, finished ? Hex(0xFFD830) : Hex(0xFF5040), Hex(0x100808));
        Hud.CourseMap(52, 116, 88, 29, path, true, globalT, 4);
        Hud.TextC($"SCORE  {(long)score:N0}", W / 2f, 206, 10, Hex(0x9CFF80));
        Hud.TextC($"BEST  {(long)best:N0}", W / 2f, 218, 10, Hex(0xC0C0D0));
        if (laps.Count > 0)
            Hud.TextC("LAPS  " + string.Join("   ", laps.Select(FormatLap)), W / 2f, 232, 10, Color.White);
        if (stateT > 1 && (int)(globalT * 2) % 2 == 0) Hud.TextC("PRESS ENTER", W / 2f, 252, 10, Hex(0xFFE040));
    }

    // ================================================================== meilleur score

    static double LoadBest()
    {
        try { return File.Exists(BestFile) ? double.Parse(File.ReadAllText(BestFile)) : 0; }
        catch { return 0; }
    }

    static void SaveBest(double v)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BestFile)!);
            File.WriteAllText(BestFile, ((long)v).ToString());
        }
        catch { }
    }

    void SaveBest() => SaveBest(best);

    // ================================================================== captures automatiques (--shots)

    public void RunShots(string dir)
    {
        Directory.CreateDirectory(dir);
        void Snap(string name)
        {
            RenderScene();
            var img = Raylib.LoadImageFromTexture(target.Texture);
            Raylib.ImageFlipVertical(ref img);
            Raylib.ExportImage(img, Path.Combine(dir, name + ".png"));
            Raylib.UnloadImage(img);
        }
        void Sim(float seconds)
        {
            for (int i = 0; i < seconds * 60; i++) Update(Cfg.Step);
        }

        foreach (var song in Songs.All)
        {
            audio.Music.Play(song);
            Console.WriteLine($"music {song.Name}: {audio.Analyze(8)}");
        }
        audio.Music.Stop();
        audio.Engine = 1;
        foreach (float r in new[] { 0.1f, 0.5f, 1f })
        {
            audio.Rpm = r; audio.Throttle = 1;
            Console.WriteLine($"engine rpm {r}: {audio.Analyze(2)}");
        }
        audio.Music.Play(Songs.All[0]);
        Console.WriteLine($"engine+music: {audio.Analyze(4)}");
        audio.Music.Stop();
        audio.Engine = 0;

        Sim(2); Snap("00_title");
        state = State.Radio; Snap("01_radio");
        StartRace(); Sim(1.2f); Snap("02_countdown");
        autopilot = true;
        autoSide = 1;
        int n = 0;
        for (int k = 0; k < 160 && state != State.Ending; k++)
        {
            if (state == State.Racing) timeLeft = MathF.Max(timeLeft, 60);
            Sim(2);
            var s = track.Find(position + Cfg.PlayerZ);
            if (k % 3 == 0 || s.Dual) Snap($"race_{n++:00}_{s.Theme.Name.Replace(' ', '_').Replace("'", "")}");
        }
        while (state == State.Ending && ending!.T < 6) Sim(1);
        if (state == State.Ending) Snap("ending_reached");
        for (int k = 0; k < 5; k++)
        {
            ending = new EndingScene(k, Themes.Get(4, k));
            state = State.Ending;
            foreach (float t in new[] { 3f, 6.8f, 10f })
            {
                ending.T = t;
                Snap($"ending_{(char)('A' + k)}_{t:0.0}");
            }
        }
        finished = true;
        ToResults();
        Snap("zz_results");
    }

    public void Dispose()
    {
        audio.Dispose();
        Raylib.UnloadRenderTexture(target);
    }
}
