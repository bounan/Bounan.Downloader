FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS base

VOLUME /tmp/bounan-downloader

RUN apk add ffmpeg font-roboto

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Common/cs/Common.csproj", "Common/cs/"]
COPY ["src/Worker/Worker.csproj", "Worker/"]
RUN dotnet restore "Worker/Worker.csproj" -r linux-musl-x64

COPY ["stylecop.json", "./"]
COPY [".editorconfig", "./"]
COPY src .
WORKDIR /src/Worker

RUN dotnet publish "Worker.csproj" --no-restore --self-contained true --configuration Release --runtime linux-musl-x64 --output /app/publish

FROM base AS final

COPY --from=build /app/publish .

ENTRYPOINT ["/app/Bounan.Downloader.Worker"]
