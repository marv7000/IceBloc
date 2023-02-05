using System.Drawing;

namespace IceBlocLib.Frostbite;

/// <summary>
/// A <see cref="RelocPtr"/> is a simple pointer that stores a reference with additional padding;
/// </summary>
public struct RelocPtr
{
    public int Ptr = 0;
    public int Pad = 0;
    public object Value = default;

    public RelocPtr() { }

    public override string ToString()
    {
        return $"RelocPtr({Ptr}), {Value}";
    }
}

/// <summary>
/// An array of <see cref="RelocPtr"/>s, prefixed by an array size.
/// </summary>
public struct RelocArray
{
    public uint Size;
    public RelocPtr BaseAddress;
    public List<object> Value = default;

    public RelocArray() { }

    public override string ToString()
    {
        return $"RelocArray({BaseAddress.Ptr}), {Size}";
    }
}

public static class RelocPtrExtensions
{
    public static RelocPtr ReadRelocPtr<T>(this BinaryReader reader, int size = 0)
    {
        RelocPtr ptr;
        ptr.Ptr = reader.ReadInt32();
        ptr.Pad = reader.ReadInt32(); // Should always be 0, but just read it for consistency.
        // Save our current stream position so we can find the value at the Ptr offset.
        long currentStreamPos = reader.BaseStream.Position;

        ptr.Value = default;
        if (ptr.Ptr != 0)
        {
            if (size == 0)
            {
                reader.BaseStream.Position = ptr.Ptr;
                ptr.Value = (T)reader.ReadByType<T>();
            }

            else
            {
                reader.BaseStream.Position = ptr.Ptr;
                List<object> values = new();
                for (int i = 0; i < size; i++)
                {
                    values.Add(reader.ReadByType<T>());
                }
                ptr.Value = values;
            }
        }

        // Return back to our old position.
        reader.BaseStream.Position = currentStreamPos;
        return ptr;
    }

    public static RelocArray ReadRelocArray<T>(this BinaryReader reader)
    {
        RelocArray arr = new();
        arr.Size = reader.ReadUInt32();
        arr.BaseAddress = reader.ReadRelocPtr<T>();

        if (arr.BaseAddress.Ptr != 0)
        {
            long currentStreamPos = reader.BaseStream.Position;

            reader.BaseStream.Position = arr.BaseAddress.Ptr;
            List<object> valueList = new();
            for (int i = 0; i < arr.Size; i++)
            {
                valueList.Add((T)reader.ReadByType<T>());
            }

            arr.Value = valueList;
            // Return back to our old position.
            reader.BaseStream.Position = currentStreamPos;
        }
        return arr;
    }
}