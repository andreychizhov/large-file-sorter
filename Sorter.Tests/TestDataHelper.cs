using FluentAssertions;

namespace Sorter.Tests;

public static class TestDataHelper
{
    public static async Task SetUpFileContent(string path, string input)
    {
        await File.WriteAllTextAsync(path, input);
    }

    public static async Task AssertFileContent(string path, string expected)
    {
        var result = await File.ReadAllTextAsync(path);

        result.Should().Be(expected);
    }
}