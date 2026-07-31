namespace SciFiEditor.Core.Manuscript;

public sealed class SceneAutosaveCoordinator : IDisposable
{
    private readonly Func<Guid, string, Task> _save;
    private readonly TimeSpan _debounceInterval;
    private readonly object _gate = new();

    private CancellationTokenSource? _pending;
    private Guid? _pendingNodeId;
    private string? _pendingContent;

    public SceneAutosaveCoordinator(Func<Guid, string, Task> save, TimeSpan debounceInterval)
    {
        _save = save;
        _debounceInterval = debounceInterval;
    }

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

        _ = DebounceAndSaveAsync(nodeId, content, token);
    }

    private async Task DebounceAndSaveAsync(Guid nodeId, string content, CancellationToken token)
    {
        try
        {
            await Task.Delay(_debounceInterval, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await SaveAndClearPendingAsync(nodeId, content);
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
            await SaveAndClearPendingAsync(nodeId.Value, content);
        }
    }

    private async Task SaveAndClearPendingAsync(Guid nodeId, string content)
    {
        await _save(nodeId, content);
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
