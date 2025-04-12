using System.Buffers;

namespace Sorter;

/// <summary>
/// Custom reader that reads file line-by-line efficiently. Leverages Span
/// and Memory data types and array pooling to minimize string allocations
/// and reduce garbage collection overhead
/// </summary>
public class LargeFileReader : IDisposable
{
    private const int BufferSize = 8192;

    private readonly StreamReader _internalReader;
    private readonly char[] _buffer;
    private readonly ArrayPool<char> _pool;
    private int _lineStart;
    private long _totalRead;
    private int _position; // Current parsing position in buffer
    private int _textEnd;  // End of valid data in buffer
    private int _charsRead;
    private bool _isDone;
    private State _state;

    private enum State
    {
        Reading,
        Parsing,
        LineFound,
        BufferPartial,
        EndOfStream
    }

    public LargeFileReader(string inputPath)
    {
        _internalReader = new StreamReader(inputPath);
        _pool = ArrayPool<char>.Shared;
        _buffer = _pool.Rent(BufferSize);
        _lineStart = 0;
        _totalRead = 0;
        _position = 0;
        _textEnd = 0;
        _charsRead = 0;
        _isDone = false;
        _state = State.Reading;
    }

    internal long TotalRead => _totalRead;

    internal bool EndOfStream => _state == State.EndOfStream;

    internal void Close() => _internalReader.Close();

    internal async Task<LineData> ReadLineAsync()
    {
        if (_isDone)
            return default;

        LineData result = default;

        while (!_isDone)
        {
            switch (_state)
            {
                case State.Reading:
                    _charsRead = await _internalReader.ReadAsync(_buffer, _lineStart, BufferSize - _lineStart);
                    if (_charsRead == 0)
                    {
                        _state = State.EndOfStream;
                        break;
                    }
                    _textEnd = _charsRead + _lineStart;
                    _lineStart = 0;
                    _position = 0;
                    _state = State.Parsing;
                    break;

                case State.Parsing:
                    var bufferSpan = _buffer.AsSpan(0, _textEnd);
                    if (_position < _textEnd - 1)
                    {
                        if (bufferSpan[_position] == '\r')
                        {
                            var lineEnd = bufferSpan[_position + 1] == '\n'
                                ? _position + 2
                                : _position + 1;
                            var lineTextEnd = _position - _lineStart;

                            var lineSpan = bufferSpan.Slice(_lineStart, lineTextEnd);
                            result = ParseLine(lineSpan);

                            _totalRead += lineEnd - _lineStart;
                            _lineStart = lineEnd;
                            _position = lineEnd;

                            _state = State.LineFound;
                            return result; // Return immediately
                        }
                        _position++;
                        break;
                    }
                    _state = State.BufferPartial;
                    break;

                case State.BufferPartial:
                    if (_lineStart < _textEnd)
                    {
                        _buffer.AsSpan(_lineStart, _textEnd - _lineStart).CopyTo(_buffer.AsSpan(0));
                        _lineStart = _textEnd - _lineStart;
                    }
                    else
                    {
                        _lineStart = 0;
                    }
                    _state = State.Reading;
                    break;

                case State.LineFound:
                    // Should not reach here; handled in Parsing
                    _state = State.Parsing;
                    break;

                case State.EndOfStream:
                    _isDone = true;
                    return default;
            }
        }

        return result;
    }

    public void Dispose()
    {
        _internalReader.Dispose();

        if (_buffer != null)
        {
            _pool.Return(_buffer);
        }
    }

    private static LineData ParseLine(ReadOnlySpan<char> line)
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

internal readonly record struct LineData(int Number, ReadOnlyMemory<char> Text, char[] Buffer);