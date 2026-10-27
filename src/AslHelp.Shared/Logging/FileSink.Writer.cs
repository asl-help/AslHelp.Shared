using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace AslHelp.Logging;

public partial class FileSink
{
    /// <summary>
    ///     Writes the queued lines to the file from the writer thread.
    /// </summary>
    private sealed class Writer(
        string path,
        int maxLines,
        int linesToErase)
    {
        private readonly BlockingCollection<string> _queue = [];
        private readonly TaskCompletionSource<bool> _drained = new();

        public void Enqueue(string line)
        {
            try
            {
                _queue.Add(line);
            }
            // Writing after disposal drops the line.
            catch (InvalidOperationException) { }
        }

        public void Complete()
        {
            _queue.CompleteAdding();
        }

        /// <summary>
        ///     Waits up to 2 seconds for the queued lines to reach the file, so a stuck disk never blocks disposal.
        /// </summary>
        public void WaitForCompletion()
        {
            _ = _drained.Task.Wait(TimeSpan.FromSeconds(2));
        }

        public void Run()
        {
            StreamWriter? writer = null;

            try
            {
                int lines = CountLines();

                // Opens the file before the first line, so a sink nothing writes to still leaves its file behind.
                try
                {
                    writer = Open();
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }

                foreach (string line in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        writer ??= Open();
                        writer.WriteLine(line);
                        lines++;

                        if (lines > maxLines)
                        {
                            writer.Dispose();
                            writer = null;
                            lines = TruncateHead(lines);
                        }
                    }
                    // Drops the line when another process deleted or locked the file and opens the file again for the
                    // next line, so one failure never stops the writer.
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                        writer?.Dispose();
                        writer = null;
                    }
                }
            }
            finally
            {
                writer?.Dispose();

                // Stops taking lines if the loop ever ends early, so a line queued after it is dropped, not kept.
                _queue.CompleteAdding();
                _ = _drained.TrySetResult(true);
            }
        }

        // Opens with `FileShare.ReadWrite`, so a reader tailing the file can't break the write.
        private StreamWriter Open()
        {
            FileStream stream = new(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

            return new(stream) { AutoFlush = true };
        }

        // Counts the lines already in the file and returns 0 when the file can't be read, because a late first trim
        // beats a writer that never starts.
        private int CountLines()
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            int lines = 0;

            try
            {
                using StreamReader reader = new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                while (reader.ReadLine() is not null)
                {
                    lines++;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return 0;
            }

            return lines;
        }

        // Rewrites the log through a temporary file, so a reader never sees a half-written log. When another process
        // has the log open without delete sharing, the rewrite fails and the log grows by `linesToErase` more lines
        // before the next try.
        private int TruncateHead(int lines)
        {
            string temp = path + ".trim";

            try
            {
                return Rewrite(temp);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                TryDelete(temp);

                return lines - linesToErase;
            }
        }

        private int Rewrite(string temp)
        {
            int kept = 0;

            // The path overloads open with FileShare.Read, which the sink's own writer already rules out.
            using (StreamReader reader = new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (StreamWriter writer = new(new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                for (int i = 0; i < linesToErase && reader.ReadLine() is not null; i++)
                { }

                while (reader.ReadLine() is { } line)
                {
                    writer.WriteLine(line);
                    kept++;
                }
            }

            File.Replace(temp, path, null);

            return kept;
        }

        private static void TryDelete(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}
