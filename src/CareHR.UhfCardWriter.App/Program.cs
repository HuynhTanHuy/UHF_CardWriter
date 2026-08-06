using CareHR.UhfCardWriter.App.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace CareHR.UhfCardWriter.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var services = CompositionRoot.CreateServiceProvider();
        var mainForm = services.GetRequiredService<MainForm>();
        System.Windows.Forms.Application.Run(mainForm);
    }
}
