using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace JsonFileComparer.Core;

/// <summary>
/// Normalizes an XML document (e.g. web.config, appsettings-style XML) into the same JSON tree shape
/// used elsewhere in the app, so <see cref="JsonComparer"/> can compare XML and JSON files with identical semantics.
///
/// Conversion rules:
/// - The root element becomes a single-property JSON object keyed by its (local) element name.
/// - Element attributes become properties prefixed with "@" (e.g. key="Foo" -> "@key": "Foo").
/// - A leaf element with no attributes and no children becomes a plain JSON string (its text content).
/// - An element with attributes and/or children becomes a JSON object; if it also has direct text content,
///   that text is stored under a "#text" property.
/// - Repeated sibling elements with the same name (e.g. multiple &lt;add&gt; entries) become a JSON array,
///   preserving document order.
/// - XML namespace prefixes are ignored; only local names are used, to keep comparison paths readable.
/// </summary>
public static class XmlToJsonConverter
{
    public static JsonDocument ConvertToJsonDocument(string xmlText)
    {
        var xdoc = XDocument.Parse(xmlText, LoadOptions.None);
        if (xdoc.Root is null)
        {
            throw new InvalidOperationException("XML document has no root element.");
        }

        var root = new JsonObject
        {
            [xdoc.Root.Name.LocalName] = ConvertElement(xdoc.Root)
        };

        return JsonDocument.Parse(root.ToJsonString());
    }

    private static JsonNode? ConvertElement(XElement element)
    {
        var attributes = element.Attributes().Where(a => !a.IsNamespaceDeclaration).ToList();
        var childElements = element.Elements().ToList();
        var text = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value)).Trim();

        if (attributes.Count == 0 && childElements.Count == 0)
        {
            return JsonValue.Create(text);
        }

        var obj = new JsonObject();

        foreach (var attr in attributes)
        {
            obj[$"@{attr.Name.LocalName}"] = JsonValue.Create(attr.Value);
        }

        foreach (var group in childElements.GroupBy(e => e.Name.LocalName))
        {
            var converted = group.Select(ConvertElement).ToArray();
            obj[group.Key] = converted.Length == 1 ? converted[0] : new JsonArray(converted);
        }

        if (text.Length > 0)
        {
            obj["#text"] = JsonValue.Create(text);
        }

        return obj;
    }
}
