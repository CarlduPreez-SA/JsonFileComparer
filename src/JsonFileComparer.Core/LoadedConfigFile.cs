using System.Text.Json;

namespace JsonFileComparer.Core;

public sealed record LoadedConfigFile(JsonDocument Document, ConfigFileFormat Format) : IDisposable
{
    public void Dispose() => Document.Dispose();
}
