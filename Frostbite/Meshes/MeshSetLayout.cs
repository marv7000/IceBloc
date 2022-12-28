using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite.Meshes;

public struct MeshSetLayout
{
    public const int MaxLodCount = 5;
    public const int LodStride = 8;

    public MeshType MeshType = 0;
    public uint Flags = 0;
    public int LodCount = 0;
    public int MeshCount = 0;
    public int TotalSubsetCount = 0;
    public Vector4 BoundBoxMin = new();
    public Vector4 BoundBoxMax = new();
    public RelocPtr<MeshLayout>[] LOD = new RelocPtr<MeshLayout>[5];
    public RelocPtr<string> Name = new();
    public RelocPtr<string> ShortName = new();
    public int NameHash = 0;

    public MeshSetLayout() { }
}

public enum MeshType : uint
{
    Rigid = 0x0,
    Skinned = 0x1,
    Composite = 0x2,
};
