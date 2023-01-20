using IceBlocLib.Frostbite;
using IceBlocLib.Frostbite2.Meshes;
using IceBlocLib.InternalFormats;
using IceBlocLib.Utility;

namespace IceBlocLib.Frostbite2.Misc;

public class SkinnedMeshAsset
{
    public static List<InternalMesh> ConvertToInternal(in Dbx skinMesh, in Dbx skeleton)
    {
        var name = (string)skinMesh.Prim["$"]["$"]["Name"].Value;
        var b = IO.ActiveCatalog.Extract(IO.Assets[(name, InternalAssetType.RES)].MetaData, true, InternalAssetType.RES);
        using var stream = new MemoryStream(b);
        var meshes = MeshSet.ConvertToInternal(stream);

        return mesh;
    }
}
