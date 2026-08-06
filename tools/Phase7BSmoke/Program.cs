using CareHR.UhfCardWriter.App;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Forms;
using CareHR.UhfCardWriter.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Phase7BSmoke;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();

        var failures = 0;
        Console.WriteLine("=== Phase 7B Smoke: Composition + MainForm ===");

        try
        {
            var basePath = AppContext.BaseDirectory;
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                Console.WriteLine("FAIL appsettings.json missing next to smoke exe");
                return 1;
            }

            using var sp = CompositionRoot.CreateServiceProvider(basePath);

            failures += AssertResolve<CardConnectionService>(sp);
            failures += AssertResolve<CardScanningService>(sp);
            failures += AssertResolve<CardWriteOrchestrator>(sp);
            failures += AssertResolve<IOptions<AppSettings>>(sp);
            failures += AssertResolve<MainForm>(sp);

            using var form = sp.GetRequiredService<MainForm>();
            _ = form.Handle;
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            if (!form.IsHandleCreated)
            {
                Console.WriteLine("FAIL MainForm handle not created");
                failures++;
            }
            else
            {
                Console.WriteLine($"OK  MainForm shown ({form.Text}) Size={form.ClientSize.Width}x{form.ClientSize.Height}");
            }

            form.Close();
            System.Windows.Forms.Application.DoEvents();
            Console.WriteLine("OK  MainForm closed cleanly");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL exception: {ex}");
            failures++;
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "PHASE 7B SMOKE: PASS" : $"PHASE 7B SMOKE: FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static int AssertResolve<T>(ServiceProvider sp) where T : notnull
    {
        try
        {
            var service = sp.GetRequiredService<T>();
            Console.WriteLine($"OK  {typeof(T).Name} -> {service.GetType().Name}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL {typeof(T).Name}: {ex.Message}");
            return 1;
        }
    }
}
