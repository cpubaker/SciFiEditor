namespace SciFiEditor.Core.Manuscript;

public sealed class WordCountCoordinator : IDisposable
{
    private readonly Func<Guid, int, int, Task> _persist;
    private readonly TimeSpan _debounceInterval;
    private readonly object _gate = new();

    private CancellationTokenSource? _pending;
    private Guid? _pendingNodeId;
    private string? _pendingContent;

    public WordCountCoordinator(Func<Guid, int, int, Task> persist, TimeSpan debounceInterval)
    {
        _persist = persist;
        _debounceInterval = debounceInterval;
    }

    public event Action<Guid, int, int>? Counted;

    public void NotifyChanged(Guid nodeId, string content)
    {
        CancellationToken token;
        lock (_gate)
        {
            _pending?.Cancel();
            _pending = new CancellationTokenSource();
            _pendingNodeId = nodeId;
            _pendingContent = content;
            token = _pending.Token;
        }

        _ = DebounceAndCountAsync(nodeId, content, token);
    }

    private async Task DebounceAndCountAsync(Guid nodeId, string content, CancellationToken token)
    {
        try
        {
            await Task.Delay(_debounceInterval, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await CountAndPersistAsync(nodeId, content);
    }

    public async Task FlushAsync()
    {
        Guid? nodeId;
        string? content;
        lock (_gate)
        {
            _pending?.Cancel();
            nodeId = _pendingNodeId;
            content = _pendingContent;
        }

        if (nodeId is not null && content is not null)
        {
            await CountAndPersistAsync(nodeId.Value, content);
        }
    }

    private async Task CountAndPersistAsync(Guid nodeId, string content)
    {
        var (words, chars) = await Task.Run(() => WordCountService.Count(content));
        await _persist(nodeId, words, chars);
        Counted?.Invoke(nodeId, words, chars);

        lock (_gate)
        {
            if (_pendingNodeId == nodeId)
            {
                _pendingNodeId = null;
                _pendingContent = null;
            }
        }
    }

    public void Dispose() => _pending?.Cancel();
}
