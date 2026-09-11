using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

/// <summary>
/// Bridges a <see cref="MewProperty{T}"/> on a target <see cref="MewObject"/>
/// to a <see cref="MewProperty{T}"/> on a source <see cref="MewObject"/>.
/// When the source property changes, the target's binding value is updated.
/// </summary>
internal sealed class MewObjectPropertyBinding<T> : IPropertyBinding
{
    private static readonly BindingCapabilities _capabilities =
        BindingCapabilities.FromMode(BindingMode.OneWay);

    private readonly MewObject _target;
    private readonly MewProperty<T> _targetProperty;
    private readonly MewObject _source;
    private readonly MewProperty<T> _sourceProperty;
    private readonly PropertyForwardEntry _forwardEntry;

    public BindingCapabilities Capabilities => _capabilities;

    public MewObjectPropertyBinding(
        MewObject target,
        MewProperty<T> targetProperty,
        MewObject source,
        MewProperty<T> sourceProperty)
    {
        _target = target;
        _targetProperty = targetProperty;
        _source = source;
        _sourceProperty = sourceProperty;

        // Register forward on source: when source property changes, propagate to target.
        // Keep the returned entry so Dispose removes exactly this forward, not whatever
        // forward currently occupies the source property id (another binding may share it).
        _forwardEntry = source.AddPropertyForward(sourceProperty.Id, target, targetProperty);

    }

    public void Initialize()
    {
        T sourceValue;
        try
        {
            sourceValue = _source.GetBindingValue(_sourceProperty);
        }
        catch (Exception ex)
        {
            _target.ReportBindingError(
                _targetProperty,
                null,
                BindingStatus.BindingError,
                BindingErrorStage.SourceReadBack,
                ex);
            return;
        }

        _target.ApplyBindingTargetValue(
            _targetProperty,
            sourceValue);
    }

    public void Dispose()
    {
        _source.RemovePropertyForward(_sourceProperty.Id, _forwardEntry);
    }

    public void UpdateTargetValue(object? value)
    {
        _target.UpdateBindingTarget(_targetProperty, (T)value!);
    }

    public BindingCommitResult CommitTargetValue(object? value)
        => BindingCommitResult.Success(value);
}
