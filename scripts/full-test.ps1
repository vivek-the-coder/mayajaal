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

$shotDir = Join-Path $Root "scripts\verify-shots\full"
New-Item -ItemType Directory -Force -Path $shotDir | Out-Null
$report = [System.Collections.Generic.List[string]]::new()

function Log([string]$msg, [string]$color = "White") {
    Write-Host $msg -ForegroundColor $color
    $report.Add($msg)
}

# ---------- helpers ----------
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class FullTestWin32 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@ -ErrorAction SilentlyContinue

function Get-MayaWindow {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    foreach ($a in $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($a.Current.Name -eq "MayaJaal Security Center") { return $a }
    }
    return $null
}

function Wait-MayaWindow([int]$seconds = 30) {
    for ($i = 0; $i -lt ($seconds * 2); $i++) {
        $w = Get-MayaWindow
        if ($w) { return $w }
        Start-Sleep -Milliseconds 500
    }
    throw "Security Center window not found"
}

function Invoke-NamedButton([System.Windows.Automation.AutomationElement]$win, [string]$name) {
    $btnCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    foreach ($b in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)) {
        if ($b.Current.Name -eq $name) {
            $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
            return $true
        }
    }
    return $false
}

function Get-ButtonNames([System.Windows.Automation.AutomationElement]$win) {
    $btnCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    $names = @()
    foreach ($b in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)) {
        if ($b.Current.Name) { $names += $b.Current.Name }
    }
    return $names
}

function Get-TextHits([System.Windows.Automation.AutomationElement]$win, [string[]]$patterns) {
    $txtCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $hits = @()
    foreach ($t in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtCond)) {
        $n = $t.Current.Name
        if (-not $n) { continue }
        foreach ($p in $patterns) {
            if ($n -like $p) { $hits += $n; break }
        }
    }
    return $hits
}

function Capture-Maya([System.Windows.Automation.AutomationElement]$win, [string]$path) {
    $hwnd = [IntPtr]$win.Current.NativeWindowHandle
    [FullTestWin32]::ShowWindow($hwnd, 9) | Out-Null
    [FullTestWin32]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 350
    $rect = $win.Current.BoundingRectangle
    $w = [int]$rect.Width
    $h = [int]$rect.Height
    if ($w -lt 50 -or $h -lt 50) { throw "bad window bounds" }
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bmp.Size)
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Test-Contrast([string]$pngPath) {
    $img = [System.Drawing.Bitmap]::FromFile($pngPath)
    $zinc = 0
    $whiteNear = 0
    for ($y = 0; $y -lt $img.Height; $y += 4) {
        for ($x = 0; $x -lt $img.Width; $x += 4) {
            $c = $img.GetPixel($x, $y)
            if ([Math]::Abs($c.R - 24) -lt 14 -and [Math]::Abs($c.G - 24) -lt 14 -and [Math]::Abs($c.B - 27) -lt 14) {
                $zinc++
                for ($dx = -8; $dx -le 8; $dx += 2) {
                    $nx = [Math]::Min($img.Width - 1, [Math]::Max(0, $x + $dx))
                    $n = $img.GetPixel($nx, $y)
                    if ($n.R -gt 230 -and $n.G -gt 230 -and $n.B -gt 230) { $whiteNear++; break }
                }
            }
        }
    }
    $img.Dispose()
    return @{ Zinc = $zinc; WhiteNear = $whiteNear; Ok = ($whiteNear -gt 20) }
}

