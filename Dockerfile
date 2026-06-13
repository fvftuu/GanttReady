FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/GanttReady.Server/*.csproj src/GanttReady.Server/
RUN dotnet restore src/GanttReady.Server/GanttReady.Server.csproj
COPY . .
RUN dotnet publish src/GanttReady.Server/GanttReady.Server.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "GanttReady.Server.dll"]
