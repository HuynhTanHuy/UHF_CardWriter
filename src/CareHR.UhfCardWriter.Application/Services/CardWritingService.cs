using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for writing intended CareHR identity to a card (UC-007).
/// </summary>
public sealed class CardWritingService
{
    private readonly ICardConnection _connection;
    private readonly ICardWriter _writer;

    /// <summary>Initializes a new <see cref="CardWritingService"/>.</summary>
    public CardWritingService(ICardConnection connection, ICardWriter writer)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <summary>Writes the intended identity to the physical card.</summary>
    public DeviceResult<CardWriteResult> WriteIdentity(CardWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CardValidation.EnsureConnected(_connection.IsOpen);
        CardValidation.EnsureIdentity(request.IntendedIdentity);
        CardValidation.EnsureAccessPassword(request.AccessPassword);

        try
        {
            return _writer.WriteEpc(request.AccessPassword, request.IntendedIdentity.Epc);
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException<CardWriteResult>(ex);
        }
    }
}
