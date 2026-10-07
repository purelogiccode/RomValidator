namespace RomValidator.Models;

/// <summary>
/// Represents a group of duplicate files sharing the same hash value.
/// Used for displaying duplicate file information in the UI.
/// </summary>
public class DuplicateGroup
{
    /// <summary>Gets or sets the hash value shared by duplicate files.</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>Gets or sets the concatenated list of filenames sharing the same hash.</summary>
    public string Filenames { get; set; } = string.Empty;
}
