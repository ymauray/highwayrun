using System.Numerics;
using Raylib_cs;

namespace HighwayRun;

/// <summary>Rendu pseudo-3D façon arcade : projection des segments, puis peinture de l'arrière vers l'avant.</summary>
sealed class Renderer
{
    const int W = Cfg.Width, H = Cfg.Height;
    readonly List<Segment> visible = new(Cfg.DrawDistance);

    public float SkyOffset, FarOffset, NearOffset;

    // ------------------------------------------------------------------ arrière-plan

    public void Background(StageTheme from, StageTheme to, float blend)
    {
        if (blend < 1) DrawBackdrop(from, 1);
        DrawBackdrop(to, blend < 1 ? blend : 1);
    }

    void DrawBackdrop(StageTheme th, float alpha)
    {
        Color top = U.Fade(th.SkyTop, alpha), bottom = U.Fade(th.SkyBottom, alpha);
        Raylib.DrawRectangleGradientV(0, 0, W, H / 2 + 1, top, bottom);
        if (th.Sun)
        {
            float sx = Wrapped(900, SkyOffset), sy = 46;
            Raylib.DrawCircleV(new Vector2(sx, sy), 26, U.Fade(U.Hex(0xFFF4C0), alpha * 0.35f));
            Raylib.DrawCircleV(new Vector2(sx, sy), 19, U.Fade(U.Hex(0xFFF8D8), alpha * 0.6f));
            Raylib.DrawCircleV(new Vector2(sx, sy), 14, U.Fade(Color.White, alpha));
        }
        if (th.Clouds) Layer(Backdrops.Clouds, SkyOffset, 4, U.Fade(th.CloudTint, alpha));
        Layer(th.FarTex, FarOffset, H / 2 - Backdrops.FarH + 1, U.Fade(Color.White, alpha));
        Layer(th.NearTex, NearOffset, H / 2 - Backdrops.NearH + 2, U.Fade(Color.White, alpha));
        Raylib.DrawRectangle(0, H / 2 + 1, W, H / 2, U.Fade(U.Mix(th.Grass1, th.Fog, 0.75f), alpha));
    }

    static float Wrapped(float x, float offset)
    {
        float lw = Backdrops.LayerW;
        float v = (x - offset * lw) % lw;
        if (v < -100) v += lw;
        return v;
    }

    static void Layer(Texture2D tex, float offset, float y, Color tint)
    {
        float x = -(U.Wrap(offset) * tex.Width);
        var src = new Rectangle(0, 0, tex.Width, tex.Height);
        Raylib.DrawTextureRec(tex, src, new Vector2(MathF.Round(x), y), tint);
        Raylib.DrawTextureRec(tex, src, new Vector2(MathF.Round(x + tex.Width), y), tint);
    }

    // ------------------------------------------------------------------ route

    static void Project(ref Proj p, float worldX, float worldY, float worldZ, float camX, float camY, float camZ)
    {
        p.CamZ = worldZ - camZ;
        p.Scale = Cfg.CameraDepth / p.CamZ;
        p.X = W / 2f + p.Scale * (worldX - camX) * W / 2f;
        p.Y = H / 2f - p.Scale * (worldY - camY) * H / 2f;
        p.W = p.Scale * Cfg.RoadWidth * W / 2f;
    }

    public void World(Track track, float position, float playerX, float playerY, Segment playerSeg, Action drawPlayer)
    {
        var baseSeg = track.Find(position);
        float basePercent = position % Cfg.SegmentLength / Cfg.SegmentLength;
        float camX = playerX * Cfg.RoadWidth, camY = playerY + Cfg.CameraHeight;
        float dx = -(baseSeg.Curve * Cfg.CurveScale * basePercent);
        float x = 0;

        visible.Clear();
        for (int n = 0; n < Cfg.DrawDistance; n++)
        {
            int idx = baseSeg.Index + n;
            if (idx >= track.Count) break;
            var s = track.Segs[idx];
            s.Fog = 1 - U.FogVisibility(n / (float)Cfg.DrawDistance, s.Theme.FogDensity);
            Project(ref s.P1, x, s.Y1, s.Index * Cfg.SegmentLength, camX, camY, position);
            Project(ref s.P2, x + dx, s.Y2, (s.Index + 1) * Cfg.SegmentLength, camX, camY, position);
            x += dx;
            dx += s.Curve * Cfg.CurveScale;
            visible.Add(s);
        }

        bool drawn = false;
        for (int i = visible.Count - 1; i >= 0; i--)
        {
            var s = visible[i];
            if (s.P1.CamZ > Cfg.CameraDepth)
            {
                if (s.P2.Y < s.P1.Y && s.P2.Y < H) DrawSegment(s);
                DrawSegmentSprites(s);
            }
            if (s == playerSeg) { drawPlayer(); drawn = true; }
        }
        if (!drawn) drawPlayer();
    }

