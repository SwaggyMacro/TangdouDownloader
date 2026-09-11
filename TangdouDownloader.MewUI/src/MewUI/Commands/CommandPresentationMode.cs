namespace Aprillz.MewUI;

/// <summary>
/// Selects which parts of a command's default presentation a command source displays.
/// </summary>
public enum CommandPresentationMode
{
    /// <summary>The command source supplies its own content.</summary>
    None,

    /// <summary>Display the command's current presentation text.</summary>
    Text,

    /// <summary>Display the command's current presentation icon.</summary>
    Icon,

    /// <summary>Display both the command's current presentation icon and text.</summary>
    TextAndIcon,
}
