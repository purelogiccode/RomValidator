using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using RomValidator.Services;

namespace RomValidator.Views;

/// <summary>Buttons displayed by the application's themed message dialog.</summary>
public enum DialogButtons
{
    /// <summary>A single OK button.</summary>
    Ok,

    /// <summary>OK and Cancel buttons.</summary>
    OkCancel,

    /// <summary>Yes and No buttons.</summary>
    YesNo,

    /// <summary>Retry and Cancel buttons.</summary>
    RetryCancel
}

/// <summary>Result returned by the application's themed message dialog.</summary>
public enum DialogResult
{
    /// <summary>The dialog was closed without choosing an option.</summary>
    None,

    /// <summary>The OK button was chosen.</summary>
    Ok,

    /// <summary>The Cancel button was chosen.</summary>
    Cancel,

    /// <summary>The Yes button was chosen.</summary>
    Yes,

    /// <summary>The No button was chosen.</summary>
    No,

    /// <summary>The Retry button was chosen.</summary>
    Retry
}

/// <summary>Icon displayed by the application's themed message dialog.</summary>
public enum DialogIcon
{
    /// <summary>No icon.</summary>
    None,

    /// <summary>Informational icon.</summary>
    Info,

    /// <summary>Warning icon.</summary>
    Warning,

    /// <summary>Error icon.</summary>
    Error,

    /// <summary>Question icon.</summary>
    Question
}

/// <summary>
/// Provides themed, cross-platform message dialogs without relying on platform-specific
/// message boxes.
/// </summary>
public static class DialogService
{
    /// <summary>Shows a message dialog owned by the specified window.</summary>
    /// <param name="owner">The window that owns the dialog.</param>
    /// <param name="message">The message body.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <returns>The chosen result.</returns>
    public static Task<DialogResult> ShowAsync(Window owner, string message, string title,
        DialogButtons buttons, DialogIcon icon)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var dialog = new MessageDialog(title, message, buttons, icon);
        return dialog.ShowDialog<DialogResult>(owner);
    }
}

/// <summary>Themed message dialog window.</summary>
public partial class MessageDialog : Window
{
    private DialogResult _primaryResult = DialogResult.Ok;

    /// <summary>Initializes a new instance for the XAML designer.</summary>
    public MessageDialog()
        : this("ROM Validator", string.Empty, DialogButtons.Ok, DialogIcon.None)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MessageDialog"/> class.</summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message body.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    public MessageDialog(string title, string message, DialogButtons buttons, DialogIcon icon)
    {
        InitializeComponent();
        WindowThemeService.ApplyDarkTitleBar(this);
        ScreenshotShortcut.Attach(this);

        TitleText.Text = title;
        MessageText.Text = message;
        Title = title;

        (IconText.Text, IconText.Foreground) = icon switch
        {
            DialogIcon.Error => ("\u2715", Brush.Parse("#FF8A8A")),
            DialogIcon.Warning => ("\u26A0", Brush.Parse("#F0C05A")),
            DialogIcon.Question => ("?", Brush.Parse("#7FB4FF")),
            DialogIcon.Info => ("i", Brush.Parse("#7FB4FF")),
            _ => (string.Empty, Brush.Parse("#F1F3F7"))
        };

        AddButtons(buttons);
        AddHandler(KeyDownEvent, OnDialogKeyDown, RoutingStrategies.Tunnel);
    }

    private void AddButtons(DialogButtons buttons)
    {
        switch (buttons)
        {
            case DialogButtons.Ok:
                AddButton("OK", DialogResult.Ok, true);
                break;
            case DialogButtons.OkCancel:
                AddButton("OK", DialogResult.Ok, true);
                AddButton("Cancel", DialogResult.Cancel, false);
                break;
            case DialogButtons.YesNo:
                AddButton("Yes", DialogResult.Yes, true);
                AddButton("No", DialogResult.No, false);
                break;
            case DialogButtons.RetryCancel:
                AddButton("Retry", DialogResult.Retry, true);
                AddButton("Cancel", DialogResult.Cancel, false);
                break;
            default:
                AddButton("OK", DialogResult.Ok, true);
                break;
        }
    }

    private void AddButton(string label, DialogResult result, bool primary)
    {
        var button = new Button
        {
            Content = label,
            MinWidth = 92
        };

        if (primary)
        {
            button.Classes.Add("primary");
            _primaryResult = result;
        }

        button.Click += (_, _) => Close(result);
        ButtonsPanel.Children.Add(button);
    }

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(DialogResult.Cancel);
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Close(_primaryResult);
        }
    }
}