    static void Quad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, Color c)
    {
        Raylib.DrawTriangle(new Vector2(x1, y1), new Vector2(x2, y2), new Vector2(x3, y3), c);
        Raylib.DrawTriangle(new Vector2(x1, y1), new Vector2(x3, y3), new Vector2(x4, y4), c);
    }

    void DrawSegment(Segment s)
    {
        var fogCol = s.Theme.Fog;
        float y1 = s.P1.Y, y2 = s.P2.Y;
        float gy = MathF.Max(0, MathF.Floor(y2)), gh = MathF.Min(H, MathF.Ceiling(y1)) - gy + 1;
        if (gh > 0) Raylib.DrawRectangle(0, (int)gy, W, (int)gh, U.Mix(s.Grass, fogCol, s.Fog));

        int roads = s.Dual ? 2 : 1;
        Span<float> c1 = [s.A1, s.B1], c2 = [s.A2, s.B2], fog = [s.Fog, s.Fog];
        for (int r = 0; r < roads; r++)
            if (s.Chosen >= 0 && r != s.Chosen) fog[r] = MathF.Max(s.Fog, s.OtherFade);

        float w1 = s.P1.W, w2 = s.P2.W;
        float r1 = w1 / 7f, r2 = w2 / 7f, l1 = w1 / 34f, l2 = w2 / 34f;

        for (int r = 0; r < roads; r++)
        {
            if (fog[r] >= 0.999f && r != s.Chosen && s.Chosen >= 0) continue;
            float x1 = s.P1.X + c1[r] * w1, x2 = s.P2.X + c2[r] * w2;
            var col = U.Mix(s.Rumble, fogCol, fog[r]);
            Quad(x1 - w1 - r1, y1, x1 - w1, y1, x2 - w2, y2, x2 - w2 - r2, y2, col);
            Quad(x1 + w1 + r1, y1, x1 + w1, y1, x2 + w2, y2, x2 + w2 + r2, y2, col);
        }
        for (int r = 0; r < roads; r++)
        {
            if (fog[r] >= 0.999f && r != s.Chosen && s.Chosen >= 0) continue;
            float x1 = s.P1.X + c1[r] * w1, x2 = s.P2.X + c2[r] * w2;
            Quad(x1 - w1, y1, x1 + w1, y1, x2 + w2, y2, x2 - w2, y2, U.Mix(s.Road, fogCol, fog[r]));
        }
        for (int r = 0; r < roads; r++)
        {
            if (fog[r] >= 0.999f && r != s.Chosen && s.Chosen >= 0) continue;
            float x1 = s.P1.X + c1[r] * w1, x2 = s.P2.X + c2[r] * w2;
            var lane = U.Mix(s.Lane, fogCol, fog[r]);
            // lignes de rive continues
            Quad(x1 - w1, y1, x1 - w1 + l1 * 1.5f, y1, x2 - w2 + l2 * 1.5f, y2, x2 - w2, y2, lane);
            Quad(x1 + w1, y1, x1 + w1 - l1 * 1.5f, y1, x2 + w2 - l2 * 1.5f, y2, x2 + w2, y2, lane);
            if (!s.Dashes) continue;
            float lw1 = w1 * 2 / Cfg.Lanes, lw2 = w2 * 2 / Cfg.Lanes;
            float lx1 = x1 - w1 + lw1, lx2 = x2 - w2 + lw2;
            for (int k = 1; k < Cfg.Lanes; k++, lx1 += lw1, lx2 += lw2)
                Quad(lx1 - l1 / 2, y1, lx1 + l1 / 2, y1, lx2 + l2 / 2, y2, lx2 - l2 / 2, y2, lane);
        }
    }

    void DrawSegmentSprites(Segment s)
    {
        foreach (var sp in s.Sprites)
        {
            var def = sp.Offset > 0 && sp.Def.Mirror != null ? sp.Def.Mirror : sp.Def;
            if (!def.Visible) continue;
            float alpha = 1;
            if (sp.Branch >= 0 && s.Chosen >= 0 && sp.Branch != s.Chosen) alpha = 1 - s.OtherFade;
            if (alpha <= 0.01f) continue;
            float sx = s.P1.X + sp.Offset * s.P1.W;
            float ox = sp.Center ? -0.5f : sp.Offset < 0 ? -1f : 0f;
            Sprite(def, s.P1.Scale, sx, s.P1.Y, ox, -1, s.Fog, s.Theme.Fog, alpha);
        }
        foreach (var car in s.Cars)
        {
            float pct = car.Z % Cfg.SegmentLength / Cfg.SegmentLength;
            float scale = U.Lerp(s.P1.Scale, s.P2.Scale, pct);
            float cx = U.Lerp(s.P1.X, s.P2.X, pct) + scale * car.X * Cfg.RoadWidth * W / 2f;
            float cy = U.Lerp(s.P1.Y, s.P2.Y, pct);
            Sprite(car.Def, scale, cx, cy, -0.5f, -1, s.Fog, s.Theme.Fog, 1);
        }
    }

    public static void Sprite(SpriteDef d, float scale, float x, float y, float ox, float oy, float fog, Color fogCol, float alpha)
    {
        float w = d.WorldW * scale * W / 2f;
        float h = w * d.Tex.Height / d.Tex.Width;
        float dx = MathF.Round(x + w * ox), dy = MathF.Round(y + h * oy);
        if (w < 0.8f || dx > W || dx + w < 0 || dy > H || dy + h < 0) return;
        var src = new Rectangle(0, 0, d.Tex.Width, d.Tex.Height);
        var dst = new Rectangle(dx, dy, MathF.Round(w), MathF.Round(h));
        Raylib.DrawTexturePro(d.Tex, src, dst, Vector2.Zero, 0, U.Fade(Color.White, alpha));
        if (fog > 0.02f) Raylib.DrawTexturePro(d.Mask, src, dst, Vector2.Zero, 0, U.Fade(fogCol, fog * alpha));
    }
}
