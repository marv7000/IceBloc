namespace IceBloc.Frostbite;

/// <summary>
/// A <see cref="RelocPtr"/> is a simple pointer that stores a reference with additional padding;
/// </summary>
public struct RelocPtr<T>
{
    public int Ptr = 0;
    public int Pad = 0;
    public T Value = default(T);

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