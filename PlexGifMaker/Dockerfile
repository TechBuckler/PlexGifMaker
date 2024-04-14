# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install FFmpeg in the base image
RUN apt-get update -y && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

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
ENTRYPOINT ["dotnet", "PlexGifMaker.dll"]
