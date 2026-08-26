using System.Text.Json;

namespace JsonFileComparer.Core;

public static class JsonFileLoader
{
    public static JsonDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new JsonFileLoadException(path, $"File not found: {path}");
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new JsonFileLoadException(path, $"Could not read file: {ex.Message}", ex);
        }

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
            throw new JsonFileLoadException(path, $"Invalid JSON ({Path.GetFileName(path)}): {ex.Message}", ex);
        }
    }
}
