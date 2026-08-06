using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

/// <summary>
/// Translates SDK exceptions to Application <see cref="DeviceException"/>.
/// </summary>
internal static class DeviceExceptionTranslator
{
    /// <summary>
    /// Executes an SDK call and maps <see cref="SdkException"/> to <see cref="DeviceException"/>.
    /// Argument and disposed exceptions pass through.
    /// </summary>
    public static T Execute<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (SdkException ex)
        {
            throw new DeviceException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Executes an SDK call that returns void-like results via getter.
    /// </summary>
    public static void Execute(Action action)
    {
        try
        {
            action();
        }
        catch (SdkException ex)
        {
            throw new DeviceException(ex.Message, ex);
        }
    }
}
