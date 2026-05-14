$ErrorActionPreference = "Continue"
$start = Get-Date

while ($true) {
    try {
        $process = Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" `
            -ArgumentList "run","--project","i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj","--urls","http://localhost:5000" `
            -PassThru -WindowStyle Hidden
        
        $startTime = Get-Date
        $running = $false
        
        while ((Get-Date) - $startTime -lt [TimeSpan]::FromMinutes(5)) {
            Start-Sleep -Seconds 2
            
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:5000/" -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Host "服务启动成功!"
                    $running = $true
                    break
                }
            } catch {
                # 还没启动，继续等待
            }
            
            if ($process.HasExited) {
                Write-Host "进程已退出，退出码: $($process.ExitCode)"
                break
            }
        }
        
        if ($running) {
            Write-Host "服务正在运行，进程ID: $($process.Id)"
            $process.WaitForExit()
        } else {
            Write-Host "服务启动失败，5分钟后重试..."
            if (!$process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
            Start-Sleep -Seconds 5
        }
    } catch {
        Write-Host "错误: $_"
        Start-Sleep -Seconds 5
    }
}
