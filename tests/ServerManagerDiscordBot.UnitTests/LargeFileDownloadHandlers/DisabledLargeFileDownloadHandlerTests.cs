namespace ServerManagerDiscordBot.LargeFileDownloadHandlers;

public class DisabledLargeFileDownloadHandlerTests
{
    [Test]
    public async Task GetDownloadUrlAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var handler = new DisabledLargeFileDownloadHandler();
        var fileInfo = new FileInfo(Path.GetTempFileName());

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await handler.GetDownloadUrlAsync(fileInfo));
        }
        finally
        {
            // Cleanup
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }
    }

    [Test]
    public async Task GetDownloadUrlAsync_ThrowsNotSupportedException_WithCorrectMessage()
    {
        // Arrange
        var handler = new DisabledLargeFileDownloadHandler();
        var fileInfo = new FileInfo(Path.GetTempFileName());

        try
        {
            // Act
            Exception? caughtException = null;
            try
            {
                await handler.GetDownloadUrlAsync(fileInfo);
            }
            catch (NotSupportedException ex)
            {
                caughtException = ex;
            }

            // Assert
            await Assert.That(caughtException).IsNotNull();
            await Assert.That(caughtException!.Message).Contains("disabled");
        }
        finally
        {
            // Cleanup
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }
    }
}
