# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
# The Web project's restore depends on its .razor/wwwroot content
# (the SDK auto-references Microsoft.AspNetCore.App.Internal.Assets, which ships blazor.web.js),
# so restore must run with the source present. A cache mount keeps NuGet downloads fast instead.
COPY src/ src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "./src/Beacon.Web/Beacon.Web.csproj" -c Release -o /app/publish
# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet","Beacon.Web.dll"]