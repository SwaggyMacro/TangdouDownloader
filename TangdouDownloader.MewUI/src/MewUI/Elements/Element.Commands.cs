namespace Aprillz.MewUI.Controls;

public abstract partial class Element
{
    private CommandScope? _commandScope;
    private InputMap? _elementInputMap;

    /// <summary>
    /// Gets this element's command handler scope, creating it on first access.
    /// </summary>
    public CommandScope Commands => _commandScope ??= new CommandScope();

    /// <summary>
    /// Gets this element's local input map, creating it on first access.
    /// </summary>
    public InputMap InputMap => _elementInputMap ??= new InputMap();

    /// <summary>
    /// Returns the command scope without allocating one; resolution paths use this so lookups
    /// never materialize empty scopes.
    /// </summary>
    internal CommandScope? TryGetCommandScope() => _commandScope;

    /// <summary>
    /// Returns the local input map without allocating one.
    /// </summary>
    internal InputMap? TryGetInputMap() => _elementInputMap;
}
