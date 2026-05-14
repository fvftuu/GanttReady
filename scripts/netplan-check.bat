@echo off
REM NetPlan Pre/Post-Modification Validation Script
REM Usage: scripts\netplan-check.bat [pre|post]

setlocal
set BASE=I:\NetPlan\src\NetPlan.Server\wwwroot\js
set JS=%BASE%\netplan.js
set BUILD=I:\NetPlan\src\NetPlan.Server

echo === NetPlan Validation Check ===
echo.

REM 1. Syntax check
echo [1/4] Syntax check (node --check)...
node --check "%JS%" 2>&1
if %ERRORLEVEL% neq 0 (
    echo FAIL: netplan.js has syntax errors!
    exit /b 1
)
echo PASS: netplan.js syntax OK
echo.

REM 2. Key function presence check
echo [2/4] Key function check...
for %%f in (
    "calculateTimeParams"
    "buildNetworkSvg"
    "renderNetwork"
    "applySingleStartEnd"
    "calculateVerticalLayout"
    "validateNetworkRender"
) do (
    findstr /c:"function %%~f" "%JS%" >nul
    if %ERRORLEVEL% neq 0 (
        echo WARN: function %%~f not found in netplan.js
    ) else (
        echo  OK: function %%~f found
    )
)
echo.

REM 3. Build check
echo [3/4] Dotnet build check...
dotnet build "%BUILD%" --no-restore 2>&1 | findstr /c:"error" >nul
if %ERRORLEVEL% equ 0 (
    echo FAIL: Build has errors!
    dotnet build "%BUILD%" --no-restore 2>&1 | findstr /c:"error"
    exit /b 1
)
echo PASS: Build clean
echo.

REM 4. Git status
echo [4/4] Git status...
cd /d I:\NetPlan
git status --short
echo.

if "%1"=="post" (
    echo === Post-fix check complete ===
) else (
    echo === Pre-fix check complete - ready to work ===
)
