using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

/// <summary>
/// Bridges a <see cref="MewProperty{T}"/> on a <see cref="MewObject"/> to an <see cref="ObservableValue{T}"/>.
/// Handles cycle prevention automatically via a re-entrancy guard.
/// </summary>
internal sealed class MewPropertyBinding<T> : IPropertyBinding
{
    private readonly MewObject _owner;
    private readonly MewProperty<T> _property;
    private readonly ObservableValue<T> _source;
    private readonly BindingCapabilities _capabilities;
    private bool _updating;

    public BindingCapabilities Capabilities => _capabilities;

    public MewPropertyBinding(
        MewObject owner,
        MewProperty<T> property,
        ObservableValue<T> source,
        BindingMode mode)
    {
        _owner = owner;
        _property = property;
        _source = source;
        _capabilities = BindingCapabilities.FromMode(mode);

        if (_capabilities.ObservesSourceChanges)
        {
            WeakEventManager.AddHandler(
                ObservableValueWeakEvents<T>.Changed,
                source,
                this,
                static binding => binding.OnSourceChanged());
        }

    }

    public void Initialize()
    {
        if (_capabilities.ProvidesTargetValue)
        {
            OnSourceChanged();
        }
    }

    private void OnSourceChanged()
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        try
        {
            var value = _source.Value;
            _owner.ApplyBindingTargetValue(_property, value);
        }
        finally
        {
            _updating = false;
        }
    }

    public void UpdateTargetValue(object? value)
    {
        _owner.UpdateBindingTarget(_property, (T)value!);
    }

    public BindingCommitResult CommitTargetValue(object? value)
    {
        _updating = true;
        try
        {
            try
            {
                _source.Value = (T)value!;
            }
            catch (Exception ex)
            {
                return BindingCommitResult.Failure(
                    BindingStatus.BindingError,
                    BindingErrorStage.SourceWrite,
                    ex);
            }

            try
            {
                return BindingCommitResult.Success(_source.Value);
            }
            catch (Exception ex)
            {
                return BindingCommitResult.Failure(
                    BindingStatus.BindingError,
                    BindingErrorStage.Consistency,
                    ex);
            }
        }
        finally
        {
            _updating = false;
        }
    }

    public void Dispose()
    {
        if (_capabilities.ObservesSourceChanges)
        {
            WeakEventManager.RemoveHandler(ObservableValueWeakEvents<T>.Changed, _source, this);
        }

    }
}
