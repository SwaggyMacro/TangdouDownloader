using Aprillz.MewUI.Platform;

namespace Aprillz.MewUI;

public partial class Window
{
    private readonly DispatcherMergeKey _commandStateMergeKey = new(DispatcherPriority.Background);
    private CommandStateTracker? _commandStateTracker;
    private CommandRouter? _commandRouter;
    private Action? _cachedCommandStatePass;

    /// <summary>
    /// Gets the command router owning target resolution and invocation for this window.
    /// </summary>
    public CommandRouter CommandRouter => _commandRouter ??= new CommandRouter(this);

    /// <summary>
    /// Returns the router without allocating one, for lookup paths that must not materialize state.
    /// </summary>
    internal CommandRouter? TryGetCommandRouter() => _commandRouter;

    internal CommandStateTracker CommandStateTracker => _commandStateTracker ??= new CommandStateTracker();

    internal void RegisterCommandSource(ICommandSource source) => CommandStateTracker.Register(source);

    internal void UnregisterCommandSource(ICommandSource source) => _commandStateTracker?.Unregister(source);

    /// <summary>
    /// Schedules a coalesced command state evaluation pass; multiple requests in one dispatcher
    /// turn merge into a single pass.
    /// </summary>
    internal void RequestCommandStateEvaluation()
    {
        if (_commandStateTracker == null || !_commandStateTracker.HasSources)
        {
            return;
        }

        var dispatcher = ApplicationDispatcher;
        if (dispatcher == null)
        {
            // Headless/pre-run: evaluate immediately so state stays observable without a dispatcher.
            EvaluateCommandStates();
            return;
        }

        _cachedCommandStatePass ??= EvaluateCommandStates;
        (dispatcher as IDispatcherCore)?.PostMerged(_commandStateMergeKey, _cachedCommandStatePass, DispatcherPriority.Background);
    }

    /// <summary>
    /// Runs one command state evaluation pass over the registered command sources.
    /// </summary>
    internal void EvaluateCommandStates() => _commandStateTracker?.EvaluateAll();

    internal void ClearCommandSources() => _commandStateTracker?.Clear();
}
