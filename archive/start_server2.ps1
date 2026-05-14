$env:NUGET_PACKAGES = "C:\Users\REX\.nuget\packages"
$env:NuGetPackageRoot = "C:\Users\REX\.nuget\packages"
$env:ProgramData = "C:\ProgramData"

Write-Host "=== Starting NetPlan Server ===" -ForegroundColor Cyan
Write-Host "Working Directory: $(Get-Location)"

$ErrorActionPreference = "Continue"

# Run with output capture
$process = Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" `
    -ArgumentList "run","--project","i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj","--urls","http://localhost:5000" `
    -NoNewWindow `
    -PassThru `
    -RedirectStandardOutput "i:\NetPlan\stdout.log" `
    -RedirectStandardError "i:\NetPlan\stderr.log"

Write-Host "Process ID: $($process.Id)"

# Wait a bit
Start-Sleep -Seconds 20

# Check if still running
if (!$process.HasExited) {
    Write-Host "Server is running!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Process exited with code: $($process.ExitCode)" -ForegroundColor Red
    
    if (Test-Path "i:\NetPlan\stdout.log") {
        Write-Host "=== STDOUT ===" -ForegroundColor Yellow
        Get-Content "i:\NetPlan\stdout.log"
    }
    
    if (Test-Path "i:\NetPlan\stderr.log") {
        Write-Host "=== STDERR ===" -ForegroundColor Yellow
        Get-Content "i:\NetPlan\stderr.log"
    }
    
    exit 1
}
