using Raylib_cs;

namespace HighwayRun;

/// <summary>Synthèse sonore en temps réel : moteur, crissements, bruitages et musique. Aucun fichier audio.</summary>
sealed class Audio : IDisposable
{
    public const int Rate = 44100;
    const int Block = 2048;
    const float Dt = 1f / Rate;

    readonly AudioStream stream;
    readonly bool ok;
    readonly short[] buf = new short[Block];
    uint noise = 0x9E3779B9;

    // paramètres pilotés par le jeu
    public float Rpm, Throttle, Engine, Skid, Offroad;
    public float MasterVolume = 0.8f;
    public bool Muted;
    public readonly Music Music = new();

    float eFreq = 40, ePhase, ePhase2, eLp, sPhase, sT, oLp;

    sealed class Sfx { public int Kind; public float T, Freq, Dur, Delay, Vol = 1; }
    readonly List<Sfx> sfx = [];
    const int Beep = 0, Crash = 1, Bump = 2, Shift = 3;

    public Audio()
    {
        ok = Raylib.IsAudioDeviceReady();
        if (!ok) return;
        Raylib.SetAudioStreamBufferSizeDefault(Block);
        stream = Raylib.LoadAudioStream(Rate, 16, 1);
        Raylib.PlayAudioStream(stream);
    }

    public void Update()
    {
        if (!ok) return;
        while (Raylib.IsAudioStreamProcessed(stream))
        {
            Fill();
            Raylib.UpdateAudioStream(stream, new ReadOnlySpan<short>(buf), Block);
        }
    }

    float Noise()
    {
        noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
        return noise / (float)uint.MaxValue * 2 - 1;
    }

    /// <summary>Rendu hors temps réel (diagnostic) : renvoie crête et RMS du mixage.</summary>
    public (float peak, float rms) Analyze(float seconds)
    {
        double sum = 0; float peak = 0; int n = 0;
        for (int b = 0; b < seconds * Rate / Block; b++)
        {
            Fill();
            foreach (short v in buf) { float f = v / 30000f; peak = MathF.Max(peak, MathF.Abs(f)); sum += f * f; n++; }
        }
        return (peak, (float)Math.Sqrt(sum / n));
    }

    void Fill()
    {
        float master = Muted ? 0 : MasterVolume;
        for (int i = 0; i < Block; i++)
        {
            float s = EngineSample() + Effects() + Music.Next() * 0.5f;
            s = MathF.Tanh(s * master * 1.2f);
            buf[i] = (short)(s * 30000);
        }
    }

    float EngineSample()
    {
        float target = 38 + Rpm * 190;
        eFreq += (target - eFreq) * 0.0012f;
        ePhase += eFreq * Dt; if (ePhase >= 1) ePhase -= 1;
        ePhase2 += eFreq * 0.5f * Dt; if (ePhase2 >= 1) ePhase2 -= 1;
        float saw = ePhase * 2 - 1;
        float sq = ePhase2 < 0.5f ? 1 : -1;
        float pulse = ePhase < 0.16f ? 1 : 0;
        float n = Noise();
        float raw = saw * 0.35f + sq * 0.25f + pulse * 0.4f * Throttle + n * 0.08f * (0.3f + Throttle);
        float cutoff = 0.05f + 0.22f * Throttle + 0.12f * Rpm;
        eLp += (raw - eLp) * cutoff;
        float o = eLp * Engine * (0.13f + 0.08f * Throttle);

        if (Skid > 0.01f)
        {
            sT += Dt;
            sPhase += (1050 + 45 * MathF.Sin(sT * MathF.Tau * 9)) * Dt; if (sPhase >= 1) sPhase -= 1;
            o += (MathF.Sin(sPhase * MathF.Tau) * 0.08f + n * 0.03f) * Skid;
        }
        if (Offroad > 0.01f)
        {
            oLp += (n - oLp) * 0.06f;
            o += oLp * 0.9f * Offroad;
        }
        return o;
    }

