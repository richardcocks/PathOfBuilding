namespace PathOfBuilding.Core.Modifiers.Parsing;

/// <summary>
/// Result of a pattern scan operation.
/// Contains the matched value, the remainder of the line with the match removed, and any regex captures.
/// </summary>
public readonly record struct ScanResult<T>(
    T? Value,
    string Remainder,
    string[]? Captures
)
{
    public bool HasMatch => Value is not null;
}
