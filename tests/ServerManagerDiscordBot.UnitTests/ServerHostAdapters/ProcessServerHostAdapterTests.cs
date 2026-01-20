using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;

namespace ServerManagerDiscordBot.ServerHostAdapters;

public class ProcessServerHostAdapterTests
{
    private static ProcessServerHostAdapter CreateAdapter(
        Mock<IProcessRunner> processRunnerMock,
        ProcessServerHostAdapterOptions? options = null,
        string fileName = "server.exe",
        string? arguments = null,
        string? workingDirectory = null)
    {
        options ??= new ProcessServerHostAdapterOptions();

        // Create properties dictionary for the ServerHostContext
        var propertiesDict = new Dictionary<string, string?>
        {
            { nameof(ProcessServerHostProperties.FileName), fileName },
            { nameof(ProcessServerHostProperties.Arguments), arguments },
            { nameof(ProcessServerHostProperties.WorkingDirectory), workingDirectory }
        };

        var adapter = new ProcessServerHostAdapter(
            Options.Create(options),
            processRunnerMock.Object);

        var baseContext = new ServerHostContext("test-server", "process", propertiesDict);
        adapter.Context = new ServerHostContext<ProcessServerHostProperties>(baseContext);

        return adapter;
    }

    private static Mock<IProcessHandle> CreateProcessHandleMock(
        string? mainModuleFileName = null,
        string standardOutput = "",
        string standardError = "")
    {
        var processHandleMock = new Mock<IProcessHandle>();
        processHandleMock.Setup(p => p.MainModuleFileName).Returns(mainModuleFileName);
        processHandleMock.Setup(p => p.StandardOutput)
            .Returns(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(standardOutput))));
        processHandleMock.Setup(p => p.StandardError)
            .Returns(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(standardError))));
        processHandleMock.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return processHandleMock;
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsRunning_WhenProcessExists()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(mainModuleFileName: "server.exe");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Running);
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsStopped_WhenNoProcessExists()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Stopped);
    }

    [Test]
    public async Task GetServerStatusAsync_ReturnsStopped_WhenProcessHasDifferentFileName()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(mainModuleFileName: "other.exe");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Stopped);
    }

    [Test]
    public async Task GetServerStatusAsync_ThrowsException_WhenMultipleProcessesMatch()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock1 = CreateProcessHandleMock(mainModuleFileName: "server.exe");
        var processHandleMock2 = CreateProcessHandleMock(mainModuleFileName: "server.exe");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock1.Object, processHandleMock2.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.GetServerStatusAsync());
    }

    [Test]
    public async Task StartServerAsync_StartsProcess_WhenNotRunning()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var startedProcessMock = new Mock<IProcessHandle>();
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([]);
        
        ProcessStartInfo? capturedStartInfo = null;
        processRunnerMock.Setup(p => p.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => capturedStartInfo = psi)
            .Returns(startedProcessMock.Object);

        var adapter = CreateAdapter(processRunnerMock, fileName: "myserver.exe", arguments: "--port 8080");

        // Act
        await adapter.StartServerAsync();

        // Assert
        await Assert.That(capturedStartInfo).IsNotNull();
        await Assert.That(capturedStartInfo!.FileName).IsEqualTo("myserver.exe");
        await Assert.That(capturedStartInfo.Arguments).IsEqualTo("--port 8080");
    }

    [Test]
    public async Task StartServerAsync_ThrowsException_WhenAlreadyRunning()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(mainModuleFileName: "server.exe");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.StartServerAsync());
    }

    [Test]
    public async Task StopServerAsync_KillsProcess_WhenRunning()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(mainModuleFileName: "server.exe");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        await adapter.StopServerAsync();

        // Assert
        processHandleMock.Verify(p => p.Kill(), Times.Once);
        processHandleMock.Verify(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StopServerAsync_ThrowsException_WhenNotRunning()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.StopServerAsync());
    }

    [Test]
    public async Task GetServerLogsAsync_ReturnsEmptyLogs_WhenNoProcessRunning()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerLogsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetServerLogsAsync_ReturnsOutputLogs_WhenProcessHasOutput()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(
            mainModuleFileName: "server.exe",
            standardOutput: "Server started on port 8080");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerLogsAsync();

        // Assert
        await Assert.That(result).ContainsKey("output");
    }

    [Test]
    public async Task GetServerLogsAsync_ReturnsErrorLogs_WhenProcessHasErrors()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(
            mainModuleFileName: "server.exe",
            standardError: "Error: Port already in use");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(processRunnerMock);

        // Act
        var result = await adapter.GetServerLogsAsync();

        // Assert
        await Assert.That(result).ContainsKey("error");
    }

    [Test]
    public async Task GetProcess_UsesWorkingDirectory_WhenConfigured()
    {
        // Arrange
        var processRunnerMock = new Mock<IProcessRunner>();
        var processHandleMock = CreateProcessHandleMock(mainModuleFileName: "/opt/servers/server.exe");
        
        processRunnerMock.Setup(p => p.GetProcessesByName(It.IsAny<string>()))
            .Returns([processHandleMock.Object]);

        var adapter = CreateAdapter(
            processRunnerMock, 
            fileName: "server.exe",
            workingDirectory: "/opt/servers");

        // Act
        var result = await adapter.GetServerStatusAsync();

        // Assert
        await Assert.That(result).IsEqualTo(ServerStatus.Running);
    }
}
