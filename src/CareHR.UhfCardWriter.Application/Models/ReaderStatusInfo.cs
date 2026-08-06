namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Connection status of the desk reader session.</summary>
public enum ReaderStatus
{
    Disconnected,
    Connected,
}

/// <summary>Snapshot of reader connection status.</summary>
public sealed class ReaderStatusInfo
{
    /// <summary>Initializes status info.</summary>
    public ReaderStatusInfo(ReaderStatus status, string message)
    {
        Status = status;
        Message = message ?? string.Empty;
    }

    /// <summary>Gets the status.</summary>
    public ReaderStatus Status { get; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; }

    /// <summary>Gets whether the reader session is open.</summary>
    public bool IsConnected => Status == ReaderStatus.Connected;
}
