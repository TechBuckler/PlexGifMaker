# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Switch to root to install packages and set up user
USER root

# Install FFmpeg and create user
RUN apt-get update -y && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    groupadd -g 1000 plexgifmaker && \
    useradd -u 1000 -g plexgifmaker -s /bin/bash plexgifmaker

# Create necessary directories and set permissions
RUN mkdir -p /usr/shared/plexgifmaker_keys "/app/subtitles" "/app/logs" "/app/wwwroot/gifs" && \
    chown -R plexgifmaker:plexgifmaker /usr/shared/plexgifmaker_keys /app && \
    chmod 755 /usr/shared/plexgifmaker_keys && \
    chmod 775 "/app/wwwroot/gifs" && \
    chown -R plexgifmaker:plexgifmaker /app/wwwroot/gifs

# Use the .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PlexGifMaker/PlexGifMaker.csproj", "./"]
RUN dotnet restore "PlexGifMaker.csproj"
COPY ["PlexGifMaker/", "./"]
WORKDIR "/src"
RUN dotnet build "PlexGifMaker.csproj" -c Release -o /app/build

# Publish the project
FROM build AS publish
RUN dotnet publish "PlexGifMaker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Reapply permissions and ownership
USER root
RUN chown -R plexgifmaker:plexgifmaker /app && \
    chmod 775 "/app/wwwroot/gifs"

# Switch to the plexgifmaker user
USER plexgifmaker
ENTRYPOINT ["dotnet", "PlexGifMaker.dll"]