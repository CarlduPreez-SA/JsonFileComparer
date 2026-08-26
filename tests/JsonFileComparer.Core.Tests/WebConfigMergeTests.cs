using System.Text.Json.Nodes;
using JsonFileComparer.Core;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class WebConfigMergeTests
{
    private const string LeftWebConfig = """
        <configuration>
          <appSettings>
            <add key="Environment" value="Staging" />
            <add key="ApiTimeoutSeconds" value="30" />
            <add key="FeatureFlagX" value="false" />
          </appSettings>
          <system.web>
            <compilation debug="true" targetFramework="4.8" />
          </system.web>
        </configuration>
        """;

    private const string RightWebConfig = """
        <configuration>
          <appSettings>
            <add key="Environment" value="Production" />
            <add key="ApiTimeoutSeconds" value="30" />
            <add key="FeatureFlagY" value="true" />
          </appSettings>
          <system.web>
            <compilation debug="false" targetFramework="4.8" />
          </system.web>
        </configuration>
        """;

    [Fact]
    public void MergingSelectedFieldsIntoLeftWebConfig_ProducesValidUpdatedXml()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var resolutions = new Dictionary<string, MergeSide>
        {
            ["$.configuration.appSettings.add[@key=Environment].@value"] = MergeSide.Right,
            ["$.configuration.appSettings.add[@key=FeatureFlagY]"] = MergeSide.Right
            // FeatureFlagX removal and debug flag change deliberately left at default (Left) -> should be kept as-is.
        };

        var merger = new JsonMerger();
        var mergedNode = merger.Merge(left, right, MergeSide.Left, resolutions);
        var mergedXml = JsonToXmlConverter.ConvertToXmlString(mergedNode);

        using var reparsed = XmlToJsonConverter.ConvertToJsonDocument(mergedXml);
        var appSettings = reparsed.RootElement.GetProperty("configuration").GetProperty("appSettings").GetProperty("add");

        var byKey = appSettings.EnumerateArray()
            .ToDictionary(e => e.GetProperty("@key").GetString()!, e => e.GetProperty("@value").GetString());

        Assert.Equal("Production", byKey["Environment"]); // pulled from right
        Assert.Equal("30", byKey["ApiTimeoutSeconds"]); // unchanged
        Assert.True(byKey.ContainsKey("FeatureFlagX")); // kept (default = left)
        Assert.True(byKey.ContainsKey("FeatureFlagY")); // added from right

        var debugAttr = reparsed.RootElement.GetProperty("configuration").GetProperty("system.web")
            .GetProperty("compilation").GetProperty("@debug").GetString();
        Assert.Equal("true", debugAttr); // kept from left (default), not pulled from right
    }

    [Fact]
    public void MergedXmlIsWellFormedAndReparsable()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var resolutions = new Dictionary<string, MergeSide>
        {
            ["$.configuration.system.web.compilation.@debug"] = MergeSide.Right
        };

        var mergedNode = new JsonMerger().Merge(left, right, MergeSide.Left, resolutions);
        var mergedXml = JsonToXmlConverter.ConvertToXmlString(mergedNode);

        var exception = Record.Exception(() => XmlToJsonConverter.ConvertToJsonDocument(mergedXml));
        Assert.Null(exception);
        Assert.StartsWith("<?xml", mergedXml);
    }
}
