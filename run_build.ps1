$exe = "C:\Program Files\dotnet\dotnet.exe"
$proj = "i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj"
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.Arguments = "build `"$proj`" --no-restore"
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()
Write-Host "Exit code: $($proc.ExitCode)"
if ($proc.ExitCode -eq 0) {
    Write-Host "BUILD SUCCESS!" -ForegroundColor Green
    Write-Host "请刷新浏览器页面（Ctrl+F5），然后按 F12 打开控制台查看 [NET] 日志" -ForegroundColor Yellow
} else {
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    Write-Host $stdout
    Write-Host $stderr -ForegroundColor Red
}
