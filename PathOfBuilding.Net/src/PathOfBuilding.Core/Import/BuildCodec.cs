using System.IO.Compression;
using System.Text;

namespace PathOfBuilding.Core.Import;

public static class BuildCodec
{
    public static string Encode(string xml)
    {
        var bytes = Encoding.UTF8.GetBytes(xml);

        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflate.Write(bytes, 0, bytes.Length);
        }

        var base64 = Convert.ToBase64String(output.ToArray());

        // URL-safe Base64
        return base64.Replace('+', '-').Replace('/', '_');
    }

    public static string Decode(string code)
    {
        // Restore standard Base64 from URL-safe
        var base64 = code.Replace('-', '+').Replace('_', '/');

        // Restore padding if stripped
        var mod = base64.Length % 4;
        if (mod == 2) base64 += "==";
        else if (mod == 3) base64 += "=";

        var compressed = Convert.FromBase64String(base64);

        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray());
    }
}
