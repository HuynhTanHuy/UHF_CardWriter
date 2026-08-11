using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Exceptions;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for registering a verified CareHR card (UC-009).
/// </summary>
public sealed class CardRegistrationService
{
    private readonly ICardRegistrar _registrar;

    public CardRegistrationService(ICardRegistrar registrar)
    {
        _registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
    }

    /// <summary>
    /// Registers a card only after successful verify (BR-004).
    /// </summary>
    public RegistrationResult Register(RegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CardValidation.EnsureIdentity(request.Identity);

        if (!request.IsVerified)
            throw new BusinessException("Register is allowed only after successful verify (BR-004).");

        if (string.IsNullOrWhiteSpace(request.HospitalId))
            throw new ValidationException("Hospital id is required for registration.");

        if (string.IsNullOrWhiteSpace(request.CardTypeId))
            throw new ValidationException("Card type id is required for registration.");

        if (string.IsNullOrWhiteSpace(request.BatchCode))
            throw new ValidationException("Batch code is required for registration.");

        try
        {
            var result = _registrar.Register(request);
            if (result is null)
                return RegistrationResult.Fail(DeviceErrorCode.RegistrationFailed, "Registrar returned no result.");

            return result;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OperationException("Card registration failed.", ex);
        }
    }

    /// <summary>
    /// Checks whether a scanned logical card number already exists in CareHR for the hospital.
    /// </summary>
    public CardExistenceResult Exists(string hospitalId, string rfidCardNumber)
    {
        if (string.IsNullOrWhiteSpace(hospitalId))
            return CardExistenceResult.Failed("Hospital id is required for existence check.");

        if (string.IsNullOrWhiteSpace(rfidCardNumber))
            return CardExistenceResult.NotFound("Empty card number.");

        try
        {
            return _registrar.Exists(hospitalId.Trim(), rfidCardNumber.Trim());
        }
        catch (Exception ex)
        {
            return CardExistenceResult.Failed("Card existence check failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Next serial for HospitalNumber + Batch prefix (MAX matching serial + 1, or 1 if none).
    /// </summary>
    public NextSerialResult GetNextSerial(string hospitalId, string numberPrefix, int serialWidth)
    {
        if (string.IsNullOrWhiteSpace(hospitalId))
            return NextSerialResult.Fail("Hospital id is required for next serial.");

        if (string.IsNullOrWhiteSpace(numberPrefix))
            return NextSerialResult.Fail("Number prefix is required for next serial.");

        try
        {
            return _registrar.GetNextSerial(hospitalId.Trim(), numberPrefix.Trim(), serialWidth);
        }
        catch (Exception ex)
        {
            return NextSerialResult.Fail("Next serial resolve failed: " + ex.Message);
        }
    }
}
