namespace ServerManagerDiscordBot.Extensions;

public class DisposableExtensionsTests
{
    private class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }
        
        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    [Test]
    public async Task DisposeAll_DisposesAllItems()
    {
        // Arrange
        var disposables = new List<TestDisposable>
        {
            new TestDisposable(),
            new TestDisposable(),
            new TestDisposable()
        };

        // Act
        ((IEnumerable<IDisposable>)disposables).DisposeAll();

        // Assert
        foreach (var disposable in disposables)
        {
            await Assert.That(disposable.IsDisposed).IsTrue();
        }
    }

    [Test]
    public async Task DisposeAll_HandlesEmptyCollection()
    {
        // Arrange
        var disposables = new List<IDisposable>();

        // Act - should not throw
        disposables.DisposeAll();

        // Assert
        await Assert.That(disposables.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DisposeAll_HandlesSingleItem()
    {
        // Arrange
        var disposable = new TestDisposable();
        var disposables = new List<IDisposable> { disposable };

        // Act
        disposables.DisposeAll();

        // Assert
        await Assert.That(disposable.IsDisposed).IsTrue();
    }
}
