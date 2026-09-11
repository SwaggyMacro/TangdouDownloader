namespace Aprillz.MewUI;

internal enum BindingStatus
{
    Valid,
    ValidationError,
    BindingError,
}

internal enum BindingErrorStage
{
    ConvertBack,
    SourceWrite,
    SourceValidation,
    SourceReadBack,
    Convert,
    TargetValidation,
    Consistency,
}

internal sealed record BindingError(
    BindingStatus Status,
    BindingErrorStage Stage,
    string Message,
    Exception? Exception = null);

internal readonly record struct BindingCommitResult(
    object? Value,
    BindingError? Error)
{
    public bool Succeeded => Error == null;

    public static BindingCommitResult Success(object? value) => new(value, null);

    public static BindingCommitResult Failure(
        BindingStatus status,
        BindingErrorStage stage,
        Exception exception)
        => new(null, new BindingError(status, stage, exception.Message, exception));

    public static BindingCommitResult Failure(
        BindingStatus status,
        BindingErrorStage stage,
        string message)
        => new(null, new BindingError(status, stage, message));
}

internal readonly record struct BindingStateSnapshot(
    bool HasCurrentCandidate,
    object? CurrentCandidate,
    bool HasLastSuccessfulTargetValue,
    object? LastSuccessfulTargetValue,
    BindingError? Error);
