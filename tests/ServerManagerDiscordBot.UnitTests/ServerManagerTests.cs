using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

public class ServerManagerTests
{
    private readonly IOptions<AppSettings> _appSettings;
    private readonly IMemoryCache _memoryCache;
    private readonly IEnumerable<IServerInfoProvider> _serverInfoProviders;
    private readonly IServiceProvider _serviceProvider;

    public ServerManagerTests()
    {
        _appSettings = Options.Create(new AppSettings() {
            BotToken = "BotToken"
        });
        _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _serverInfoProviders = new List<IServerInfoProvider>();
        _serviceProvider = Mock.Of<IServiceProvider>();
    }

    [Fact]
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
        var serverManager = new ServerManager(_appSettings, memoryCacheMock.Object, _serverInfoProviders, _serviceProvider);

        // Act
        var result = await serverManager.GetServersAsync(cancellationToken);

        // Assert
        Assert.Equal(servers, result);
    }

    [Fact]
    public async Task GetServersAsync_ReturnsFreshServers()
    {
        // Arrange
        object? servers = new Dictionary<string, ServerInfo>
        {
            { "Server1", new ServerInfo() },
            { "Server2", new ServerInfo() }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(false);
        memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());
        var serverInfoProviderMock = new Mock<IServerInfoProvider>();
        serverInfoProviderMock.Setup(p => p.GetServerInfoAsync(cancellationToken)).ReturnsAsync((Dictionary<string, ServerInfo>)servers);
        var serverInfoProviders = new List<IServerInfoProvider> { serverInfoProviderMock.Object };
        var serverManager = new ServerManager(_appSettings, memoryCacheMock.Object, serverInfoProviders, _serviceProvider);

        // Act
        var result = await serverManager.GetServersAsync(cancellationToken);

        // Assert
        Assert.Equal(servers, result);
        memoryCacheMock.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetServerInfoAsync_ThrowsArgumentException_WhenServerNotFound()
    {
        // Arrange
        var serverName = "Server1";
        var cancellationToken = CancellationToken.None;
        var serverManager = new ServerManager(_appSettings, _memoryCache, _serverInfoProviders, _serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => serverManager.GetServerInfoAsync(serverName, cancellationToken));
    }
    
    [Fact]
    public async Task StartServerAsync_ThrowsArgumentException_WhenServerNotFound()
    {
        // Arrange
        var serverName = "Server1";
        var cancellationToken = CancellationToken.None;
        var serverManager = new ServerManager(_appSettings, _memoryCache, _serverInfoProviders, _serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => serverManager.StartServerAsync(serverName, cancellationToken: cancellationToken));
    }
}