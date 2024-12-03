#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install package dependencies, this line must be before the USER command to avoid permission issues with apt-get install command
RUN apt-get update -y && \
    #apt-get install -y pulseaudio && \
    #apt-get install -y pipewire && \
    #apt-get install -y pipewire-alsa && \
    #apt-get install pipewire-audio-client-libraries && \
    #apt-get install -y libsdl2-dev && \
    apt-get install -y alsa-utils && \
    #apt-get install -y libasound2-dev && \
    #apt-get install -y alsa-lib && \
    #apt-get install -y alsa-plugins && \
    apt-get clean

# USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV PULSE_SERVER=/mnt/wslg/PulseServer

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["song-id-site/song-id-site.csproj", "song-id-site/"]
COPY ["song-id/song-id.csproj", "song-id/"]
ARG TARGETARCH
RUN dotnet restore -a $TARGETARCH "./song-id-site/song-id-site.csproj"
COPY . .
WORKDIR "/src/song-id-site"
RUN dotnet build "./song-id-site.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./song-id-site.csproj" -a $TARGETARCH -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY ["lib/.asoundrc", "/root/.asoundrc"]
COPY ["lib/.asoundrc", "/home/app/.asoundrc"]
COPY ["lib/.asoundrc", "/home/alec/.asoundrc"]
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "song-id-site.dll"]