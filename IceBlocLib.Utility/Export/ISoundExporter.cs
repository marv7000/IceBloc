using IceBlocLib.InternalFormats;

namespace IceBlocLib.Utility.Export;

/// <summary>
/// Handles audio export to more a common format.
/// </summary>
public interface ISoundExporter
{
    /// <summary>
    /// Exports a <see cref="InternalSound"/> and saves it to a given path.
    /// </summary>
    public void Export(InternalSound sound, string path);
}
