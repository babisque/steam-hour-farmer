FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/SteamHourFarmer.sln .
COPY src/SteamHourFarmer.Core/SteamHourFarmer.Core.csproj ./SteamHourFarmer.Core/
COPY src/SteamHourFarmer.Infrastructure/SteamHourFarmer.Infrastructure.csproj ./SteamHourFarmer.Infrastructure/
COPY src/SteamHourFarmer.Worker/SteamHourFarmer.Worker.csproj ./SteamHourFarmer.Worker/

RUN dotnet restore SteamHourFarmer.sln

COPY src/ .

WORKDIR /src/SteamHourFarmer.Worker
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir /app/data

VOLUME /app/data

ENV TOKEN_STORAGE_DIRECTORY="app/data/tokens"

ENTRYPOINT [ "dotnet", "SteamHourFarmer.Worker.dll" ]