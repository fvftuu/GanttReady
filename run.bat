@echo off
cd /d i:\NetPlan
dotnet run --project src\NetPlan.Server\NetPlan.Server.csproj --urls http://localhost:5000
