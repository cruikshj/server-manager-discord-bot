FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
WORKDIR /source

ARG PROJECT_NAME=ServerManagerDiscordBot

COPY src/${PROJECT_NAME}/${PROJECT_NAME}.csproj .
RUN dotnet restore

COPY src/${PROJECT_NAME}/ .
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

# Install Docker CLI (using static binary to avoid Debian version compatibility issues)
ARG DOCKER_VERSION=27.5.1
ARG TARGETARCH
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates && \
    ARCH=$(case ${TARGETARCH:-amd64} in amd64) echo x86_64;; arm64) echo aarch64;; *) echo ${TARGETARCH};; esac) && \
    curl -fsSL "https://download.docker.com/linux/static/stable/${ARCH}/docker-${DOCKER_VERSION}.tgz" | tar xz --strip-components=1 -C /usr/local/bin docker/docker && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

ENTRYPOINT dotnet ServerManagerDiscordBot.dll
