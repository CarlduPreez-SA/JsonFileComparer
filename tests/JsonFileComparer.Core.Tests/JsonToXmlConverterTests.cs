using JsonFileComparer.Core;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class JsonToXmlConverterTests
{
    [Fact]
    public void RoundTrip_SimpleElement_PreservesTextValue()
    {
        using var json = XmlToJsonConverter.ConvertToJsonDocument("<root><name>Widget</name></root>");
        var node = System.Text.Json.Nodes.JsonNode.Parse(json.RootElement.GetRawText());

        var xml = JsonToXmlConverter.ConvertToXmlString(node);

        Assert.Contains("<name>Widget</name>", xml);
    }

    [Fact]
    public void RoundTrip_Attributes_PreservesAttributeValues()
    {
        using var json = XmlToJsonConverter.ConvertToJsonDocument("""<root><add key="Foo" value="Bar" /></root>""");
        var node = System.Text.Json.Nodes.JsonNode.Parse(json.RootElement.GetRawText());

        var xml = JsonToXmlConverter.ConvertToXmlString(node);

        Assert.Contains("key=\"Foo\"", xml);
        Assert.Contains("value=\"Bar\"", xml);
    }

    [Fact]
    public void RoundTrip_RepeatedElements_ProducesMultipleSiblings()
    {
        const string original = """
            <root>
              <appSettings>
                <add key="A" value="1" />
                <add key="B" value="2" />
              </appSettings>
            </root>
            """;
        using var json = XmlToJsonConverter.ConvertToJsonDocument(original);
        var node = System.Text.Json.Nodes.JsonNode.Parse(json.RootElement.GetRawText());

        var xml = JsonToXmlConverter.ConvertToXmlString(node);
        using var reparsed = XmlToJsonConverter.ConvertToJsonDocument(xml);

        var addArray = reparsed.RootElement.GetProperty("root").GetProperty("appSettings").GetProperty("add");
        Assert.Equal(2, addArray.GetArrayLength());
        Assert.Equal("A", addArray[0].GetProperty("@key").GetString());
        Assert.Equal("B", addArray[1].GetProperty("@key").GetString());
    }

    [Fact]
    public void XmlDeclaration_DeclaresUtf8_NotUtf16()
    {
        using var json = XmlToJsonConverter.ConvertToJsonDocument("<root><name>Widget</name></root>");
        var node = System.Text.Json.Nodes.JsonNode.Parse(json.RootElement.GetRawText());

        var xml = JsonToXmlConverter.ConvertToXmlString(node);

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
    }

    [Fact]
    public void RoundTrip_XmlToJsonToXmlToJson_IsStable()
    {
        const string original = """
            <configuration>
              <appSettings>
                <add key="Environment" value="Staging" />
              </appSettings>
              <system.web>
                <compilation debug="true" targetFramework="4.8" />
              </system.web>
            </configuration>
            """;

        using var firstJson = XmlToJsonConverter.ConvertToJsonDocument(original);
        var firstText = firstJson.RootElement.GetRawText();

        var node = System.Text.Json.Nodes.JsonNode.Parse(firstText);
        var xml = JsonToXmlConverter.ConvertToXmlString(node);

        using var secondJson = XmlToJsonConverter.ConvertToJsonDocument(xml);
        var secondText = secondJson.RootElement.GetRawText();

        Assert.Equal(firstText, secondText);
    }
}
