namespace IceBreaker.Frostbite;

/// <summary>
/// Stores asset headers for <see cref="IFrostbiteAsset"/>s.
/// </summary>
public interface IFrostbiteResource
{
    /// <summary>
    /// Exports the object into a conventional asset that can be read by external programs.
    /// </summary>
    /// <returns>A byte array containing the asset in a respective format.</returns>
    public byte[] Export();

}
