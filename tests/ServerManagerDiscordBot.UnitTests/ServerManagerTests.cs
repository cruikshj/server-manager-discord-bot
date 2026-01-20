using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

public class ServerManagerTests
{
    private IOptions<AppSettings> GetAppSettings()
    {
        return Options.Create(new AppSettings() {
            BotToken = "BotToken"
        });
    }

    private IMemoryCache GetMemoryCache()
    {
        return new MemoryCache(Options.Create(new MemoryCacheOptions()));
    }

    private IEnumerable<IServerInfoProvider> GetServerInfoProviders()
    {
        return new List<IServerInfoProvider>();
    }

    private IServiceProvider GetServiceProvider()
    {
        return Mock.Of<IServiceProvider>();
    }

    [Test]
    public async Task GetServersAsync_ReturnsCachedServers()
    {
        // Arrange
        object? servers = new Dictionary<string, ServerInfo>
        {
            { "Server1", new ServerInfo() },
            { "Server2", new ServerInfo() }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act
        var result = await serverManager.GetServersAsync(cancellationToken);

        // Assert
        await Assert.That(result).IsSameReferenceAs(servers);
    }

    [Test]
    public async Task GetServersAsync_ReturnsFreshServers()
    {
        // Arrange
        var serverInfo1 = new ServerInfo();
        var serverInfo2 = new ServerInfo();
        var providerServers = new Dictionary<string, ServerInfo>
        {
            { "Server1", serverInfo1 },
            { "Server2", serverInfo2 }
        };
        object? cachedServers = null;
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out cachedServers)).Returns(false);
        memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());
        var serverInfoProviderMock = new Mock<IServerInfoProvider>();
        serverInfoProviderMock.Setup(p => p.GetServerInfoAsync(cancellationToken)).ReturnsAsync(providerServers);
        var serverInfoProviders = new List<IServerInfoProvider> { serverInfoProviderMock.Object };
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, serverInfoProviders, GetServiceProvider());

        // Act
        var result = await serverManager.GetServersAsync(cancellationToken);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result["Server1"]).IsSameReferenceAs(serverInfo1);
        await Assert.That(result["Server2"]).IsSameReferenceAs(serverInfo2);
        memoryCacheMock.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
    }

    [Test]
    public async Task GetServerInfoAsync_ThrowsArgumentException_WhenServerNotFound()
    {
        // Arrange
        var serverName = "Server1";
        var cancellationToken = CancellationToken.None;
        var serverManager = new ServerManager(GetAppSettings(), GetMemoryCache(), GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await serverManager.GetServerInfoAsync(serverName, cancellationToken));
    }
    
    [Test]
    public async Task StartServerAsync_ThrowsArgumentException_WhenServerNotFound()
    {
        // Arrange
        var serverName = "Server1";
        var cancellationToken = CancellationToken.None;
        var serverManager = new ServerManager(GetAppSettings(), GetMemoryCache(), GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await serverManager.StartServerAsync(serverName, cancellationToken: cancellationToken));
    }
}
