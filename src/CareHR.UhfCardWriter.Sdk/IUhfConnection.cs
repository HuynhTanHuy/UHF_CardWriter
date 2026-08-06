using CareHR.UhfCardWriter.Sdk.Models;

namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Reader connection and USB discovery surface.
/// </summary>
public interface IUhfConnection
{
    /// <summary>Gets a value indicating whether a reader session is open.</summary>
    bool IsOpen { get; }

    /// <summary>Opens a serial (COM) connection.</summary>
    /// <param name="comPort">COM port name.</param>
    /// <param name="baudRate">Baud rate.</param>
    /// <returns>SDK result.</returns>
    SdkResult OpenSerial(string comPort, int baudRate);

    /// <summary>Opens a USB HID connection by device index.</summary>
    /// <param name="index">Zero-based USB index.</param>
    /// <returns>SDK result.</returns>
    SdkResult OpenHid(ushort index);

    /// <summary>Opens a TCP network connection.</summary>
    /// <param name="ip">Device IP address.</param>
    /// <param name="port">TCP port.</param>
    /// <param name="timeoutMs">Connect timeout in milliseconds.</param>
    /// <returns>SDK result.</returns>
    SdkResult OpenNet(string ip, ushort port, int timeoutMs);

    /// <summary>Closes the current session.</summary>
    /// <returns>SDK result.</returns>
    SdkResult Close();

    /// <summary>Gets the USB HID device count (no open session required).</summary>
    /// <returns>SDK result with count.</returns>
    SdkResult<int> GetUsbDeviceCount();

    /// <summary>Gets USB device info text for an index (no open session required).</summary>
    /// <param name="index">Zero-based USB index.</param>
    /// <param name="capacity">String buffer capacity.</param>
    /// <returns>SDK result with info string.</returns>
    SdkResult<string> GetUsbDeviceInfo(ushort index, int capacity = SdkConstants.DefaultUsbInfoCapacity);
}
