FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
VOLUME /usr/shared/plexgifmaker_keys

# Install FFmpeg in the base image
RUN apt-get update -y && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PlexGifMaker/PlexGifMaker.csproj", "PlexGifMaker/"]
RUN dotnet restore "PlexGifMaker/PlexGifMaker.csproj"
COPY . .
WORKDIR "/src/PlexGifMaker"
RUN dotnet build "PlexGifMaker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlexGifMaker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PlexGifMaker.dll"]
