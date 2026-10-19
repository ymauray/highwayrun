using Raylib_cs;

namespace HighwayRun;

/// <summary>Clavier (flèches, Espace) et manette.</summary>
static class Input
{
    const int Pad = 0;
    static bool PadOn => Raylib.IsGamepadAvailable(Pad);
    static bool Key(KeyboardKey k) => Raylib.IsKeyDown(k);
    static bool Pressed(KeyboardKey k) => Raylib.IsKeyPressed(k);
    static bool PadDown(GamepadButton b) => PadOn && Raylib.IsGamepadButtonDown(Pad, b);
    static bool PadPressed(GamepadButton b) => PadOn && Raylib.IsGamepadButtonPressed(Pad, b);

    public static float Steer
    {
        get
        {
            float s = 0;
            if (Key(KeyboardKey.Left)) s -= 1;
            if (Key(KeyboardKey.Right)) s += 1;
            if (PadOn)
            {
                float ax = Raylib.GetGamepadAxisMovement(Pad, GamepadAxis.LeftX);
                if (MathF.Abs(ax) > 0.15f) s += ax;
                if (PadDown(GamepadButton.LeftFaceLeft)) s -= 1;
                if (PadDown(GamepadButton.LeftFaceRight)) s += 1;
            }
            return Math.Clamp(s, -1f, 1f);
        }
    }

    public static bool Accel => Key(KeyboardKey.Up) || PadDown(GamepadButton.RightFaceDown) || PadDown(GamepadButton.RightTrigger2)
                                || (PadOn && Raylib.GetGamepadAxisMovement(Pad, GamepadAxis.RightTrigger) > 0.2f);
    public static bool Brake => Key(KeyboardKey.Down) || PadDown(GamepadButton.RightFaceLeft) || PadDown(GamepadButton.LeftTrigger2)
                                || (PadOn && Raylib.GetGamepadAxisMovement(Pad, GamepadAxis.LeftTrigger) > 0.2f);
    public static bool Shift => Pressed(KeyboardKey.Space) || Pressed(KeyboardKey.LeftShift)
                                || PadPressed(GamepadButton.RightTrigger1) || PadPressed(GamepadButton.RightFaceRight);
    public static bool Confirm => Pressed(KeyboardKey.Enter) || Pressed(KeyboardKey.KpEnter) || Pressed(KeyboardKey.Space)
                                  || PadPressed(GamepadButton.MiddleRight) || PadPressed(GamepadButton.RightFaceDown);
    public static bool Back => Pressed(KeyboardKey.Escape) || PadPressed(GamepadButton.MiddleLeft);
    public static bool Pause => Pressed(KeyboardKey.P) || Pressed(KeyboardKey.Escape) || PadPressed(GamepadButton.MiddleRight);
    public static bool Left => Pressed(KeyboardKey.Left) || PadPressed(GamepadButton.LeftFaceLeft);
    public static bool Right => Pressed(KeyboardKey.Right) || PadPressed(GamepadButton.LeftFaceRight);
    public static bool Quit => Pressed(KeyboardKey.Q);
    // F11 est pris par macOS (afficher le bureau) : F, ⌃⌘F et ⌥Entrée marchent partout
    public static bool Fullscreen => Pressed(KeyboardKey.F) || Pressed(KeyboardKey.F11)
        || (Pressed(KeyboardKey.Enter) && (Key(KeyboardKey.LeftAlt) || Key(KeyboardKey.RightAlt)));
    public static bool Mute => Pressed(KeyboardKey.M);
}
