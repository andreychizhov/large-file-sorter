using System.Buffers;

namespace Sorter;

/// <summary>
/// Custom reader that reads file line-by-line efficiently. Leverages Span
/// and Memory data types and array pooling to minimize string allocations
/// and reduce garbage collection overhead. This is an attempt to create a more
/// efficient alternative to StreamReader's ReadLineAsync method.
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

    public long TotalRead => _totalRead;

    public bool EndOfStream => _state == State.EndOfStream;

    public void Close() => _internalReader.Close();

    public async Task<LineData> ReadLineAsync()
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
                        // Edge case when last line has no moving symbol(s) is not covered here
                        // to not overwhelm solution with unnecessary details. Let's assume that
                        // input file always has last line followed by new line
                        // Let's also assume that all the lines fit entirely to 8KB buffer
                        if (bufferSpan[_position] == '\r')
                        {
                            var lineEnd = bufferSpan[_position + 1] == '\n'
                                ? _position + 2
                                : _position + 1;
                            var lineTextEnd = _position - _lineStart;

                            var lineSpan = bufferSpan.Slice(_lineStart, lineTextEnd);
                            result = LineParser.Parse(lineSpan);

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
}