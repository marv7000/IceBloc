using IceBloc.Export;

namespace IceBloc.Utility;

/// <summary>
/// Stores all settings as a static object.
/// </summary>
public static class Settings
{
    public static Game CurrentGame = Game.Battlefield3;
    public static IModelExporter CurrentModelFormat = new ModelExporterOBJ();
    public static ITextureExporter CurrentTextureFormat = new TextureExporterDDS();
    public static bool Debug = false;
    public static string GamePath = "";
    public static bool ExportRaw = true;
    public static bool ExportConverted = false;
}

public enum Game
{
    Battlefield3,
    Battlefield4
}