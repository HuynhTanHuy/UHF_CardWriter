namespace CareHR.UhfCardWriter.App.Presentation;

/// <summary>Presentation-only UI state (not Application workflow enum).</summary>
public enum UiState
{
    Disconnected,
    Connected,
    Scanning,
    Writing,
    Verifying,
    Registering,
    Completed,
    Error,
    Busy,
}
