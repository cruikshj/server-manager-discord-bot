namespace ServerManagerDiscordBot;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

public class ServerManagerTests
{
    private IOptions<AppSettings> GetAppSettings()
    {
        return Options.Create(new AppSettings() {
            BotToken = "BotToken",
            ServersCacheExpiration = TimeSpan.FromMinutes(5),
            ServerStatusWaitTimeout = TimeSpan.FromMinutes(10)
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

    #region GetServersAsync Tests

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
    public async Task GetServersAsync_MergesServersFromMultipleProviders()
    {
        // Arrange
        var provider1Servers = new Dictionary<string, ServerInfo>
        {
            { "Server1", new ServerInfo { Game = "Game1" } }
        };
        var provider2Servers = new Dictionary<string, ServerInfo>
        {
            { "Server2", new ServerInfo { Game = "Game2" } }
        };
        object? cachedServers = null;
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out cachedServers)).Returns(false);
        memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());
        
        var serverInfoProvider1Mock = new Mock<IServerInfoProvider>();
        serverInfoProvider1Mock.Setup(p => p.GetServerInfoAsync(cancellationToken)).ReturnsAsync(provider1Servers);
        var serverInfoProvider2Mock = new Mock<IServerInfoProvider>();
        serverInfoProvider2Mock.Setup(p => p.GetServerInfoAsync(cancellationToken)).ReturnsAsync(provider2Servers);
        
        var serverInfoProviders = new List<IServerInfoProvider> 
        { 
            serverInfoProvider1Mock.Object, 
            serverInfoProvider2Mock.Object 
        };
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, serverInfoProviders, GetServiceProvider());

        // Act
        var result = await serverManager.GetServersAsync(cancellationToken);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result).ContainsKey("Server1");
        await Assert.That(result).ContainsKey("Server2");
    }

    #endregion

    #region GetServerInfoAsync Tests

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
    public async Task GetServerInfoAsync_ReturnsServerInfo_WhenServerExists()
    {
        // Arrange
        var serverName = "TestServer";
        var expectedServerInfo = new ServerInfo { Game = "TestGame" };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, expectedServerInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act
        var result = await serverManager.GetServerInfoAsync(serverName, cancellationToken);

        // Assert
        await Assert.That(result).IsSameReferenceAs(expectedServerInfo);
    }

    #endregion
    
    #region StartServerAsync Tests

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

    [Test]
    public async Task StartServerAsync_ThrowsInvalidOperationException_WhenNoHostAdapter()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { HostAdapterName = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.StartServerAsync(serverName, cancellationToken: cancellationToken));
    }

    #endregion

    #region StopServerAsync Tests

    [Test]
    public async Task StopServerAsync_ThrowsArgumentException_WhenServerNotFound()
    {
        // Arrange
        var serverName = "NonExistentServer";
        var cancellationToken = CancellationToken.None;
        var serverManager = new ServerManager(GetAppSettings(), GetMemoryCache(), GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await serverManager.StopServerAsync(serverName, cancellationToken: cancellationToken));
    }

    [Test]
    public async Task StopServerAsync_ThrowsInvalidOperationException_WhenNoHostAdapter()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { HostAdapterName = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.StopServerAsync(serverName, cancellationToken: cancellationToken));
    }

    #endregion

    #region GetServerStatusAsync Tests

    [Test]
    public async Task GetServerStatusAsync_ReturnsUnknown_WhenNoHostAdapter()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { HostAdapterName = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act
        var result = await serverManager.GetServerStatusAsync(serverName, cancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Unknown);
    }

    #endregion

    #region GetServerFilesAsync Tests

    [Test]
    public async Task GetServerFilesAsync_ThrowsInvalidOperationException_WhenFilesPathNotConfigured()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { FilesPath = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.GetServerFilesAsync(serverName, cancellationToken));
    }

    #endregion

    #region GetServerFileAsync Tests

    [Test]
    public async Task GetServerFileAsync_ThrowsInvalidOperationException_WhenFilesPathNotConfigured()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { FilesPath = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.GetServerFileAsync(serverName, "test.txt", cancellationToken));
    }

    #endregion

    #region GetServerGalleryFilesAsync Tests

    [Test]
    public async Task GetServerGalleryFilesAsync_ThrowsInvalidOperationException_WhenGalleryPathNotConfigured()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { GalleryPath = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.GetServerGalleryFilesAsync(serverName, cancellationToken));
    }

    #endregion

    #region UploadServerGalleryFileAsync Tests

    [Test]
    public async Task UploadServerGalleryFileAsync_ThrowsInvalidOperationException_WhenGalleryPathNotConfigured()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { GalleryPath = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.UploadServerGalleryFileAsync(serverName, "http://example.com/image.png", "image.png", cancellationToken));
    }

    #endregion

    #region GetServerLogsAsync Tests

    [Test]
    public async Task GetServerLogsAsync_ThrowsInvalidOperationException_WhenNoHostAdapter()
    {
        // Arrange
        var serverName = "TestServer";
        var serverInfo = new ServerInfo { HostAdapterName = null };
        object? servers = new Dictionary<string, ServerInfo>
        {
            { serverName, serverInfo }
        };
        var cancellationToken = CancellationToken.None;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out servers)).Returns(true);
        var serverManager = new ServerManager(GetAppSettings(), memoryCacheMock.Object, GetServerInfoProviders(), GetServiceProvider());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await serverManager.GetServerLogsAsync(serverName, cancellationToken));
    }

    #endregion
}
