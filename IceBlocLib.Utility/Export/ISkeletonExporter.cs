using IceBlocLib.InternalFormats;

namespace IceBlocLib.Utility.Export;

public interface ISkeletonExporter
{
    /// <summary>
    /// Exports a <see cref="InternalSkeleton"/> and saves it to a given path.
    /// </summary>
    public void Export(InternalSkeleton skel, string path);
}
