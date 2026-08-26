namespace JsonFileComparer.Core;

public sealed class ConfigFileLoadException : Exception
{
    public string FilePath { get; }

    public ConfigFileLoadException(string filePath, string message, Exception? inner = null)
        : base(message, inner)
    {
        FilePath = filePath;
    }
}