    float Effects()
    {
        float o = 0;
        for (int i = sfx.Count - 1; i >= 0; i--)
        {
            var e = sfx[i];
            if (e.Delay > 0) { e.Delay -= Dt; continue; }
            float t = e.T;
            switch (e.Kind)
            {
                case Beep:
                    float env = t < e.Dur * 0.8f ? 1 : 1 - (t - e.Dur * 0.8f) / (e.Dur * 0.2f);
                    o += ((t * e.Freq) % 1 < 0.5f ? 1 : -1) * 0.12f * env * e.Vol;
                    break;
                case Crash:
                    o += Noise() * MathF.Exp(-t * 3.5f) * 0.55f + MathF.Sin(t * MathF.Tau * (70 - t * 30)) * MathF.Exp(-t * 5) * 0.5f;
                    break;
                case Bump:
                    o += MathF.Sin(t * MathF.Tau * (95 - 50 * t)) * MathF.Exp(-t * 16) * 0.6f + Noise() * MathF.Exp(-t * 30) * 0.3f;
                    break;
                case Shift:
                    o += Noise() * MathF.Exp(-t * 70) * 0.25f;
                    break;
            }
            e.T += Dt;
            if (e.T >= e.Dur) sfx.RemoveAt(i);
        }
        return o;
    }

    public void PlayBeep(float freq, float dur, float delay = 0, float vol = 1) =>
        sfx.Add(new Sfx { Kind = Beep, Freq = freq, Dur = dur, Delay = delay, Vol = vol });
    public void PlayCrash() => sfx.Add(new Sfx { Kind = Crash, Dur = 1.4f });
    public void PlayBump() => sfx.Add(new Sfx { Kind = Bump, Dur = 0.3f });
    public void PlayShift() => sfx.Add(new Sfx { Kind = Shift, Dur = 0.06f });

    public void PlayJingle()
    {
        float[] notes = [72, 76, 79, 84, 79, 84];
        for (int i = 0; i < notes.Length; i++)
            PlayBeep(Music.Freq(notes[i]), i == notes.Length - 1 ? 0.35f : 0.1f, i * 0.1f, 0.8f);
    }

    public void Dispose()
    {
        if (!ok) return;
        Raylib.StopAudioStream(stream);
        Raylib.UnloadAudioStream(stream);
    }
}

sealed record Chord(int Root, int[] Tones);

sealed class Song
{
    public required string Name, Station;
    public float Bpm;
    public Chord[] Chords = [];
    public string[][] Melody = [];
    public string Kick = "x.......x.......", Snare = "....x.......x...", Hat = "x.x.x.x.x.x.x.o.";
    public int[] Bass = [0, 0, 12, 0, 0, 12, 7, 12];
}

/// <summary>Séquenceur minimal : lead (pulse + écho), basse, arpège et batterie, en doubles-croches.</summary>
sealed class Music
{
    const float Rate = Audio.Rate, Dt = 1f / Audio.Rate;
    Song? song;
    double stepLen, untilNext;
    int step;
    uint noise = 0x2545F491;

    float leadF, leadPh, leadT, leadEnv; bool leadOn;
    float bassF, bassPh, bassT, bassLp;
    float arpF, arpPh, arpT = 9;
    float kickT = 9, kickPh, snareT = 9, hatT = 9, hatLp; bool hatOpen;
    readonly float[] echo = new float[(int)(Rate * 0.33f)];
    int echoPos;

    public Song? Current => song;

    public static float Freq(float midi) => 440f * MathF.Pow(2, (midi - 69) / 12f);

    public void Play(Song s)
    {
        song = s;
        step = -1;
        untilNext = 0;
        stepLen = Rate * 60.0 / s.Bpm / 4.0;
        leadOn = false;
    }

    public void Stop() => song = null;

    float Noise()
    {
        noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
        return noise / (float)uint.MaxValue * 2 - 1;
    }

