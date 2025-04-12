using TestFileGenerator;

var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var outputPath = Path.Combine(homeDirectory, "test.txt");
var targetSize = ParsingHelper.ParseSize(args[0]);

new TestDataGenerator().GenerateTestFile(outputPath, targetSize);
Console.WriteLine($"Generated test file: {outputPath}, Size: {new FileInfo(outputPath).Length} bytes");