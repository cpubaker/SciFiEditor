using FluentAssertions;
using SciFiEditor.Core.Manuscript;

namespace SciFiEditor.Core.Tests.Manuscript;

public class WordCountCoordinatorTests
{
    private static readonly TimeSpan ShortDebounce = TimeSpan.FromMilliseconds(30);
    private static readonly TimeSpan LongDebounce = TimeSpan.FromSeconds(5);

    private sealed class RecordingPersister
    {
        private readonly object _gate = new();
        public List<(Guid NodeId, int Words, int Chars)> Calls { get; } = new();

        public Task Persist(Guid nodeId, int words, int chars)
        {
            lock (_gate)
            {
                Calls.Add((nodeId, words, chars));
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task NotifyChanged_ComputesAndPersistsCounts_AfterDebounceInterval()
    {
        var persister = new RecordingPersister();
        var coordinator = new WordCountCoordinator(persister.Persist, ShortDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "one two three");
        await Task.Delay(ShortDebounce + TimeSpan.FromMilliseconds(150));

        persister.Calls.Should().ContainSingle(c => c.NodeId == nodeId && c.Words == 3 && c.Chars == "one two three".Length);
    }

    [Fact]
    public async Task NotifyChanged_RapidChanges_OnlyPersistsLatestContentOnce()
    {
        var persister = new RecordingPersister();
        var coordinator = new WordCountCoordinator(persister.Persist, ShortDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "one");
        coordinator.NotifyChanged(nodeId, "one two");
        coordinator.NotifyChanged(nodeId, "one two three");
        await Task.Delay(ShortDebounce + TimeSpan.FromMilliseconds(150));

        persister.Calls.Should().ContainSingle();
        persister.Calls[0].Words.Should().Be(3);
    }

    [Fact]
    public async Task FlushAsync_PersistsImmediately_WithoutWaitingForDebounce()
    {
        var persister = new RecordingPersister();
        var coordinator = new WordCountCoordinator(persister.Persist, LongDebounce);
        var nodeId = Guid.NewGuid();

        coordinator.NotifyChanged(nodeId, "flush this now");
        await coordinator.FlushAsync();

        persister.Calls.Should().ContainSingle(c => c.Words == 3);
    }

    [Fact]
    public async Task FlushAsync_DoesNothing_WhenNoPendingChange()
    {
        var persister = new RecordingPersister();
        var coordinator = new WordCountCoordinator(persister.Persist, ShortDebounce);

        await coordinator.FlushAsync();

        persister.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Counted_EventFires_WithComputedValues()
    {
        var persister = new RecordingPersister();
        var coordinator = new WordCountCoordinator(persister.Persist, ShortDebounce);
        var nodeId = Guid.NewGuid();
        (Guid NodeId, int Words, int Chars)? received = null;

        coordinator.Counted += (id, words, chars) => received = (id, words, chars);
        coordinator.NotifyChanged(nodeId, "alpha beta");
        await Task.Delay(ShortDebounce + TimeSpan.FromMilliseconds(150));

        received.Should().NotBeNull();
        received!.Value.NodeId.Should().Be(nodeId);
        received.Value.Words.Should().Be(2);
    }
}
