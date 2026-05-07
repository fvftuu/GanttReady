$env:NUGET_PACKAGES = "C:\Users\REX\.nuget\packages"
$env:NuGetPackageRoot = "C:\Users\REX\.nuget\packages"
$env:ProgramData = "C:\ProgramData"

Write-Host "Starting NetPlan Server..."
Write-Host "NUGET_PACKAGES: $env:NUGET_PACKAGES"

& "C:\Program Files\dotnet\dotnet.exe" run --project "i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj" --urls "http://localhost:5000"
