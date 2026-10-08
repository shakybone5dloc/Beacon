#-------------- Stage 1: build-------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY ["src/Beacon.Api/Beacon.Api.csproj", "src/Beacon.Api/"]
COPY ["src/Beacon.Contracts/Beacon.Contracts.csproj", "src/Beacon.Contracts/"]
COPY ["src/Beacon.Application/Beacon.Application.csproj", "src/Beacon.Application/"]
COPY ["src/Beacon.Domain/Beacon.Domain.csproj", "src/Beacon.Domain/"]
COPY ["src/Beacon.Infrastructure/Beacon.Infrastructure.csproj", "src/Beacon.Infrastructure/"]
RUN dotnet restore "./src/Beacon.Api/Beacon.Api.csproj"
COPY src/ src/
RUN dotnet publish "./src/Beacon.Api/Beacon.Api.csproj" -c Release -o /app/publish --no-restore

#--------------- Stage 2: runtime ---------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .	

EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "Beacon.Api.dll"]