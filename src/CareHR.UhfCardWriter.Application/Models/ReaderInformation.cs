namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>USB reader list item for Operator selection.</summary>
public sealed class ReaderInformation
{
    /// <summary>Initializes reader information.</summary>
    public ReaderInformation(ushort index, string displayName)
    {
        Index = index;
        DisplayName = displayName ?? string.Empty;
    }

    /// <summary>Gets the USB HID device index.</summary>
    public ushort Index { get; }

    /// <summary>Gets a display name / info string from the device.</summary>
    public string DisplayName { get; }
}
