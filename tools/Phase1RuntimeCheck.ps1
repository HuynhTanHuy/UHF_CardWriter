# Phase 1 runtime smoke check — not part of product layers.
param(
    [Parameter(Mandatory = $true)]
    [string]$AppOutputDir
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $AppOutputDir "CareHR.UhfCardWriter.App.exe"
$uhf = Join-Path $AppOutputDir "UHFPrimeReader.dll"
$hid = Join-Path $AppOutputDir "hidapi.dll"

Write-Host "EXE : $exe"
Write-Host "UHF : $uhf"
Write-Host "HID : $hid"

if (-not (Test-Path -LiteralPath $exe)) { throw "App exe missing." }
if (-not (Test-Path -LiteralPath $uhf)) { throw "UHFPrimeReader.dll missing in output." }
if (-not (Test-Path -LiteralPath $hid)) { throw "hidapi.dll missing in output." }

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class NativeLoadCheck {
  [DllImport("kernel32", SetLastError=true, CharSet=CharSet.Unicode)]
  public static extern bool SetDllDirectory(string lpPathName);
  [DllImport("kernel32", SetLastError=true, CharSet=CharSet.Unicode)]
  public static extern IntPtr LoadLibrary(string lpFileName);
  [DllImport("kernel32", SetLastError=true)]
  public static extern bool FreeLibrary(IntPtr hModule);
  [DllImport("kernel32", SetLastError=true, CharSet=CharSet.Ansi)]
  public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}
"@

# Ensure dependent DLLs (e.g. hidapi.dll) resolve from app output, not PowerShell's directory.
[NativeLoadCheck]::SetDllDirectory($AppOutputDir) | Out-Null
Set-Location -LiteralPath $AppOutputDir

$h = [NativeLoadCheck]::LoadLibrary($uhf)
if ($h -eq [IntPtr]::Zero) {
    $err = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
    throw "LoadLibrary(UHFPrimeReader.dll) failed. Win32=$err"
}

$requiredExports = @(
    "OpenDevice",
    "OpenHidConnection",
    "OpenNetConnection",
    "CloseDevice",
    "InventoryContinue",
    "GetTagUii",
    "InventoryStop",
    "SetSelectMask",
    "WriteTag",
    "GetTagResp",
    "ReadTag",
    "GetReadTagResp"
)

foreach ($name in $requiredExports) {
    $p = [NativeLoadCheck]::GetProcAddress($h, $name)
    if ($p -eq [IntPtr]::Zero) {
        [NativeLoadCheck]::FreeLibrary($h) | Out-Null
        throw "Export missing: $name"
    }
}

[NativeLoadCheck]::FreeLibrary($h) | Out-Null
Write-Host "LoadLibrary OK + required exports present."

$proc = Start-Process -FilePath $exe -WorkingDirectory $AppOutputDir -PassThru
Start-Sleep -Seconds 2
if ($proc.HasExited) {
    throw "App exited early. ExitCode=$($proc.ExitCode)"
}

Stop-Process -Id $proc.Id -Force
Write-Host "App started and stayed alive (PID $($proc.Id))."
Write-Host "PHASE1_RUNTIME_OK"
