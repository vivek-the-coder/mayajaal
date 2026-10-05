#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$localDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"
$dotnetExe = "dotnet"
if (Test-Path (Join-Path $localDotnet "dotnet.exe")) {
    $env:DOTNET_ROOT = $localDotnet
    $env:PATH = "$localDotnet;$env:PATH"
    $dotnetExe = Join-Path $localDotnet "dotnet.exe"
}

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$CenterProj = Join-Path $Root "src\MayaJaal.SecurityCenter\MayaJaal.SecurityCenter.csproj"

Write-Host "Building MayaJaal (Release)..." -ForegroundColor Cyan
& $dotnetExe build "$Root\MayaJaal.sln" -c Release | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# Single window: Security Center auto-starts Guardian hidden in the background.
Write-Host ""
Write-Host "Starting Security Center (Guardian starts hidden)..." -ForegroundColor Cyan
$centerArgs = @(
    "run"
    "--project"
    "`"$CenterProj`""
    "-c"
    "Release"
    "--no-build"
) -join " "
Start-Process `
    -FilePath $dotnetExe `
    -ArgumentList $centerArgs `
    -WorkingDirectory $Root `
    -WindowStyle Normal

Write-Host ""
Write-Host "Demo launched - only Security Center is visible." -ForegroundColor Green
Write-Host "Open Attack Lab, then click Run attack simulation for the faculty demo."
Write-Host "Closing Security Center also stops the background Guardian process it started."
