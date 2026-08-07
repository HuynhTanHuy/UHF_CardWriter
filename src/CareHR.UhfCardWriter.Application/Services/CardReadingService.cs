using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for reading CareHR card identity (UC-006).
/// </summary>
public sealed class CardReadingService
{
    private readonly ICardConnection _connection;
    private readonly ICardReader _reader;

    public CardReadingService(ICardConnection connection, ICardReader reader)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>
    /// Reads EPC identity from the present/selected card.
    /// </summary>
    /// <param name="accessPassword">Exactly four bytes.</param>
    /// <param name="wordCount">Number of EPC words to read (&gt; 0).</param>
    public DeviceResult<CardReadResult> ReadIdentity(byte[] accessPassword, byte wordCount)
    {
        CardValidation.EnsureConnected(_connection.IsOpen);
        CardValidation.EnsureAccessPassword(accessPassword);

        if (wordCount == 0)
            throw new Exceptions.ValidationException("Word count must be greater than zero.");

        try
        {
            return _reader.ReadEpc(accessPassword, wordCount);
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException<CardReadResult>(ex);
        }
    }

    public DeviceResult<CardIdentity> ReadCardIdentity(byte[] accessPassword, byte wordCount)
    {
        var read = ReadIdentity(accessPassword, wordCount);
        if (!read.Success || read.Value is null)
            return DeviceResult<CardIdentity>.Fail(read.ErrorCode, read.Message);

        var data = read.Value.Data;
        if (data.Length == 0)
            return DeviceResult<CardIdentity>.Fail(DeviceErrorCode.ReadFailed, "Read returned empty EPC data.");

        return DeviceResult<CardIdentity>.Ok(new CardIdentity(data), read.Message);
    }
}
