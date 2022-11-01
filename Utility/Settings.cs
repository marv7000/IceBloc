namespace IceBloc.Utility;

/// <summary>
/// Stores all settings as a static object.
/// </summary>
public static class Settings
{
    public static Game CurrentGame = Game.Battlefield3;
    public static bool Debug;

    public static string GamePath = "";

    // Export settings.

    // Models.
    public static bool ExportModelOBJ;
    public static bool ExportModelGLTF;
    public static bool ExportModelSMD;
    public static bool ExportModelXMODEL;
    public static bool ExportModelSEMODEL;

    public static bool ExportRaw;
    public static bool ExportConverted;

}

public enum Game
{
    Battlefield3,
    Battlefield4
}