using System.Buffers;
using Sorter;

var inputFile = "test.txt";
var outputFile = "sorted.txt";
var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

await new LargeFileSorter().SortLargeFile(
    Path.Combine(homeDirectory, inputFile),
    Path.Combine(homeDirectory, outputFile),
    chunkSize: 1_073_741_824); // 1GB

// static async Task MergeFiles(List<string> tempFiles, string output)
// {
//     // If no temp files, nothing to merge
//     if (tempFiles.Count == 0)
//     {
//         await File.WriteAllTextAsync(output, string.Empty);
//         return;
//     }
//     // If only one temp file, copy it directly to output
//     if (tempFiles.Count == 1)
//     {
//         File.Copy(tempFiles[0], output, overwrite: true);
//         return;
//     }
//
//     // Priority queue to hold the smallest line from each file
//     var heap = new PriorityQueue<(int Number, string Text, int FileIndex, StreamReader Reader),
//                                  (int Number, string Text)>(new HeapLineComparer());
//
//     // Open readers for all temp files and seed the heap with the first line from each
//     var readers = new List<StreamReader>(tempFiles.Count);
//     try
//     {
//         for (var i = 0; i < tempFiles.Count; i++)
//         {
//             var reader = new StreamReader(tempFiles[i]);
//             readers.Add(reader);
//             if (!reader.EndOfStream)
//             {
//                 var line = reader.ReadLine();
//                 var (number, text) = ParseLine(line);
//                 heap.Enqueue((number, text, i, reader), (number, text));
//             }
//         }
//
//         // Merge into the output file
//         await using var writer = new StreamWriter(output);
//         while (heap.Count > 0)
//         {
//             // Dequeue the smallest line
//             var (number, text, fileIndex, reader) = heap.Dequeue();
//             writer.WriteLine($"{number}. {text}");
//
//             // Read the next line from the same file, if available
//             if (!reader.EndOfStream)
//             {
//                 var nextLine = await reader.ReadLineAsync();
//                 var (nextNumber, nextText) = ParseLine(nextLine);
//                 heap.Enqueue((nextNumber, nextText, fileIndex, reader), (nextNumber, nextText));
//             }
//             else
//             {
//                 // Close the reader when its file is fully processed
//                 reader.Close();
//                 readers[fileIndex] = null;
//             }
//         }
//     }
//     finally
//     {
//         // Ensure all readers are closed
//         foreach (var reader in readers)
//         {
//             reader?.Close();
//         }
//     }
// }
//
// static async Task MergeFiles2(List<string> tempFiles, string output)
// {
//     var heap = new PriorityQueue<LineData, LineData>(new LineComparer());
//     var readers = new List<(StreamReader Reader, Memory<char> Buffer)>(tempFiles.Count);
//     var pool = ArrayPool<char>.Shared;
//
//     try
//     {
//         for (int i = 0; i < tempFiles.Count; i++)
//         {
//             var reader = new StreamReader(tempFiles[i]);
//             var buffer = pool.Rent(8192);
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
//             pool.Return(min.Text.ToArray());
//             // Add logic to read next line
//         }
//     }
//     finally
//     {
//         foreach (var (reader, buffer) in readers)
//         {
//             reader?.Close();
//             if (buffer.Length > 0) pool.Return(buffer.ToArray());
//         }
//     }
// }
//
// static (int Number, string Text) ParseLine(ReadOnlySpan<char> line)
// {
//     var dotIndex = line.IndexOf(". ");
//     if (dotIndex == -1)
//         throw new FormatException("Invalid line format.");
//
//     var numberSpan = line.Slice(0, dotIndex);
//     var textSpan = line.Slice(dotIndex + 2);
//
//     if (!int.TryParse(numberSpan, out var number))
//         throw new FormatException("Invalid number format.");
//
//     return (number, textSpan.ToString());
// }


internal readonly record struct LineData(int Number, ReadOnlyMemory<char> Text, char[] Buffer);