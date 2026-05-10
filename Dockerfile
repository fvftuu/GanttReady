FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/NetPlan.Server/*.csproj src/NetPlan.Server/
RUN dotnet restore src/NetPlan.Server/NetPlan.Server.csproj
COPY . .
RUN dotnet publish src/NetPlan.Server/NetPlan.Server.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "NetPlan.Server.dll"]