    public float Next()
    {
        if (song == null && leadEnv <= 0 && kickT > 1) return 0;
        if (song != null)
        {
            if (untilNext <= 0) { step++; Trigger(song); untilNext += stepLen; }
            untilNext -= 1;
        }
        else leadOn = false;

        float o = 0;
        leadT += Dt;
        if (leadOn) leadEnv = leadT < 0.015f ? leadT / 0.015f : MathF.Max(0.62f, 1 - (leadT - 0.015f) * 1.4f);
        else leadEnv = MathF.Max(0, leadEnv - Dt / 0.1f);
        float vib = leadT > 0.2f ? 1 + 0.006f * MathF.Sin(leadT * MathF.Tau * 5.5f) : 1;
        leadPh += leadF * vib * Dt; if (leadPh >= 1) leadPh -= 1;
        float lead = ((leadPh < 0.3f ? 1 : -1) * 0.7f + (leadPh * 2 - 1) * 0.3f) * leadEnv * 0.12f;
        float e = echo[echoPos];
        echo[echoPos] = lead + e * 0.35f;
        echoPos = (echoPos + 1) % echo.Length;
        o += lead + e * 0.3f;

        bassT += Dt;
        bassPh += bassF * Dt; if (bassPh >= 1) bassPh -= 1;
        bassLp += (bassPh * 2 - 1 - bassLp) * 0.09f;
        o += bassLp * MathF.Max(0.35f, MathF.Exp(-bassT * 5)) * 0.3f;

        arpT += Dt;
        if (arpT < 0.3f)
        {
            arpPh += arpF * Dt; if (arpPh >= 1) arpPh -= 1;
            o += (4 * MathF.Abs(arpPh - 0.5f) - 1) * MathF.Exp(-arpT * 12) * 0.06f;
        }

        kickT += Dt;
        if (kickT < 0.4f)
        {
            kickPh += (45 + 110 * MathF.Exp(-kickT * 35)) * Dt;
            o += MathF.Sin(kickPh * MathF.Tau) * MathF.Exp(-kickT * 8) * 0.45f;
        }
        snareT += Dt;
        if (snareT < 0.3f)
            o += Noise() * MathF.Exp(-snareT * 16) * 0.16f + MathF.Sin(snareT * MathF.Tau * 185) * MathF.Exp(-snareT * 30) * 0.12f;
        hatT += Dt;
        if (hatT < 0.3f)
        {
            float n = Noise();
            hatLp += (n - hatLp) * 0.35f;
            o += (n - hatLp) * MathF.Exp(-hatT * (hatOpen ? 10 : 55)) * 0.1f;
        }
        return o;
    }

    void Trigger(Song s)
    {
        int st = step % 16, bar = step / 16 % s.Melody.Length;
        var chord = s.Chords[bar % s.Chords.Length];
        if (s.Kick[st] == 'x') { kickT = 0; kickPh = 0; }
        if (s.Snare[st] == 'x') snareT = 0;
        if (s.Hat[st] != '.') { hatT = 0; hatOpen = s.Hat[st] == 'o'; }
        if (st % 2 == 0)
        {
            int b = s.Bass[st / 2 % s.Bass.Length];
            if (b >= 0) { bassF = Freq(chord.Root + b); bassT = 0; }
            var tok = s.Melody[bar][st / 2];
            if (tok == ".") leadOn = false;
            else if (tok != "-") { leadF = Freq(Songs.N(tok)); leadT = 0; leadOn = true; }
        }
        var tones = chord.Tones;
        arpF = Freq(tones[st % tones.Length] + 12 * (st / tones.Length % 2));
        arpT = 0;
    }
}

