using System.Numerics;
using System.Runtime.InteropServices;

namespace IceBlocLib.Frostbite2.Meshes;

public struct GeometryDeclarationDesc
{
    public Element[] Elements = new Element[16]; // 16 Elements
    public Stream[] Streams = new Stream[4]; // 4 Streams
    public byte ElementCount = 0;
    public byte StreamCount = 0;
    public byte Padding0 = 0;
    public byte Padding1 = 0;

    public GeometryDeclarationDesc() { }

    public Element GetByUsage(VertexElementUsage usage)
    {
        for (int i = 0; i < ElementCount; i++)
        {
            if (Elements[i].Usage == usage)
                return Elements[i];
        }
        return default;
    }

    public struct Element
    {
        public VertexElementUsage Usage = 0;
        public VertexElementFormat Format = 0;
        public byte Offset = 0;
        public byte StreamIndex = 0;

        public Element() { }

        public Vector4 Read(BinaryReader r, int vertexStride)
        {
            long currentPosition = r.BaseStream.Position;

            var buffer = r.ReadBytes(vertexStride);
            var data = buffer[Offset..];

            var result = Vector4.Zero;

            switch (Format)
            {
                case VertexElementFormat.Half2:
                    {
                        var casted = MemoryMarshal.Cast<byte, Half>(data);
                        r.BaseStream.Position = currentPosition;
                        return new((float)casted[0], (float)casted[1], 0.0f, 0.0f);
                    }
                case VertexElementFormat.Half3:
                    {
                        var casted = MemoryMarshal.Cast<byte, Half>(data);
                        r.BaseStream.Position = currentPosition;
                        return new((float)casted[0], (float)casted[1], (float)casted[2], 0.0f);
                    }
                case VertexElementFormat.Half4:
                    {
                        var casted = MemoryMarshal.Cast<byte, Half>(data);
                        r.BaseStream.Position = currentPosition;
                        return new((float)casted[0], (float)casted[1], (float)casted[2], (float)casted[3]);
                    }
                case VertexElementFormat.Byte4:
                    {
                        var casted = MemoryMarshal.Cast<byte, sbyte>(data);
                        r.BaseStream.Position = currentPosition;
                        return new(casted[0], casted[1], casted[2], casted[3]);
                    }
                case VertexElementFormat.Byte4N:
                    {
                        var casted = MemoryMarshal.Cast<byte, sbyte>(data);
                        r.BaseStream.Position = currentPosition;
                        return new(casted[0] / 255.0f, casted[1] / 255.0f, casted[2] / 255.0f, casted[3] / 255.0f);
                    }
                case VertexElementFormat.UByte4:
                    {
                        var casted = MemoryMarshal.Cast<byte, byte>(data);
                        r.BaseStream.Position = currentPosition;
                        return new(casted[0], casted[1], casted[2], casted[3]);
                    }
                case VertexElementFormat.UByte4N:
                    {
                        var casted = MemoryMarshal.Cast<byte, byte>(data);
                        r.BaseStream.Position = currentPosition;
                        return new(casted[0] / 255.0f, casted[1] / 255.0f, casted[2] / 255.0f, casted[3] / 255.0f);
                    }
            }
            r.BaseStream.Position = currentPosition;
            return result;
        }
    }

    public struct Stream
    {
        public byte Stride;
        public VertexElementClassification Classification;
    };
}

public enum VertexElementClassification : byte
{
    VertexElementClassification_PerVertex = 0x0,
    VertexElementClassification_PerInstance = 0x1,
    VertexElementClassification_Index = 0x2,
};

