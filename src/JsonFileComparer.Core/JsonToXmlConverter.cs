using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace JsonFileComparer.Core;

/// <summary>
/// Converts a normalized JSON tree (in the shape produced by <see cref="XmlToJsonConverter"/>) back into XML text.
/// This is the inverse of <see cref="XmlToJsonConverter"/>: "@"-prefixed properties become attributes, "#text"
/// becomes direct text content, array values become repeated sibling elements, and plain string values become
/// leaf element text.
/// </summary>
public static class JsonToXmlConverter
{
    public static string ConvertToXmlString(JsonNode? root)
    {
        if (root is not JsonObject obj || obj.Count != 1)
        {
            throw new InvalidOperationException("Expected a single root-element JSON object to convert to XML.");
        }

        var rootProperty = obj.First();
        var element = BuildElement(rootProperty.Key, rootProperty.Value);

        var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", null), element);
        using var writer = new Utf8StringWriter();
        xdoc.Save(writer, SaveOptions.None);
        return writer.ToString();
    }

    /// <summary>
    /// XDocument.Save(TextWriter) writes the encoding declared by the writer's own Encoding property, not the
    /// one on XDeclaration — a plain StringWriter reports UTF-16 (since .NET strings are UTF-16), which would
    /// produce a mismatched "utf-16" declaration in a file actually written to disk as UTF-8. This reports
    /// UTF-8 instead, matching how the resulting string is subsequently saved via File.WriteAllText.
    /// </summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    private static XElement BuildElement(string name, JsonNode? node)
    {
        var element = new XElement(name);

        switch (node)
        {
            case null:
                break;

            case JsonValue value:
                element.Value = ScalarToText(value);
                break;

            case JsonObject obj:
                foreach (var (key, child) in obj)
                {
                    if (key == "#text")
                    {
                        element.Add(new XText(NodeToText(child)));
                    }
                    else if (key.StartsWith('@'))
                    {
                        element.SetAttributeValue(key[1..], NodeToText(child));
                    }
                    else if (child is JsonArray array)
                    {
                        foreach (var item in array)
                        {
                            element.Add(BuildElement(key, item));
                        }
                    }
                    else
                    {
                        element.Add(BuildElement(key, child));
                    }
                }
                break;

            case JsonArray:
                throw new InvalidOperationException($"Unexpected array directly at element '{name}'; arrays must be values of an object property.");
        }

        return element;
    }

    private static string NodeToText(JsonNode? node) => node is JsonValue value ? ScalarToText(value) : node?.ToJsonString() ?? string.Empty;

    private static string ScalarToText(JsonValue value) => value.TryGetValue<string>(out var s) ? s : value.ToJsonString();
}
