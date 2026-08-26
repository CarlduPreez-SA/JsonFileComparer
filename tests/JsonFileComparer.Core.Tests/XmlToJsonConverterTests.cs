using System.Text.Json;
using JsonFileComparer.Core;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class XmlToJsonConverterTests
{
    [Fact]
    public void LeafElement_BecomesStringValue()
    {
        using var doc = XmlToJsonConverter.ConvertToJsonDocument("<root><name>Widget</name></root>");

        Assert.Equal("Widget", doc.RootElement.GetProperty("root").GetProperty("name").GetString());
    }

    [Fact]
    public void Attributes_BecomeAtPrefixedProperties()
    {
        using var doc = XmlToJsonConverter.ConvertToJsonDocument("""<root><add key="Foo" value="Bar" /></root>""");

        var add = doc.RootElement.GetProperty("root").GetProperty("add");
        Assert.Equal("Foo", add.GetProperty("@key").GetString());
        Assert.Equal("Bar", add.GetProperty("@value").GetString());
    }

    [Fact]
    public void RepeatedSiblingElements_BecomeJsonArray_PreservingOrder()
    {
        using var doc = XmlToJsonConverter.ConvertToJsonDocument("""
            <root>
              <appSettings>
                <add key="A" value="1" />
                <add key="B" value="2" />
              </appSettings>
            </root>
            """);

        var addArray = doc.RootElement.GetProperty("root").GetProperty("appSettings").GetProperty("add");
        Assert.Equal(JsonValueKind.Array, addArray.ValueKind);
        Assert.Equal(2, addArray.GetArrayLength());
        Assert.Equal("A", addArray[0].GetProperty("@key").GetString());
        Assert.Equal("B", addArray[1].GetProperty("@key").GetString());
    }

    [Fact]
    public void SingleChildElement_DoesNotBecomeArray()
    {
        using var doc = XmlToJsonConverter.ConvertToJsonDocument("""
            <root>
              <appSettings>
                <add key="A" value="1" />
              </appSettings>
            </root>
            """);

        var add = doc.RootElement.GetProperty("root").GetProperty("appSettings").GetProperty("add");
        Assert.Equal(JsonValueKind.Object, add.ValueKind);
    }

    [Fact]
    public void ElementWithAttributesAndText_StoresTextUnderHashText()
    {
        using var doc = XmlToJsonConverter.ConvertToJsonDocument("""<root><note lang="en">Hello</note></root>""");

        var note = doc.RootElement.GetProperty("root").GetProperty("note");
        Assert.Equal("en", note.GetProperty("@lang").GetString());
        Assert.Equal("Hello", note.GetProperty("#text").GetString());
    }

    [Fact]
    public void EmptySelfClosingElement_BecomesEmptyString()
    {
        using var doc = XmlToJsonConverter.ConvertToJsonDocument("<root><value/></root>");

        Assert.Equal(string.Empty, doc.RootElement.GetProperty("root").GetProperty("value").GetString());
    }
}
