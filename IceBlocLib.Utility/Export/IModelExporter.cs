using IceBlocLib.InternalFormats;

namespace IceBlocLib.Utility.Export;

/// <summary>
/// Handles mesh export to more a common CG format.
/// </summary>
public interface IModelExporter
{
    /// <summary>
    /// Exports a <see cref="InternalMesh"/> and saves it to a given path.
    /// </summary>
    public void Export(InternalMesh mesh, string path);

    /// <summary> 
    /// Exports a <see cref="InternalMesh"/> with a <see cref="InternalSkeleton"/> and saves it to a given path.
    /// </summary>
    public void Export(InternalMesh mesh, InternalSkeleton skeleton, string path);

    public void Export(List<InternalMesh> meshes, InternalSkeleton skeleton, string path);
}
