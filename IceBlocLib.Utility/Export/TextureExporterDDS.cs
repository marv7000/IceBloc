using IceBlocLib.InternalFormats;
using IceBloc.Utility;

namespace IceBlocLib.Export;

public class TextureExporterDDS : ITextureExporter
{
    public void Export(InternalTexture texture, string path)
    {
        using var w = new BinaryWriter(File.Create(path + ".dds"));

        var metadata = DirectXTexUtility.GenerateMataData(texture.Width, texture.Height, texture.MipmapCount, texture.Format
        switch
        {
            InternalTextureFormat.DXT1 => DirectXTexUtility.DXGIFormat.BC1UNORM,
            InternalTextureFormat.DXT3 => DirectXTexUtility.DXGIFormat.BC2UNORM,
            InternalTextureFormat.DXT5 => DirectXTexUtility.DXGIFormat.BC3UNORM,
            InternalTextureFormat.RGBA => DirectXTexUtility.DXGIFormat.R8G8B8A8UNORM,
            InternalTextureFormat.RGB0 => DirectXTexUtility.DXGIFormat.R8G8B8A8UNORM,
            InternalTextureFormat.DXN => DirectXTexUtility.DXGIFormat.BC5UNORM,
            _ => 0
        }, false);

        DirectXTexUtility.GenerateDDSHeader(metadata, DirectXTexUtility.DDSFlags.NONE, out var header, out var dx10h);

        w.Write(DirectXTexUtility.EncodeDDSHeader(header, dx10h));
        w.Write(texture.Data);
    }
}
