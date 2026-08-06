using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Outcome of a scan for CareHR cards in the RF field.</summary>
public enum ScanOutcome
{
    None,
    Single,
    Multiple,
    Cancelled,
}

/// <summary>Result of UC-004 Scan Card.</summary>
public sealed class ScanResult
{
    private ScanResult(ScanOutcome outcome, CardInformation? card, DeviceErrorCode errorCode, string message)
    {
        Outcome = outcome;
        Card = card;
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
    }

    /// <summary>Gets the scan outcome.</summary>
    public ScanOutcome Outcome { get; }

    /// <summary>Gets the single card when <see cref="Outcome"/> is <see cref="ScanOutcome.Single"/>.</summary>
    public CardInformation? Card { get; }

    /// <summary>Gets the Application error code.</summary>
    public DeviceErrorCode ErrorCode { get; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; }

    /// <summary>Gets whether exactly one card was found.</summary>
    public bool Success => Outcome == ScanOutcome.Single && Card is not null;

    /// <summary>Creates a single-card success result.</summary>
    public static ScanResult SingleCard(CardInformation card) =>
        new(ScanOutcome.Single, card, DeviceErrorCode.None, "OK");

    /// <summary>Creates a no-card result.</summary>
    public static ScanResult NoCard(string message = "No card detected.") =>
        new(ScanOutcome.None, null, DeviceErrorCode.TagNotFound, message);

    /// <summary>Creates a multiple-cards result.</summary>
    public static ScanResult MultipleCards(string message = "Multiple cards detected. Leave exactly one card in the field.") =>
        new(ScanOutcome.Multiple, null, DeviceErrorCode.MultipleCardsDetected, message);

    /// <summary>Creates a cancelled scan result (UC-010).</summary>
    public static ScanResult Cancelled(string message = "Scan cancelled.") =>
        new(ScanOutcome.Cancelled, null, DeviceErrorCode.None, message);

    /// <summary>Creates a device/operation failure result.</summary>
    public static ScanResult Fail(DeviceErrorCode errorCode, string message) =>
        new(ScanOutcome.None, null, errorCode, message);
}
