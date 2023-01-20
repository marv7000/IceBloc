namespace IceBlocLib.Frostbite;

/// <summary>
/// A <see cref="RelocPtr"/> is a simple pointer that stores a reference with additional padding;
/// </summary>
public struct RelocPtr<T>
{
    public int Ptr = 0;
    public int Pad = 0;
    public T Value = default;

    public RelocPtr() { }

    public override string ToString()
    {
        return $"RelocPtr({Ptr}), {Value}";
    }
}

/// <summary>
/// An array of <see cref="RelocPtr"/>s, prefixed by an array size.
/// </summary>
public struct RelocArray<T>
{
    public uint Size;
    public RelocPtr<T> BaseAddress;

    public override string ToString()
    {
        return $"RelocArray({BaseAddress.Ptr}), {Size}";
    }
}

public static class RelocPtrExtensions
{
    public static RelocPtr<T> ReadRelocPtr<T>(this BinaryReader reader)
    {
        RelocPtr<T> ptr;
        ptr.Ptr = reader.ReadInt32();
        ptr.Pad = reader.ReadInt32(); // Should always be 0, but just read it for consistency.
        // Save our current stream position so we can find the value at the Ptr offset.
        long currentStreamPos = reader.BaseStream.Position;

        ptr.Value = default;
        if (ptr.Ptr != 0)
        {
            reader.BaseStream.Position = ptr.Ptr;
            ptr.Value = (T)reader.ReadByType<T>();
        }

        // Return back to our old position.
        reader.BaseStream.Position = currentStreamPos;
        return ptr;
    }
    public static RelocArray<T> ReadRelocArray<T>(this BinaryReader reader)
    {
        RelocArray<T> arr;
        arr.Size = reader.ReadUInt32();
        arr.BaseAddress = reader.ReadRelocPtr<T>();

        if (arr.BaseAddress.Ptr != 0)
        {
            long currentStreamPos = reader.BaseStream.Position;

            reader.BaseStream.Position = arr.BaseAddress.Ptr;
            List<T> valueList = new();
            for (int i = 0; i < arr.Size; i++)
            {
                valueList.Add((T)reader.ReadByType<T>());
            }
            // Return back to our old position.
            reader.BaseStream.Position = currentStreamPos;
        }
        return arr;
    }
}