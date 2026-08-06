using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Exceptions;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Shared Application validation helpers for CareHR card services.
/// </summary>
internal static class CardValidation
{
    public static void EnsureConnected(bool isOpen)
    {
        if (!isOpen)
            throw new BusinessException("Reader must be connected before this operation (BR-001).");
    }

    public static void EnsureAccessPassword(byte[]? accessPassword)
    {
        if (accessPassword is null || accessPassword.Length != DeviceConstants.AccessPasswordLength)
            throw new ValidationException($"Access password must be exactly {DeviceConstants.AccessPasswordLength} bytes.");
    }

    public static void EnsureEpcPayload(byte[]? epc)
    {
        if (epc is null || epc.Length == 0)
            throw new ValidationException("EPC identity must not be empty.");

        if ((epc.Length & 1) != 0)
            throw new ValidationException("EPC identity length must be an even number of bytes (word-aligned).");
    }

    public static void EnsureIdentity(CardIdentity? identity)
    {
        if (identity is null)
            throw new ValidationException("Card identity is required.");

        EnsureEpcPayload(identity.Epc);
    }

    public static bool EpcEquals(CardIdentity left, CardIdentity right)
    {
        if (ReferenceEquals(left, right))
            return true;

        return left.Epc.AsSpan().SequenceEqual(right.Epc);
    }

    public static DeviceResult MapDeviceException(Exception ex)
    {
        if (ex is Devices.DeviceException)
            return DeviceResult.Fail(DeviceErrorCode.Unknown, ex.Message);

        throw new OperationException(ex.Message, ex);
    }

    public static DeviceResult<T> MapDeviceException<T>(Exception ex)
    {
        if (ex is Devices.DeviceException)
            return DeviceResult<T>.Fail(DeviceErrorCode.Unknown, ex.Message);

        throw new OperationException(ex.Message, ex);
    }
}
