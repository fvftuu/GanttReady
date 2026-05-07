# 强制设置NuGet环境变量
[Environment]::SetEnvironmentVariable("NUGET_PACKAGES", "C:\Users\REX\.nuget\packages", "Process")
[Environment]::SetEnvironmentVariable("NuGetPackageRoot", "C:\Users\REX\.nuget\packages", "Process")
[Environment]::SetEnvironmentVariable("ProgramData", "C:\ProgramData", "Process")

# 直接运行dotnet命令
$env:NUGET_PACKAGES = "C:\Users\REX\.nuget\packages"
$env:NuGetPackageRoot = "C:\Users\REX\.nuget\packages"
$env:ProgramData = "C:\ProgramData"

Write-Host "Environment variables set"
Write-Host "NUGET_PACKAGES = $env:NUGET_PACKAGES"

# Test restore first
Write-Host "`n=== Testing dotnet restore ===" -ForegroundColor Cyan
& "C:\Program Files\dotnet\dotnet.exe" restore "i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj" 2>&1
Write-Host "Restore exit code: $LASTEXITCODE"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}

# Now run the server
Write-Host "`n=== Starting NetPlan Server ===" -ForegroundColor Cyan
& "C:\Program Files\dotnet\dotnet.exe" run --project "i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj" --urls "http://localhost:5000" 2>&1
