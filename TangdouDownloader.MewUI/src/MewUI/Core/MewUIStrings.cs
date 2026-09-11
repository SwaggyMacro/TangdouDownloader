namespace Aprillz.MewUI;

/// <summary>
/// Centralized UI strings for localization.
/// Default values are English. Assign <see cref="ObservableValue{T}.Value"/> at runtime to update all bound UI,
/// and call <see cref="ResetToDefaults"/> to restore every string to its built-in English default.
/// </summary>
public static class MewUIStrings
{
    private static readonly List<Action> _resetters = [];

    private static ObservableValue<string> Define(string defaultValue)
    {
        var value = new ObservableValue<string>(defaultValue);
        _resetters.Add(() => value.Value = defaultValue);
        return value;
    }

    /// <summary>Restores every string to its built-in English default.</summary>
    public static void ResetToDefaults()
    {
        foreach (var reset in _resetters)
        {
            reset();
        }
    }

    // Common - shared button labels reused across surfaces (access-key mnemonic in value)
    public static ObservableValue<string> CommonOK { get; } = Define("_OK");

    public static ObservableValue<string> CommonCancel { get; } = Define("_Cancel");

    public static ObservableValue<string> CommonYes { get; } = Define("_Yes");

    public static ObservableValue<string> CommonNo { get; } = Define("_No");

    public static ObservableValue<string> CommonRetry { get; } = Define("_Retry");

    public static ObservableValue<string> CommonIgnore { get; } = Define("_Ignore");

    public static ObservableValue<string> CommonAbort { get; } = Define("_Abort");

    // Prompt - MessageBox icon titles and detail toggle
    public static ObservableValue<string> PromptInformation { get; } = Define("Information");

    public static ObservableValue<string> PromptWarning { get; } = Define("Warning");

    public static ObservableValue<string> PromptError { get; } = Define("Error");

    public static ObservableValue<string> PromptQuestion { get; } = Define("Confirm");

    public static ObservableValue<string> PromptSuccess { get; } = Define("Success");

    public static ObservableValue<string> PromptShield { get; } = Define("Security");

    public static ObservableValue<string> PromptCrash { get; } = Define("Crash");

    public static ObservableValue<string> PromptShowDetail { get; } = Define("Show _Details");

    // BusyIndicator
    public static ObservableValue<string> BusyIndicatorAbortConfirmation { get; } = Define("Are you sure you want to abort this operation?");

    public static ObservableValue<string> BusyIndicatorAborting { get; } = Define("Aborting...");

    // Standard command presentation
    public static ObservableValue<string> CommandUndo { get; } = Define("_Undo");

    public static ObservableValue<string> CommandRedo { get; } = Define("_Redo");

    public static ObservableValue<string> CommandCut { get; } = Define("Cu_t");

    public static ObservableValue<string> CommandCopy { get; } = Define("_Copy");

    public static ObservableValue<string> CommandPaste { get; } = Define("_Paste");

    public static ObservableValue<string> CommandDelete { get; } = Define("_Delete");

    public static ObservableValue<string> CommandSelectAll { get; } = Define("Select _All");

    // Compatibility names share the same live command-presentation sources.
    public static ObservableValue<string> TextBoxContextMenuUndo => CommandUndo;

    public static ObservableValue<string> TextBoxContextMenuRedo => CommandRedo;

    public static ObservableValue<string> TextBoxContextMenuCut => CommandCut;

    public static ObservableValue<string> TextBoxContextMenuCopy => CommandCopy;

    public static ObservableValue<string> TextBoxContextMenuPaste => CommandPaste;

    public static ObservableValue<string> TextBoxContextMenuSelectAll => CommandSelectAll;

    // FileDialog
    public static ObservableValue<string> FileDialogTitleOpenSingle { get; } = Define("Open File");

    public static ObservableValue<string> FileDialogTitleOpenMultiple { get; } = Define("Open Files");

    public static ObservableValue<string> FileDialogTitleSave { get; } = Define("Save File");

    public static ObservableValue<string> FileDialogTitleSelectFolder { get; } = Define("Select Folder");

    public static ObservableValue<string> FileDialogTitleFallback { get; } = Define("File Dialog");

    public static ObservableValue<string> FileDialogAcceptOpen { get; } = Define("_Open");

    public static ObservableValue<string> FileDialogAcceptSave { get; } = Define("_Save");

    public static ObservableValue<string> FileDialogAcceptSelect { get; } = Define("_Select");

    public static ObservableValue<string> FileDialogFileNameLabel { get; } = Define("File name:");

    public static ObservableValue<string> FileDialogFileTypeLabel { get; } = Define("File type:");

    public static ObservableValue<string> FileDialogNavBack { get; } = Define("Back");

    public static ObservableValue<string> FileDialogNavForward { get; } = Define("Forward");

    public static ObservableValue<string> FileDialogNavUp { get; } = Define("Up");

    public static ObservableValue<string> FileDialogViewGrid { get; } = Define("Grid");

    public static ObservableValue<string> FileDialogViewList { get; } = Define("List");

    public static ObservableValue<string> FileDialogAllFiles { get; } = Define("All files");

    public static ObservableValue<string> FileDialogColumnName { get; } = Define("Name");

    public static ObservableValue<string> FileDialogColumnSize { get; } = Define("Size");

    public static ObservableValue<string> FileDialogColumnModified { get; } = Define("Modified");

    // Sidebar - file dialog places section headers
    public static ObservableValue<string> SidebarQuickAccess { get; } = Define("Quick access");

    public static ObservableValue<string> SidebarThisPC { get; } = Define("This PC");

    public static ObservableValue<string> SidebarFavorites { get; } = Define("Favorites");

    public static ObservableValue<string> SidebarLocations { get; } = Define("Locations");

    public static ObservableValue<string> SidebarPlaces { get; } = Define("Places");

    public static ObservableValue<string> SidebarDevices { get; } = Define("Devices");

    // Folder - known folder labels shared across platforms
    public static ObservableValue<string> FolderHome { get; } = Define("Home");

    public static ObservableValue<string> FolderDesktop { get; } = Define("Desktop");

    public static ObservableValue<string> FolderDownloads { get; } = Define("Downloads");

    public static ObservableValue<string> FolderDocuments { get; } = Define("Documents");

    public static ObservableValue<string> FolderPictures { get; } = Define("Pictures");

    public static ObservableValue<string> FolderMusic { get; } = Define("Music");

    public static ObservableValue<string> FolderVideos { get; } = Define("Videos");

    public static ObservableValue<string> FolderApplications { get; } = Define("Applications");

    // ColorPicker
    public static ObservableValue<string> ColorPickerHex { get; } = Define("Hex");
}
