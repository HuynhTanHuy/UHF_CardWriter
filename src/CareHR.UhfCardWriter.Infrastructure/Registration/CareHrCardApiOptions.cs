namespace CareHR.UhfCardWriter.Infrastructure.Registration;

/// <summary>
/// Options for CareHR RFID card registry HTTP API (Infrastructure only).
/// Wire contract: <c>POST /api/rfid/cards</c> (<see cref="UpsertRFIDCardRequest"/> shape).
/// </summary>
public sealed class CareHrCardApiOptions
{
    /// <summary>Base URL of the CareHR API (no trailing slash required).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relative path for create RFID card.
    /// CareHR production: <c>/api/rfid/cards</c>.
    /// </summary>
    public string CreateRfidCardPath { get; set; } = "/api/rfid/cards";

    /// <summary>
    /// Default hospital id when <c>RegistrationRequest.HospitalId</c> is empty.
    /// </summary>
    public string DefaultHospitalId { get; set; } = string.Empty;

    /// <summary>
    /// <c>status</c> on create (CareHR <c>RFIDCardStatuses.Stock</c> = 4).
    /// </summary>
    public int DefaultStatus { get; set; } = 4;

    /// <summary><c>isActive</c> on create.</summary>
    public bool DefaultIsActive { get; set; } = true;
}
