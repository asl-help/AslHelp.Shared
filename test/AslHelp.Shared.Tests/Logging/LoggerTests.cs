using AslHelp.Logging;
using AslHelp.Options;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AslHelp.Tests.Logging;

[TestFixture]
public class LoggerTests
{
    private static LogEvent Event(Logger logger, LogLevel level, string message)
    {
        return new(level, logger.Prefix, message, "", "", 0);
    }

    [Test]
    public void Log_BelowASinksMinimum_NeverReachesIt()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Warning);
        logger.Add(sink);

        logger.Log(Event(logger, LogLevel.Information, "chatter"));
        logger.Log(Event(logger, LogLevel.Warning, "worth knowing"));

        string[] expected = ["worth knowing"];

        Assert.That(sink.Lines.Select(l => l.Event.Message), Is.EqualTo(expected));
    }

    [Test]
    public void Log_SinksAtDifferentLevels_EachGetsItsOwnShare()
    {
        using Logger logger = new();
        RecordingSink quiet = new(LogLevel.Error);
        RecordingSink verbose = new(LogLevel.Trace);
        logger.Add(quiet);
        logger.Add(verbose);

        logger.Log(Event(logger, LogLevel.Debug, "detail"));
        logger.Log(Event(logger, LogLevel.Error, "broken"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(quiet.Lines, Has.Count.EqualTo(1));
            Assert.That(verbose.Lines, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void WithPrefix_TwoVoicesOverOneLogger_WritesEachUnderItsOwnOverTheSameSinks()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        var library = logger.WithPrefix("asl-help");
        var script = logger.WithPrefix("Celeste");

        library.Log(Event(library, LogLevel.Information, "attached"));
        script.Log(Event(script, LogLevel.Information, "hp 42"));

        string[] voices = ["asl-help", "Celeste"];

        Assert.That(sink.Lines.Select(l => l.Event.Prefix), Is.EqualTo(voices));
    }

    [Test]
    public void WithPrefix_ASinkAddedAfterTheView_IsWrittenToAsWell()
    {
        using Logger logger = new();
        var view = logger.WithPrefix("asl-help");

        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        view.Log(Event(view, LogLevel.Information, "late"));

        Assert.That(sink.Lines, Has.Count.EqualTo(1));
    }

    [Test]
    public void BeginScope_LinesWrittenInsideIt_AreIndentedUntilItIsDisposed()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        logger.Log(Event(logger, LogLevel.Information, "outer"));

        using (logger.BeginScope())
        {
            logger.Log(Event(logger, LogLevel.Information, "inner"));

            using (logger.BeginScope())
            {
                logger.Log(Event(logger, LogLevel.Information, "deeper"));
            }
        }

        logger.Log(Event(logger, LogLevel.Information, "after"));

        int[] depths = [0, 1, 2, 0];

        Assert.That(sink.Lines.Select(l => l.Indent), Is.EqualTo(depths));
    }

    // A scope opened inside an awaited method raised the depth only in that method's context, so the caller disposing
    // it must not take its own depth below zero.
    [Test]
    public async Task BeginScope_OpenedInAnAwaitedMethodAndDisposedByTheCaller_LeavesTheCallersDepth()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        var scope = await OpenScope(logger).ConfigureAwait(false);
        scope.Dispose();
        logger.Log(Event(logger, LogLevel.Information, "after"));

        Assert.That(sink.Lines.Single().Indent, Is.Zero);
    }

    // A prefixed view shares its parent's sinks, so disposing the view must leave them open for the parent.
    [Test]
    public void Dispose_APrefixedView_LeavesTheSinksOpen()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        logger.WithPrefix("view").Dispose();
        logger.Log(Event(logger, LogLevel.Information, "still here"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sink.Disposed, Is.False);
            Assert.That(sink.Lines.Single().Event.Message, Is.EqualTo("still here"));
        }
    }

    [Test]
    public void IsEnabled_WithNoSinkThatWould_IsFalse()
    {
        using Logger logger = new();

        Assert.That(logger.IsEnabled(LogLevel.Error), Is.False);
    }

    [Test]
    public void IsEnabled_WithASinkTurnedOff_IsFalse()
    {
        using Logger logger = new();
        logger.Add(new RecordingSink(LogLevel.Off));

        Assert.That(logger.IsEnabled(LogLevel.Error), Is.False);
    }

    [Test]
    public void IsEnabled_SinksAtDifferentLevels_TakesTheLowestAnyOfThemAccepts()
    {
        using Logger logger = new();
        logger.Add(new RecordingSink(LogLevel.Error));
        logger.Add(new RecordingSink(LogLevel.Debug));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logger.IsEnabled(LogLevel.Debug), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Trace), Is.False);
        }
    }

    [Test]
    public void IsEnabled_OnAPrefixedLogger_SeesASinkTheOriginalAdded()
    {
        using Logger logger = new();
        var view = logger.WithPrefix("asl-help");

        logger.Add(new RecordingSink(LogLevel.Trace));

        Assert.That(view.IsEnabled(LogLevel.Trace), Is.True);
    }

    [Test]
    public void GetSink_WithNoSinkOfThatType_IsNull()
    {
        using Logger logger = new();

        Assert.That(logger.GetSink<RecordingSink>(), Is.Null);
    }

    [Test]
    public void Dispose_ASinkStillAttached_DisposesItAndStopsWriting()
    {
        Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        logger.Dispose();
        logger.Log(Event(logger, LogLevel.Error, "after"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sink.Disposed, Is.True);
            Assert.That(sink.Lines, Is.Empty);
        }
    }

    [Test]
    public void Add_WithNoSink_ThrowsSayingTheArgument()
    {
        using Logger logger = new();

        Assert.That(() => logger.Add(null!), Throws.ArgumentNullException);
    }

    // Every tag is the same width, so the level column stays aligned.
    [TestCase(LogLevel.Trace)]
    [TestCase(LogLevel.Debug)]
    [TestCase(LogLevel.Information)]
    [TestCase(LogLevel.Warning)]
    [TestCase(LogLevel.Error)]
    [TestCase(LogLevel.Off)]
    public void Plain_AnyLevel_HasAFourColumnTag(LogLevel level)
    {
        LogEvent e = new(level, "", "line", "", "", 0);

        Assert.That(LogFormatter.Plain(in e, 0), Does.Match(@"^\[.{4}\] line$"));
    }

    // A formatted line's tag and the alias an options file parses are the same spelling.
    [TestCase(LogLevel.Trace)]
    [TestCase(LogLevel.Debug)]
    [TestCase(LogLevel.Information)]
    [TestCase(LogLevel.Warning)]
    [TestCase(LogLevel.Error)]
    public void Plain_AnyLevel_ShowsTheLevelsAlias(LogLevel level)
    {
        LogEvent e = new(level, "", "line", "", "", 0);
        string alias = typeof(LogLevel).GetField(level.ToString())!.GetCustomAttribute<ChoiceAliasAttribute>()!.Spelling;

        Assert.That(LogFormatter.Plain(in e, 0), Is.EqualTo($"[{alias}] line"));
    }

    private static async Task<IDisposable> OpenScope(Logger logger)
    {
        await Task.Yield();

        return logger.BeginScope();
    }
}
