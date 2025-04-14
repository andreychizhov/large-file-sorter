using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Sorter;

public class LargeFileSorter
{
    private const long MaxChunkSize = 1_073_741_824; // 1GB
    private const int MinChunkSize = 10 * 1 << 10; // 10KB

    private readonly ConcurrentBag<string> _tempFiles = new ConcurrentBag<string>();

    public async Task SortFile(string input, string output)
    {
        await SplitAndSort(input);

        await MergeFiles(output);

        foreach (var file in _tempFiles) File.Delete(file);
    }

    private async Task SplitAndSort(string input)
    {
        // Partition algorithm: create one temp file per worker thread if input
        // is less than 16 GB (could be tuned) and sort them concurrently in memory;
        // in case when input is bigger, create more files
        var channelCapacity = Environment.ProcessorCount;
        var inputSize = new FileInfo(input).Length;

        var chunkSize = GetChunkSize(inputSize, channelCapacity);

        // Limit the number of chunks to process in parallel; use asynchronous producer-consumer pattern
        var channel = Channel.CreateBounded<List<LineData>>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var producer = Task.Run(() => Producer(channel.Writer, input, chunkSize));

        var consumers = Parallel.ForEachAsync(
            Enumerable.Range(0, channelCapacity), async (i, ct) => await Consumer(channel.Reader));

        await producer;
        channel.Writer.Complete();

        await consumers;
    }

    private static long GetChunkSize(long inputSize, int channelCapacity)
    {
        if (inputSize < MinChunkSize)
            return MinChunkSize;

        return inputSize > MaxChunkSize * channelCapacity
            ? MaxChunkSize
            : inputSize / channelCapacity;
    }


    private async Task Producer(
        ChannelWriter<List<LineData>> writer,
        string input,
        long chunkSize)
    {
        var lines = new List<LineData>();

        var reader = new LargeFileReader(input);

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
        var heap = new PriorityQueue<(LineData, LargeFileReader), LineData>(new LineComparer());

        // Open readers for all temp files and seed the heap with the first line from each
        var readers = new List<LargeFileReader>(_tempFiles.Count);
        try
        {
            foreach (var tempFile in _tempFiles)
            {
                var reader = new LargeFileReader(tempFile);

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
}