public enum VertexElementUsage : byte
{
    Unknown = 0x0,
    Pos = 0x1,
    BoneIndices = 0x2,
    BoneIndices2 = 0x3,
    BoneWeights = 0x4,
    BoneWeights2 = 0x5,
    Normal = 0x6,
    Tangent = 0x7,
    Binormal = 0x8,
    BinormalSign = 0x9,
    WorldTrans1 = 0xA,
    WorldTrans2 = 0xB,
    WorldTrans3 = 0xC,
    InstanceId = 0xD,
    InstanceUserData0 = 0xE,
    InstanceUserData1 = 0xF,
    XenonIndex = 0x10,
    XenonBarycentric = 0x11,
    XenonQuadID = 0x12,
    Index = 0x13,
    ViewIndex = 0x14,
    Color0 = 0x1E,
    Color1 = 0x1F,
    TexCoord0 = 0x21,
    TexCoord1 = 0x22,
    TexCoord2 = 0x23,
    TexCoord3 = 0x24,
    TexCoord4 = 0x25,
    TexCoord5 = 0x26,
    TexCoord6 = 0x27,
    TexCoord7 = 0x28,
    RadiosityTexCoord = 0x29,
    VisInfo = 0x2A,
    SpriteSize = 0x2B,
    PackedTexCoord0 = 0x2C,
    PackedTexCoord1 = 0x2D,
    PackedTexCoord2 = 0x2E,
    PackedTexCoord3 = 0x2F,
    ClipDistance0 = 0x30,
    ClipDistance1 = 0x31,
    SubMaterialIndex = 0x32,
    BranchInfo = 0x3C,
    PosAndScale = 0x3D,
    Rotation = 0x3E,
    SpriteSizeAndUv = 0x3F,
    FadePos = 0x5A,
    SpawnTime = 0x5B,
    PosAndSoftMul = 0x96,
    Alpha = 0x97,
    Misc0 = 0x98,
    Misc1 = 0x99,
    LeftAndRotation = 0x9A,
    UpAndNormalBlend = 0x9B,
    SH_R = 0x9C,
    SH_G = 0x9D,
    SH_B = 0x9E,
    PosAndRejectCulling = 0x9F,
    Shadow = 0xA0,
    PatchUv = 0xB4,
    Height = 0xB5,
    MaskUVs0 = 0xB6,
    MaskUVs1 = 0xB7,
    MaskUVs2 = 0xB8,
    MaskUVs3 = 0xB9,
    UserMasks = 0xBA,
    HeightfieldUv = 0xBB,
    MaskUv = 0xBC,
    GlobalColorUv = 0xBD,
    HeightfieldPixelSizeAndAspect = 0xBE,
    WorldPositionXz = 0xBF,
    TerrainTextureNodeUv = 0xC0,
    ParentTerrainTextureNodeUv = 0xC1,
    Uv01 = 0xD2,
    WorldPos = 0xD3,
    EyeVector = 0xD4,
    LightParams1 = 0xDC,
    LightParams2 = 0xDD,
    LightSubParams = 0xDE,
    LightSideVector = 0xDF,
    LightInnerAndOuterAngle = 0xE0,
    LightDir = 0xE1,
    LightMatrix1 = 0xE2,
    LightMatrix2 = 0xE3,
    LightMatrix3 = 0xE4,
    LightMatrix4 = 0xE5,
    Custom = 0xE6,
};

public enum VertexElementFormat : byte
{
    None = 0x0,
    Float = 0x1,
    Float2 = 0x2,
    Float3 = 0x3,
    Float4 = 0x4,
    Half = 0x5,
    Half2 = 0x6,
    Half3 = 0x7,
    Half4 = 0x8,
    UByteN = 0x32,
    Byte4 = 0xA,
    Byte4N = 0xB,
    UByte4 = 0xC,
    UByte4N = 0xD,
    Short = 0xE,
    Short2 = 0xF,
    Short3 = 0x10,
    Short4 = 0x11,
    ShortN = 0x12,
    Short2N = 0x13,
    Short3N = 0x14,
    Short4N = 0x15,
    UShort2 = 0x16,
    UShort4 = 0x17,
    UShort2N = 0x18,
    UShort4N = 0x19,
    Int = 0x1A,
    Int2 = 0x1B,
    Int3 = 0x33,
    Int4 = 0x1C,
    IntN = 0x1D,
    Int2N = 0x1E,
    Int4N = 0x1F,
    UInt = 0x20,
    UInt2 = 0x21,
    UInt3 = 0x34,
    UInt4 = 0x22,
    UIntN = 0x23,
    UInt2N = 0x24,
    UInt4N = 0x25,
    Comp3_10_10_10 = 0x26,
    Comp3N_10_10_10 = 0x27,
    UComp3_10_10_10 = 0x28,
    UComp3N_10_10_10 = 0x29,
    Comp3_11_11_10 = 0x2A,
    Comp3N_11_11_10 = 0x2B,
    UComp3_11_11_10 = 0x2C,
    UComp3N_11_11_10 = 0x2D,
    Comp4_10_10_10_2 = 0x2E,
    Comp4N_10_10_10_2 = 0x2F,
    UComp4_10_10_10_2 = 0x30,
    UComp4N_10_10_10_2 = 0x31,
};
