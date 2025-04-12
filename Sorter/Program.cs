using Sorter;

var inputFile = "test.txt";
var outputFile = "sorted.txt";
var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

await new LargeFileSorter().SortLargeFile(
    Path.Combine(homeDirectory, inputFile),
    Path.Combine(homeDirectory, outputFile));