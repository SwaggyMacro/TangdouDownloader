using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

/// <summary>
/// Bridges a <see cref="MewProperty{TProp}"/> to an <see cref="ObservableValue{TSource}"/>
/// when the types differ, applying convert/convertBack functions.
/// </summary>
internal sealed class MewPropertyBinding<TProp, TSource> : IPropertyBinding
{
    private readonly MewObject _owner;
    private readonly MewProperty<TProp> _property;
    private readonly ObservableValue<TSource> _source;
    private readonly Func<TSource, TProp> _convert;
    private readonly Func<TProp, TSource>? _convertBack;
    private readonly BindingCapabilities _capabilities;
    private bool _updating;

    public BindingCapabilities Capabilities => _capabilities;

    public MewPropertyBinding(
        MewObject owner,
        MewProperty<TProp> property,
        ObservableValue<TSource> source,
        Func<TSource, TProp> convert,
        Func<TProp, TSource>? convertBack,
        BindingMode mode)
    {
        _owner = owner;
        _property = property;
        _source = source;
        _convert = convert;
        _convertBack = convertBack;
        _capabilities = BindingCapabilities.FromMode(mode);

        if (_capabilities.ObservesSourceChanges)
        {
            WeakEventManager.AddHandler(
                ObservableValueWeakEvents<TSource>.Changed,
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
                sourceValue = _source.Value;
            }
            catch (Exception ex)
            {
                _owner.ReportBindingError(
                    _property,
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
                _owner.ReportBindingError(
                    _property,
                    sourceValue,
                    BindingStatus.BindingError,
                    BindingErrorStage.Convert,
                    ex);
                return;
            }

            _owner.ApplyBindingTargetValue(_property, converted);
        }
        finally { _updating = false; }
    }

    public void UpdateTargetValue(object? value)
    {
        _owner.UpdateBindingTarget(_property, (TProp)value!);
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
                _source.Value = sourceCandidate;
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
                return BindingCommitResult.Success(_convert(_source.Value));
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
            WeakEventManager.RemoveHandler(ObservableValueWeakEvents<TSource>.Changed, _source, this);
        }
    }
}
