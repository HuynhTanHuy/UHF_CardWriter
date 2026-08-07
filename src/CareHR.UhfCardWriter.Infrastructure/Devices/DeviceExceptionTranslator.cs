using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Infrastructure.Devices;

internal static class DeviceExceptionTranslator
{
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
