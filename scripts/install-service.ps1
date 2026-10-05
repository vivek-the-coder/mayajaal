#Requires -RunAsAdministrator
param(
    [string]$InstallDir = "C:\Program Files\MayaJaal",
    [string]$ServiceName = "MayaJaal Guardian",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

$localDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"
if (Test-Path (Join-Path $localDotnet "dotnet.exe")) {
    $env:DOTNET_ROOT = $localDotnet
    $env:PATH = "$localDotnet;$env:PATH"
}

if (-not $SkipBuild) {
    Write-Host "Publishing Guardian + Security Center (Release)..."
    dotnet publish (Join-Path $repoRoot "src\MayaJaal.Guardian\MayaJaal.Guardian.csproj") -c Release -o (Join-Path $InstallDir "Guardian") --self-contained false
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet publish (Join-Path $repoRoot "src\MayaJaal.SecurityCenter\MayaJaal.SecurityCenter.csproj") -c Release -o (Join-Path $InstallDir "SecurityCenter") --self-contained false
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$guardianExe = Join-Path $InstallDir "Guardian\MayaJaal.Guardian.exe"
if (-not (Test-Path $guardianExe)) {
    throw "Guardian exe not found at $guardianExe — run without -SkipBuild first."
}

# ProgramData + ACLs
& (Join-Path $PSScriptRoot "harden-acls.ps1")

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

$binPath = "`"$guardianExe`""
Write-Host "Creating service $ServiceName..."
sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= "MayaJaal Guardian Service" | Out-Null
sc.exe description $ServiceName "MayaJaal deception and behavioral containment service" | Out-Null
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/30000/restart/60000 | Out-Null
sc.exe failureflag $ServiceName 1 | Out-Null

Start-Service -Name $ServiceName
Write-Host "Service started."

$scExe = Join-Path $InstallDir "SecurityCenter\MayaJaal.SecurityCenter.exe"
if (Test-Path $scExe) {
    $startMenu = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\MayaJaal"
    New-Item -ItemType Directory -Force -Path $startMenu | Out-Null
    $ws = New-Object -ComObject WScript.Shell
    $shortcut = $ws.CreateShortcut((Join-Path $startMenu "MayaJaal Security Center.lnk"))
    $shortcut.TargetPath = $scExe
    $shortcut.WorkingDirectory = Split-Path $scExe
    $shortcut.Save()
    Write-Host "Start Menu shortcut created."
}

Write-Host "Install complete."
Write-Host "  Service: $ServiceName"
Write-Host "  Guardian: $guardianExe"
Write-Host "  UI: $scExe"
