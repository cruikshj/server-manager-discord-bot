# Docker compose examples

The following examples show how to setup the bot application using Docker compose. As discussed in the README, configuration can be done in a variety of ways. These examples will use `appsettings.yaml` for configuration, but feel free to use environment variables, command line arguments, other file types or the config directory.

## Bot application

This example shows how to run the bot application using Docker compose.

_folder structure_
```
/some/folder
    docker-compose.yml
    appsettings.yaml
```

_docker-compose.yml_
```yaml
name: server-manager-discord-bot
services:
  smdb:
    image: ghcr.io/cruikshj/server-manager-discord-bot
    volumes:
    - /some/folder/appsettings.yaml:/app/appsettings.yaml
```

_appsettings.yaml_
```yaml
BotToken: "<your token from Discord>"
Servers:
  "minecraft-1":
    Game: Minecraft (Bedrock)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:12345
      Mode: Survival
  "minecraft-2":
    Game: Minecraft (Java)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:54321
      Mode: Creative
      Version: 1.12.2
    Readme: |
      # Minecraft
      This is a Minecraft server.
```

Run bot application with `docker compose up`.
```sh
cd /some/folder
docker compose up
```

## Docker compose integration

This example expands on the above example to show how the bot can manage dedicated servers using docker compose.

_folder structure_
```
/some/folder
    smdb.yml
    appsettings.yaml

/other/folder
    minecraft-1.yml        
```

_smdb.yml_
```yaml
name: server-manager-discord-bot
services:
  smdb:
    image: ghcr.io/cruikshj/server-manager-discord-bot
    volumes:
    - /some/folder/appsettings.yaml:/app/appsettings.yaml
    - /other/folder:/mnt/folder
    - /var/run/docker.sock:/var/run/docker.sock
```

_appsettings.yaml_
```yaml
BotToken: "<your token from Discord>"
ServerHostAdapters:
  DockerCompose:
    Type: DockerCompose
Servers:
  "minecraft-1":
    Game: Minecraft (Bedrock)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:12345
      Mode: Survival
    HostAdapter: DockerCompose
    HostProperties:
      DockerComposeFilePath: /mnt/folder/minecraft-1.yml
```

_minecraft-1.yml_
```yaml
name: minecraft-1
services:
  bds:
    image: itzg/minecraft-bedrock-server
    environment:
      EULA: "TRUE"
      GAMEMODE: survival
      DIFFICULTY: normal
    ports:
      - "12345:19132/udp"
    volumes:
      - bds:/data
    stdin_open: true
    tty: true
volumes:
  bds: {}
```

Run bot application with `docker compose up`.
```sh
cd /some/folder
docker compose -f smdb.yml up
```