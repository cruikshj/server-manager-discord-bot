ARG DOTNET_VERSION=8.0
ARG PROJECT_NAME=ServerManager.DiscordBot

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} as build
WORKDIR /source

COPY src/${PROJECT_NAME}/${PROJECT_NAME}.csproj .
RUN dotnet restore

COPY src/${PROJECT_NAME}/ .
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "${PROJECT_NAME}.dll"]