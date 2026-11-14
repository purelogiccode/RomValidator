using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RomValidator.Models;
using RomValidator.Services;

namespace RomValidator;

/// <summary>
/// Window that lists groups of duplicate files (same hash, different filenames)
/// detected during DAT generation.
/// </summary>
public partial class DuplicateFilesWindow : Window
{
    /// <summary>Initializes a new instance of the DuplicateFilesWindow class.</summary>
    public DuplicateFilesWindow()
    {
        try
        {
            InitializeComponent();
            WindowThemeService.ApplyDarkTitleBar(this);
            ScreenshotShortcut.Attach(this);
        }
        catch (Exception ex)
        {
            LoggerService.LogException("DuplicateFilesWindow", ex, "Error initializing Duplicate Files window");
            throw;
        }
    }

    /// <summary>Sets the duplicate file data to display in the window.</summary>
    /// <param name="hashToFilenames">Dictionary mapping hash values to duplicate filenames.</param>
    /// <param name="customTitle">Optional custom title for the window.</param>
    public void SetDuplicateData(Dictionary<string, List<string>> hashToFilenames, string? customTitle = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(customTitle))
            {
                Title = customTitle;
            }

            var duplicateGroups = new ObservableCollection<DuplicateGroup>();
            foreach (var kvp in hashToFilenames)
            {
                if (kvp.Value.Count > 1)
                {
                    duplicateGroups.Add(new DuplicateGroup
                    {
                        Hash = kvp.Key,
                        Filenames = string.Join(", ", kvp.Value)
                    });
                }
            }

            DuplicateCountText.Text = $"{duplicateGroups.Count} duplicate group(s) found";
            DuplicateListView.ItemsSource = duplicateGroups;
        }
        catch (Exception ex)
        {
            LoggerService.LogException("DuplicateFilesWindow", ex, "Error populating duplicate file data");
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            LoggerService.LogException("DuplicateFilesWindow", ex, "Error closing Duplicate Files window");
        }
    }
}
