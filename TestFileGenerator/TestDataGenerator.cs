namespace TestFileGenerator;

public class TestDataGenerator
{
    static readonly string[] Nouns =
    [
        "Apple", "Banana", "Cherry", "Dragonfruit", "Elderberry",
        "Fig", "Grape", "Honeydew", "Kiwi", "Lemon",
        "Mango", "Nectarine", "Orange", "Papaya", "Quince",
        "Raspberry", "Strawberry", "Tangerine", "Ugli", "Watermelon",
        "Bear", "Cat", "Deer", "Elephant", "Fox",
        "Giraffe", "Horse", "Iguana", "Jaguar", "Kangaroo",
        "Lion", "Monkey", "Otter", "Panda", "Quail",
        "Rabbit", "Snake", "Tiger", "Unicorn", "Wolf",
        "Zebra", "Book", "Chair", "Desk", "Lamp",
        "Pen", "Table", "Vase", "Window", "Clock"
    ];

    static readonly string[] Adjectives =
    [
        "red", "yellow", "green", "blue", "purple",
        "sweet", "sour", "bitter", "salty", "spicy",
        "big", "small", "tall", "short", "wide",
        "soft", "hard", "smooth", "rough", "shiny",
        "dull", "bright", "dark", "warm", "cold",
        "fast", "slow", "loud", "quiet", "happy"
    ];

    static readonly string[] Verbs = ["is", "looks", "feels", "shines", "tastes"];

    public void GenerateTestFile(string outputPath, long targetSizeInBytes, int minNumber = 1, int maxNumber = 1000000)
    {
        var random = new Random();
        long bytesWritten = 0;
        var numberBuffer = new char[11];

        using var writer = new StreamWriter(outputPath);
        while (bytesWritten < targetSizeInBytes)
        {
            var number = random.Next(minNumber, maxNumber);
            var success = number.TryFormat(numberBuffer, out var charsWritten);
            if (!success) throw new InvalidOperationException("Number formatting failed.");
            ReadOnlySpan<char> numberSpan = numberBuffer.AsSpan(0, charsWritten);

            var adjective = Adjectives[random.Next(Adjectives.Length)].AsSpan();
            var noun = Nouns[random.Next(Nouns.Length)].AsSpan();
            var verb = Verbs[random.Next(Verbs.Length)].AsSpan();

            writer.Write(numberSpan);
            writer.Write(". ");
            writer.Write(noun);
            writer.Write(" ");
            writer.Write(verb);
            writer.Write(" ");
            writer.Write(adjective);
            writer.WriteLine();

            // Estimate bytes (approximate due to encoding, but close enough)
            var lineLength = charsWritten + 2 + adjective.Length + 1 + noun.Length + 1 + verb.Length;
            bytesWritten += lineLength + Environment.NewLine.Length;
        }
    }
}