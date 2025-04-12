using System.Buffers;

namespace Sorter;

public class LargeFileReader
{
    private const int BufferSize = 8192; // 8KB buffer
    private readonly StreamReader _internalReader;
    private long currentSize = 0;
    private char[] buffer = ArrayPool<char>.Shared.Rent(BufferSize);
    private int lineStart = 0;

    public LargeFileReader(string inputFilePath)
    {
        _internalReader = new StreamReader(inputFilePath);
    }

    internal bool EndOfStream => _internalReader.EndOfStream;

    internal void Close() => _internalReader.Close();

    internal async Task<LineData> ReadLineAsync()
    {
        try
        {
            int charsRead;
            LineData result = default;
            while ((charsRead = await _internalReader.ReadAsync(buffer, lineStart, BufferSize - lineStart)) > 0)
            {
                var textEnd = charsRead + lineStart;
                lineStart = 0;
                var bufferSpan = buffer.AsSpan()[..textEnd];

                for (var i = 0; i < textEnd - 1; i++)
                {
                    if (bufferSpan[i] == '\r')
                    {
                        var lineEnd = bufferSpan[i + 1] == '\n'
                                ? i + 2
                                : i + 1;
                        var lineTextEnd = i - lineStart;

                        var lineSpan = bufferSpan.Slice(lineStart, lineTextEnd);
                        var line = ParseLine3(lineSpan);

                        result = line;

                        currentSize += lineEnd - lineStart;

                        lineStart = lineEnd;
                    }
                }

                if (lineStart < BufferSize) // Partial line at buffer end
                {
                    buffer.AsSpan().Slice(lineStart, BufferSize - lineStart).CopyTo(buffer);
                    lineStart = BufferSize - lineStart;
                }
                else
                {
                    lineStart = 0;
                }

                if (result != default)
                {
                    break;
                }
            }

            return result;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static LineData ParseLine3(ReadOnlySpan<char> line)
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