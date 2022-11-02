using IceBloc.Utility;

namespace IceBloc.Export;

public interface IModelExporter
{
    public void Export(InternalMesh mesh, string path);
}
