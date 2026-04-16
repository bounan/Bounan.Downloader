
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:adc02be8b87957d07208a4a3e51775935b33bad3317de8c45b1e67357b4c073b AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Common/cs/Common.csproj", "Common/cs/"]
COPY ["src/Domain/Domain.csproj", "Domain/"]
COPY ["src/Application/Application.csproj", "Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["src/Worker/Worker.csproj", "Worker/"]
RUN dotnet restore "Worker/Worker.csproj" -r linux-musl-x64

COPY ["stylecop.json", ".editorconfig", "./"]
COPY src .
WORKDIR /src/Worker

RUN dotnet publish "Worker.csproj" --no-restore --self-contained true --configuration Release --runtime linux-musl-x64 --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine@sha256:f8a0978d56136514d1d2f9c893a8797eb47d42f6522da7b8d1b2fcdc51e95198 AS final

VOLUME /tmp/bounan-downloader

RUN apk add ffmpeg font-roboto

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["/app/Bounan.Downloader.Worker"]
