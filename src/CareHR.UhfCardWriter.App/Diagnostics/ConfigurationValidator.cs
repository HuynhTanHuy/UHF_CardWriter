using CareHR.UhfCardWriter.App.Configuration;

namespace CareHR.UhfCardWriter.App.Diagnostics;

/// <summary>Startup configuration checks (no business rules).</summary>
internal static class ConfigurationValidator
{
    public sealed record Finding(string Code, string Severity, string Message);

    public static IReadOnlyList<Finding> Validate(AppSettings settings)
    {
        var list = new List<Finding>();

        if (string.IsNullOrWhiteSpace(settings.Api.BaseUrl))
            list.Add(new("CFG-API-URL", "Error", "Api.BaseUrl is empty. Register will fail."));
        else if (!Uri.TryCreate(settings.Api.BaseUrl.Trim(), UriKind.Absolute, out var uri)
                 || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            list.Add(new("CFG-API-URL", "Error", "Api.BaseUrl must be an absolute http/https URL."));

        if (string.IsNullOrWhiteSpace(settings.Api.BearerToken))
            list.Add(new("CFG-API-TOKEN", "Warning", "Api.BearerToken is empty. Register will fail until configured."));

        if (string.IsNullOrWhiteSpace(settings.Api.CreateRfidCardPath))
            list.Add(new("CFG-API-PATH", "Error", "Api.CreateRfidCardPath is empty."));

        if (settings.Hospitals.Count == 0)
            list.Add(new("CFG-HOSPITAL", "Error", "No hospitals configured in appsettings.json."));
        else
        {
            foreach (var h in settings.Hospitals)
            {
                if (string.IsNullOrWhiteSpace(h.Id) || !Guid.TryParse(h.Id, out _))
                    list.Add(new("CFG-HOSPITAL", "Error", $"Hospital '{h.Name}' has invalid Id (GUID required)."));
            }
        }

        if (settings.CardTypes.Count == 0)
            list.Add(new("CFG-CARDTYPE", "Error", "No card types configured in appsettings.json."));
        else
        {
            foreach (var t in settings.CardTypes)
            {
                if (string.IsNullOrWhiteSpace(t.Id) || !Guid.TryParse(t.Id, out _))
                    list.Add(new("CFG-CARDTYPE", "Error", $"Card type '{t.Name}' has invalid Id (GUID required)."));
            }
        }

        if (settings.Reader.ScanTimeoutMs is < 500 or > 60000)
            list.Add(new("CFG-TIMEOUT", "Warning", "Reader.ScanTimeoutMs should be between 500 and 60000."));

        if (settings.Reader.BaudRate <= 0)
            list.Add(new("CFG-READER", "Warning", "Reader.BaudRate is invalid."));

        if (!string.Equals(settings.Card.EpcEncoding, "Ascii", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(settings.Card.EpcEncoding, "Hex", StringComparison.OrdinalIgnoreCase))
            list.Add(new("CFG-EPC", "Warning", "Card.EpcEncoding should be Ascii or Hex."));

        if (!UiPasswordOk(settings.Card.AccessPasswordHex))
            list.Add(new("CFG-PASSWORD", "Warning",
                "Card.AccessPasswordHex is missing or invalid (expect 8 hex chars). App will use 00000000."));

        return list;
    }

    public static bool HasBlockingErrors(IReadOnlyList<Finding> findings) =>
        findings.Any(f => string.Equals(f.Severity, "Error", StringComparison.OrdinalIgnoreCase));

    private static bool UiPasswordOk(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return false;
        var cleaned = hex.Trim().Replace(" ", "", StringComparison.Ordinal);
        return cleaned.Length == 8 && cleaned.All(Uri.IsHexDigit);
    }
}
