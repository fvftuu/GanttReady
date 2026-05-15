@echo off
chcp 65001 >nul
cd /d I:\NetPlan\src\NetPlan.Server\wwwroot\js\netplan
echo === 1. tsc ===
npx tsc
if %errorlevel% neq 0 exit /b %errorlevel%
echo === 2. esbuild ===
node esbuild.config.mjs
if %errorlevel% neq 0 exit /b %errorlevel%
echo === 3. verify ===
node -e "var c=require('fs').readFileSync('../netplan.js','utf-8');console.log('lastRowY:',c.includes('lastRowY'));console.log('existingOffXDays:',c.includes('existingOffXDays'));console.log('size:',c.length)"
cd /d I:\NetPlan
echo === 4. restart server ===
powershell -Command "Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue; Start-Sleep 2; Start-Process -WindowStyle Hidden -FilePath dotnet -ArgumentList 'run --project I:\NetPlan\src\NetPlan.Server --urls http://localhost:5000'; Start-Sleep 4"
echo DONE
