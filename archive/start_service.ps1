# Start NetPlan Service
$ErrorActionPreference = "Continue"
$start = Get-Date

Write-Host "Starting NetPlan Server..."
Write-Host "Working Directory: $PWD"

try {
    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", "NetPlan.Server.csproj" `
        -WorkingDirectory $PWD `
        -NoNewWindow `
        -PassThru `
        -RedirectStandardOutput "$env:TEMP\netplan_stdout.log" `
        -RedirectStandardError "$env:TEMP\netplan_stderr.log"

    Write-Host "Process started with PID: $($process.Id)"

    # Wait for startup
    Start-Sleep -Seconds 15

    # Check if process is still running
    if ($process.HasExited) {
        Write-Host "[ERROR] Process exited with code: $($process.ExitCode)"
        Write-Host "--- STDOUT ---"
        Get-Content "$env:TEMP\netplan_stdout.log" -ErrorAction SilentlyContinue
        Write-Host "--- STDERR ---"
        Get-Content "$env:TEMP\netplan_stderr.log" -ErrorAction SilentlyContinue
    } else {
        Write-Host "[OK] Process is running"
        # Test connection
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5000/" -TimeoutSec 5
            Write-Host "[OK] Service responding with status: $($response.StatusCode)"
        } catch {
            Write-Host "[WARN] Service may not be responding yet"
        }
    }
} catch {
    Write-Host "[ERROR] Failed to start: $_"
}
