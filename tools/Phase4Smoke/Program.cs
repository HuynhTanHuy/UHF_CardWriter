using System.Runtime.InteropServices;
using CareHR.UhfCardWriter.Sdk;
using CareHR.UhfCardWriter.Sdk.Models;

[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
static extern bool SetDllDirectory(string lpPathName);

SetDllDirectory(AppContext.BaseDirectory);

using IUhfSdk sdk = new UhfPrimeSdk();

var count = sdk.Connection.GetUsbDeviceCount();
Console.WriteLine($"GetUsbDeviceCount Success={count.Success} Status=0x{count.StatusCode:X8} Value={count.Value}");

if (sdk.Connection.IsOpen)
    throw new InvalidOperationException("IsOpen should be false.");

try
{
    sdk.Inventory.Start();
    throw new InvalidOperationException("Expected SdkException without open handle.");
}
catch (SdkException)
{
    Console.WriteLine("Inventory.Start without open → SdkException OK.");
}

var open = sdk.Connection.OpenSerial("COM_INVALID_PHASE4", 115200);
Console.WriteLine($"OpenSerial(invalid) Success={open.Success} Status=0x{open.StatusCode:X8} Msg={open.Message}");
if (open.GetType().FullName?.Contains("NativeResult", StringComparison.Ordinal) == true)
    throw new InvalidOperationException("NativeResult must not appear on public surface.");

Console.WriteLine("Dispose via using OK.");
Console.WriteLine("PHASE4_RUNTIME_OK");
