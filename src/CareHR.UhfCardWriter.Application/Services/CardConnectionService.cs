using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Exceptions;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for reader connection and USB discovery (UC-001, UC-002, UC-003).
/// </summary>
public sealed class CardConnectionService
{
    private readonly ICardConnection _connection;

    public CardConnectionService(ICardConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public bool IsConnected => _connection.IsOpen;

    public ReaderStatusInfo GetStatus() =>
        _connection.IsOpen
            ? new ReaderStatusInfo(ReaderStatus.Connected, "Connected")
            : new ReaderStatusInfo(ReaderStatus.Disconnected, "Disconnected");

    /// <summary>Connects using the given endpoint (UC-001).</summary>
    public DeviceResult Connect(ReaderEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        try
        {
            return endpoint.Kind switch
            {
                ReaderConnectionKind.Serial => ConnectSerial(endpoint.ComPort!, endpoint.BaudRate),
                ReaderConnectionKind.UsbHid => _connection.OpenHid(endpoint.UsbIndex),
                ReaderConnectionKind.Network => ConnectNetwork(endpoint),
                _ => throw new ValidationException($"Unsupported connection kind: {endpoint.Kind}."),
            };
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException(ex);
        }
    }

    /// <summary>Disconnects the reader session (UC-002). Idempotent when already closed.</summary>
    public DeviceResult Disconnect()
    {
        try
        {
            if (!_connection.IsOpen)
                return DeviceResult.Ok("Already disconnected");

            return _connection.Close();
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException(ex);
        }
    }

    /// <summary>Lists available USB HID readers (UC-003).</summary>
    public DeviceResult<IReadOnlyList<ReaderInformation>> ListUsbReaders()
    {
        try
        {
            var countResult = _connection.GetUsbDeviceCount();
            if (!countResult.Success)
                return DeviceResult<IReadOnlyList<ReaderInformation>>.Fail(countResult.ErrorCode, countResult.Message);

            var count = countResult.Value;
            if (count < 0)
                count = 0;

            var list = new List<ReaderInformation>(count);
            for (ushort i = 0; i < count; i++)
            {
                var infoResult = _connection.GetUsbDeviceInfo(i);
                var name = infoResult.Success ? infoResult.Value ?? $"USB Reader {i}" : $"USB Reader {i}";
                list.Add(new ReaderInformation(i, name));
            }

            return DeviceResult<IReadOnlyList<ReaderInformation>>.Ok(list);
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException<IReadOnlyList<ReaderInformation>>(ex);
        }
    }

    private DeviceResult ConnectSerial(string comPort, int baudRate)
    {
        if (string.IsNullOrWhiteSpace(comPort))
            throw new ValidationException("COM port is required.");

        if (baudRate <= 0)
            throw new ValidationException("Baud rate must be greater than zero.");

        return _connection.OpenSerial(comPort.Trim(), baudRate);
    }

    private DeviceResult ConnectNetwork(ReaderEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.IpAddress))
            throw new ValidationException("IP address is required.");

        if (endpoint.NetworkPort == 0)
            throw new ValidationException("Network port must be greater than zero.");

        if (endpoint.NetworkTimeoutMs <= 0)
            throw new ValidationException("Network timeout must be greater than zero.");

        return _connection.OpenNet(endpoint.IpAddress.Trim(), endpoint.NetworkPort, endpoint.NetworkTimeoutMs);
    }
}
