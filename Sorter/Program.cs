using Sorter;

var inputFile = "test.txt";
var outputFile = "sorted.txt";
var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var inputPath = Path.Combine(homeDirectory, inputFile);
var outputPath = Path.Combine(homeDirectory, outputFile);

await new LargeFileSorter().SortFile(
    inputPath,
    outputPath);

Console.WriteLine($"Sorted test file: {outputPath}, Size: {new FileInfo(outputPath).Length} bytes");