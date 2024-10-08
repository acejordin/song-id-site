#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install package dependencies, this line must be before the USER command to avoid permission issues with apt-get install command
RUN apt-get update -y && \
    apt-get install -y pulseaudio && \
    apt-get install -y alsa-utils && \
    apt-get install -y libsdl2-dev && \
    apt-get clean

USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV PULSE_SERVER=/mnt/wslg/PulseServer

ARG TARGETPLATFORM
ARG TARGETOS
ARG TARGETARCH
ARG TARGETVARIANT
ARG BUILDPLATFORM
ARG BUILDOS
ARG BUILDARCH
ARG BUILDVARIANT
RUN echo "Building on $BUILDPLATFORM, targeting $TARGETPLATFORM"
RUN echo "Building on ${BUILDOS} and ${BUILDARCH} with optional variant ${BUILDVARIANT}"
RUN echo "Targeting ${TARGETOS} and ${TARGETARCH} with optional variant ${TARGETVARIANT}"

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["song-id-site/song-id-site.csproj", "song-id-site/"]
COPY ["song-id/song-id.csproj", "song-id/"]
RUN dotnet restore -a $TARGETARCH "./song-id-site/song-id-site.csproj"
COPY . .
WORKDIR "/src/song-id-site"
RUN dotnet build "./song-id-site.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./song-id-site.csproj" -a $TARGETARCH -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "song-id-site.dll"]