namespace CareHR.UhfCardWriter.Infrastructure.Registration;

/// <summary>
/// Options for CareHR RFID tag registry HTTP API (Infrastructure only).
/// </summary>
public sealed class CareHrCardApiOptions
{
    /// <summary>Base URL of the CareHR API (no trailing slash required).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Bearer token for Authorization header (with or without "Bearer " prefix).</summary>
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>Relative path for Create RFID tag (default matches CardWritter OData route).</summary>
    public string CreateRfidTagPath { get; set; } = "/odata/rfid/RfidTags";
}
