namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>USB reader list item for Operator selection.</summary>
public sealed class ReaderInformation
{
    public ReaderInformation(ushort index, string displayName)
    {
        Index = index;
        DisplayName = displayName ?? string.Empty;
    }

    public ushort Index { get; }

    public string DisplayName { get; }
}
