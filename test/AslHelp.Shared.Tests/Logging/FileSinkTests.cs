using AslHelp.Logging;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace AslHelp.Tests.Logging;

[TestFixture]
public class FileSinkTests
{
    private string _directory = null!;

    [SetUp]
    public void CreateDirectory()
    {
        _directory = Path.GetTempPath().Replace('\\', '/') + $"aslhelp-log-{Guid.NewGuid():N}";
        _ = Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void DeleteDirectory()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException) { }
    }

    [Test]
    public void Dispose_LinesStillQueued_WritesThemBeforeClosing()
    {
        string path = _directory + "/tail.log";

        using (FileSink sink = new(path, LogLevel.Trace, maxLines: 4096, linesToErase: 512))
        {
            for (int i = 0; i < 200; i++)
            {
                sink.Write(new LogEvent(LogLevel.Information, "", $"line {i}", "", "", 0), 0);
            }
        }

        Assert.That(File.ReadAllLines(path), Has.Length.EqualTo(200));
    }

    [Test]
    public void Write_AWarningUnderAPrefix_WritesTheLevelAndTheMessage()
    {
        string path = _directory + "/format.log";

        using (FileSink sink = new(path, LogLevel.Trace, maxLines: 4096, linesToErase: 512))
        {
            sink.Write(new LogEvent(LogLevel.Warning, "asl-help", "careful", "", "", 0), 0);
        }

        string line = File.ReadAllLines(path).Single();

        Assert.That(line, Does.Contain("[warn]"));
        Assert.That(line, Does.Contain("[asl-help] careful"));
    }

    [Test]
    public void Write_AfterDispose_DropsTheLineInsteadOfThrowing()
    {
        string path = _directory + "/disposed.log";
        FileSink sink = new(path, LogLevel.Trace, maxLines: 4096, linesToErase: 512);

        sink.Write(new LogEvent(LogLevel.Information, "", "before", "", "", 0), 0);
        sink.Dispose();

        Assert.DoesNotThrow(() => sink.Write(new LogEvent(LogLevel.Information, "", "after", "", "", 0), 0));

        string[] lines = File.ReadAllLines(path);

        Assert.That(lines, Has.Length.EqualTo(1));
        Assert.That(lines[0], Does.EndWith("before"));
    }

    [Test]
    public void Write_PastTheMaximum_DropsTheOldestLines()
    {
        string path = _directory + "/rotate.log";

        using (FileSink sink = new(path, LogLevel.Trace, maxLines: 20, linesToErase: 10))
        {
            for (int i = 0; i < 25; i++)
            {
                sink.Write(new LogEvent(LogLevel.Information, "", $"line {i}", "", "", 0), 0);
            }
        }

        string[] lines = File.ReadAllLines(path);

        // The 21st line trims the 10 oldest lines and leaves lines 10 to 20, which the last 4 lines follow.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(lines, Has.Length.EqualTo(15));
            Assert.That(lines[0], Does.EndWith("line 10"));
            Assert.That(lines[^1], Does.EndWith("line 24"));
        }
    }

    // A reader that opens the log without delete sharing makes the trim fail, which must not stop the writer.
    [Test]
    [Platform("win")]
    public void Write_PastTheMaximumWhileAReaderHoldsTheFile_KeepsWriting()
    {
        string path = _directory + "/held.log";
        File.WriteAllText(path, "");

        using (FileStream reader = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (FileSink sink = new(path, LogLevel.Trace, maxLines: 20, linesToErase: 10))
        {
            for (int i = 0; i < 25; i++)
            {
                sink.Write(new LogEvent(LogLevel.Information, "", $"line {i}", "", "", 0), 0);
            }
        }

        string[] lines = File.ReadAllLines(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lines, Has.Length.EqualTo(25));
            Assert.That(lines[^1], Does.EndWith("line 24"));
        }
    }

    [Test]
    public void Open_WhileAnotherReaderHoldsTheFile_StillWrites()
    {
        string path = _directory + "/shared.log";
        File.WriteAllText(path, "existing" + Environment.NewLine);

        using FileStream reader = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        using (FileSink sink = new(path, LogLevel.Trace, maxLines: 4096, linesToErase: 512))
        {
            sink.Write(new LogEvent(LogLevel.Information, "", "appended", "", "", 0), 0);
        }

        Assert.That(File.ReadAllText(path), Does.Contain("appended"));
    }

    [Test]
    public void Construct_WithGarbageBounds_ThrowsSayingTheArgument()
    {
        string path = _directory + "/bounds.log";

        Assert.That(() => new FileSink(path, LogLevel.Information, maxLines: 0, linesToErase: 1),
            Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("maxLines"));

        Assert.That(() => new FileSink(path, LogLevel.Information, maxLines: 10, linesToErase: 11),
            Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("linesToErase"));
    }
}
