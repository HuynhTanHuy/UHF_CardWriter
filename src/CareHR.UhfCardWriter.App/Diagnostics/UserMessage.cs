namespace CareHR.UhfCardWriter.App.Diagnostics;

internal static class UserMessage
{
    public static string ForException(Exception ex)
    {
        return ex switch
        {
            DllNotFoundException =>
                "Reader driver library is missing. Reinstall the application or copy UHFPrimeReader.dll next to the EXE.",
            BadImageFormatException =>
                "Reader driver architecture mismatch (need x64). Use the x64 build of the application.",
            OperationCanceledException => "Operation cancelled.",
            HttpRequestException => "Cannot reach the CareHR API. Check network and Api.BaseUrl.",
            UnauthorizedAccessException => "Access denied. Check file or API permissions.",
            _ => SafeMessage(ex.Message),
        };
    }

    public static string ForDeviceOrOperation(string? message) => SafeMessage(message);

    public static string SafeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "An error occurred.";

        var redacted = AppLog.Redact(message.Trim());
        if (redacted.Length > 280)
            redacted = redacted[..277] + "...";
        return redacted;
    }

    public static string ForHttpStatus(int statusCode, string? body)
    {
        var duplicate = !string.IsNullOrEmpty(body)
                        && body.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase);
        if (duplicate)
            return "Card number already exists in this hospital.";

        return statusCode switch
        {
            401 => "API authentication failed. Authorize Card Writer from CareHR Frontend again.",
            403 => "Not authorized to create RFID cards. Check account permissions.",
            404 => "API endpoint not found. Check Api.BaseUrl and CreateRfidCardPath.",
            409 => "Conflict while registering the card. It may already exist.",
            >= 500 => "CareHR server error. Retry later or contact IT.",
            _ => $"Registration failed (HTTP {statusCode}).",
        };
    }
}
