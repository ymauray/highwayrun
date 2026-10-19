using HighwayRun;
using Raylib_cs;

// --shots <dossier> : joue une partie automatiquement et enregistre des captures (vérification visuelle)
string? shots = args.Length >= 2 && args[0] == "--shots" ? args[1] : null;

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint | (shots != null ? ConfigFlags.HiddenWindow : 0));
Raylib.InitWindow(Cfg.Width * 3, Cfg.Height * 3, "HighwayRun");
Raylib.SetWindowMinSize(Cfg.Width, Cfg.Height);
Raylib.SetExitKey(KeyboardKey.Null);
Raylib.SetTargetFPS(60);
if (shots == null) Raylib.InitAudioDevice();
Rlgl.DisableBackfaceCulling();

Fonts.Load();
Art.Load();
Themes.Init();
Backdrops.Load();

using (var game = new Game())
{
    if (shots != null) game.RunShots(shots);
    else
        while (!Raylib.WindowShouldClose() && !game.QuitRequested)
            game.Frame(Raylib.GetFrameTime());
}

Fonts.Unload();
if (Raylib.IsAudioDeviceReady()) Raylib.CloseAudioDevice();
Raylib.CloseWindow();
