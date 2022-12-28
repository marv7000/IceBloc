using IceBloc.InternalFormats;

namespace IceBloc.Export;

public interface IAnimationExporter
{
    /// <summary> 
    /// Exports a <see cref="InternalAnimation"/> with a <see cref="InternalSkeleton"/> and saves it to a given path.
    /// </summary>
    public void Export(InternalAnimation mesh, InternalSkeleton skeleton, string path);
}
