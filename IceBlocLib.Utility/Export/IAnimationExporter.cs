using IceBlocLib.InternalFormats;

namespace IceBlocLib.Utility.Export;

public interface IAnimationExporter
{
    /// <summary> 
    /// Exports a <see cref="InternalAnimation"/> with a <see cref="InternalSkeleton"/> and saves it to a given path.
    /// </summary>
    public void Export(InternalAnimation animation, InternalSkeleton skeleton, string path);
}
