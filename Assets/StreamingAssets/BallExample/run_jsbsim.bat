@echo off
REM JSBSim Ball Simulation Launcher
REM Double-click to run in real-time mode
REM Usage: run_jsbsim.bat [fast]

set JSBSIM_EXE=C:\Users\User\AppData\Local\JSBSim\JSBSim.exe
set ROOT_PATH=%~dp0
set AIRCRAFT=ball
set INITFILE=cannonball_init

echo ============================================
echo JSBSim Ball Simulation
echo ============================================
echo Aircraft: %AIRCRAFT%
echo Init File: %INITFILE%
echo Root Path: %ROOT_PATH%
echo ============================================
echo.

if "%1"=="fast" (
    echo Running in FAST mode...
    "%JSBSIM_EXE%" --root="%ROOT_PATH%" --aircraft=%AIRCRAFT% --initfile=%INITFILE%
) else (
    echo Running in REAL-TIME mode (default)...
    echo Press Ctrl+C to stop the simulation
    echo.
    "%JSBSIM_EXE%" --root="%ROOT_PATH%" --aircraft=%AIRCRAFT% --initfile=%INITFILE% --realtime
)

echo.
echo ============================================
echo Simulation complete.
echo ============================================
echo.
echo Press any key to close this window...
pause >nul
cmd /k
