using JsonFileComparer.Core;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class ConfigFileLoaderTests
{
    private static string WriteTempFile(string extension, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"jfc-loader-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Load_JsonExtension_IsDetectedAsJson()
    {
        var path = WriteTempFile(".json", """{"a":1}""");
        try
        {
            using var loaded = ConfigFileLoader.Load(path);
            Assert.Equal(ConfigFileFormat.Json, loaded.Format);
            Assert.Equal(1, loaded.Document.RootElement.GetProperty("a").GetInt32());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_XmlExtension_IsDetectedAsXml()
    {
        var path = WriteTempFile(".xml", "<root><a>1</a></root>");
        try
        {
            using var loaded = ConfigFileLoader.Load(path);
            Assert.Equal(ConfigFileFormat.Xml, loaded.Format);
            Assert.Equal("1", loaded.Document.RootElement.GetProperty("root").GetProperty("a").GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ConfigExtension_IsDetectedAsXml()
    {
        var path = WriteTempFile(".config", "<configuration><appSettings/></configuration>");
        try
        {
            using var loaded = ConfigFileLoader.Load(path);
            Assert.Equal(ConfigFileFormat.Xml, loaded.Format);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UnknownExtension_SniffsXmlContent()
    {
        var path = WriteTempFile(".txt", "  <root><a>1</a></root>");
        try
        {
            using var loaded = ConfigFileLoader.Load(path);
            Assert.Equal(ConfigFileFormat.Xml, loaded.Format);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UnknownExtension_SniffsJsonContent()
    {
        var path = WriteTempFile(".txt", """  {"a":1}""");
        try
        {
            using var loaded = ConfigFileLoader.Load(path);
            Assert.Equal(ConfigFileFormat.Json, loaded.Format);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingFile_ThrowsConfigFileLoadException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"jfc-missing-{Guid.NewGuid():N}.json");

        var ex = Assert.Throws<ConfigFileLoadException>(() => ConfigFileLoader.Load(missingPath));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_InvalidXml_ThrowsConfigFileLoadException()
    {
        var path = WriteTempFile(".xml", "<root><unclosed></root>");
        try
        {
            Assert.Throws<ConfigFileLoadException>(() => ConfigFileLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidJson_ThrowsConfigFileLoadException()
    {
        var path = WriteTempFile(".json", "{not valid json");
        try
        {
            Assert.Throws<ConfigFileLoadException>(() => ConfigFileLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
