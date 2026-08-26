namespace JsonFileComparer.Core;

public sealed class JsonFileLoadException : Exception
{
    public string FilePath { get; }

    public JsonFileLoadException(string filePath, string message, Exception? inner = null)
        : base(message, inner)
    {
        FilePath = filePath;
    }
}
