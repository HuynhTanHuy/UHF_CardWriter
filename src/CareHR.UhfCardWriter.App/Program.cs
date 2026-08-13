using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.App.Forms;
using CareHR.UhfCardWriter.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CareHR.UhfCardWriter.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        var startupSw = System.Diagnostics.Stopwatch.StartNew();
        CrashHandler.Wire();
        AppLog.Initialize();
        ApplicationConfiguration.Initialize();

        try
        {
            using var services = CompositionRoot.CreateServiceProvider();

            var authSession = services.GetRequiredService<IWriterAuthSession>();
            if (!authSession.HasToken)
            {
                using var loginForm = services.GetRequiredService<LoginForm>();
                var loginResult = loginForm.ShowDialog();
                if (loginResult != DialogResult.OK || !authSession.HasToken)
                {
                    AppLog.Info("Shutdown", "Login cancelled; application exit.");
                    return;
                }
            }

            var mainForm = services.GetRequiredService<MainForm>();
            startupSw.Stop();
            AppLog.Info("Startup", $"Composition ready in {startupSw.ElapsedMilliseconds} ms");
            mainForm.Tag = startupSw.ElapsedMilliseconds;
            System.Windows.Forms.Application.Run(mainForm);
            AppLog.Info("Shutdown", "Application exited normally.");
        }
        catch (Exception ex)
        {
            CrashHandler.WriteCrashReport("Startup", ex, isTerminating: true);
            MessageBox.Show(
                "Application failed to start.\n\n" + UserMessage.ForException(ex)
                + "\n\nCheck appsettings.json next to the executable and the log folder:\n"
                + AppPaths.Logs,
                "CareHR UHF Card Writer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
