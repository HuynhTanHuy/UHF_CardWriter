# Phase 3 driver smoke check — no reader hardware required.
param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$smokeProj = Join-Path $PSScriptRoot "Phase3Smoke\Phase3Smoke.csproj"

Write-Host "Building/running Phase3Smoke ($Configuration)..."
dotnet run --project $smokeProj -c $Configuration -p:Platform=x64 --no-launch-profile
if ($LASTEXITCODE -ne 0) { throw "Phase3Smoke failed with exit $LASTEXITCODE" }
Write-Host "PHASE3_RUNTIME_CHECK_OK"
