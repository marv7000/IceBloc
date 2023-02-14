using System.Text;

namespace IceBlocLib.Frostbite2013.Animations.DCT;

public class RimeReader : EndianBinaryReader
{
    // public bool Obfuscated { get; internal set; }

    //public byte[] XorTable { get; set; } = new byte[260];

    //protected long m_ObfuscatedDataOffset;

    public RimeReader(Stream p_Stream, Endianness p_Endianness = Endianness.LittleEndian, bool p_ShouldDispose = true) :
        base(p_Endianness == Endianness.BigEndian ? EndianBitConverter.Big : EndianBitConverter.Little, p_Stream, p_ShouldDispose)
    {
    }
    /// <summary>
    /// Reads out a null terminated string
    /// </summary>
    /// <returns>String with the data inside</returns>
    public string ReadNullTerminatedString()
    {
        char s_TempChar;
        var s_ReturnString = "";

        while ((s_TempChar = (char)ReadUByte()) != '\0')
            if (s_TempChar != '\0')
                s_ReturnString += s_TempChar;

        return s_ReturnString;
    }

    public string ReadFixedLengthString(int p_Length)
    {
        var s_Data = ReadBytes(p_Length);

        // Find null terminator.
        var s_NullIdx = Array.IndexOf(s_Data, (byte)0x00);

        if (s_NullIdx == -1)
            s_NullIdx = s_Data.Length;

        return Encoding.UTF8.GetString(s_Data[..s_NullIdx]);
    }

    /// <summary>
    /// Reads out a basic-unicode based string
    /// </summary>
    /// <param name="p_Length">Length of the unicode string</param>
    /// <returns>String in ASCII formatting</returns>
    public string ReadUnicodeString(int p_Length)
    {
        var s_ReturnString = "";

        while (p_Length-- > 0)
        {
            var s_Char = ReadUInt16();
            s_ReturnString += (char)s_Char;
        }

        return s_ReturnString.Replace("\0", "");
    }

    public char ReadChar()
    {
        return (char)ReadUByte();
    }

    public int Decode77Number()
    {
        var s_Total = 0;

        while (true)
        {
            var s_Byte = ReadUByte();
            s_Total += s_Byte;
            if (s_Byte != 0xFF)
                return s_Total;
        }
    }

    public uint Decode7Bit(out int p_BytesRead)
    {
        p_BytesRead = 1;
        var s_Slice = (uint)ReadUByte();

        var s_Result = s_Slice & 0x7F;

        if ((s_Slice & 0x80) == 0)
            return s_Result;

        ++p_BytesRead;
        s_Slice = ReadUByte();
        s_Result |= (s_Slice & 0x7F) << 7;

        if ((s_Slice & 0x80) == 0)
            return s_Result;

        ++p_BytesRead;
        s_Slice = ReadUByte();
        s_Result |= (s_Slice & 0x7F) << 14;

        var s_Shift = 21;

        while ((s_Slice & 0x80) != 0)
        {
            ++p_BytesRead;
            s_Slice = ReadUByte();
            s_Result |= (s_Slice & 0x7F) << s_Shift;
            s_Shift += 7;
        }

        return s_Result;
    }

    public ulong Decode7Bit64(out int p_BytesRead)
    {
        p_BytesRead = 1;
        var s_Slice = (uint)ReadUByte();

        ulong s_Result = s_Slice & 0x7F;

        if ((s_Slice & 0x80) == 0)
            return s_Result;

        ++p_BytesRead;
        s_Slice = ReadUByte();
        s_Result |= (ulong)(s_Slice & 0x7F) << 7;

        if ((s_Slice & 0x80) == 0)
            return s_Result;

        ++p_BytesRead;
        s_Slice = ReadUByte();
        s_Result |= (ulong)(s_Slice & 0x7F) << 14;

        var s_Shift = 21;

        while ((s_Slice & 0x80) != 0)
        {
            ++p_BytesRead;
            s_Slice = ReadUByte();
            s_Result |= (ulong)(s_Slice & 0x7F) << s_Shift;
            s_Shift += 7;
        }

        return s_Result;
    }

    public uint Read7Bit()
    {
        return Decode7Bit(out _);
    }

    public int DecodeZigZag(out int p_BytesRead)
    {
        var v1 = Decode7Bit(out p_BytesRead);
        var v2 = v1 >> 1;
        var v3 = (int)(v1 << 31);
        return (int)(v2 ^ v3 >> 31);
    }

    public long DecodeZigZag64(out int p_BytesRead)
    {
        var v1 = Decode7Bit64(out p_BytesRead);
        var v2 = (long)(v1 >> 1);
        var v3 = v2 ^ (long)(v1 << 63 >> 63);
        return v3;
    }

    public int ReadZigZag()
    {
        return DecodeZigZag(out _);
    }

    public int Free7Bit()
    {
        int s_Gap = 0;

        while (ReadUByte() != 0)
            ++s_Gap;

        return s_Gap;
    }

    public void Align(int p_Alignment)
    {
        if (Position % p_Alignment == 0)
            return;

        var s_Number = p_Alignment - Position % p_Alignment;
        ReadBytes((int)s_Number);
    }

    /*
    protected override int ReadInternal(byte[] p_Data, int p_Index, int p_Count)
    {
        var s_ReadBytes = base.ReadInternal(p_Data, p_Index, p_Count);
        
        
        var s_CurrentOffset = BaseStream.Position + p_Index - m_ObfuscatedDataOffset;

        if (!Obfuscated)
            return s_ReadBytes;

        for (var i = 0; i < s_ReadBytes; ++i)
            p_Data[i] ^= (byte)((XorTable[(s_CurrentOffset + i) % 257]) ^ 123);

        return s_ReadBytes;
    }
    */
}
