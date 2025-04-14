namespace Sorter.Tests;

public class SorterTests
{
    private readonly string _inputPath;
    private readonly string _outputPath;

    public SorterTests()
    {
        var sessionId = Guid.NewGuid().ToString("D")[..8];

        _inputPath = $"test_input_{sessionId}.txt";
        _outputPath = $"test_output_{sessionId}.txt";
    }

    [Fact]
    public async Task SortingWithCorrectOrder_BasicSort()
    {
        await TestDataHelper.SetUpFileContent(_inputPath,
            """
            415. Apple
            30432. Something something something
            1. Apple
            32. Cherry is the best
            2. Banana is yellow

            """);

        // Act
        var target = new LargeFileSorter();

        await target.SortFile(_inputPath, _outputPath);

        // Assert
        await TestDataHelper.AssertFileContent(_outputPath,
            """
            1. Apple
            415. Apple
            2. Banana is yellow
            32. Cherry is the best
            30432. Something something something

            """);
    }

    [Fact]
    public async Task SortingWithCorrectOrder_DuplicateNumbers()
    {
        await TestDataHelper.SetUpFileContent(_inputPath,
            """
            1. Zebra runs
            1. Apple is
            1. Banana shines
            1. Cherry tastes
            1. Yellow flower

            """);

        // Act
        var target = new LargeFileSorter();

        await target.SortFile(_inputPath, _outputPath);

        // Assert
        await TestDataHelper.AssertFileContent(_outputPath,
            """
            1. Apple is
            1. Banana shines
            1. Cherry tastes
            1. Yellow flower
            1. Zebra runs

            """);
    }

    [Fact]
    public async Task SortingWithCorrectOrder_MixedCase()
    {
        await TestDataHelper.SetUpFileContent(_inputPath,
            """
            1. apple shines
            2. Apple is
            3. APPLE feels
            4. Banana runs

            """);

        // Act
        var target = new LargeFileSorter();

        await target.SortFile(_inputPath, _outputPath);

        // Assert
        await TestDataHelper.AssertFileContent(_outputPath,
            """
            3. APPLE feels
            2. Apple is
            1. apple shines
            4. Banana runs

            """);
    }

    [Fact]
    public async Task SortingWithCorrectOrder_SingleLine()
    {
        await TestDataHelper.SetUpFileContent(_inputPath,
            """
            1. Apple shines

            """);

        // Act
        var target = new LargeFileSorter();

        await target.SortFile(_inputPath, _outputPath);

        // Assert
        await TestDataHelper.AssertFileContent(_outputPath,
            """
            1. Apple shines

            """);
    }

    [Fact]
    public async Task SortingWithCorrectOrder_EmptyFile()
    {
        await TestDataHelper.SetUpFileContent(_inputPath, string.Empty);

        // Act
        var target = new LargeFileSorter();

        await target.SortFile(_inputPath, _outputPath);

        // Assert
        await TestDataHelper.AssertFileContent(_outputPath, string.Empty);
    }
}