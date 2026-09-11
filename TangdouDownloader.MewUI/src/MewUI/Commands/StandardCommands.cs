namespace Aprillz.MewUI;

/// <summary>
/// Universal editing commands shared by keyboard defaults, menus and toolbars; controls provide
/// handlers via their <see cref="Controls.Element.Commands"/> scope.
/// </summary>
public static class StandardCommands
{
    public static Command Cut { get; } = new Command("edit.cut").BindText(MewUIStrings.CommandCut);

    public static Command Copy { get; } = new Command("edit.copy").BindText(MewUIStrings.CommandCopy);

    public static Command Paste { get; } = new Command("edit.paste").BindText(MewUIStrings.CommandPaste);

    public static Command Delete { get; } = new Command("edit.delete").BindText(MewUIStrings.CommandDelete);

    public static Command Undo { get; } = new Command("edit.undo").BindText(MewUIStrings.CommandUndo);

    public static Command Redo { get; } = new Command("edit.redo").BindText(MewUIStrings.CommandRedo);

    public static Command SelectAll { get; } = new Command("edit.selectAll").BindText(MewUIStrings.CommandSelectAll);
}