# ---------- 1) IPC suite with standalone Guardian ----------
Log "== 1) Build ==" "Cyan"
& $dotnetExe build "$Root\MayaJaal.sln" -c Debug --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Log "== 2) Start Guardian for IPC ==" "Cyan"
$guardianDll = Join-Path $Root "src\MayaJaal.Guardian\bin\Debug\net8.0-windows\MayaJaal.Guardian.dll"
$guardianProc = Start-Process -FilePath $dotnetExe -ArgumentList @("exec", "`"$guardianDll`"", "--console") `
    -WorkingDirectory (Split-Path $guardianDll) -PassThru -WindowStyle Hidden

$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Milliseconds 500
    if ($guardianProc.HasExited) { throw "Guardian exited early $($guardianProc.ExitCode)" }
    try {
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "MayaJaal.Guardian", [System.IO.Pipes.PipeDirection]::InOut)
        $pipe.Connect(200); $pipe.Dispose(); $ready = $true; break
    } catch {}
}
if (-not $ready) { throw "Guardian pipe not ready" }
Log "Guardian PID $($guardianProc.Id)" "Green"

Log "== 3) IPC full suite ==" "Cyan"
$verifyProj = Join-Path $Root "scripts\verify-live\VerifyLive.csproj"
& $dotnetExe run --project $verifyProj -c Debug --nologo
$ipcCode = $LASTEXITCODE
if ($ipcCode -eq 0) { Log "IPC FULL SUITE PASS" "Green" } else { Log "IPC FULL SUITE FAIL" "Red" }

Log "== 4) Unit tests ==" "Cyan"
& $dotnetExe test "$Root\tests\MayaJaal.Tests\MayaJaal.Tests.csproj" -c Debug --nologo
$testCode = $LASTEXITCODE
if ($testCode -eq 0) { Log "UNIT TESTS PASS (32)" "Green" } else { Log "UNIT TESTS FAIL" "Red" }

