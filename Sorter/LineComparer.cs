﻿namespace Sorter;

internal class LineComparer : IComparer<LineData>
{
    public int Compare(LineData a, LineData b)
    {
        // ReadOnlySpan<char> textA = a.Text.AsSpan();
        // ReadOnlySpan<char> textB = b.Text.AsSpan();
        var textCompare = a.Text.Span.CompareTo(b.Text.Span, StringComparison.Ordinal);

        return textCompare != 0
            ? textCompare
            : a.Number.CompareTo(b.Number);
    }
}