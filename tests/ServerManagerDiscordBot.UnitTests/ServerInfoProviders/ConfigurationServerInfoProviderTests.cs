using Microsoft.Extensions.Configuration;

namespace ServerManagerDiscordBot.ServerInfoProviders;

public class ConfigurationServerInfoProviderTests
{
    [Test]
    public async Task GetServerInfoAsync_ReturnsEmptyDictionary_WhenNoServersConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetServerInfoAsync_ReturnsServers_WhenConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:Server1:Game", "Minecraft" },
                { "Servers:Server1:HostAdapterName", "docker-compose" },
                { "Servers:Server2:Game", "Valheim" },
                { "Servers:Server2:HostAdapterName", "kubernetes" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result).ContainsKey("Server1");
        await Assert.That(result).ContainsKey("Server2");
    }

    [Test]
    public async Task GetServerInfoAsync_BindsGameProperty_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:TestServer:Game", "Terraria" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result["TestServer"].Game).IsEqualTo("Terraria");
    }

    [Test]
    public async Task GetServerInfoAsync_BindsHostAdapterName_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:TestServer:HostAdapterName", "process-adapter" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result["TestServer"].HostAdapterName).IsEqualTo("process-adapter");
    }

    [Test]
    public async Task GetServerInfoAsync_BindsFilesPath_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:TestServer:FilesPath", "/data/server/files" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result["TestServer"].FilesPath).IsEqualTo("/data/server/files");
    }

    [Test]
    public async Task GetServerInfoAsync_BindsGalleryPath_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:TestServer:GalleryPath", "/data/server/gallery" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result["TestServer"].GalleryPath).IsEqualTo("/data/server/gallery");
    }

    [Test]
    public async Task GetServerInfoAsync_BindsIcon_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:TestServer:Icon", "https://example.com/icon.png" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result["TestServer"].Icon).IsEqualTo("https://example.com/icon.png");
    }

    [Test]
    public async Task GetServerInfoAsync_BindsReadme_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:TestServer:Readme", "This is the server readme." }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result["TestServer"].Readme).IsEqualTo("This is the server readme.");
    }

    [Test]
    public async Task GetServerInfoAsync_UsesConfigurationSectionName()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:MyServer:Game", "Test" },
                { "OtherSection:NotAServer:Game", "ShouldNotBeIncluded" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result).ContainsKey("MyServer");
    }

    [Test]
    public async Task GetServerInfoAsync_HandlesMultipleServers()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Servers:Alpha:Game", "Game1" },
                { "Servers:Beta:Game", "Game2" },
                { "Servers:Gamma:Game", "Game3" },
                { "Servers:Delta:Game", "Game4" }
            })
            .Build();

        var provider = new ConfigurationServerInfoProvider(configuration);

        // Act
        var result = await provider.GetServerInfoAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(4);
    }

    [Test]
    public async Task ConfigurationSectionName_IsServers()
    {
        // Assert
        await Assert.That(ConfigurationServerInfoProvider.ConfigurationSectionName).IsEqualTo("Servers");
    }
}
