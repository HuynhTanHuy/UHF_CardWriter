using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Models;

namespace CareHR.UhfCardWriter.Application.Services;

/// <summary>
/// Application service for scanning and selecting a single CareHR card (UC-004, UC-005, UC-010).
/// </summary>
public sealed class CardScanningService
{
    private readonly ICardConnection _connection;
    private readonly ICardScanner _scanner;

    public CardScanningService(ICardConnection connection, ICardScanner scanner)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    /// <summary>
    /// Scans for exactly one card in the RF field (UC-004).
    /// </summary>
    /// <param name="timeoutMs">Scan window duration.</param>
    /// <param name="cancellationToken">Cancel mid-scan (UC-010).</param>
    public ScanResult ScanForSingleCard(
        ushort timeoutMs = DeviceConstants.DefaultScanTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        CardValidation.EnsureConnected(_connection.IsOpen);

        if (timeoutMs == 0)
            timeoutMs = DeviceConstants.DefaultScanTimeoutMs;

        try
        {
            var start = _scanner.StartScan();
            if (!start.Success)
                return ScanResult.Fail(start.ErrorCode, start.Message);

            var unique = new Dictionary<string, CardInformation>(StringComparer.OrdinalIgnoreCase);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pollTimeout = (ushort)Math.Min(
                    DeviceConstants.DefaultScanPollIntervalMs,
                    Math.Max(1, (int)(deadline - DateTime.UtcNow).TotalMilliseconds));

                var poll = _scanner.TryGetCard(pollTimeout);
                if (poll.Success && poll.Value is not null)
                {
                    var key = poll.Value.Identity.EpcHex;
                    if (!unique.ContainsKey(key))
                        unique[key] = poll.Value;

                    if (unique.Count > 1)
                    {
                        StopBestEffort();
                        return ScanResult.MultipleCards();
                    }
                }

                if ((deadline - DateTime.UtcNow).TotalMilliseconds > DeviceConstants.DefaultScanPollIntervalMs)
                    Thread.Sleep(DeviceConstants.DefaultScanPollIntervalMs);
            }

            StopBestEffort();

            if (unique.Count == 0)
                return ScanResult.NoCard();

            if (unique.Count > 1)
                return ScanResult.MultipleCards();

            return ScanResult.SingleCard(unique.Values.First());
        }
        catch (OperationCanceledException)
        {
            StopBestEffort();
            return ScanResult.Cancelled();
        }
        catch (DeviceException ex)
        {
            StopBestEffort();
            var mapped = CardValidation.MapDeviceException(ex);
            return ScanResult.Fail(mapped.ErrorCode, mapped.Message);
        }
    }

    /// <summary>Selects a card by identity for subsequent access (UC-005).</summary>
    public DeviceResult SelectCard(CardIdentity identity)
    {
        CardValidation.EnsureConnected(_connection.IsOpen);
        CardValidation.EnsureIdentity(identity);

        try
        {
            return _scanner.SelectByIdentity(identity);
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException(ex);
        }
    }

    /// <summary>Stops scanning (UC-010 cancel / cleanup).</summary>
    public DeviceResult StopScan(ushort timeoutMs = DeviceConstants.DefaultInventoryStopTimeoutMs)
    {
        try
        {
            return _scanner.StopScan(timeoutMs);
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException(ex);
        }
    }

    private void StopBestEffort()
    {
        try
        {
            _ = _scanner.StopScan();
        }
        catch (DeviceException)
        {
            // Best-effort stop on cancel/error paths (UC-010).
        }
    }
}
