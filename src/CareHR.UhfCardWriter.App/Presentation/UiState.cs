namespace CareHR.UhfCardWriter.App.Presentation;

/// <summary>Presentation-only UI state (not Application workflow enum).</summary>
public enum UiState
{
    Disconnected,
    Ready,
    WaitingForCard,
    Scanning,
    Writing,
    Verifying,
    Registering,
    Success,
    Failed,
    Done,
    Busy,
}
