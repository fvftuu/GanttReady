$env:ProgramData = "C:\ProgramData"
$env:NUGET_PACKAGES = "C:\Users\REX\.nuget\packages"
$env:NUGET_HTTP_CACHE_PATH = "C:\Users\REX\AppData\Local\NuGet\v3-cache"
$env:NUGET_PLUGINS_CACHE_PATH = "C:\Users\REX\AppData\Local\NuGet\plugins-cache"

$exe = "C:\Program Files\dotnet\dotnet.exe"
$proj = "i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj"

Write-Host "=== dotnet restore with msbuild override ===" -ForegroundColor Cyan
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.Arguments = "msbuild `"$proj`" -target:Restore -p:RestoreConfigFile=`"i:\NetPlan\NuGet.Config`";RestorePackagesPath=`"i:\NetPlan\packages`";RestoreRootConfigDirectory=`"i:\NetPlan\`";RestoreSources=`"https://api.nuget.org/v3/index.json`";MachineWideSettings=`"`";_GetRestoreSettings=`"`";RestoreNoCache=`"true`""
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.EnvironmentVariables["ProgramData"] = "C:\ProgramData"
$psi.EnvironmentVariables["NUGET_PACKAGES"] = "C:\Users\REX\.nuget\packages"
$psi.EnvironmentVariables["NUGET_HTTP_CACHE_PATH"] = "C:\Users\REX\AppData\Local\NuGet\v3-cache"
$psi.EnvironmentVariables["NUGET_PLUGINS_CACHE_PATH"] = "C:\Users\REX\AppData\Local\NuGet\plugins-cache"
$psi.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER"] = "false"
$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()
Write-Host "Exit code: $($proc.ExitCode)"
Write-Host $stdout
if ($proc.ExitCode -ne 0) { Write-Host $stderr -ForegroundColor Red }
