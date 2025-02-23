# Server Manager Discord Bot

This repository represents a Discord Bot application for managing dedicated game servers.  It is intended to be self-hosted, and configured to need.  There is no SaaS instance available.

## Features

- Displays list of configured servers

  ![list](https://raw.githubusercontent.com/cruikshj/server-manager-discord-bot/main/assets/servers-list.png)

- Displays detailed server information

  ![info](https://raw.githubusercontent.com/cruikshj/server-manager-discord-bot/main/assets/servers-info.png)

- Provides server readme
- Provides server files (such as backups, saves and mod files)
- Provides a per server gallery for screenshots and other images
- Process, DockerCompose, and Kubernetes integrations
    - Show server status
    - Start/Stop/Restart servers
    - Provides server logs

## Commands

- `/sm list`
- `/sm info`
- `/sm status`
- `/sm start`
- `/sm restart`
- `/sm stop`
- `/sm logs`
- `/sm files`
- `/sm readme`
- `/sm gallery`
- `/sm gallery-upload`

## Setup

### Bot Registration

In order to self-host this bot application, you will need to register an application and bot with Discord at https://discord.com/developers.  The configuration for this bot should be simple as it requires no special permissions, intents or scopes other than `applications.commands`. Once your bot is registered, you should have a bot token, which the application needs to communicate through Discord.

### Installation

#### Docker

This bot application is distributed as a Docker image and therefore can be hosted in a variety of ways, such as Docker, Docker Compose, Kubernetes, etc.  This application communicates with Discord over a socket connect and virtually all the functionality is available without exposing any ports or endpoints. One optional feature enabling large file downloads would require exposure, but more on that in the configuration section.
 
The Docker image is available here: [server-manager-discord-bot](https://github.com/cruikshj/server-manager-discord-bot/pkgs/container/server-manager-discord-bot)

#### Executable

This bot application is distributed as a self-contained executable.  The executable is available as an asset on [releases](https://github.com/cruikshj/server-manager-discord-bot/releases).

### Configuration

Configuration of the bot application can be done in a variety of ways. The application uses `Microsoft.Extensions.Configuration` with the [WebApplication.CreateBuilder defaults](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0#default-application-configuration-sources), plus `appsettings.yml` support, `SERVERMANAGER_` environment variable prefix support and support for reading all `.json` and `.yaml` files from a `Config` directory. You can learn how to use standard configuration providers [here](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers).  You may even mix and match these forms of configuration to fit your needs. When using file based configuration, you will need to mount the configuration files into your container. 

#### Examples

Each example will show how to host the bot application itself as well as integrating with the hosting platform to control dedicated servers. The bot application does not have to be hosted the same way as the dedicated servers. Feel free to host the bot application however you see fit.

- [Process](examples/process.md)
- [Docker Compose](examples/docker-compose.md)
- [Kubernetes](examples/kubernetes.md)

#### All Settings

| Section | Description | Default |
|---|---|---|
| BotToken | (Required) The Discord Bot token. |  |
| GuildIds | An array of Discord guild (server) IDs. This can be used for testing or security purposes to limit which Discord servers the bot application will communicate with. | |
| HostUri | The bot application host URI. Only used if `LargeFileDownloadHandler.BuiltIn` is used. | https://localhost:5000 |
| SlashCommandPrefix | The command group or prefix to use for the bot. | sm |
| EnableFileDownloadHandler | This is a global setting for whether to enable the file downloads feature.  Configured servers must still opt-in by providing a `FilesPath` value. | true |
| LargeFileDownloadHandler | Enable file downloads larger than 25MB. See [Handling Large File Downloads](#handlinglargefiledownloads). | Disabled |
| EnableGallery | Enable server gallery. | true |
| EnableGalleryUploads | Enable uploads to the gallery. | false |
| GalleryFileExtensions | Gallery file extensions. | [ png, jpg, jpeg, gif, webp ] |
| ServersCacheExpiration | The server info cache expiration. | 5 minutes |
| DownloadLinkExpiration | The lifetime of a large file download link. | 24 hours |
| ServerStatusWaitTimeout | The timeout for waiting for server status after a start or stop interaction. | 10 minutes |
| ServerInfoProviders | This is a collection additional of `ServerInfoProvider` implementations. All providers will be used to to retrieve server info. A configuration based provider is always used, which reads the `Servers` section of configuration. |  |
| ServerHostAdapters | This is a keyed collection of `ServerHostAdapter` implementations. These adapters allow for servers to be controlled and logs retrieved from a specific host. | |
| Servers | This is keyed collection of servers for the bot to represent and control. Additional servers can be provided with `ServerInfoProviders`. | |

##### ServerInfoProviders

| Section | Description | Default |
|---|---|---|
| Type | (Required) The type of the provider.  Can be `KubernetesConfigMap`. |  |
| KubeConfigPath (KubernetesConfigMap) | The path to a Kube Config file to use to connect to Kubernetes. If not provided, `InCluster` configuration will be used. | |
| LabelSelector (KubernetesConfigMap) | A Kubernetes label selector for finding `ConfigMap` objects to read. | server-manager=default |

##### ServerHostAdapters

| Section | Description | Default |
|---|---|---|
| -Key- | (Required) The section key or name is used to lookup the adapter matching the `HostAdapter` value on server info. | |
| Type | (Required) The type of the adapter.  Can be `Process`, `DockerCompose`, or `Kubernetes`. | |
| DockerProcessFilePath (DockerCompose) | The file name of the docker executable. | docker |
| DockerHost (DockerCompose) | Daemon socket to connect to.  | |
| KubeConfigPath (Kubernetes) | The path to a Kube Config file to use to connect to Kubernetes. If not provided, `InCluster` configuration will be used. | |

##### Servers

| Section | Description | Default |
|---|---|---|
| -Key- | (Required) The section key or name is used as the name of the server for the purposes of the bot interactions. | |
| Game | The display name of the game. | |
| Icon | The icon of the game. | |
| Readme | A multiline text field that can be provided by bot interaction. This value supports string formatting using the server info object in the form of "{Game}" or "{Fields.Whatever}" | |
| FilesPath | This is a path to the files directory to be used to server files through interactions for this server. It should be a file path a mount drive for the bot container. The idea here is to utilize existing volume mount capabilities of hosting platforms like Docker and Kubernetes, to mount in whatever is necessary, such as an S3 bucket or NFS or host drive. | |
| Fields | This is a map of free form fields, or key value pairs to display as part of the server info. | |
| HostAdapterName | The name of the `ServerHostAdapter` to use for this server. | |
| HostProperties | The identifier to pass to the adapter. The child properties are determined by the adapter. |  |
| HostProperties.FileName (Process) | The process filename. | |
| HostProperties.Arguments (Process) | The process arguments. | |
| HostProperties.WorkingDirectory (Process) | The working directory to use when starting the process. | |
| HostProperties.DockerComposeFilePath (DockerCompose) | The file path to the docker compose configuration file for the server. | |
| HostProperties.Kind (Kubernetes) | The workload kind. Can be `Deployment` or `StatefulSet`. | |
| HostProperties.Namespace (Kubernetes) | The workload namespace. | |
| HostProperties.Name (Kubernetes) | The workload name. | |

#### Handling Large File Downloads
<a name="handlinglargefiledownloads"></a>

Files 25MB or less will be sent via Discord interactions. Files greater than this limit cannot be sent via Discord. The `LargeFileDownloadHandler` is intended to provide an alternative download link in these cases. The current options are `Disabled` and `BuiltIn`. 

##### BuiltIn Handler

```yaml
HostUri: https://smdb.example.com/
LargeFileDownloadHandler: BuiltIn
DownloadLinkExpiration: "24:00:00"
```

The `BuiltInLargeFileDownloadHandler` will create a temporary download link using the `HostUri` and a guid and make it available to download a file for the `DownloadLinkExpiration`. If this feature is used, HTTP traffic to the app will need to be exposed to your end users in some way.  This is the only endpoint exposed by the bot. If this feature is not used, no exposue is necessary.

## Contribution

This repository is open to contribution. I tried to decouple many of the opinionated features (such as Kubernetes integrations) and allow for extensibility.

## Credits

This bot was built using the following open-source projects and of course many more directly and indirectly.

- [.NET](https://github.com/dotnet)
- [Discord.NET](https://github.com/discord-net/Discord.Net)
- [KubernetesClient](https://github.com/kubernetes-client/csharp)
- [NetEscapades.Configuration](https://github.com/andrewlock/NetEscapades.Configuration)
- [SmartFormat](https://github.com/axuno/SmartFormat)
- [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- [Docker](https://github.com/docker)