#Requires -RunAsAdministrator
param(
    [string]$Root = (Join-Path $env:ProgramData "MayaJaal")
)

$ErrorActionPreference = "Stop"

Write-Host "Hardening ACLs for $Root"

$dirs = @(
    $Root,
    (Join-Path $Root "Logs"),
    (Join-Path $Root "Data"),
    (Join-Path $Root "Vaults"),
    (Join-Path $Root "Incidents"),
    (Join-Path $Root "Evidence"),
    (Join-Path $Root "Config")
)

foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}

# Remove inheritance, grant SYSTEM + Administrators, limited Users on Evidence.
icacls $Root /inheritance:r | Out-Null
icacls $Root /grant:r "SYSTEM:(OI)(CI)F" | Out-Null
icacls $Root /grant:r "BUILTIN\Administrators:(OI)(CI)F" | Out-Null
icacls $Root /grant:r "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" | Out-Null

$evidence = Join-Path $Root "Evidence"
icacls $evidence /grant:r "BUILTIN\Users:(OI)(CI)R" | Out-Null

# Config secrets: SYSTEM + Admins only
$config = Join-Path $Root "Config"
icacls $config /inheritance:r | Out-Null
icacls $config /grant:r "SYSTEM:(OI)(CI)F" | Out-Null
icacls $config /grant:r "BUILTIN\Administrators:(OI)(CI)F" | Out-Null

Write-Host "ACLs hardened for $Root"
icacls $Root
