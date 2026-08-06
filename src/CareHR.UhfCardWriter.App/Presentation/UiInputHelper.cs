using System.Globalization;
using CareHR.UhfCardWriter.App.Configuration;

namespace CareHR.UhfCardWriter.App.Presentation;

/// <summary>
/// Presentation helpers for empty/required/format checks and EPC byte packing for the form.
/// Does not enforce CareHR business rules (verify/register/etc.).
/// </summary>
internal static class UiInputHelper
{
    public static bool TryParsePositiveInt(string? text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0;
    }

    public static bool TryParseHexBytes(string? hex, out byte[] bytes, out string error)
    {
        bytes = Array.Empty<byte>();
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(hex))
        {
            error = "Value is required.";
            return false;
        }

        var cleaned = hex.Trim().Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (cleaned.Length == 0 || (cleaned.Length & 1) != 0)
        {
            error = "Hex length must be even and non-empty.";
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(cleaned);
            return true;
        }
        catch (FormatException)
        {
            error = "Invalid hex characters.";
            return false;
        }
    }

    public static string BuildAsciiEpcPreview(string hospitalCode, int serial, int padWidth)
    {
        var code = (hospitalCode ?? string.Empty).Trim();
        var width = padWidth <= 0 ? 8 : padWidth;
        var serialPart = serial.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        if (serialPart.Length > width)
            serialPart = serialPart[^width..];
        return code + serialPart;
    }

    public static byte[] AsciiToEpcBytes(string ascii)
    {
        var raw = System.Text.Encoding.ASCII.GetBytes(ascii ?? string.Empty);
        if (raw.Length == 0)
            return raw;
        if ((raw.Length & 1) != 0)
        {
            var padded = new byte[raw.Length + 1];
            Buffer.BlockCopy(raw, 0, padded, 0, raw.Length);
            return padded;
        }

        return raw;
    }

    public static byte[] ResolveAccessPassword(CardSettings card)
    {
        if (!TryParseHexBytes(card.AccessPasswordHex, out var bytes, out _))
            return new byte[] { 0, 0, 0, 0 };
        if (bytes.Length != 4)
            return new byte[] { 0, 0, 0, 0 };
        return bytes;
    }

    public static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;
        try
        {
            return ColorTranslator.FromHtml(hex.Trim());
        }
        catch
        {
            return fallback;
        }
    }
}
