using JsonFileComparer.Core.Models;

namespace JsonFileComparer.App.ViewModels;

public sealed class DiffRowViewModel(DiffEntry entry)
{
    public string Path { get; } = entry.Path;

    public string Type { get; } = entry.Type.ToString();

    public string LeftValue { get; } = entry.LeftValue ?? "(missing)";

    public string RightValue { get; } = entry.RightValue ?? "(missing)";

    public DiffType DiffType { get; } = entry.Type;
}
