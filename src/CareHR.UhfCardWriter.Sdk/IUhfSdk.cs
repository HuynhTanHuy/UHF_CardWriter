namespace CareHR.UhfCardWriter.Sdk;

/// <summary>
/// Root UHF SDK façade for Infrastructure and upper layers.
/// </summary>
/// <remarks>
/// Hides Driver / Native types. Not thread-safe. No business workflows.
/// </remarks>
public interface IUhfSdk : IDisposable
{
    /// <summary>Gets the connection facet.</summary>
    IUhfConnection Connection { get; }

    /// <summary>Gets the inventory facet.</summary>
    IUhfInventory Inventory { get; }

    /// <summary>Gets the writer facet.</summary>
    IUhfWriter Writer { get; }

    /// <summary>Gets the reader facet.</summary>
    IUhfReader Reader { get; }

    /// <summary>Gets the tag-control facet (select/lock/kill).</summary>
    IUhfTagControl TagControl { get; }
}
