using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
class Shot {
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  struct RECT { public int Left, Top, Right, Bottom; }
  const uint WM_CLOSE = 0x0010;
  static int Main(string[] args) {
    var exe = args[0]; var outPath = args[1];
    foreach (var existing in Process.GetProcessesByName("CareHR.UhfCardWriter.App")) { try { existing.Kill(); } catch {} }
    Thread.Sleep(500);
    var p = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe)! });
    if (p is null) return 1;
    IntPtr hwnd = IntPtr.Zero;
    for (var i = 0; i < 80 && hwnd == IntPtr.Zero; i++) {
      Thread.Sleep(200); p.Refresh();
      // Prefer the main form by title (avoid splash / tiny dialogs).
      EnumWindows((h, _) => {
        GetWindowThreadProcessId(h, out var pid);
        if (pid != (uint)p.Id || !IsWindowVisible(h)) return true;
        var cls = new StringBuilder(64); GetClassName(h, cls, 64);
        if (cls.ToString() == "#32770") { PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); return true; }
        var t = new StringBuilder(256); GetWindowText(h, t, 256);
        if (t.ToString().IndexOf("UHF Card Writer", StringComparison.OrdinalIgnoreCase) >= 0) {
          GetWindowRect(h, out var rr);
          if (rr.Right - rr.Left >= 800 && rr.Bottom - rr.Top >= 500) { hwnd = h; return false; }
        }
        return true;
      }, IntPtr.Zero);
    }
    Thread.Sleep(800); p.Refresh();
    if (hwnd == IntPtr.Zero) hwnd = p.MainWindowHandle;
    if (hwnd == IntPtr.Zero) { Console.WriteLine("no hwnd"); return 2; }
    var title = new StringBuilder(256); GetWindowText(hwnd, title, 256);
    Console.WriteLine("title=" + title);
    SetForegroundWindow(hwnd); Thread.Sleep(400);
    GetWindowRect(hwnd, out var r);
    var w = Math.Max(1, r.Right - r.Left); var h = Math.Max(1, r.Bottom - r.Top);
    if (w < 400 || h < 300) { Console.WriteLine($"window too small {w}x{h}"); return 3; }
    using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp)) {
      var hdc = g.GetHdc();
      var ok = PrintWindow(hwnd, hdc, 2 /* PW_RENDERFULLCONTENT */);
      g.ReleaseHdc(hdc);
      Console.WriteLine("PrintWindow=" + ok);
    }
    bmp.Save(outPath, ImageFormat.Png);
    var btn = bmp.GetPixel(Math.Min(140, w-1), Math.Max(0, h-70));
    var gear = bmp.GetPixel(Math.Max(0, w-45), Math.Min(55, h-1));
    Console.WriteLine($"Saved {outPath} ({w}x{h}) btn=#{btn.R:X2}{btn.G:X2}{btn.B:X2} gear=#{gear.R:X2}{gear.G:X2}{gear.B:X2}");
    try { p.Kill(); } catch {}
    return 0;
  }
}
