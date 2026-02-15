using PathOfBuilding.Core.Import;

namespace PathOfBuilding.Core.Tests.Import;

public class BuildCodecTests
{
    private const string SampleXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PathOfBuilding>
  <Build level=""1"" className=""Marauder"" ascendClassName=""None"" bandit=""None""
         targetVersion=""3_0"" mainSocketGroup=""1""/>
</PathOfBuilding>";

    [Fact]
    public void Encode_ThenDecode_RoundTrips()
    {
        var code = BuildCodec.Encode(SampleXml);
        var decoded = BuildCodec.Decode(code);
        Assert.Equal(SampleXml, decoded);
    }

    [Fact]
    public void Decode_UrlSafeChars_Handled()
    {
        // Encode produces URL-safe output; verify it round-trips
        var code = BuildCodec.Encode(SampleXml);
        // Manually verify URL-safe chars are handled on decode
        Assert.DoesNotContain("+", code);
        Assert.DoesNotContain("/", code);
        var decoded = BuildCodec.Decode(code);
        Assert.Equal(SampleXml, decoded);
    }

    [Fact]
    public void Encode_ProducesUrlSafeString()
    {
        var code = BuildCodec.Encode(SampleXml);
        Assert.DoesNotContain("+", code);
        Assert.DoesNotContain("/", code);
        // Should only contain URL-safe Base64 chars
        Assert.Matches("^[A-Za-z0-9_=-]*$", code);
    }

    [Fact]
    public void Decode_EmptyString_ReturnsEmpty()
    {
        var result = BuildCodec.Decode("");
        Assert.Equal("", result);
    }

    [Fact]
    public void RoundTrip_RealBuild_Matches()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "OccVortex.xml");
        var xml = File.ReadAllText(path);
        var code = BuildCodec.Encode(xml);
        var decoded = BuildCodec.Decode(code);
        Assert.Equal(xml, decoded);
    }
}
