# 停止旧进程
Get-Process -Name "NetPlan.Server" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# 重启
$exe = "C:\Program Files\dotnet\dotnet.exe"
$proj = "i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj"
Write-Host "Starting NetPlan server..." -ForegroundColor Cyan

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.Arguments = "run --project `"$proj`" --no-build"
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.WorkingDirectory = "i:\NetPlan\src\NetPlan.Server"
$proc = [System.Diagnostics.Process]::Start($psi)

Write-Host "Server PID: $($proc.Id)"
Write-Host "Waiting for server to start..." -ForegroundColor Yellow
$started = $false
$deadline = [DateTime]::Now.AddSeconds(30)
while (-not $started -and [DateTime]::Now -lt $deadline) {
    $line = $proc.StandardOutput.ReadLine()
    if ($line) {
        Write-Host $line
        if ($line -match "Now listening on" -or $line -match "Application started") {
            $started = $true
        }
    }
    Start-Sleep -Milliseconds 200
}

if ($started) {
    Write-Host "Server is running! Open https://localhost:5001" -ForegroundColor Green
    # Keep reading output
    while (-not $proc.HasExited) {
        $line = $proc.StandardOutput.ReadLine()
        if ($line) { Write-Host $line }
    }
} else {
    $err = $proc.StandardError.ReadToEnd()
    Write-Host "Server failed to start:" -ForegroundColor Red
    Write-Host $err
}
