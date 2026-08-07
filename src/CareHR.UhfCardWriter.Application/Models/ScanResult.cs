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

    public ScanOutcome Outcome { get; }

    public CardInformation? Card { get; }

    public DeviceErrorCode ErrorCode { get; }

    public string Message { get; }

    public bool Success => Outcome == ScanOutcome.Single && Card is not null;

    public static ScanResult SingleCard(CardInformation card) =>
        new(ScanOutcome.Single, card, DeviceErrorCode.None, "OK");

    public static ScanResult NoCard(string message = "No card detected.") =>
        new(ScanOutcome.None, null, DeviceErrorCode.TagNotFound, message);

    public static ScanResult MultipleCards(string message = "Multiple cards detected. Leave exactly one card in the field.") =>
        new(ScanOutcome.Multiple, null, DeviceErrorCode.MultipleCardsDetected, message);

    /// <summary>Creates a cancelled scan result (UC-010).</summary>
    public static ScanResult Cancelled(string message = "Scan cancelled.") =>
        new(ScanOutcome.Cancelled, null, DeviceErrorCode.None, message);

    public static ScanResult Fail(DeviceErrorCode errorCode, string message) =>
        new(ScanOutcome.None, null, errorCode, message);
}
