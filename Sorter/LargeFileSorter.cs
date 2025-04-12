using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Sorter;

public class LargeFileSorter
{
    private readonly ConcurrentBag<string> _tempFiles = new ConcurrentBag<string>();

    public async Task SortLargeFile(string input, string output, long chunkSize)
    {
        await SplitAndSort(input, chunkSize);

        await MergeFiles(output);

        foreach (var file in _tempFiles) File.Delete(file);
    }

    private async Task SplitAndSort(string input, long chunkSize)
    {
        var channelCapacity = Environment.ProcessorCount;
        var size = new FileInfo(input).Length / channelCapacity;
        var channel = Channel.CreateBounded<List<LineData>>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var producer = Task.Run(() => Producer2(channel.Writer, input, size));

        var consumers = Parallel.ForEachAsync(
            Enumerable.Range(0, channelCapacity), async (i, ct) => await Consumer(channel.Reader));

        await producer;
        channel.Writer.Complete();

        await consumers;
    }


    private async Task Producer2(
        ChannelWriter<List<LineData>> writer,
        string input,
        long chunkSize)
    {
        var lines = new List<LineData>();

        var reader = new StateMachineFileReader(input);

        long currentSize = 0;
        while (!reader.EndOfStream)
        {
            var nextLine = await reader.ReadLineAsync();

            if (nextLine != default)
            {
                lines.Add(nextLine);
            }

            if (reader.TotalRead - currentSize >= chunkSize || reader.EndOfStream)
            {
                await writer.WriteAsync(lines);

                lines = new List<LineData>(lines.Count);
                currentSize = reader.TotalRead;
            }
        }
    }

