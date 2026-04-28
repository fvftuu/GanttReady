@echo off
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0NetPlan-Sync.ps1"
pause
