using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

/// <summary>
/// Bridges a <see cref="MewProperty{TProp}"/> on a target <see cref="MewObject"/>
/// to a <see cref="MewProperty{TSource}"/> on a source <see cref="MewObject"/>
/// with type conversion via convert/convertBack functions.
/// </summary>
internal sealed class MewObjectPropertyBinding<TProp, TSource> : IPropertyBinding
{
    private readonly MewObject _target;
    private readonly MewProperty<TProp> _targetProperty;
    private readonly MewObject _source;
    private readonly MewProperty<TSource> _sourceProperty;
    private readonly Func<TSource, TProp> _convert;
    private readonly Func<TProp, TSource>? _convertBack;
    private readonly BindingCapabilities _capabilities;
    private readonly WeakEventKey<MewObject, Action> _sourceChangedEvent;
    private bool _updating;

    public BindingCapabilities Capabilities => _capabilities;

    public MewObjectPropertyBinding(
        MewObject target,
        MewProperty<TProp> targetProperty,
        MewObject source,
        MewProperty<TSource> sourceProperty,
        Func<TSource, TProp> convert,
        Func<TProp, TSource>? convertBack,
        BindingMode mode)
    {
        _target = target;
        _targetProperty = targetProperty;
        _source = source;
        _sourceProperty = sourceProperty;
        _convert = convert;
        _convertBack = convertBack;
        _capabilities = BindingCapabilities.FromMode(mode);
        // Source → Target
        _sourceChangedEvent = new WeakEventKey<MewObject, Action>(
            (owner, handler) => owner.AddPropertyBindingCallback(sourceProperty.Id, handler),
            (owner, handler) => owner.RemovePropertyBindingCallback(sourceProperty.Id, handler),
            requireStaticAccessors: false);

        if (_capabilities.ObservesSourceChanges)
        {
            WeakEventManager.AddHandler(
                _sourceChangedEvent,
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
        if (_updating) return;
        _updating = true;
        try
        {
            TSource sourceValue = default!;
            TProp converted;
            try
            {
                sourceValue = _source.GetBindingValue(_sourceProperty);
            }
            catch (Exception ex)
            {
                _target.ReportBindingError(
                    _targetProperty,
                    sourceValue,
                    BindingStatus.BindingError,
                    BindingErrorStage.SourceReadBack,
                    ex);
                return;
            }

            try
            {
                converted = _convert(sourceValue);
            }
            catch (Exception ex)
            {
                _target.ReportBindingError(
                    _targetProperty,
                    sourceValue,
                    BindingStatus.BindingError,
                    BindingErrorStage.Convert,
                    ex);
                return;
            }

            _target.ApplyBindingTargetValue(_targetProperty, converted);
        }
        finally { _updating = false; }
    }

    public void UpdateTargetValue(object? value)
    {
        _target.UpdateBindingTarget(_targetProperty, (TProp)value!);
    }

    public BindingCommitResult CommitTargetValue(object? value)
    {
        if (_convertBack == null)
        {
            return BindingCommitResult.Success(value);
        }

        _updating = true;
        try
        {
            TSource sourceCandidate;
            try
            {
                sourceCandidate = _convertBack((TProp)value!);
            }
            catch (Exception ex)
            {
                return BindingCommitResult.Failure(
                    BindingStatus.ValidationError,
                    BindingErrorStage.ConvertBack,
                    ex);
            }

            try
            {
                _source.PropertyStore.ValidateValueCandidate(_sourceProperty, sourceCandidate);
            }
            catch (Exception ex)
            {
                return BindingCommitResult.Failure(
                    BindingStatus.ValidationError,
                    BindingErrorStage.SourceValidation,
                    ex);
            }

            try
            {
                _source.PropertyStore.SetLocalPrevalidated(_sourceProperty, sourceCandidate);
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
                return BindingCommitResult.Success(_convert(_source.GetBindingValue(_sourceProperty)));
            }
            catch (Exception ex)
            {
                return BindingCommitResult.Failure(
                    BindingStatus.BindingError,
                    BindingErrorStage.Consistency,
                    ex);
            }
        }
        finally { _updating = false; }
    }

    public void Dispose()
    {
        if (_capabilities.ObservesSourceChanges)
        {
            WeakEventManager.RemoveHandler(_sourceChangedEvent, _source, this);
        }
    }
}