    private async Task Producer(
        ChannelWriter<List<LineData>> writer,
        string input,
        long chunkSize)
    {
        const int bufferSize = 8192; // 8KB buffer
        using var reader = new StreamReader(input);
        long currentSize = 0;
        var lines = new List<LineData>();
        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        var lineStart = 0;

        try
        {
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer, lineStart, bufferSize - lineStart)) > 0)
            {
                var textEnd = charsRead + lineStart;
                lineStart = 0;
                var bufferSpan = buffer.AsSpan()[..textEnd];

                for (var i = 0; i < textEnd - 1; i++)
                {
                    if (bufferSpan[i] == '\r')
                    {
                        int lineEnd, lineTextEnd;
                        // if (i == textEnd - 1)
                        // {
                        //     // Edge case when last line has no moving symbols
                        //     lineEnd = i;
                        //     lineTextEnd = i - lineStart + 1;
                        // }
                        // else
                        // {
                            lineEnd = bufferSpan[i + 1] == '\n'
                                ? i + 2
                                : i + 1;
                            lineTextEnd = i - lineStart;
                        // }

                        var lineSpan = bufferSpan.Slice(lineStart, lineTextEnd);
                        var line = ParseLine3(lineSpan);
                        lines.Add(line);

                        currentSize += lineEnd - lineStart;

                        lineStart = lineEnd;
                    }
                }

                if (lineStart < bufferSize) // Partial line at buffer end
                {
                    buffer.AsSpan().Slice(lineStart, bufferSize - lineStart).CopyTo(buffer);
                    lineStart = bufferSize - lineStart;
                }
                else
                {
                    lineStart = 0;
                }

                if (currentSize >= chunkSize || reader.EndOfStream)
                {
                    await writer.WriteAsync(lines);

                    lines = new List<LineData>();
                    currentSize = 0;
                }
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer); // Return buffer to pool
        }
    }

    private async Task Consumer(ChannelReader<List<LineData>> reader)
    {
        await foreach (var lines in reader.ReadAllAsync())
        {
            lines.Sort(new LineComparer());
            var tempFile = Path.GetTempFileName();
            await using (var writer = new StreamWriter(tempFile))
            {
                foreach (var l in lines)
                {
                    writer.Write(l.Number);
                    await writer.WriteAsync(". ");
                    await writer.WriteLineAsync(l.Text);

                    ArrayPool<char>.Shared.Return(l.Buffer);
                }
            }

            _tempFiles.Add(tempFile);
        }
    }

    static async Task<List<string>> SplitAndSort2(string input, long chunkSize)
    {
        const int bufferSize = 8192; // 8KB buffer
        var tempFiles = new List<string>();
        using var reader = new StreamReader(input);
        long currentSize = 0;
        var lines = new List<LineData>();
        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        var lineStart = 0;

        try
        {
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer, lineStart, bufferSize - lineStart)) > 0)
            {
                var textEnd = charsRead + lineStart;
                lineStart = 0;
                var bufferSpan = buffer.AsSpan()[..textEnd];

                for (var i = 0; i < textEnd; i++)
                {
                    if (bufferSpan[i] == '\r' || i == textEnd - 1)
                    {
                        int lineEnd, lineTextEnd;
                        if (i == textEnd - 1)
                        {
                            // Edge case when last line has no moving symbols
                            lineEnd = i;
                            lineTextEnd = i - lineStart + 1;
                        }
                        else
                        {
                            lineEnd = bufferSpan[i + 1] == '\n'
                                ? i + 2
                                : i + 1;
                            lineTextEnd = i - lineStart;
                        }

                        var lineSpan = bufferSpan.Slice(lineStart, lineTextEnd);
                        var line = ParseLine3(lineSpan);
                        lines.Add(line);

                        currentSize += lineEnd - lineStart;

                        lineStart = lineEnd;
                    }
                }

                if (lineStart < charsRead) // Partial line at buffer end
                {
                    buffer.AsSpan().Slice(lineStart, charsRead - lineStart).CopyTo(buffer);
                    lineStart = charsRead - lineStart;
                }
                else
                {
                    lineStart = 0;
                }

                if (currentSize >= chunkSize || reader.EndOfStream)
                {
                    lines.Sort(new LineComparer());
                    var tempFile = Path.GetTempFileName();
                    using (var writer = new StreamWriter(tempFile))
                    {
                        foreach (var l in lines)
                        {
                            writer.Write(l.Number);
                            writer.Write(". ");
                            writer.WriteLine(l.Text.Span);

                            ArrayPool<char>.Shared.Return(l.Buffer);
                        }
                    }

                    tempFiles.Add(tempFile);
                    lines.Clear();
                    currentSize = 0;
                }
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer); // Return buffer to pool
        }

        return tempFiles;
    }

    // private async Task MergeFiles2(string output)
    // {
    //     var heap = new PriorityQueue<LineData, LineData>(new LineComparer());
    //     var readers = new List<(StreamReader Reader, Memory<char> Buffer)>(_tempFiles.Count);
    //
    //     try
    //     {
    //         foreach (var tempFile in _tempFiles)
    //         {
    //             var reader = new StreamReader(tempFile);
    //             var buffer = ArrayPool<char>.Shared.Rent(8192);
    //             int charsRead = await reader.ReadAsync(buffer);
    //             if (charsRead > 0)
    //             {
    //                 var lineSpan = buffer.AsSpan().Slice(0, charsRead);
    //                 int newlineIndex = lineSpan.IndexOf('\n');
    //                 if (newlineIndex != -1)
    //                 {
    //                     LineData lineData = ParseLine3(lineSpan.Slice(0, newlineIndex));
    //                     heap.Enqueue(lineData, lineData);
    //                 }
    //             }
    //
    //             readers.Add((reader, buffer));
    //         }
    //
    //         await using var writer = new StreamWriter(output);
    //         while (heap.Count > 0)
    //         {
    //             var min = heap.Dequeue();
    //             writer.Write(min.Number);
    //             writer.Write(". ");
    //             // writer.WriteLine(min.Text.Span);
    //             ArrayPool<char>.Shared.Return(min.Text.ToArray());
    //             // Add logic to read next line
    //         }
    //     }
    //     finally
    //     {
    //         foreach (var (reader, buffer) in readers)
    //         {
    //             reader?.Close();
    //             if (buffer.Length > 0)
    //                 ArrayPool<char>.Shared.Return(buffer);
    //         }
    //     }
    // }

    private async Task MergeFiles(string output)
    {
        // If no temp files, nothing to merge
        if (_tempFiles.IsEmpty)
        {
            await File.WriteAllTextAsync(output, string.Empty);
            return;
        }

        // If only one temp file, copy it directly to output
        if (_tempFiles.Count == 1)
        {
            File.Copy(_tempFiles.First(), output, overwrite: true);
            return;
        }

        // Priority queue to hold the smallest line from each file
        var heap = new PriorityQueue<(LineData, StateMachineFileReader), LineData>(new LineComparer());

        // Open readers for all temp files and seed the heap with the first line from each
        var readers = new List<StateMachineFileReader>(_tempFiles.Count);
        try
        {
            foreach (var tempFile in _tempFiles)
            {
                var reader = new StateMachineFileReader(tempFile);

                readers.Add(reader);
                if (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    heap.Enqueue((line, reader), line);
                }
            }

            // Merge into the output file
            await using var writer = new StreamWriter(output);
            while (heap.Count > 0)
            {
                // Dequeue the smallest line
                var (min, reader) = heap.Dequeue();

                writer.Write(min.Number);
                writer.Write(". ");
                writer.WriteLine(min.Text.Span);

                ArrayPool<char>.Shared.Return(min.Buffer);

                // Read the next line from the same file, if available
                LineData nextLine;
                if ((nextLine = await reader.ReadLineAsync()) != default)
                {
                    heap.Enqueue((nextLine, reader), nextLine);
                }
                else
                {
                    // Close the reader when its file is fully processed
                    reader.Close();
                    readers.Remove(reader);
                }
            }
        }
        finally
        {
            // Ensure all readers are closed
            foreach (var reader in readers)
            {
                reader?.Close();
            }
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