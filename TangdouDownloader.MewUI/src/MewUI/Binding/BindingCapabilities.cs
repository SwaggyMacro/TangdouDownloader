namespace Aprillz.MewUI;

internal readonly record struct BindingCapabilities(
    bool ProvidesTargetValue,
    bool ObservesSourceChanges,
    bool AcceptsTargetCommit)
{
    public static BindingCapabilities FromMode(BindingMode mode)
        => mode switch
        {
            BindingMode.OneWay => new(
                ProvidesTargetValue: true,
                ObservesSourceChanges: true,
                AcceptsTargetCommit: false),
            BindingMode.TwoWay => new(
                ProvidesTargetValue: true,
                ObservesSourceChanges: true,
                AcceptsTargetCommit: true),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
}

internal interface IPropertyBinding : IDisposable
{
    BindingCapabilities Capabilities { get; }

    void Initialize();

    void UpdateTargetValue(object? value);

    BindingCommitResult CommitTargetValue(object? value);
}
