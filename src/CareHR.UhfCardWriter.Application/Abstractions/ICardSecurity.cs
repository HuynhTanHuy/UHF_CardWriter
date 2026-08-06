using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for optional card security operations (lock / kill).
/// </summary>
public interface ICardSecurity
{
    /// <summary>Locks card memory according to device area/action codes.</summary>
    DeviceResult Lock(byte[] accessPassword, byte area, byte action);

    /// <summary>Permanently kills a card (destructive).</summary>
    DeviceResult Kill(byte[] accessPassword);
}
