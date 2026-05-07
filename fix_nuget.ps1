# fix_nuget.ps1 - .NET SDK NuGet 环境修复

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ".NET SDK NuGet Environment Fix" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Check admin
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: Please run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell -> Run as administrator" -ForegroundColor Yellow
    exit 1
}

Write-Host "[1/6] Stopping related processes..." -ForegroundColor Yellow
Get-Process | Where-Object {
    $_.ProcessName -like "*dotnet*" -or
    $_.ProcessName -like "*msbuild*" -or
    $_.ProcessName -like "*devenv*" -or
    $_.ProcessName -like "*vscode*" -or
    $_.ProcessName -like "*nuget*"
} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
Write-Host "  Done" -ForegroundColor Green

Write-Host "[2/6] Deleting all NuGet config and cache dirs..." -ForegroundColor Yellow
$nugetPaths = @(
    "C:\ProgramData\NuGet",
    "$env:USERPROFILE\.nuget",
    "$env:LOCALAPPDATA\NuGet",
    "$env:TEMP\NuGetScratch"
)
foreach ($path in $nugetPaths) {
    if (Test-Path $path) {
        Write-Host "  Removing: $path"
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }
}
Write-Host "  Done" -ForegroundColor Green

Write-Host "[3/6] Rebuilding clean directory structure..." -ForegroundColor Yellow
$newPaths = @(
    "C:\ProgramData\NuGet\Config",
    "$env:USERPROFILE\.nuget\packages",
    "$env:LOCALAPPDATA\NuGet\v3-cache",
    "$env:APPDATA\NuGet"
)
foreach ($path in $newPaths) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    Write-Host "  Created: $path"
}
Write-Host "  Done" -ForegroundColor Green

Write-Host "[4/6] Resetting ACLs (inherit from parent)..." -ForegroundColor Yellow
$aclPaths = @("C:\ProgramData\NuGet", "C:\ProgramData\NuGet\Config", "$env:USERPROFILE\.nuget")
foreach ($p in $aclPaths) {
    if (Test-Path $p) {
        icacls $p /reset /T 2>$null
        Write-Host "  Reset ACL: $p"
    }
}
Write-Host "  Done" -ForegroundColor Green

Write-Host "[5/6] Writing config files..." -ForegroundColor Yellow

# Machine-level config (C:\ProgramData\NuGet\Config\NuGet.Config)
$machineXml = '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
'
Set-Content -Path "C:\ProgramData\NuGet\Config\NuGet.Config" -Value $machineXml -Encoding UTF8
Write-Host "  Written: C:\ProgramData\NuGet\Config\NuGet.Config"

# User-level config ($env:APPDATA\NuGet\NuGet.Config)
$userXml = '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="C:\Users\REX\.nuget\packages" />
  </config>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
'
Set-Content -Path "$env:APPDATA\NuGet\NuGet.Config" -Value $userXml -Encoding UTF8
Write-Host "  Written: $env:APPDATA\NuGet\NuGet.Config"
Write-Host "  Done" -ForegroundColor Green

Write-Host "[6/6] Verification..." -ForegroundColor Yellow
try {
    $ver = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  dotnet --version: OK (v$ver)" -ForegroundColor Green
    } else {
        throw $ver
    }
} catch {
    Write-Host "  dotnet check: needs restart" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "FIX COMPLETED!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Please RESTART YOUR PC, then run:" -ForegroundColor Yellow
Write-Host "  dotnet restore i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj" -ForegroundColor White
Write-Host ""
Write-Host "If still broken after restart, reinstall .NET SDK." -ForegroundColor Cyan
