using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for CareHR card reader connection and USB discovery.
/// </summary>
public interface ICardConnection
{
    bool IsOpen { get; }

    DeviceResult OpenSerial(string comPort, int baudRate);

    DeviceResult OpenHid(ushort index);

    DeviceResult OpenNet(string ip, ushort port, int timeoutMs);

    DeviceResult Close();

    DeviceResult<int> GetUsbDeviceCount();

    DeviceResult<string> GetUsbDeviceInfo(ushort index, int capacity = DeviceConstants.DefaultUsbInfoCapacity);
}
