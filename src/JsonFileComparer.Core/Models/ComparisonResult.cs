namespace JsonFileComparer.Core.Models;

public sealed record ComparisonResult
{
    public required IReadOnlyList<DiffEntry> Entries { get; init; }

    public bool AreEqual => Entries.All(e => e.Type == DiffType.Unchanged);

    public int AddedCount => Entries.Count(e => e.Type == DiffType.Added);

    public int RemovedCount => Entries.Count(e => e.Type == DiffType.Removed);

    public int ChangedCount => Entries.Count(e => e.Type is DiffType.Changed or DiffType.TypeChanged);

    public int UnchangedCount => Entries.Count(e => e.Type == DiffType.Unchanged);
}
