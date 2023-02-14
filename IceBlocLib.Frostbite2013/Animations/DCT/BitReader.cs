namespace IceBlocLib.Frostbite2013.Animations.DCT;

public class BitReader : RimeReader
{
    /// <summary>
    /// Current position of the stream.
    /// </summary>
    public override long Position
    {
        get => 8 * BaseStream.Position - m_CurrentBitsLeft / 8;
        set => throw new NotSupportedException();
    }


    //public override long Position => BaseStream.Position * 8 + (m_BitBuffer.Length == 0? 0 : m_BitBuffer.Length*8 - m_CurrentBitsLeft);

    /// <summary>
    /// Length of the underlying stream.
    /// </summary>
    /// 
    public override long Length => BaseStream.Length * 8;

    // Declare our capabilities.
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => false;


    protected byte[] m_BitBuffer = new byte[0];

    protected int m_BitsPerSlice = 0;
    protected int m_CurrentBitsLeft = 0;


    protected int AlignedBitsPerSlice => BytesPerSlice * 8;
    protected int BytesPerSlice => (m_BitsPerSlice + 7) / 8;

    protected int CurrentByteIndexLow => (m_CurrentBitsLeft - 1) / 8;
    protected int CurrentByteIndexHigh => BytesPerSlice - CurrentByteIndexLow - 1;


    private bool HasReadHigh { get; set; } = false;
    private bool HasReadLow { get; set; } = false;


    /// <summary>
    /// Constructs a new binary reader with the given bit converter, reading
    /// to the given stream.
    /// </summary>
    /// <param name="p_BitConverter">Converter to use when reading data</param>
    /// <param name="p_Stream">Stream to read data from</param>
    /// <param name="p_ShouldDispose">Whether to dispose the input stream when disposing the reader or not.</param>
    public BitReader(Stream p_Stream, int p_BitCountPerSlice = 8, Endianness p_Endianness = Endianness.BigEndian, bool p_ShouldDispose = true)
        : base(p_Stream, p_Endianness, p_ShouldDispose)
    {
        if (!p_Stream.CanRead)
            throw new ArgumentException("Stream isn't readable", nameof(p_Stream));

        if (p_BitCountPerSlice % 8 != 0)
            throw new ArgumentException("BitReader does not support bit slices that are not 8 bit aligned");

        m_BitsPerSlice = p_BitCountPerSlice;
        m_BitBuffer = new byte[BytesPerSlice];
    }



    public ulong ReadUIntLow(int p_BitCount)
    {
        ulong Result = 0;
        for (var i = 0; i < p_BitCount; i++)
        {
            Result <<= 1;
            Result |= ReadLowBit() ? (ulong)1 : 0;
        }


        return Result;
    }


    public long ReadIntLow(int p_BitCount)
    {
        var s_Result = ReadUIntLow(p_BitCount);


        if ((s_Result & (ulong)1 << p_BitCount - 1) != 0)
            return (long)(s_Result | ulong.MaxValue << p_BitCount);

        return (long)s_Result;
    }

    public bool ReadLowBit()
    {
        UpdateBits();

        if (m_CurrentBitsLeft == 0)
            throw new Exception("Bit reader issue!");

        if (HasReadHigh)
            throw new Exception("Trying to read low bit after reading high bit. Will result in data loss. pLz fix!");
        HasReadLow = true;


        var s_BitMask = 1 << (m_CurrentBitsLeft - m_BitsPerSlice) % 8;

        bool Result = (m_BitBuffer[CurrentByteIndexLow] & s_BitMask) != 0;

        //m_BitBuffer[CurrentByteIndexLow] >>= 1;
        m_CurrentBitsLeft--;

        return Result;
    }


    public ulong ReadUIntHigh(int p_BitCount)
    {
        ulong Result = 0;
        for (var i = 0; i < p_BitCount; i++)
        {
            Result <<= 1;
            Result |= ReadHighBit() ? (ulong)1 : 0;
        }

        return Result;
    }
    public long ReadIntHigh(int p_BitCount)
    {
        var s_Result = ReadUIntHigh(p_BitCount);

        if ((s_Result & (ulong)1 << p_BitCount - 1) != 0)
            return (long)(s_Result | ulong.MaxValue << p_BitCount);

        return (long)s_Result;
    }


    public bool ReadHighBit()
    {
        UpdateBits();

        if (m_CurrentBitsLeft == 0)
            throw new Exception("Bit reader issue!");

        if (HasReadLow)
            throw new Exception("Trying to read high bit after reading low bit. Will result in data loss. pLz fix!");
        HasReadHigh = true;


        var s_BitMask = 1 << m_CurrentBitsLeft - 8 * CurrentByteIndexLow - 1;

        bool Result = (m_BitBuffer[CurrentByteIndexHigh] & s_BitMask) != 0;

        // not mask, so it would be disabled after this
        //m_BitBuffer[CurrentByteIndexHigh] &= (byte)~(s_BitMask);
        m_CurrentBitsLeft--;

        return Result;
    }


    private void UpdateBits()
    {
        CheckDisposed();

        if (m_CurrentBitsLeft == 0)
        {
            if (ReadInternal(m_BitBuffer, 0, BytesPerSlice) == 0)
                throw new Exception("End of stream bitreader");

            if (BytesPerSlice > 1 && Endianness != Endianness.BigEndian)
            {
                var s_TempBuffer = new byte[BytesPerSlice];

                for (var i = 0; i < BytesPerSlice; i++)
                    s_TempBuffer[BytesPerSlice - i - 1] = m_BitBuffer[i];

                s_TempBuffer.CopyTo(m_BitBuffer, 0);
            }

            m_CurrentBitsLeft = m_BitsPerSlice;
        }
    }


    /// <summary>
    /// Reads a single byte from the stream.
    /// </summary>
    /// <returns>The byte read</returns>
    public override int ReadByte()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads the specified number of bytes into the given buffer, starting at
    /// the given offset.
    /// </summary>
    /// <param name="p_Data">The buffer to copy data into</param>
    /// <param name="p_Offset">The offset to copy data into</param>
    /// <param name="p_Count"></param>
    /// <returns>The number of bytes actually read. This will only be less than
    /// the requested number of bytes if the end of the stream is reached.
    /// </returns>
    public override int ReadBytes(byte[] p_Data, int p_Offset, int p_Count)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads the specified number of bytes, returning them in a new byte array.
    /// If not enough bytes are available before the end of the stream, this
    /// method will throw an exception.
    /// </summary>
    /// <param name="p_Count">The number of bytes to read</param>
    /// <returns>The bytes read</returns>
    public override byte[] ReadBytes(int p_Count)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Disposes of the underlying stream.
    /// </summary>
    public new virtual void Dispose()
    {
        base.Dispose();

        CheckDisposed();

        m_Disposed = true;

        if (m_ShouldDispose)
            BaseStream.Dispose();
    }

    public override void Flush()
    {
        CheckDisposed();
        BaseStream.Flush();
    }

    public override long Seek(long p_Offset, SeekOrigin p_Origin)
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] p_Buffer, int p_Offset, int p_Count)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long p_Value)
    {
        CheckDisposed();
        BaseStream.SetLength(p_Value);
    }

    public override void Write(byte[] p_Buffer, int p_Offset, int p_Count)
    {
        throw new NotImplementedException();
    }
}