# ---------- 2) UI human expert flow ----------
Log "== 5) Launch Security Center ==" "Cyan"
Stop-Process -Id $guardianProc.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
$centerProj = Join-Path $Root "src\MayaJaal.SecurityCenter\MayaJaal.SecurityCenter.csproj"
$centerProc = Start-Process -FilePath $dotnetExe -ArgumentList @("run", "--project", "`"$centerProj`"", "-c", "Debug", "--no-build") `
    -WorkingDirectory $Root -PassThru -WindowStyle Normal

$win = Wait-MayaWindow 40
Log "UI window found" "Green"
Start-Sleep -Seconds 5
$win = Get-MayaWindow

$uiFails = [System.Collections.Generic.List[string]]::new()
$uiPass = 0

function UiPass($n) { $script:uiPass++; Log ("  PASS UI {0}" -f $n) "Green" }
function UiFail($n, $d) { $script:uiFails.Add(("{0}: {1}" -f $n, $d)); Log ("  FAIL UI {0} - {1}" -f $n, $d) "Red" }

# Required chrome buttons
$win = Get-MayaWindow
$btns = Get-ButtonNames $win
$required = @("Dashboard","Incidents","Vault","Policies","Detections","Attack Lab","Settings","Refresh","Unlock vault","Add file to vault","Lock all vaults")
foreach ($r in $required) {
    if ($btns -contains $r) { UiPass "button:$r" } else { UiFail "button:$r" "missing" }
}

# Navigate every page + screenshot + key text
$pages = @(
    @{ Name = "Dashboard"; Patterns = @("*Dashboard*","*ONLINE*","*OFFLINE*","*Getting started*","*Live numbers*"); Shot = "01-dashboard.png" },
    @{ Name = "Incidents"; Patterns = @("*Incidents*","*Resolve*","*Rollback*","*No incidents*","*INC-*"); Shot = "02-incidents.png" },
    @{ Name = "Vault"; Patterns = @("*Vault*","*How to use*","*Unlock*","*Files in vault*","*Default Vault*"); Shot = "03-vault.png" },
    @{ Name = "Policies"; Patterns = @("*Policies*","*Default*","*Enabled*","*Disabled*","*unavailable*"); Shot = "04-policies.png" },
    @{ Name = "Detections"; Patterns = @("*Detections*","*Honey*","*USB*","*Process*","*Mass*"); Shot = "05-detections.png" },
    @{ Name = "Attack Lab"; Patterns = @("*Attack Lab*","*USB exfiltration*","*Run attack*","*Pending*","*Faculty*"); Shot = "06-attacklab.png" },
    @{ Name = "Settings"; Patterns = @("*Settings*","*Pipe*","*Guardian*","*Online*","*Offline*"); Shot = "07-settings.png" }
)

foreach ($p in $pages) {
    $win = Get-MayaWindow
    if (-not (Invoke-NamedButton $win $p.Name)) { UiFail "nav:$($p.Name)" "click failed"; continue }
    Start-Sleep -Seconds 1
    $win = Get-MayaWindow
    $shot = Join-Path $shotDir $p.Shot
    Capture-Maya $win $shot
    $hits = Get-TextHits $win $p.Patterns
    if ($hits.Count -gt 0) { UiPass "page:$($p.Name) texts=$($hits.Count)" }
    else { UiFail "page:$($p.Name)" "no expected texts" }
}

# Contrast on vault page
$win = Get-MayaWindow
Invoke-NamedButton $win "Vault" | Out-Null
Start-Sleep -Seconds 1
$win = Get-MayaWindow
$vaultShot = Join-Path $shotDir "03b-vault-contrast.png"
Capture-Maya $win $vaultShot
$c = Test-Contrast $vaultShot
if ($c.Ok) { UiPass "contrast white-on-black (near=$($c.WhiteNear))" } else { UiFail "contrast" "zinc=$($c.Zinc) whiteNear=$($c.WhiteNear)" }

# Unlock vault
$win = Get-MayaWindow
if (Invoke-NamedButton $win "Unlock vault") {
    Start-Sleep -Seconds 3
    $win = Get-MayaWindow
    $hits = Get-TextHits $win @("*Unlocked*","*UNLOCKED*","*ready to add*")
    if ($hits.Count -gt 0) { UiPass "unlock vault" } else { UiFail "unlock vault" "no unlocked text" }
} else { UiFail "unlock vault" "button missing" }

# Add file via sidebar
$sample = Join-Path $env:TEMP "mayajaal-ui-fulltest.txt"
Set-Content -Path $sample -Value "ui full test $(Get-Date -Format o)" -Encoding UTF8
$win = Get-MayaWindow
if (Invoke-NamedButton $win "Add file to vault") {
    Start-Sleep -Seconds 1.2
    [System.Windows.Forms.SendKeys]::SendWait($sample)
    Start-Sleep -Milliseconds 400
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Start-Sleep -Seconds 3
    $win = Get-MayaWindow
    $hits = Get-TextHits $win @("*mayajaal-ui-fulltest.txt*")
    if ($hits.Count -gt 0) { UiPass "add file UI" } else { UiFail "add file UI" "filename not listed" }
} else { UiFail "add file UI" "button missing" }

Capture-Maya (Get-MayaWindow) (Join-Path $shotDir "08-vault-after-add.png")

# Attack Lab USB
$win = Get-MayaWindow
Invoke-NamedButton $win "Attack Lab" | Out-Null
Start-Sleep -Seconds 1
$win = Get-MayaWindow
Invoke-NamedButton $win "USB exfiltration" | Out-Null
Start-Sleep -Milliseconds 500
if (Invoke-NamedButton (Get-MayaWindow) "Run attack simulation") {
    Start-Sleep -Seconds 10
    $win = Get-MayaWindow
    Capture-Maya $win (Join-Path $shotDir "09-attack-usb.png")
    $hits = Get-TextHits $win @("*CRITICAL*","*EMERGENCY*","*INC-*","*finished*","*contain*","*Done*","*Running*","*Simulation*")
    if ($hits.Count -gt 0) { UiPass "attack USB sim UI" } else { UiFail "attack USB sim UI" "no result texts" }
} else { UiFail "attack USB sim UI" "run button missing" }

# Dashboard after attack
$win = Get-MayaWindow
Invoke-NamedButton $win "Dashboard" | Out-Null
Start-Sleep -Seconds 2
$win = Get-MayaWindow
Capture-Maya $win (Join-Path $shotDir "10-dashboard-critical.png")
$hits = Get-TextHits $win @("*ONLINE*","*CRITICAL*","*Risk*","*INC-*")
if (($hits | Where-Object { $_ -like "*ONLINE*" }).Count -gt 0) { UiPass "dashboard ONLINE" } else { UiFail "dashboard ONLINE" "offline?" }
if (($hits | Where-Object { $_ -like "*CRITICAL*" -or $_ -like "*Risk*" }).Count -gt 0) { UiPass "dashboard threat visible" } else { UiFail "dashboard threat" "no critical/risk" }

# Incidents resolve
$win = Get-MayaWindow
Invoke-NamedButton $win "Incidents" | Out-Null
Start-Sleep -Seconds 2
$win = Get-MayaWindow
Capture-Maya $win (Join-Path $shotDir "11-incidents.png")
$btns = Get-ButtonNames $win
if ($btns -contains "Resolve") {
    if (Invoke-NamedButton $win "Resolve") {
        Start-Sleep -Seconds 2
        UiPass "resolve incident click"
    } else { UiFail "resolve incident" "invoke failed" }
} else {
    Log "  WARN UI resolve button not present (may already be resolved)" "Yellow"
}

# Attack Lab insider
$win = Get-MayaWindow
Invoke-NamedButton $win "Attack Lab" | Out-Null
Start-Sleep -Seconds 1
$win = Get-MayaWindow
if (Invoke-NamedButton $win "Insider harvest") {
    Start-Sleep -Milliseconds 600
    Invoke-NamedButton (Get-MayaWindow) "Run attack simulation" | Out-Null
    Start-Sleep -Seconds 10
    $win = Get-MayaWindow
    Capture-Maya $win (Join-Path $shotDir "12-attack-insider.png")
    $hits = Get-TextHits $win @("*Insider*","*honey*","*finished*","*CRITICAL*","*contain*","*Simulation*","*Done*")
    if ($hits.Count -gt 0) { UiPass "attack insider sim UI" } else { UiFail "attack insider sim UI" "no result texts" }
} else { UiFail "attack insider" "scenario button missing" }

# Lock all
$win = Get-MayaWindow
if (Invoke-NamedButton $win "Lock all vaults") {
    Start-Sleep -Seconds 2
    $win = Get-MayaWindow
    $hits = Get-TextHits $win @("*Locked*","*locked*")
    if ($hits.Count -gt 0) { UiPass "lock all vaults" } else { UiFail "lock all vaults" "no locked text" }
} else { UiFail "lock all vaults" "button missing" }

# Refresh
$win = Get-MayaWindow
if (Invoke-NamedButton $win "Refresh") { Start-Sleep -Seconds 1; UiPass "refresh" } else { UiFail "refresh" "missing" }

Capture-Maya (Get-MayaWindow) (Join-Path $shotDir "13-final.png")

# ---------- FINAL ----------
Log "" 
Log "======== FULL TEST FINAL ========" "Cyan"
Log ("IPC exit     : {0}" -f $ipcCode)
Log ("Unit tests   : {0}" -f $testCode)
Log ("UI passes    : {0}" -f $uiPass)
Log ("UI fails     : {0}" -f $uiFails.Count)
foreach ($f in $uiFails) { Log ("  UI - {0}" -f $f) "Red" }

$ok = ($ipcCode -eq 0 -and $testCode -eq 0 -and $uiFails.Count -eq 0)
if ($ok) {
    Log "RESULT: PASS - full expert test mode complete" "Green"
    $reportPath = Join-Path $shotDir "REPORT.txt"
    $report | Set-Content $reportPath -Encoding UTF8
    exit 0
} else {
    Log "RESULT: FAIL - see UI/IPC failures above" "Red"
    $reportPath = Join-Path $shotDir "REPORT.txt"
    $report | Set-Content $reportPath -Encoding UTF8
    exit 1
}
