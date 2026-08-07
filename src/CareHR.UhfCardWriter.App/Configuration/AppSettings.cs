namespace CareHR.UhfCardWriter.App.Configuration;

/// <summary>Root appsettings.json binding for the WinForms host.</summary>
public sealed class AppSettings
{
    public ApiSettings Api { get; set; } = new();
    public ReaderSettings Reader { get; set; } = new();
    public CardSettings Card { get; set; } = new();
    public List<HospitalOption> Hospitals { get; set; } = new();
    public List<CardTypeOption> CardTypes { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
}

public sealed class ApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    /// <summary>CareHR create card path (<c>POST /api/rfid/cards</c>).</summary>
    public string CreateRfidCardPath { get; set; } = "/api/rfid/cards";
    /// <summary>Default <c>status</c> on create (Stock = 4).</summary>
    public int DefaultStatus { get; set; } = 4;
    public bool DefaultIsActive { get; set; } = true;
}

public sealed class ReaderSettings
{
    public string DefaultMode { get; set; } = "UsbHid";
    public string ComPort { get; set; } = "COM3";
    public int BaudRate { get; set; } = 115200;
    public string NetworkIp { get; set; } = "192.168.1.100";
    public ushort NetworkPort { get; set; } = 8080;
    public int NetworkTimeoutMs { get; set; } = 3000;
    public ushort ScanTimeoutMs { get; set; } = 3000;
}

public sealed class CardSettings
{
    public string AccessPasswordHex { get; set; } = "00000000";
    public string DefaultBatchCode { get; set; } = "BATCH-001";
    /// <summary>Ascii = hospital code + padded serial as ASCII bytes; Hex = operator hex in Target EPC.</summary>
    public string EpcEncoding { get; set; } = "Ascii";
    public int SerialPadWidth { get; set; } = 8;
}

public sealed class HospitalOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Name : $"{Name} ({Code})";
}

public sealed class CardTypeOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

public sealed class ThemeSettings
{
    public string AccentHex { get; set; } = "#0D7377";
    public string SuccessHex { get; set; } = "#2E7D32";
    public string ErrorHex { get; set; } = "#C62828";
    public string WarningHex { get; set; } = "#EF6C00";
    public string NeutralHex { get; set; } = "#455A64";
}
