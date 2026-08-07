namespace CareHR.UhfCardWriter.App.Diagnostics;

internal static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CareHR",
        "UhfCardWriter");

    public static string Logs => Path.Combine(Root, "logs");
    public static string Crashes => Path.Combine(Root, "crashes");
    public static string Exports => Path.Combine(Root, "exports");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Crashes);
        Directory.CreateDirectory(Exports);
    }
}
