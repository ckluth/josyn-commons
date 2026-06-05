using NUnit.Framework;

namespace JOSYN.Commons.Log.Test;

/// <summary>
/// Pure unit tests for <see cref="LocalLog.FormatEntry"/> — no I/O involved.
/// </summary>
[TestFixture]
public sealed class LocalLogFormatEntryTests
{
    // ── Header ────────────────────────────────────────────────────────────────

    [Test]
    public void FormatEntry_Header_ContainsTimestampPattern()
    {
        var result = LocalLog.FormatEntry("INFO", "msg");

        Assert.That(result, Does.Match(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2}\]"));
    }

    [TestCase("INFO")]
    [TestCase("ERROR")]
    [TestCase("WARN")]
    public void FormatEntry_Header_ContainsLevel(string level)
    {
        var result = LocalLog.FormatEntry(level, "msg");

        Assert.That(result, Does.Contain($"[{level}]"));
    }

    // ── Message ───────────────────────────────────────────────────────────────

    [Test]
    public void FormatEntry_Message_AppearsInOutput()
    {
        var result = LocalLog.FormatEntry("INFO", "Verbindung fehlgeschlagen.");

        Assert.That(result, Does.Contain("Verbindung fehlgeschlagen."));
    }

    // ── CallStack section ─────────────────────────────────────────────────────

    [Test]
    public void FormatEntry_WithCallStack_IncludesCallStackHeader()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", callStack: "  at Foo.Bar (Foo.cs:12)");

        Assert.That(result, Does.Contain("--- CallStack ---"));
    }

    [Test]
    public void FormatEntry_WithCallStack_IncludesCallStackContent()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", callStack: "  at Foo.Bar (Foo.cs:12)");

        Assert.That(result, Does.Contain("at Foo.Bar (Foo.cs:12)"));
    }

    [Test]
    public void FormatEntry_WithNullCallStack_OmitsCallStackSection()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", callStack: null);

        Assert.That(result, Does.Not.Contain("--- CallStack ---"));
    }

    [Test]
    public void FormatEntry_WithEmptyCallStack_OmitsCallStackSection()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", callStack: "");

        Assert.That(result, Does.Not.Contain("--- CallStack ---"));
    }

    // ── Exception section ─────────────────────────────────────────────────────

    [Test]
    public void FormatEntry_WithExceptionDetails_IncludesExceptionHeader()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", exceptionDetails: "System.IO.IOException: ...");

        Assert.That(result, Does.Contain("--- Exception ---"));
    }

    [Test]
    public void FormatEntry_WithExceptionDetails_IncludesExceptionContent()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", exceptionDetails: "System.IO.IOException: Zugriff verweigert.");

        Assert.That(result, Does.Contain("System.IO.IOException: Zugriff verweigert."));
    }

    [Test]
    public void FormatEntry_WithNullExceptionDetails_OmitsExceptionSection()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", exceptionDetails: null);

        Assert.That(result, Does.Not.Contain("--- Exception ---"));
    }

    [Test]
    public void FormatEntry_WithEmptyExceptionDetails_OmitsExceptionSection()
    {
        var result = LocalLog.FormatEntry("ERROR", "msg", exceptionDetails: "");

        Assert.That(result, Does.Not.Contain("--- Exception ---"));
    }

    // ── Separator ─────────────────────────────────────────────────────────────

    [Test]
    public void FormatEntry_AlwaysContains80DashSeparator()
    {
        var result = LocalLog.FormatEntry("INFO", "msg");

        Assert.That(result, Does.Contain(new string('-', 80)));
    }

    [Test]
    public void FormatEntry_SeparatorIsExactly80Dashes_NotMore()
    {
        var result = LocalLog.FormatEntry("INFO", "msg");

        Assert.That(result, Does.Not.Contain(new string('-', 81)));
    }

    // ── Section order ─────────────────────────────────────────────────────────

    [Test]
    public void FormatEntry_SectionOrder_MessageBeforeCallStackBeforeException()
    {
        var result = LocalLog.FormatEntry("ERROR", "the message",
            callStack: "  at Foo (Foo.cs:1)",
            exceptionDetails: "System.Exception: boom");

        var idxMessage   = result.IndexOf("the message",          StringComparison.Ordinal);
        var idxCallStack = result.IndexOf("--- CallStack ---",    StringComparison.Ordinal);
        var idxException = result.IndexOf("--- Exception ---",    StringComparison.Ordinal);
        var idxSeparator = result.IndexOf(new string('-', 80),    StringComparison.Ordinal);

        Assert.That(idxMessage,   Is.LessThan(idxCallStack));
        Assert.That(idxCallStack, Is.LessThan(idxException));
        Assert.That(idxException, Is.LessThan(idxSeparator));
    }
}
