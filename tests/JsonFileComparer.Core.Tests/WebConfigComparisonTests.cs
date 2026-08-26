using JsonFileComparer.Core;
using JsonFileComparer.Core.Models;
using Xunit;

namespace JsonFileComparer.Core.Tests;

public class WebConfigComparisonTests
{
    private const string LeftWebConfig = """
        <configuration>
          <appSettings>
            <add key="Environment" value="Staging" />
            <add key="ApiTimeoutSeconds" value="30" />
            <add key="FeatureFlagX" value="false" />
          </appSettings>
          <connectionStrings>
            <add name="Default" connectionString="Server=stg-db;Database=App" providerName="System.Data.SqlClient" />
          </connectionStrings>
          <system.web>
            <compilation debug="true" targetFramework="4.8" />
          </system.web>
        </configuration>
        """;

    private const string RightWebConfig = """
        <configuration>
          <appSettings>
            <add key="Environment" value="Production" />
            <add key="ApiTimeoutSeconds" value="30" />
            <add key="FeatureFlagY" value="true" />
          </appSettings>
          <connectionStrings>
            <add name="Default" connectionString="Server=prod-db;Database=App" providerName="System.Data.SqlClient" />
          </connectionStrings>
          <system.web>
            <compilation debug="false" targetFramework="4.8" />
          </system.web>
        </configuration>
        """;

    [Fact]
    public void WebConfigDiff_DetectsChangedAppSettingByKey_NotByPosition()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var result = new JsonComparer().Compare(left, right);

        var changed = Assert.Single(result.Entries,
            e => e.Type == DiffType.Changed && e.Path.Contains("Environment"));
        Assert.Equal("\"Staging\"", changed.LeftValue);
        Assert.Equal("\"Production\"", changed.RightValue);
    }

    [Fact]
    public void WebConfigDiff_DetectsAddedAndRemovedFeatureFlags_ByKey()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var result = new JsonComparer().Compare(left, right);

        Assert.Contains(result.Entries, e => e.Type == DiffType.Removed && e.Path.Contains("FeatureFlagX"));
        Assert.Contains(result.Entries, e => e.Type == DiffType.Added && e.Path.Contains("FeatureFlagY"));
    }

    [Fact]
    public void WebConfigDiff_UnchangedAppSetting_CanBeExcludedFromOutput()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var comparer = new JsonComparer(new JsonCompareOptions { IncludeUnchanged = false });
        var result = comparer.Compare(left, right);

        Assert.DoesNotContain(result.Entries, e => e.Path.Contains("ApiTimeoutSeconds"));
    }

    [Fact]
    public void WebConfigDiff_DetectsConnectionStringChange_ByNameAttribute()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var result = new JsonComparer().Compare(left, right);

        var changed = Assert.Single(result.Entries,
            e => e.Type == DiffType.Changed && e.Path.Contains("connectionStrings") && e.Path.Contains("@connectionString"));
        Assert.Contains("stg-db", changed.LeftValue);
        Assert.Contains("prod-db", changed.RightValue);
    }

    [Fact]
    public void WebConfigDiff_DetectsDebugFlagChange_OnSingleElementAttribute()
    {
        using var left = XmlToJsonConverter.ConvertToJsonDocument(LeftWebConfig);
        using var right = XmlToJsonConverter.ConvertToJsonDocument(RightWebConfig);

        var result = new JsonComparer().Compare(left, right);

        Assert.Contains(result.Entries, e =>
            e.Type == DiffType.Changed &&
            e.Path == "$.configuration.system.web.compilation.@debug");
    }
}
