#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$localDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"
if (Test-Path (Join-Path $localDotnet "dotnet.exe")) {
    $env:DOTNET_ROOT = $localDotnet
    $env:PATH = "$localDotnet;$env:PATH"
}

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "MayaJaal setup" -ForegroundColor Cyan
Write-Host "Root: $Root"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
}

$sdk = dotnet --list-sdks
Write-Host "Installed SDKs:`n$sdk"

Write-Host "`nRestoring solution..." -ForegroundColor Cyan
dotnet restore "$Root\MayaJaal.sln"

Write-Host "`nBuilding Release..." -ForegroundColor Cyan
dotnet build "$Root\MayaJaal.sln" -c Release --no-restore

Write-Host "`nRunning tests..." -ForegroundColor Cyan
dotnet test "$Root\tests\MayaJaal.Tests\MayaJaal.Tests.csproj" -c Release --no-build

Write-Host "`nSetup complete." -ForegroundColor Green
Write-Host "Next: .\scripts\run-demo.ps1   or   dotnet run --project src\MayaJaal.Guardian -- --console"
