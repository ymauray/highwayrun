namespace HighwayRun;

/// <summary>Constantes du moteur pseudo-3D et de la physique.</summary>
static class Cfg
{
    // Résolution interne (rendu pixelisé, puis mise à l'échelle dans la fenêtre)
    public const int Width = 480, Height = 270;

    public const float SegmentLength = 200f;
    public const float RoadWidth = 1100f;          // demi-largeur de la route
    public const int Lanes = 3;
    public const int RumbleLength = 3;
    public const float CameraHeight = 1000f;
    // Aussi loin que la route reste plus large qu'un pixel ; le brouillard estompe le lointain.
    public const int DrawDistance = 1000;
    // Les voitures apparaissent à cette distance (en segments), pas à l'horizon.
    public const int TrafficHorizon = 300;
    // Longueur de la fourche après le choix : son extrémité doit rester noyée dans le brouillard.
    public const int ForkTail = DrawDistance * 6 / 10;
    public const float FieldOfView = 100f;
    public static readonly float CameraDepth = 1f / MathF.Tan(FieldOfView / 2f * MathF.PI / 180f);
    public static readonly float PlayerZ = CameraHeight * CameraDepth;

    public const float MaxSpeed = SegmentLength * 60f;  // 60 segments par seconde
    public const float TopKmh = 293f;
    public const float LowGearTop = 0.64f;              // vitesse max en LOW (fraction)
    public const float OffRoadLimit = MaxSpeed / 4f;
    public const float CurveScale = 0.55f;
    public const float Centrifugal = 0.3f;
    public const float PlayerWidth = 420f;
    public const float Step = 1f / 60f;

    // Fourche : écart entre les centres des deux routes (en demi-largeurs)
    public const float ForkSplit = 2.4f;

    public const float StartTime = 78f;
    public static readonly float[] Extension = [0f, 60f, 58f, 57f, 56f];
}
