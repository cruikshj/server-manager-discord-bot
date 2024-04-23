FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /source

ARG PROJECT_NAME=ServerManager.DiscordBot

COPY src/${PROJECT_NAME}/${PROJECT_NAME}.csproj .
RUN dotnet restore

COPY src/${PROJECT_NAME}/ .
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT dotnet ServerManager.DiscordBot.dll