
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:127d7d4d601ae26b8e04c54efb37e9ce8766931bded0ee59fcd799afd21d6850 AS build
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

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine@sha256:9ec585b84e539eff25a0c925137263ecb900db78d47e02a3b9ceb5d5009551fb AS final

VOLUME /tmp/bounan-downloader

RUN apk add ffmpeg font-roboto

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["/app/Bounan.Downloader.Worker"]
