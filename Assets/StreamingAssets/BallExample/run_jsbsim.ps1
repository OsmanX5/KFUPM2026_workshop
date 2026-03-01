# JSBSim Ball Simulation Launcher (PowerShell)
# Usage: .\run_jsbsim.ps1 [-RealTime] [-WithReceiver]

param(
    [switch]$Fast        # Use -Fast to disable real-time mode
)

$JSBSIM_EXE = "C:\Users\User\AppData\Local\JSBSim\JSBSim.exe"
$ROOT_PATH = $PSScriptRoot
$AIRCRAFT = "ball"
$INITFILE = "cannonball_init"

$RealTime = -not $Fast

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "JSBSim Ball Simulation" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Aircraft:  $AIRCRAFT"
Write-Host "Init File: $INITFILE"
Write-Host "Root Path: $ROOT_PATH"
Write-Host "Real-Time: $RealTime"
Write-Host "============================================" -ForegroundColor Cyan

# Build command arguments
$args = @(
    "--root=`"$ROOT_PATH`"",
    "--aircraft=$AIRCRAFT",
    "--initfile=$INITFILE"
)

if ($RealTime) {
    $args += "--realtime"
    Write-Host "Running in REAL-TIME mode..." -ForegroundColor Green
} else {
    Write-Host "Running in FAST mode..." -ForegroundColor Green
}

# Run JSBSim
& $JSBSIM_EXE $args

Write-Host "`nSimulation complete." -ForegroundColor Cyan
