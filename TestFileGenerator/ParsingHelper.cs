namespace TestFileGenerator;

public static class ParsingHelper
{
    public static long ParseSize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        input = input.Trim().ToUpperInvariant();
        long multiplier = 1;
        var numberPart = input;

        if (input.EndsWith("GB"))
        {
            multiplier = 1L << 30;
            numberPart = input[..^2];
        }
        else if (input.EndsWith("MB"))
        {
            multiplier = 1L << 20;
            numberPart = input[..^2];
        }
        else if (input.EndsWith("KB"))
        {
            multiplier = 1L << 10;
            numberPart = input[..^2];
        }
        else if (input.EndsWith("B"))
        {
            numberPart = input[..^1];
        }

        if (!double.TryParse(numberPart, out var size) || size < 0)
            throw new ArgumentException($"Invalid size value: '{input}'. Must be a non-negative number.", nameof(input));

        var bytes = size * multiplier;
        if (bytes > long.MaxValue)
            throw new ArgumentException($"Size '{input}' exceeds maximum value of {long.MaxValue} bytes.", nameof(input));

        return (long)bytes;
    }
}