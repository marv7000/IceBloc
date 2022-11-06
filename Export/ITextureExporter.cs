using IceBloc.InternalFormats;

namespace IceBloc.Export;

/// <summary>
/// Handles texture export to more a common CG format.
/// </summary>
public interface ITextureExporter
{
    /// <summary>
    /// Exports a <see cref="InternalTexture"/> and saves it to a given folder.
    /// </summary>
    public void Export(InternalTexture texture, string path);
}
