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

    /// <summary>Initializes a new <see cref="CardRegistrationService"/>.</summary>
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
}
