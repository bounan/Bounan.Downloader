
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:dc8430e6024d454edadad1e160e1973be3cabbb7125998ef190d9e5c6adf7dbb AS build
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

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine@sha256:f276c0256ffca8fe816d48ba261962b54fea1b0e6f870b6a60b3b705c89e78ac AS final

VOLUME /tmp/bounan-downloader

RUN apk add ffmpeg font-roboto

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["/app/Bounan.Downloader.Worker"]
