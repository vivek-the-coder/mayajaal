#Requires -RunAsAdministrator
param(
    [string]$InstallDir = "C:\Program Files\MayaJaal",
    [string]$ServiceName = "MayaJaal Guardian",
    [switch]$RemoveFiles
)

$ErrorActionPreference = "Stop"

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc) {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Service $ServiceName removed."
}

$startMenu = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\MayaJaal"
if (Test-Path $startMenu) {
    Remove-Item -Recurse -Force $startMenu -ErrorAction SilentlyContinue
}

if ($RemoveFiles -and (Test-Path $InstallDir)) {
    Remove-Item -Recurse -Force $InstallDir
    Write-Host "Removed $InstallDir"
}

Write-Host "Uninstall complete. ProgramData\MayaJaal left intact (evidence/vaults)."
