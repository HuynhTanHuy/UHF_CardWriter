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
    public ReaderStatusInfo(ReaderStatus status, string message)
    {
        Status = status;
        Message = message ?? string.Empty;
    }

    public ReaderStatus Status { get; }

    public string Message { get; }

    public bool IsConnected => Status == ReaderStatus.Connected;
}
