using System;
using System.Collections.Generic;
using System.IO;

namespace IceBloc.Frostbite.Packed;

public class CatalogEntry
{
    public byte[] SHA;
    public uint Offset;
    public int DataSize;
    public int CasFileIndex;

    public CatalogEntry(int version, BinaryReader catReader)
    {
        if (version < 3)
        {
            SHA = new byte[0];
            Offset = catReader.ReadUInt32();
            DataSize = catReader.ReadInt32();
            CasFileIndex = catReader.ReadInt32();
        }
        else
        {
            SHA = catReader.ReadBytes(20);
            Offset = catReader.ReadUInt32();
            DataSize = catReader.ReadInt32();
            CasFileIndex = catReader.ReadInt32();
        }
    }
}
