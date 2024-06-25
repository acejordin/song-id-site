#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV PULSE_SERVER=/mnt/wslg/PulseServer

# Install package dependencies
RUN apt-get update -y && \
    apt-get install -y pulseaudio && \
    apt-get install -y alsa-utils && \
    apt-get install -y libsdl2-dev && \
    apt-get clean

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["song-id-site/song-id-site.csproj", "song-id-site/"]
RUN dotnet restore "./song-id-site/song-id-site.csproj"
COPY . .
WORKDIR "/src/song-id-site"
RUN dotnet build "./song-id-site.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./song-id-site.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "song-id-site.dll"]