$env:ProgramData = [Environment]::GetEnvironmentVariable("ProgramData", "Machine")
$env:NUGET_PACKAGES = "C:\Users\REX\.nuget\packages"
$env:NuGetPackageRoot = "C:\Users\REX\.nuget\packages"

Write-Host "ProgramData: $env:ProgramData"
Write-Host "NUGET_PACKAGES: $env:NUGET_PACKAGES"

$pinfo = New-Object System.Diagnostics.ProcessStartInfo
$pinfo.FileName = "C:\Program Files\dotnet\dotnet.exe"
$pinfo.Arguments = "run --project `"i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj`" --urls http://localhost:5000"
$pinfo.RedirectStandardOutput = $true
$pinfo.RedirectStandardError = $true
$pinfo.UseShellExecute = $false
$pinfo.EnvironmentVariables["ProgramData"] = $env:ProgramData
$pinfo.EnvironmentVariables["NUGET_PACKAGES"] = $env:NUGET_PACKAGES
$pinfo.EnvironmentVariables["NuGetPackageRoot"] = $env:NuGetPackageRoot

$p = New-Object System.Diagnostics.Process
$p.StartInfo = $pinfo
$started = $p.Start()

Write-Host "Started: $started"
Write-Host "Process ID: $($p.Id)"

Start-Sleep -Seconds 20

if ($p.HasExited) {
    Write-Host "Process exited with code: $($p.ExitCode)"
    Write-Host "STDOUT:"
    Write-Host $p.StandardOutput.ReadToEnd()
    Write-Host "STDERR:"
    Write-Host $p.StandardError.ReadToEnd()
}
else {
    Write-Host "Process is still running!"
}

Start-Sleep -Seconds 600
