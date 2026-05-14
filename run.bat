@echo off
cd /d I:\NetPlan
echo Starting NetPlan at http://localhost:5000
dotnet run --project src\NetPlan.Server --urls http://localhost:5000
