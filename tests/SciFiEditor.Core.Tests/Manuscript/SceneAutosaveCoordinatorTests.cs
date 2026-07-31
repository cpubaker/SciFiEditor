using FluentAssertions;
using SciFiEditor.Core.Manuscript;

namespace SciFiEditor.Core.Tests.Manuscript;

public class SceneAutosaveCoordinatorTests
{
    private static readonly TimeSpan ShortDebounce = TimeSpan.FromMilliseconds(30);
    private static readonly TimeSpan LongDebounce = TimeSpan.FromSeconds(5);

    private sealed class RecordingSaver
    {
        private readonly object _gate = new();
        public List<(Guid NodeId, string Content)> Calls { get; } = new();

        public Task Save(Guid nodeId, string content)
        {
            lock (_gate)
            {
                Calls.Add((nodeId, content));
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task NotifyChanged_SavesAfterDebounceInterval()
    {
        var saver = new RecordingSaver();
        var coordinator = new SceneAutosaveCoordinator(saver.Save, ShortDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "hello world");
        await Task.Delay(ShortDebounce + TimeSpan.FromMilliseconds(150));

        saver.Calls.Should().ContainSingle(c => c.NodeId == nodeId && c.Content == "hello world");
    }

    [Fact]
    public async Task NotifyChanged_RapidChanges_OnlySavesLatestContentOnce()
    {
        var saver = new RecordingSaver();
        var coordinator = new SceneAutosaveCoordinator(saver.Save, ShortDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "v1");
        coordinator.NotifyChanged(nodeId, "v2");
        coordinator.NotifyChanged(nodeId, "v3");
        await Task.Delay(ShortDebounce + TimeSpan.FromMilliseconds(150));

        saver.Calls.Should().ContainSingle();
        saver.Calls[0].Content.Should().Be("v3");
    }

    [Fact]
    public async Task FlushAsync_SavesImmediately_WithoutWaitingForDebounce()
    {
        var saver = new RecordingSaver();
        var coordinator = new SceneAutosaveCoordinator(saver.Save, LongDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "flush me");
        await coordinator.FlushAsync();

        saver.Calls.Should().ContainSingle(c => c.Content == "flush me");
    }

    [Fact]
    public async Task FlushAsync_PreventsLaterDebouncedDuplicateSave()
    {
        var saver = new RecordingSaver();
        var coordinator = new SceneAutosaveCoordinator(saver.Save, ShortDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "content");
        await coordinator.FlushAsync();
        await Task.Delay(ShortDebounce + TimeSpan.FromMilliseconds(150));

        saver.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task FlushAsync_DoesNothing_WhenNoPendingChange()
    {
        var saver = new RecordingSaver();
        var coordinator = new SceneAutosaveCoordinator(saver.Save, ShortDebounce);

        await coordinator.FlushAsync();

        saver.Calls.Should().BeEmpty();
    }
}