/// <summary>Trois morceaux originaux, sélectionnables à la radio avant le départ.</summary>
static class Songs
{
    public static int N(string s)
    {
        int semi = s[0] switch { 'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, _ => 11 };
        int i = 1;
        if (s[i] == '#') { semi++; i++; }
        else if (s[i] == 'b') { semi--; i++; }
        return 12 * (int.Parse(s[i..]) + 1) + semi;
    }

    static Chord C(string root, string tones) => new(N(root), tones.Split(' ').Select(N).ToArray());
    static string[][] M(params string[] bars) => bars.Select(b => b.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();

    public static readonly Song[] All =
    [
        new Song
        {
            Name = "SUNSHINE DRIVE", Station = "FM 76.5", Bpm = 136,
            Chords =
            [
                C("Bb2", "Bb3 D4 F4 A4"), C("A2", "A3 C4 E4 G4"), C("G2", "G3 Bb3 D4 F4"), C("C3", "G3 Bb3 C4 E4"),
                C("Bb2", "Bb3 D4 F4 A4"), C("A2", "A3 C4 E4 G4"), C("G2", "G3 Bb3 D4 F4"), C("C3", "G3 Bb3 C4 E4"),
                C("F2", "A3 C4 E4 F4"), C("D3", "A3 C4 D4 F4"), C("G2", "G3 Bb3 D4 F4"), C("C3", "G3 Bb3 C4 E4"),
                C("F2", "A3 C4 E4 F4"), C("D3", "A3 C4 D4 F4"), C("G2", "G3 Bb3 D4 F4"), C("C3", "G3 Bb3 C4 E4"),
            ],
            Melody = M(
                "F5 - A5 - C6 - A5 G5", "E5 - - - . C5 E5 G5", "F5 - D5 - Bb4 - D5 F5", "E5 - - - G5 - - -",
                "F5 - A5 - C6 - D6 C6", "A5 - G5 - E5 - C5 -", "D5 - F5 - A5 - G5 F5", "G5 - - - . . . .",
                "A5 - C6 - A5 - F5 -", "F5 E5 F5 - A5 - - -", "Bb5 - A5 - G5 - F5 -", "E5 - G5 - C6 - - -",
                "A5 - C6 - D6 - C6 A5", "C6 - A5 - F5 - D5 -", "F5 - G5 - Bb5 - A5 G5", "F5 - - - . . . ."),
        },
        new Song
        {
            Name = "COASTAL BREEZE", Station = "FM 82.1", Bpm = 124,
            Kick = "x.....x...x.....", Hat = "..x...x...x...x.", Bass = [0, -1, 0, 12, -1, 7, 0, 10],
            Chords =
            [
                C("A2", "A3 C4 E4 G4"), C("F2", "A3 C4 E4 F4"), C("G2", "G3 B3 D4"), C("E2", "G3 B3 D4 E4"),
                C("A2", "A3 C4 E4 G4"), C("F2", "A3 C4 E4 F4"), C("G2", "G3 B3 D4"), C("E2", "G#3 B3 D4 E4"),
            ],
            Melody = M(
                "E5 - - - A5 - G5 E5", "C5 - - - D5 E5 - -", "D5 - - - G5 - F5 D5", "B4 - - - . . E5 G5",
                "A5 - - - C6 - B5 A5", "G5 - E5 - C5 - E5 -", "D5 - G5 - B5 - A5 G5", "G#5 - - - E5 - - ."),
        },
        new Song
        {
            Name = "NEON HORIZON", Station = "FM 89.8", Bpm = 150,
            Kick = "x...x...x...x...", Hat = "xxxxxxxxxxxxxxox",
            Chords =
            [
                C("D2", "D4 F#4 A4"), C("B1", "B3 D4 F#4"), C("G2", "G3 B3 D4"), C("A2", "A3 C#4 E4"),
                C("D2", "D4 F#4 A4"), C("B1", "B3 D4 F#4"), C("E2", "E4 G4 B4"), C("A2", "A3 C#4 E4 G4"),
            ],
            Melody = M(
                "A4 - D5 - F#5 - A5 -", "B5 - A5 - F#5 - D5 -", "G5 - - B5 - - D6 -", "C#6 - B5 - A5 - - -",
                "F#5 A5 - F#5 E5 D5 - E5", "F#5 - - - B4 - D5 -", "E5 - G5 - B5 - G5 -", "A5 - - - E5 - C#5 -"),
        },
    ];
}
