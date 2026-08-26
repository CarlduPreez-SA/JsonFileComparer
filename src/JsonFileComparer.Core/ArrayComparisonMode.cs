namespace JsonFileComparer.Core;

public enum ArrayComparisonMode
{
    /// <summary>Compare elements strictly by their position in the array.</summary>
    Index,

    /// <summary>Match elements of arrays-of-objects using a key field (e.g. "id"); falls back to Index if no suitable key is found.</summary>
    Key,

    /// <summary>Use Key matching when a suitable key field is present on every element, otherwise fall back to Index.</summary>
    Auto
}
