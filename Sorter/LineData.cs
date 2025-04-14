namespace Sorter;

public readonly record struct LineData(
    int Number,
    ReadOnlyMemory<char> Text,
    char[] Buffer);