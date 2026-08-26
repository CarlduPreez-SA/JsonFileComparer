namespace JsonFileComparer.Core;

public sealed class JsonCompareOptions
{
    public static JsonCompareOptions Default => new();

    /// <summary>How array elements are paired up for comparison.</summary>
    public ArrayComparisonMode ArrayComparisonMode { get; init; } = ArrayComparisonMode.Auto;

    /// <summary>
    /// Candidate property names checked, in order, when matching array elements by key
    /// (used by <see cref="ArrayComparisonMode.Key"/> and <see cref="ArrayComparisonMode.Auto"/>).
    /// </summary>
    public IReadOnlyList<string> ArrayKeyFieldNames { get; init; } =
        ["id", "Id", "ID", "key", "Key", "name", "Name", "@key", "@Key", "@name", "@Name", "@id", "@Id", "@ID"];

    /// <summary>Whether object property name matching is case-sensitive.</summary>
    public bool CaseSensitivePropertyNames { get; init; } = true;

    /// <summary>Absolute tolerance used when comparing two numeric values (0 = exact match).</summary>
    public double NumericTolerance { get; init; } = 0;

    /// <summary>Whether an explicit JSON null and a missing property/element are treated as equal.</summary>
    public bool TreatNullAsMissing { get; init; } = false;

    /// <summary>When false, only differing entries are included in the result (unchanged entries are omitted).</summary>
    public bool IncludeUnchanged { get; init; } = true;

    internal StringComparer PropertyNameComparer =>
        CaseSensitivePropertyNames ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}
