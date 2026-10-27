using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AslHelp.Logging;

/// <summary>
///     Writes log lines to a file on a dedicated writer thread, so a slow disk never slows down the code that writes a line.
/// </summary>
public sealed partial class FileSink
    : ILogSink
{
    private readonly LogFormatter _formatter;
    private readonly Writer _writer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileSink"/> class appending to <paramref name="path"/>,
    ///     which is trimmed by <paramref name="linesToErase"/> lines whenever it grows past
    ///     <paramref name="maxLines"/>.
    /// </summary>
    /// <param name="path">The file to write to. Its directory is created when it's missing.</param>
    /// <param name="minimumLevel">The lowest level to write.</param>
    /// <param name="maxLines">The number of lines the file may have before it's trimmed.</param>
    /// <param name="linesToErase">The number of lines to erase from the start of the file when it's trimmed.</param>
    /// <param name="formatter">
    ///     The formatter for each line. <see langword="null"/> picks
    ///     <c>LogFormatter.Create</c> for <paramref name="minimumLevel"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="maxLines"/> or <paramref name="linesToErase"/> is less than 1.<br/>
    ///     -or-<br/>
    ///     <paramref name="linesToErase"/> is greater than <paramref name="maxLines"/>.
    /// </exception>
    public FileSink(string path, LogLevel minimumLevel, int maxLines, int linesToErase, LogFormatter? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLines, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(linesToErase, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(linesToErase, maxLines);

        _formatter = formatter ?? LogFormatter.Create(minimumLevel);

        // Created here so an unwritable path throws at construction, not on the writer thread.
        if (System.IO.Path.GetDirectoryName(path) is { Length: > 0 } directory)
        {
            _ = Directory.CreateDirectory(directory);
        }

        Path = path;
        MinimumLevel = minimumLevel;

        _writer = new(path, maxLines, linesToErase);

        _ = Task.Factory.StartNew(
            _writer.Run,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    /// <summary>
    ///     Gets the file being written to.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc/>
    public LogLevel MinimumLevel { get; }

    /// <inheritdoc/>
    public void Write(in LogEvent logEvent, int indent)
    {
        _writer.Enqueue(_formatter(in logEvent, indent));
    }

    /// <summary>
    ///     Stops the writer and waits up to 2 seconds for the queued lines to reach the file before closing it.
    /// </summary>
    public void Dispose()
    {
        _writer.Complete();
        _writer.WaitForCompletion();
    }
}
