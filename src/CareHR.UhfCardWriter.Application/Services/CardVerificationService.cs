using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for verifying physical card identity after write (UC-008).
/// </summary>
public sealed class CardVerificationService
{
    private readonly CardReadingService _readingService;

    public CardVerificationService(CardReadingService readingService)
    {
        _readingService = readingService ?? throw new ArgumentNullException(nameof(readingService));
    }

    /// <summary>
    /// Reads back the card EPC and compares it to the intended identity (BR-003).
    /// </summary>
    public CardVerifyResult Verify(CardVerifyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CardValidation.EnsureIdentity(request.IntendedIdentity);
        CardValidation.EnsureAccessPassword(request.AccessPassword);

        var wordCount = (byte)(request.IntendedIdentity.Epc.Length / 2);
        if (wordCount == 0)
            throw new Exceptions.ValidationException("Intended EPC has no words to verify.");

        LogVerifyDiag("[Verify] Start");
        LogVerifyDiag(
            $"[Verify] ExpectedEpcHex={request.IntendedIdentity.EpcHex} " +
            $"ExpectedLength={request.IntendedIdentity.Epc.Length} WordCount={wordCount}");

        var read = _readingService.ReadCardIdentity(request.AccessPassword, wordCount);
        if (!read.Success || read.Value is null)
        {
            LogVerifyDiag($"[Verify] ActualEpcHex=");
            LogVerifyDiag(
                $"[Verify] VerifyResult=FAIL ErrorCode={read.ErrorCode} Message={read.Message}");
            LogVerifyDiag("[Verify] FailAt=ReadPath");
            return CardVerifyResult.Fail(request.IntendedIdentity, read.ErrorCode, read.Message);
        }

        LogVerifyDiag($"[Verify] ActualEpcHex={read.Value.EpcHex} ActualLength={read.Value.Epc.Length}");

        if (CardValidation.EpcEquals(request.IntendedIdentity, read.Value))
        {
            LogVerifyDiag("[Verify] VerifyResult=PASS");
            LogVerifyDiag("[Verify] FailAt=NONE");
            return CardVerifyResult.Match(request.IntendedIdentity, read.Value);
        }

        LogVerifyDiag("[Verify] VerifyResult=FAIL Mismatch");
        LogVerifyDiag("[Verify] FailAt=Compare");
        return CardVerifyResult.Mismatch(request.IntendedIdentity, read.Value);
    }

    private static void LogVerifyDiag(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CareHR",
                "UhfCardWriter",
                "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{message}";
            File.AppendAllText(Path.Combine(dir, "write-diag.log"), line + Environment.NewLine);
        }
        catch
        {
            // Diagnostic only.
        }
    }
}
