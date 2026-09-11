using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

internal sealed class MewPropertyPathBinding<TProp, TRoot, TSource> : IPropertyBinding
    where TRoot : class
{
    private readonly MewObject _target;
    private readonly MewProperty<TProp> _targetProperty;
    private readonly BindingPathObserver<TRoot, TSource> _observer;
    private readonly Func<TSource, TProp> _convert;
    private readonly Func<TProp, TSource>? _convertBack;
    private readonly TProp _fallbackValue;
    private readonly BindingCapabilities _capabilities;
    private bool _updating;
    private bool _disposed;

    public BindingCapabilities Capabilities => _capabilities;

    internal MewPropertyPathBinding(
        MewObject target,
        MewProperty<TProp> targetProperty,
        TRoot root,
        BindingPath<TRoot, TSource> path,
        Func<TSource, TProp> convert,
        Func<TProp, TSource>? convertBack,
        BindingMode mode,
        TProp fallbackValue)
    {
        _target = target;
        _targetProperty = targetProperty;
        _convert = convert;
        _convertBack = convertBack;
        _fallbackValue = fallbackValue;
        _capabilities = BindingCapabilities.FromMode(mode);
        _observer = path.Attach(root);

        try
        {
            if (_capabilities.ObservesSourceChanges)
            {
                _observer.Changed += OnSourceChanged;
            }

        }
        catch
        {
            Dispose();
            throw;
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
        if (_updating || _disposed)
        {
            return;
        }

        _updating = true;
        try
        {
            if (_observer.Error is { } observerError)
            {
                _target.ReportBindingError(
                    _targetProperty,
                    null,
                    BindingStatus.BindingError,
                    BindingErrorStage.SourceReadBack,
                    observerError);
                return;
            }

            TProp value;
            try
            {
                value = _observer.IsAvailable
                    ? _convert(_observer.CurrentValue)
                    : _fallbackValue;
            }
            catch (Exception ex)
            {
                _target.ReportBindingError(
                    _targetProperty,
                    _observer.IsAvailable ? _observer.CurrentValue : default,
                    BindingStatus.BindingError,
                    BindingErrorStage.Convert,
                    ex);
                return;
            }

            _target.ApplyBindingTargetValue(_targetProperty, value);
        }
        finally
        {
            _updating = false;
        }
    }

    public void UpdateTargetValue(object? value)
    {
        if (_disposed)
        {
            return;
        }

        _target.UpdateBindingTarget(_targetProperty, (TProp)value!);
    }

    public BindingCommitResult CommitTargetValue(object? value)
    {
        if (_disposed)
        {
            return BindingCommitResult.Failure(
                BindingStatus.BindingError,
                BindingErrorStage.SourceWrite,
                "The binding path has been disposed.");
        }

        if (!_observer.IsAvailable)
        {
            return BindingCommitResult.Failure(
                BindingStatus.BindingError,
                BindingErrorStage.SourceWrite,
                "The binding path is not currently available.");
        }

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
                _observer.ValidateWrite(sourceCandidate);
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
                _observer.Write(sourceCandidate);
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
                return BindingCommitResult.Success(_convert(_observer.CurrentValue));
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_capabilities.ObservesSourceChanges)
        {
            _observer.Changed -= OnSourceChanged;
        }
        _observer.Dispose();

    }
}
