using RomValidator.Models;
using Xunit;

namespace RomValidator.Tests.Models;

public class DuplicateGroupTests
{
    [Fact]
    public void DuplicateGroupDefaultValuesAreEmpty()
    {
        var group = new DuplicateGroup();

        Assert.Equal(string.Empty, group.Hash);
        Assert.Equal(string.Empty, group.Filenames);
    }

    [Fact]
    public void DuplicateGroupPropertiesCanBeSet()
    {
        var group = new DuplicateGroup
        {
            Hash = "abc123",
            Filenames = "game1.nes, game2.nes"
        };

        Assert.Equal("abc123", group.Hash);
        Assert.Equal("game1.nes, game2.nes", group.Filenames);
    }
}
