#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"
$dotnetExe = "dotnet"
if (Test-Path (Join-Path $localDotnet "dotnet.exe")) {
    $env:DOTNET_ROOT = $localDotnet
    $env:PATH = "$localDotnet;$env:PATH"
    $dotnetExe = Join-Path $localDotnet "dotnet.exe"
}

Set-Location $Root

Write-Host "== Build ==" -ForegroundColor Cyan
& $dotnetExe build "$Root\MayaJaal.sln" -c Debug --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$guardianDll = Join-Path $Root "src\MayaJaal.Guardian\bin\Debug\net8.0-windows\MayaJaal.Guardian.dll"
$verifyProj = Join-Path $Root "scripts\verify-live\VerifyLive.csproj"

Write-Host "== Stop leftover Guardians ==" -ForegroundColor Cyan
Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and ($_.CommandLine -like "*MayaJaal.Guardian*") } |
    ForEach-Object {
        Write-Host ("Stopping PID {0}" -f $_.ProcessId) -ForegroundColor Yellow
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
Start-Sleep -Seconds 1

Write-Host "== Start Guardian ==" -ForegroundColor Cyan
$guardianProc = Start-Process `
    -FilePath $dotnetExe `
    -ArgumentList @("exec", "`"$guardianDll`"", "--console") `
    -WorkingDirectory (Split-Path $guardianDll) `
    -PassThru `
    -WindowStyle Hidden

$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 500
    if ($guardianProc.HasExited) {
        throw ("Guardian exited early with code {0}" -f $guardianProc.ExitCode)
    }
    try {
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "MayaJaal.Guardian", [System.IO.Pipes.PipeDirection]::InOut)
        $pipe.Connect(200)
        $pipe.Dispose()
        $ready = $true
        break
    } catch {
    }
}
if (-not $ready) {
    Stop-Process -Id $guardianProc.Id -Force -ErrorAction SilentlyContinue
    throw "Guardian pipe not ready"
}
Write-Host ("Guardian ready (PID {0})" -f $guardianProc.Id) -ForegroundColor Green

Write-Host "== IPC live checks ==" -ForegroundColor Cyan
& $dotnetExe run --project $verifyProj -c Debug --nologo
$verifyCode = $LASTEXITCODE

Write-Host "== Unit tests ==" -ForegroundColor Cyan
& $dotnetExe test "$Root\tests\MayaJaal.Tests\MayaJaal.Tests.csproj" -c Debug --nologo
$testCode = $LASTEXITCODE

Write-Host "== Launch Security Center ==" -ForegroundColor Cyan
$centerProj = Join-Path $Root "src\MayaJaal.SecurityCenter\MayaJaal.SecurityCenter.csproj"
Stop-Process -Id $guardianProc.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$centerProc = Start-Process `
    -FilePath $dotnetExe `
    -ArgumentList @("run", "--project", "`"$centerProj`"", "-c", "Debug", "--no-build") `
    -WorkingDirectory $Root `
    -PassThru `
    -WindowStyle Normal

Start-Sleep -Seconds 8
if ($centerProc.HasExited) {
    Write-Host ("Security Center exited early: {0}" -f $centerProc.ExitCode) -ForegroundColor Red
    $uiCode = 1
} else {
    Write-Host ("Security Center running (PID {0})" -f $centerProc.Id) -ForegroundColor Green
    $uiCode = 0
}

Write-Host ""
Write-Host "======== FINAL ========" -ForegroundColor Cyan
Write-Host ("IPC verify exit : {0}" -f $verifyCode)
Write-Host ("Unit tests exit : {0}" -f $testCode)
Write-Host ("UI launch exit  : {0}" -f $uiCode)
if ($verifyCode -eq 0 -and $testCode -eq 0 -and $uiCode -eq 0) {
    Write-Host "RESULT: PASS - vault unlock/add, attack sim, lock, tests, UI all OK" -ForegroundColor Green
    exit 0
}

Write-Host "RESULT: FAIL" -ForegroundColor Red
exit 1
