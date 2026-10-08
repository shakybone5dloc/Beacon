#-------------- Stage 1: build---------------------
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

#----------- Stage 2: migration bundle-------------
FROM build AS migrations-build
COPY dotnet-tools.json ./
RUN dotnet tool restore
RUN ConnectionStrings__Beacon="Host=build-placeholder" \
	dotnet ef migrations bundle \
	--project src/Beacon.Infrastructure/Beacon.Infrastructure.csproj \
	--startup-project src/Beacon.Api/Beacon.Api.csproj \
	--configuration Release \
	--self-contained -r linux-x64 \
	-o /app/efbundle

#----------- Stage 3: migration image -------------
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=migrations-build /app/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

#-------------- Stage 4: runtime ------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Beacon.Api.dll"]