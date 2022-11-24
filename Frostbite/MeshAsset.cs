using System;
using System.Collections.Generic;

namespace IceBloc.Frostbite;

public class MeshAsset
{
    public List<Half[]> Vertices = new List<Half[]>();
    public List<ushort[]> Indices = new List<ushort[]>();

    public MeshAsset(List<Half[]> vertices, List<ushort[]> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public MeshAsset(byte[] data)
    {

    }
}
