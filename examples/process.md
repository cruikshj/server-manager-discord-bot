# Process examples

The following examples show how to setup the bot application using the executable process. As discussed in the README, configuration can be done in a variety of ways. These examples will use `appsettings.yaml` for configuration, but feel free to use environment variables, command line arguments, other file types or the config directory.

## Bot application

This example shows how to run the bot application executable.

_folder structure (windows)_
```
C:\some\folder
    ServerManagerDiscordBot.exe
    appsettings.yaml
```

_folder structure (linux)_
```
/some/folder
    ServerManagerDiscordBot
    appsettings.yaml
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

Simply start the executable.  It connect to the Discord gateway and remain running until stopped.  Consider using operating system features to run as a service.

## Process integration

This example expands on the above example to show how the bot can managed a dedicated server process.

_folder structure (windows)_
```
C:\some\folder
    ServerManagerDiscordBot.exe
    appsettings.yaml

C:\other\folder
    \minecraft-1
        bedrock_server.exe
        ...
```

_appsettings.yaml_
```yaml
BotToken: "<your token from Discord>"
ServerHostAdapters:
  Process:
    Type: Process
Servers:
  "minecraft-1":
    Game: Minecraft (Bedrock)
    Icon: https://cdn2.steamgriddb.com/icon_thumb/4a5b76e7170df685ed8b75c7dacce268.png
    Fields:
      Address: example.com:12345
      Mode: Survival
    HostAdapterName: Process
    HostProperties:
      FileName: "C:\\other\\folder\\minecraft-1\\bedrock_server.exe"
```
