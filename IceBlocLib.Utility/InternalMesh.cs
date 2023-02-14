using System.Collections.Generic;

namespace IceBlocLib.InternalFormats;

/// <summary>
/// This class holds mesh data being read in and is stored in our own format so we can streamline the exporting process.
/// </summary>
public sealed class InternalMesh
{
    public string Name = "";
    public List<Vertex> Vertices = new();
    public List<(int A, int B, int C)> Faces = new();
    public InternalSkeleton Skeleton;

    public InternalMesh() { }
}

/// <summary>
/// Stores vertex attributes for each element.
/// </summary>
public struct Vertex
{
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public float NormalX;
    public float NormalY;
    public float NormalZ;
    public float TexCoordX;
    public float TexCoordY;
    public int BoneIndexA;
    public int BoneIndexB;
    public int BoneIndexC;
    public int BoneIndexD;
    public float BoneWeightA;
    public float BoneWeightB;
    public float BoneWeightC;
    public float BoneWeightD;
}