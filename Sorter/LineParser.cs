using System.Buffers;

namespace Sorter;

public static class LineParser
{
    public static LineData Parse(ReadOnlySpan<char> line)
    {
        var dotIndex = line.IndexOf(". ");
        if (dotIndex == -1) throw new FormatException("Invalid line format.");

        if (!int.TryParse(line[..dotIndex], out var number))
            throw new FormatException("Invalid number format.");

        var textSpan = line[(dotIndex + 2)..];
        var textArray = ArrayPool<char>.Shared.Rent(textSpan.Length);
        textSpan.CopyTo(textArray);

        return new LineData(number, textArray.AsMemory(0, textSpan.Length), textArray);
    }
}