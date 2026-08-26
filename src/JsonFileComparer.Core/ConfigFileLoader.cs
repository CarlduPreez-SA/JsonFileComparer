using System.Text.Json;
using System.Xml;

namespace JsonFileComparer.Core;

/// <summary>Loads a JSON or XML config file (e.g. appsettings.json, web.config) into a comparable JSON document.</summary>
public static class ConfigFileLoader
{
    private static readonly string[] XmlExtensions = [".xml", ".config"];
    private static readonly string[] JsonExtensions = [".json"];

    public static LoadedConfigFile Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new ConfigFileLoadException(path, $"File not found: {path}");
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ConfigFileLoadException(path, $"Could not read file: {ex.Message}", ex);
        }

        var format = DetectFormat(path, text);

        return format == ConfigFileFormat.Xml
            ? new LoadedConfigFile(ParseXml(path, text), ConfigFileFormat.Xml)
            : new LoadedConfigFile(ParseJson(path, text), ConfigFileFormat.Json);
    }

    private static ConfigFileFormat DetectFormat(string path, string text)
    {
        var extension = Path.GetExtension(path);

        if (JsonExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return ConfigFileFormat.Json;
        }

        if (XmlExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return ConfigFileFormat.Xml;
        }

        var trimmed = text.AsSpan().TrimStart();
        return trimmed.StartsWith("<") ? ConfigFileFormat.Xml : ConfigFileFormat.Json;
    }

    private static JsonDocument ParseJson(string path, string text)
    {
        try
        {
            return JsonDocument.Parse(text, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (JsonException ex)
        {
            throw new ConfigFileLoadException(path, $"Invalid JSON ({Path.GetFileName(path)}): {ex.Message}", ex);
        }
    }

    private static JsonDocument ParseXml(string path, string text)
    {
        try
        {
            return XmlToJsonConverter.ConvertToJsonDocument(text);
        }
        catch (Exception ex) when (ex is XmlException or InvalidOperationException)
        {
            throw new ConfigFileLoadException(path, $"Invalid XML ({Path.GetFileName(path)}): {ex.Message}", ex);
        }
    }
}
