using System.Globalization;
using System.Text;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Exceptions;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Builds CareHR / CardWritter card numbers:
/// <c>HospitalNumber + Batch(D2) + Serial(D5)</c> → e.g. <c>790480100036</c>.
/// </summary>
/// <remarks>
/// Matches legacy CardWritter <c>BuildPatientPayload</c> (hospitalCode + group D2 + patientId D5).
/// Write buffer is ASCII bytes; only pads <c>0x00</c> for Gen2 word alignment — does not change the logical card number.
/// </remarks>
public static class CardNumberBuilder
{
    public const int DefaultBatchWidth = 2;
    public const int DefaultSerialWidth = 5;

    /// <summary>Builds the official card number string (no padding bytes).</summary>
    public static string Build(
        string hospitalNumber,
        int batchNumber,
        int serialNumber,
        int batchWidth = DefaultBatchWidth,
        int serialWidth = DefaultSerialWidth)
    {
        var hospital = (hospitalNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(hospital))
            throw new ValidationException("Hospital number is required.");

        if (batchWidth <= 0)
            batchWidth = DefaultBatchWidth;
        if (serialWidth <= 0)
            serialWidth = DefaultSerialWidth;

        // Legacy CardWritter: Math.Max(1, Math.Min(99, groupNumber)) for D2.
        var maxBatch = (int)Math.Pow(10, batchWidth) - 1;
        var batch = Math.Max(0, Math.Min(maxBatch, batchNumber));
        if (batchWidth == 2 && batch < 1)
            batch = 1;

        if (serialNumber < 0)
            throw new ValidationException("Serial number must be >= 0.");

        var batchPart = batch.ToString("D" + batchWidth, CultureInfo.InvariantCulture);
        var serialPart = serialNumber.ToString("D" + serialWidth, CultureInfo.InvariantCulture);
        if (serialPart.Length > serialWidth)
            serialPart = serialPart[^serialWidth..];

        return hospital + batchPart + serialPart;
    }

    /// <summary>ASCII write buffer for EPC bank; pads one <c>0x00</c> only when length is odd.</summary>
    public static byte[] ToWriteBuffer(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ValidationException("Card number is required.");

        var raw = Encoding.ASCII.GetBytes(cardNumber.Trim());
        if (raw.Length == 0)
            throw new ValidationException("Card number is empty.");

        if ((raw.Length & 1) == 0)
            return raw;

        var padded = new byte[raw.Length + 1];
        Buffer.BlockCopy(raw, 0, padded, 0, raw.Length);
        return padded;
    }

    /// <summary>Creates <see cref="CardIdentity"/> from a logical card number.</summary>
    public static CardIdentity ToIdentity(string cardNumber) =>
        new(ToWriteBuffer(cardNumber));

    /// <summary>
    /// Recovers the logical card number from an EPC write buffer (strips trailing <c>0x00</c> pads).
    /// Used for API <c>rfidCardNumber</c> — does not alter chip contents.
    /// </summary>
    public static string ToCardNumberFromEpcBytes(byte[]? epc)
    {
        if (epc is null || epc.Length == 0)
            return string.Empty;

        var end = epc.Length;
        while (end > 0 && epc[end - 1] == 0x00)
            end--;

        if (end == 0)
            return string.Empty;

        var allPrintable = true;
        for (var i = 0; i < end; i++)
        {
            var b = epc[i];
            if (b < 0x20 || b > 0x7E)
            {
                allPrintable = false;
                break;
            }
        }

        if (allPrintable)
            return Encoding.ASCII.GetString(epc, 0, end).Trim();

        return Convert.ToHexString(epc.AsSpan(0, end));
    }
}
