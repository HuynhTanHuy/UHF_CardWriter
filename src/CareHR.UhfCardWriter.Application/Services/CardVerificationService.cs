using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for verifying physical card identity after write (UC-008).
/// </summary>
public sealed class CardVerificationService
{
    private readonly CardReadingService _readingService;

    /// <summary>Initializes a new <see cref="CardVerificationService"/>.</summary>
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

        var read = _readingService.ReadCardIdentity(request.AccessPassword, wordCount);
        if (!read.Success || read.Value is null)
            return CardVerifyResult.Fail(request.IntendedIdentity, read.ErrorCode, read.Message);

        if (CardValidation.EpcEquals(request.IntendedIdentity, read.Value))
            return CardVerifyResult.Match(request.IntendedIdentity, read.Value);

        return CardVerifyResult.Mismatch(request.IntendedIdentity, read.Value);
    }
}
