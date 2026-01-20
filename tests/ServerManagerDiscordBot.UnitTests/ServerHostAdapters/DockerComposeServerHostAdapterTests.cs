using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;

namespace ServerManagerDiscordBot.ServerHostAdapters;

public class DockerComposeServerHostAdapterTests
{
    private static DockerComposeServerHostAdapter CreateAdapter(
        Mock<IProcessRunner> processRunnerMock,
        DockerComposeServerHostAdapterOptions? options = null,
        string dockerComposeFilePath = "/path/to/docker-compose.yml")
    {
        options ??= new DockerComposeServerHostAdapterOptions
        {
            DockerProcessFilePath = "docker"
        };

        // Create properties dictionary for the ServerHostContext
        var propertiesDict = new Dictionary<string, string?>
        {
            { nameof(DockerComposeServerHostProperties.DockerComposeFilePath), dockerComposeFilePath }
        };

        var adapter = new DockerComposeServerHostAdapter(
            Options.Create(options),
            processRunnerMock.Object);

        var baseContext = new ServerHostContext("test-server", "docker-compose", propertiesDict);
        adapter.Context = new ServerHostContext<DockerComposeServerHostProperties>(baseContext);

        return adapter;
    }

    private static Mock<IProcessHandle> CreateProcessHandleMock(
        int exitCode = 0,
        string standardOutput = "",
        string standardError = "")
    {
        var processHandleMock = new Mock<IProcessHandle>();
        processHandleMock.Setup(p => p.ExitCode).Returns(exitCode);
        processHandleMock.Setup(p => p.StandardOutput)
            .Returns(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(standardOutput))));
        processHandleMock.Setup(p => p.StandardError)
            .Returns(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(standardError))));
        processHandleMock.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return processHandleMock;
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsRunning_WhenStatusIsUp()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: "Up 2 hours");
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Running);
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsStarting_WhenStatusIsCreated()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: "Created");
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Starting);
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsStopped_WhenStatusIsOther()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: "Exited");
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Stopped);
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsStopped_WhenOutputIsEmpty()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: "");
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Stopped);
    }

    [Test]
    public async Task GetServerStatusAsync_ThrowsException_WhenProcessFails()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 1, standardOutput: "");
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await adapter.GetServerStatusAsync());
    }

    [Test]
    public async Task StartServerAsync_CallsDockerComposeUp()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0);
        
        ProcessStartInfo? capturedStartInfo = null;
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => capturedStartInfo = psi)
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        await adapter.StartServerAsync();

        // Assert
        await Assert.That(capturedStartInfo).IsNotNull();
        await Assert.That(capturedStartInfo!.Arguments).Contains("up -d");
    }

    [Test]
    public async Task StartServerAsync_ThrowsException_WhenProcessFails()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 1);
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await adapter.StartServerAsync());
    }

    [Test]
    public async Task StopServerAsync_CallsDockerComposeDown()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0);
        
        ProcessStartInfo? capturedStartInfo = null;
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => capturedStartInfo = psi)
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        await adapter.StopServerAsync();

        // Assert
        await Assert.That(capturedStartInfo).IsNotNull();
        await Assert.That(capturedStartInfo!.Arguments).Contains("down");
    }

    [Test]
    public async Task StopServerAsync_ThrowsException_WhenProcessFails()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 1);
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await adapter.StopServerAsync());
    }

    [Test]
    public async Task GetServerLogsAsync_ReturnsLogStream()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var expectedLogs = "Container logs here";
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: expectedLogs);
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerLogsAsync();

        // Assert
        await Assert.That(result).ContainsKey("output");
        using var reader = new StreamReader(result["output"]);
        var logContent = await reader.ReadToEndAsync();
        await Assert.That(logContent).IsEqualTo(expectedLogs);
    }

    [Test]
    public async Task GetServerLogsAsync_ThrowsException_WhenProcessFails()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 1);
        
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await adapter.GetServerLogsAsync());
    }

    [Test]
    public async Task ProcessArguments_IncludesDockerHost_WhenConfigured()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: "Up");
        
        ProcessStartInfo? capturedStartInfo = null;
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => capturedStartInfo = psi)
            .Returns(processHandleMock.Object);

        var options = new DockerComposeServerHostAdapterOptions
        {
            DockerProcessFilePath = "docker",
            DockerHost = "tcp://remote:2375"
        };

        var adapter = CreateAdapter(processRunnerMock, options);

        // Act
        await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(capturedStartInfo).IsNotNull();
        await Assert.That(capturedStartInfo!.Arguments).Contains("-H tcp://remote:2375");
    }

    [Test]
    public async Task ProcessArguments_IncludesDockerComposeFilePath()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(exitCode: 0, standardOutput: "Up");
        
        ProcessStartInfo? capturedStartInfo = null;
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => capturedStartInfo = psi)
            .Returns(processHandleMock.Object);

        var adapter = CreateAdapter(processRunnerMock, dockerComposeFilePath: "/custom/path/compose.yml");

        // Act
        await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(capturedStartInfo).IsNotNull();
        await Assert.That(capturedStartInfo!.Arguments).Contains("--file /custom/path/compose.yml");
    }
}
