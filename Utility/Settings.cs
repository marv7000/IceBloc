using IceBloc.Export;

namespace IceBloc.Utility;

/// <summary>
/// Stores all settings as a static object.
/// </summary>
public static class Settings
{
    public static Game CurrentGame = Game.Battlefield3;
    public static IModelExporter CurrentModelExporter = new ModelExporterOBJ();
    public static ITextureExporter CurrentTextureExporter = new TextureExporterDDS();
    public static bool Debug = false;
    public static string GamePath = "";
    public static bool ExportRaw = false;
    public static bool ExportConverted = true;
}

public enum Game
{
    Battlefield3,
    Battlefield4
}