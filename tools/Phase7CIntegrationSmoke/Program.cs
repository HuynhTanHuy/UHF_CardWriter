using CareHR.UhfCardWriter.App;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.Application.Models;
using CareHR.UhfCardWriter.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Phase7CIntegrationSmoke;

/// <summary>
/// Phase 7C — validates Presentation composition → Application → Infrastructure → SDK path.
/// Does not fake hardware success. Reports reader/API readiness for UAT.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var failures = 0;
        Console.WriteLine("=== Phase 7C Integration Smoke ===");

        try
        {
            var basePath = AppContext.BaseDirectory;
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                Console.WriteLine("FAIL appsettings.json missing");
                return 1;
            }

            using var sp = CompositionRoot.CreateServiceProvider(basePath);
            var options = sp.GetRequiredService<IOptions<AppSettings>>().Value;
            var connection = sp.GetRequiredService<CardConnectionService>();
            var scanning = sp.GetRequiredService<CardScanningService>();
            var orchestrator = sp.GetRequiredService<CardWriteOrchestrator>();

            Console.WriteLine($"OK  DI resolved Connection/Scanning/Orchestrator");
            Console.WriteLine($"CFG Api.BaseUrl={options.Api.BaseUrl}");
            Console.WriteLine($"CFG Api.BearerToken={(string.IsNullOrWhiteSpace(options.Api.BearerToken) ? "(empty)" : "(set)")}");
            Console.WriteLine($"CFG Reader.DefaultMode={options.Reader.DefaultMode} ScanTimeoutMs={options.Reader.ScanTimeoutMs}");

            // Native DLLs beside host
            failures += AssertFile(basePath, "UHFPrimeReader.dll");
            failures += AssertFile(basePath, "hidapi.dll");

            // Reader discovery (real SDK call — may return 0 devices)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var usb = connection.ListUsbReaders();
            sw.Stop();
            if (!usb.Success)
            {
                Console.WriteLine($"WARN USB list failed: {usb.ErrorCode} — {usb.Message} ({sw.ElapsedMilliseconds} ms)");
            }
            else
            {
                var count = usb.Value?.Count ?? 0;
                Console.WriteLine($"OK  ListUsbReaders count={count} ({sw.ElapsedMilliseconds} ms)");
                if (usb.Value is not null)
                {
                    foreach (var r in usb.Value)
                        Console.WriteLine($"     USB[{r.Index}] {r.DisplayName}");
                }

                if (count == 0)
                    Console.WriteLine("BLOCKER Hardware: no UHF USB HID reader detected — Connect/Scan/Write E2E requires desk reader.");
            }

            Console.WriteLine($"OK  IsConnected={connection.IsConnected} Status={connection.GetStatus().Status}");

            // Registration config readiness (no HTTP call required to classify)
            if (string.IsNullOrWhiteSpace(options.Api.BearerToken))
                Console.WriteLine("BLOCKER Config: Api.BearerToken empty — Register will Fail until token is set in appsettings.json.");
            if (string.IsNullOrWhiteSpace(options.Api.BaseUrl))
                Console.WriteLine("BLOCKER Config: Api.BaseUrl empty.");

            // Ensure services are usable references (no fake write)
            _ = scanning;
            _ = orchestrator;
            Console.WriteLine("OK  Scanning + Orchestrator references resolved (no hardware write attempted).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL exception: {ex.GetType().Name}: {ex.Message}");
            failures++;
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? "PHASE 7C SOFTWARE STACK: PASS (see BLOCKER lines for hardware/config UAT gates)"
            : $"PHASE 7C SOFTWARE STACK: FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static int AssertFile(string basePath, string name)
    {
        var path = Path.Combine(basePath, name);
        if (File.Exists(path))
        {
            Console.WriteLine($"OK  Native present: {name} ({new FileInfo(path).Length} bytes)");
            return 0;
        }

        Console.WriteLine($"FAIL Native missing: {name}");
        return 1;
    }
}
