using System.Diagnostics;
using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

internal static class BindingDiagnostics
{
    [Conditional("DEBUG")]
    public static void ReportDirectWrite(MewObject owner, MewProperty property)
        => Debug.WriteLine(
            $"[Binding] {owner.GetType().Name}.{property.Name}: SetValue replaces the Binding with a Local value. " +
            "Use SetCurrentValue to preserve the Binding.");

    [Conditional("DEBUG")]
    public static void ReportBindingClear(MewObject owner, MewProperty property)
        => Debug.WriteLine(
            $"[Binding] {owner.GetType().Name}.{property.Name}: ClearBinding removed the Binding expression and candidate.");

    [Conditional("DEBUG")]
    public static void ReportLocalClear(MewObject owner, MewProperty property)
        => Debug.WriteLine(
            $"[Binding] {owner.GetType().Name}.{property.Name}: ClearLocalValue removed the Local candidate.");

    [Conditional("DEBUG")]
    public static void ReportBindingReplacement(MewObject owner, MewProperty property)
        => Debug.WriteLine(
            $"[Binding] {owner.GetType().Name}.{property.Name}: SetBinding replaced the existing Binding expression.");

    [Conditional("DEBUG")]
    public static void ReportLocalReplacement(MewObject owner, MewProperty property)
        => Debug.WriteLine(
            $"[Binding] {owner.GetType().Name}.{property.Name}: SetBinding removed the Local candidate before activation.");
}
