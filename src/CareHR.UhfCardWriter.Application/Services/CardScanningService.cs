using System.Diagnostics;
using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.Devices;
using CareHR.UhfCardWriter.Application.Diagnostics;
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
    /// <param name="timeoutMs">Maximum wait for a card to appear (and short multi-card observation).</param>
    /// <param name="cancellationToken">Cancel mid-scan (UC-010).</param>
    /// <remarks>
    /// Returns as soon as a single EPC is seen
    /// <see cref="DeviceConstants.DefaultScanStableSightings"/> times with no second EPC,
    /// or when the timeout elapses. Does not keep the operator waiting the full timeout
    /// after a stable single tag is already identified.
    /// </remarks>
    public ScanResult ScanForSingleCard(
        ushort timeoutMs = DeviceConstants.DefaultScanTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        CardValidation.EnsureConnected(_connection.IsOpen);

        if (timeoutMs == 0)
            timeoutMs = DeviceConstants.DefaultScanTimeoutMs;

        var scanSw = Stopwatch.StartNew();
        DateTime? firstDetectUtc = null;
        var earlyExit = false;

        try
        {
            PerfDiag.Log($"Scan.Start TimeoutMs={timeoutMs}");
            var start = _scanner.StartScan();
            if (!start.Success)
            {
                PerfDiag.Log($"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=StartFail {start.ErrorCode}");
                return ScanResult.Fail(start.ErrorCode, start.Message);
            }

            var unique = new Dictionary<string, CardInformation>(StringComparer.OrdinalIgnoreCase);
            var sightings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

                    sightings.TryGetValue(key, out var count);
                    sightings[key] = count + 1;

                    firstDetectUtc ??= DateTime.UtcNow;
                    PerfDiag.Log(
                        $"Scan.TagSighting Epc={key} UniqueCount={unique.Count} " +
                        $"Sightings={sightings[key]} SinceFirstDetectMs=" +
                        $"{(DateTime.UtcNow - firstDetectUtc.Value).TotalMilliseconds:F0}");

                    if (unique.Count > 1)
                    {
                        var stopMs = StopBestEffortTimed();
                        PerfDiag.Log(
                            $"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=MultipleCards InventoryStopMs={stopMs}");
                        return ScanResult.MultipleCards();
                    }

                    // Stable single card: do not burn the remaining ScanTimeoutMs waiting.
                    if (sightings[key] >= DeviceConstants.DefaultScanStableSightings)
                    {
                        earlyExit = true;
                        break;
                    }
                }

                if ((deadline - DateTime.UtcNow).TotalMilliseconds > DeviceConstants.DefaultScanPollIntervalMs)
                    Thread.Sleep(DeviceConstants.DefaultScanPollIntervalMs);
            }

            var stopElapsed = StopBestEffortTimed();

            if (unique.Count == 0)
            {
                PerfDiag.Log(
                    $"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=NoCard InventoryStopMs={stopElapsed}");
                return ScanResult.NoCard();
            }

            if (unique.Count > 1)
            {
                PerfDiag.Log(
                    $"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=MultipleCards InventoryStopMs={stopElapsed}");
                return ScanResult.MultipleCards();
            }

            var postDetectMs = firstDetectUtc is null
                ? 0
                : (DateTime.UtcNow - firstDetectUtc.Value).TotalMilliseconds;
            PerfDiag.Log(
                $"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=SingleCard " +
                $"EarlyExit={earlyExit} PostDetectMs={postDetectMs:F0} InventoryStopMs={stopElapsed}");
            return ScanResult.SingleCard(unique.Values.First());
        }
        catch (OperationCanceledException)
        {
            var stopMs = StopBestEffortTimed();
            PerfDiag.Log(
                $"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=Cancelled InventoryStopMs={stopMs}");
            return ScanResult.Cancelled();
        }
        catch (DeviceException ex)
        {
            var stopMs = StopBestEffortTimed();
            var mapped = CardValidation.MapDeviceException(ex);
            PerfDiag.Log(
                $"Scan.End ElapsedMs={scanSw.ElapsedMilliseconds} Status=DeviceFail InventoryStopMs={stopMs}");
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
            return PerfDiag.Time(
                "Select",
                () => _scanner.SelectByIdentity(identity),
                r => r.Success ? "OK" : r.ErrorCode.ToString());
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
            return PerfDiag.Time(
                "InventoryStop",
                () => _scanner.StopScan(timeoutMs),
                r => r.Success ? "OK" : r.ErrorCode.ToString());
        }
        catch (DeviceException ex)
        {
            return CardValidation.MapDeviceException(ex);
        }
    }

    private long StopBestEffortTimed()
    {
        var sw = Stopwatch.StartNew();
        PerfDiag.Log("InventoryStop.Start");
        try
        {
            _ = _scanner.StopScan();
            sw.Stop();
            PerfDiag.Log($"InventoryStop.End ElapsedMs={sw.ElapsedMilliseconds} Status=OK");
            return sw.ElapsedMilliseconds;
        }
        catch (DeviceException ex)
        {
            sw.Stop();
            PerfDiag.Log(
                $"InventoryStop.End ElapsedMs={sw.ElapsedMilliseconds} Status=EXCEPTION {ex.GetType().Name}");
            return sw.ElapsedMilliseconds;
        }
    }
}
