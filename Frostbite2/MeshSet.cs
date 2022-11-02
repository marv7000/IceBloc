using System.Numerics;

namespace IceBloc.Frostbite2;

public unsafe struct MeshSet
{
    public const int MaxLodCount = 5;
    public const int LodStride = 8;

    public int MeshType;
    public int Flags;
    public int LodCount;
    public int MeshCount;
    public Vector4 BoundBoxMin;
    public Vector4 BoundBoxMax;
    public RelocPtr<MeshLayout> LOD0;
    public RelocPtr<MeshLayout> LOD1;
    public RelocPtr<MeshLayout> LOD2;
    public RelocPtr<MeshLayout> LOD3;
    public RelocPtr<MeshLayout> LOD4;
    public RelocPtrStr Name;
    public RelocPtrStr ShortName;
}
