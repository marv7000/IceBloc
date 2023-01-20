using IceBlocLib.Export;

namespace IceBlocLib.Utility;

/// <summary>
/// Stores all settings as a static object.
/// </summary>
public static class Settings
{
    public static Game CurrentGame = Game.UnknownGame;
    public static IModelExporter CurrentModelExporter = new ModelExporterOBJ();
    public static ITextureExporter CurrentTextureExporter = new TextureExporterDDS();
    public static IAnimationExporter CurrentAnimationExporter = new AnimationExporterSMD();
    public static bool Debug = false;
    public static string GamePath = "";
    public static bool ExportRaw = false;
    public static bool ExportConverted = true;
    public static AssetLoadMode LoadMode = AssetLoadMode.All;
    public static double Progress = 0.0;
}

public enum AssetLoadMode
{
    All,
    OnlyRes,
    OnlyEbx
}

public enum Game
{
    UnknownGame = -1,
    Battlefield3,
    Battlefield4,
    BattlefieldHardline,

}