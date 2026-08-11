namespace CareHR.UhfCardWriter.Application.Devices;

/// <summary>
/// Application-facing device error codes. Independent of vendor <c>STAT_*</c> values.
/// </summary>
/// <remarks>
/// Infrastructure maps vendor status codes to this enum. Application and UI must use
/// <see cref="DeviceErrorCode"/> / <see cref="DeviceResult"/> — never raw SDK status integers.
/// </remarks>
public enum DeviceErrorCode
{
    /// <summary>Operation succeeded (or no error).</summary>
    None = 0,

    /// <summary>Reader is not connected / handle invalid / not open.</summary>
    ReaderNotConnected,

    /// <summary>Open or connect to the reader failed.</summary>
    ReaderOpenFailed,

    /// <summary>Reader or module reported busy / internal command error.</summary>
    ReaderBusy,

    /// <summary>Communication or command timeout.</summary>
    ReaderTimeout,

    /// <summary>No tag response / inventory stopped without a usable tag.</summary>
    TagNotFound,

    /// <summary>Write access failed (generic).</summary>
    WriteFailed,

    /// <summary>Read access failed (generic).</summary>
    ReadFailed,

    /// <summary>Access password rejected or authentication failed.</summary>
    InvalidPassword,

    /// <summary>Tag memory locked or operation not permitted.</summary>
    TagAccessDenied,

    /// <summary>Invalid parameter for the device operation.</summary>
    InvalidParameter,

    /// <summary>SDK / DLL unavailable or internal DLL failure.</summary>
    SdkUnavailable,

    /// <summary>Disconnected during operation.</summary>
    ReaderDisconnected,

    /// <summary>Unrecognized vendor status (see <see cref="DeviceResult.Message"/>).</summary>
    Unknown,

    /// <summary>More than one distinct card identity observed during scan.</summary>
    MultipleCardsDetected,

    /// <summary>Verify read-back did not match the intended identity.</summary>
    VerificationFailed,

    /// <summary>CareHR registry persistence failed after a successful verify.</summary>
    RegistrationFailed,

    /// <summary>Scanned card number is already registered in CareHR (business guard skip).</summary>
    CardAlreadyRegistered,

    /// <summary>Pre-write existence check could not be completed (fail-closed skip).</summary>
    ExistsCheckFailed,
}
