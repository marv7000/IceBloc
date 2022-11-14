using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBloc.Frostbite2;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct MeshSetLayout
{
    public const int MaxLodCount = 5;
    public const int LodStride = 8;

    public MeshType MeshType;
    public uint Flags;
    public int LodCount;
    public int MeshCount;
    public int TotalSubsetCount;
    public Vector4 BoundBoxMin;
    public Vector4 BoundBoxMax;
    public RelocPtr LOD0;
    public RelocPtr LOD1;
    public RelocPtr LOD2;
    public RelocPtr LOD3;
    public RelocPtr LOD4;
    public RelocPtr Name;
    public RelocPtr ShortName;
    public int NameHash;
}

public enum MeshType : uint
{
    Rigid = 0x0,
    Skinned = 0x1,
    Composite = 0x2,
};
