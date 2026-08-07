namespace CareHR.UhfCardWriter.App.Presentation;

/// <summary>
/// Presentation-only display formatting for CareHR card numbers.
/// Matches CardWritter visual grouping: Hospital + Batch + Serial with spaces.
/// Does not change the logical card number used for write/API.
/// </summary>
internal static class UiCardNumberFormat
{
    /// <summary>
    /// Formats e.g. <c>790480100001</c> → <c>79048 01 00001</c>.
    /// </summary>
    public static string ForDisplay(string? cardNumber, int batchWidth = 2, int serialWidth = 5)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return cardNumber ?? string.Empty;

        var s = cardNumber.Trim();
        if (s is "—" or "-")
            return s;

        if (batchWidth <= 0)
            batchWidth = 2;
        if (serialWidth <= 0)
            serialWidth = 5;

        var tail = batchWidth + serialWidth;
        if (s.Length <= tail)
            return s;

        var hospital = s[..^tail];
        var batch = s[^tail..^serialWidth];
        var serial = s[^serialWidth..];
        return $"{hospital} {batch} {serial}";
    }
}
