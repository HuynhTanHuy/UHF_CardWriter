using System.Runtime.InteropServices;
using CareHR.UhfCardWriter.Sdk.Driver;

[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
static extern bool SetDllDirectory(string lpPathName);

// Resolve hidapi.dll beside UHFPrimeReader.dll in output directory.
SetDllDirectory(AppContext.BaseDirectory);

using var driver = new UhfPrimeDriver();
if (driver.IsOpen)
    throw new InvalidOperationException("IsOpen should be false.");

// Loads DLL + runs NativeLayout via UhfPrimeNative static ctor on first P/Invoke.
var count = driver.GetHidUsbCount();
Console.WriteLine($"GetHidUsbCount Success={count.Success} Status=0x{count.StatusCode:X8} Msg={count.Message} Value={count.Value}");

try
{
    driver.InventoryContinue();
    throw new InvalidOperationException("Expected NativeException without open handle.");
}
catch (NativeException)
{
    Console.WriteLine("InventoryContinue without handle → NativeException OK.");
}

var open = driver.OpenDevice("COM_INVALID_PHASE3", 115200);
Console.WriteLine($"OpenDevice(invalid) Success={open.Success} Status=0x{open.StatusCode:X8} Msg={open.Message} NativeStatus=0x{open.NativeStatus:X8}");
if (driver.IsOpen)
    throw new InvalidOperationException("IsOpen must remain false after failed Open.");

try
{
    driver.WriteTag(0, new byte[4], 1, 2, new byte[] { 0x30, 0x00 });
    throw new InvalidOperationException("Expected NativeException for WriteTag without handle.");
}
catch (NativeException)
{
    Console.WriteLine("WriteTag without handle → NativeException OK.");
}

try
{
    driver.WriteTag(0, new byte[3], 1, 2, new byte[] { 0x30, 0x00 });
    throw new InvalidOperationException("Expected ArgumentException for bad password length.");
}
catch (ArgumentException)
{
    Console.WriteLine("WriteTag bad password → ArgumentException OK (no SDK call).");
}

try
{
    driver.SetSelectMask(0, 8, null!);
    throw new InvalidOperationException("Expected ArgumentNullException for null mask.");
}
catch (ArgumentNullException)
{
    Console.WriteLine("SetSelectMask null mask → ArgumentNullException OK (no SDK call).");
}

Console.WriteLine("Dispose via using OK.");
Console.WriteLine("PHASE3_RUNTIME_OK");
