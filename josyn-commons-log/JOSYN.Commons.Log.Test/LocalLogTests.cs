using NUnit.Framework;
using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.Log.Test;

[TestFixture]
[NonParallelizable]
public sealed class LocalLogTests
{
    private string _testDir = null!;
    private string _savedDir = null!;
    private bool _savedConsole;

    [SetUp]
    public void SetUp()
    {
        _savedDir = LocalLog.LogDirectory;
        _savedConsole = LocalLog.EnableConsoleOutput;

        _testDir = Path.Combine(
            Path.GetTempPath(),
            "JOSYN.Commons.Log.Test",
            TestContext.CurrentContext.Test.ID);
        Directory.CreateDirectory(_testDir);

        LocalLog.LogDirectory = _testDir;
        LocalLog.EnableConsoleOutput = false;
    }

    [TearDown]
    public void TearDown()
    {
        LocalLog.LogDirectory = _savedDir;
        LocalLog.EnableConsoleOutput = _savedConsole;

        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    // ── Format ────────────────────────────────────────────────────────────────

    [Test]
    public void WriteError_ContainsErrorLevelHeader()
    {
        LocalLog.WriteError("msg");

        Assert.That(ReadLog(_testDir), Does.Contain("[ERROR]"));
    }

    [Test]
    public void WriteInfo_ContainsInfoLevelHeader()
    {
        LocalLog.WriteInfo("msg");

        Assert.That(ReadLog(_testDir), Does.Contain("[INFO]"));
    }

    [Test]
    public void WriteError_ContainsMessage()
    {
        LocalLog.WriteError("Verbindung fehlgeschlagen.");

        Assert.That(ReadLog(_testDir), Does.Contain("Verbindung fehlgeschlagen."));
    }

    [Test]
    public void WriteError_AlwaysContains80DashSeparator()
    {
        LocalLog.WriteError("msg");

        Assert.That(ReadLog(_testDir), Does.Contain(new string('-', 80)));
    }

    [Test]
    public void WriteError_WithCallStack_IncludesCallStackSection()
    {
        LocalLog.WriteError("msg", callStack: "  at SomeClass.SomeMethod (File.cs:42)");

        var content = ReadLog(_testDir);
        Assert.That(content, Does.Contain("--- CallStack ---"));
        Assert.That(content, Does.Contain("at SomeClass.SomeMethod"));
    }

    [Test]
    public void WriteError_WithoutCallStack_OmitsCallStackSection()
    {
        LocalLog.WriteError("msg", callStack: null);

        Assert.That(ReadLog(_testDir), Does.Not.Contain("--- CallStack ---"));
    }

    [Test]
    public void WriteError_WithEmptyCallStack_OmitsCallStackSection()
    {
        LocalLog.WriteError("msg", callStack: "");

        Assert.That(ReadLog(_testDir), Does.Not.Contain("--- CallStack ---"));
    }

    [Test]
    public void WriteError_WithExceptionDetails_IncludesExceptionSection()
    {
        LocalLog.WriteError("msg", exceptionDetails: "System.IO.IOException: Zugriff verweigert.");

        var content = ReadLog(_testDir);
        Assert.That(content, Does.Contain("--- Exception ---"));
        Assert.That(content, Does.Contain("System.IO.IOException"));
    }

    [Test]
    public void WriteError_WithoutExceptionDetails_OmitsExceptionSection()
    {
        LocalLog.WriteError("msg", exceptionDetails: null);

        Assert.That(ReadLog(_testDir), Does.Not.Contain("--- Exception ---"));
    }

    // ── Routing ───────────────────────────────────────────────────────────────

    [Test]
    public void WriteError_NoCauser_WritesToLogDirectoryRoot()
    {
        LocalLog.WriteError("msg");

        Assert.That(File.Exists(TodaysLogFile(_testDir)), Is.True);
    }

    [Test]
    public void WriteInfo_NoCauser_WritesToLogDirectoryRoot()
    {
        LocalLog.WriteInfo("msg");

        Assert.That(File.Exists(TodaysLogFile(_testDir)), Is.True);
    }

    [Test]
    public void WriteError_WithCauser_WritesToCauserSubdirectory()
    {
        LocalLog.WriteError(causer: "JobHost", message: "msg");

        Assert.That(File.Exists(TodaysLogFile(_testDir, "JobHost")), Is.True);
        Assert.That(File.Exists(TodaysLogFile(_testDir)), Is.False);
    }

    [Test]
    public void WriteInfo_WithCauser_WritesToCauserSubdirectory()
    {
        LocalLog.WriteInfo("JobHost", "msg");

        Assert.That(File.Exists(TodaysLogFile(_testDir, "JobHost")), Is.True);
        Assert.That(File.Exists(TodaysLogFile(_testDir)), Is.False);
    }

    [Test]
    public void WriteError_DifferentCausers_WriteToSeparateSubdirectories()
    {
        LocalLog.WriteError(causer: "ServiceA", message: "msg A");
        LocalLog.WriteError(causer: "ServiceB", message: "msg B");

        Assert.That(ReadLog(_testDir, "ServiceA"), Does.Contain("msg A"));
        Assert.That(ReadLog(_testDir, "ServiceB"), Does.Contain("msg B"));
        Assert.That(ReadLog(_testDir, "ServiceA"), Does.Not.Contain("msg B"));
    }

    // ── Result overloads ──────────────────────────────────────────────────────

    [Test]
    public void WriteError_FromResult_ContainsErrorMessage()
    {
        var result = Result.Fail("Datenbankverbindung unterbrochen.");

        LocalLog.WriteError(result);

        Assert.That(ReadLog(_testDir), Does.Contain("Datenbankverbindung unterbrochen."));
    }

    [Test]
    public void WriteError_FromResult_ContainsCallStack()
    {
        var result = Result.Fail("msg");

        LocalLog.WriteError(result);

        Assert.That(ReadLog(_testDir), Does.Contain("--- CallStack ---"));
    }

    [Test]
    public void WriteError_FromResult_WithException_ContainsExceptionSection()
    {
        Result result = Result.Fail("msg", new InvalidOperationException("Ungültiger Zustand."));

        LocalLog.WriteError(result);

        var content = ReadLog(_testDir);
        Assert.That(content, Does.Contain("--- Exception ---"));
        Assert.That(content, Does.Contain("InvalidOperationException"));
    }

    [Test]
    public void WriteError_WithCauser_FromResult_RoutesToSubdirectory()
    {
        var result = Result.Fail("msg");

        LocalLog.WriteError(causer: "JobHost", result: result);

        Assert.That(File.Exists(TodaysLogFile(_testDir, "JobHost")), Is.True);
    }

    // ── Resilience ────────────────────────────────────────────────────────────

    [Test]
    public void WriteError_InvalidDirectory_DoesNotThrow()
    {
        LocalLog.LogDirectory = @"Z:\does\not\exist\at\all\";

        Assert.DoesNotThrow(() => LocalLog.WriteError("msg"));
    }

    [Test]
    public void WriteInfo_InvalidDirectory_DoesNotThrow()
    {
        LocalLog.LogDirectory = @"Z:\does\not\exist\at\all\";

        Assert.DoesNotThrow(() => LocalLog.WriteInfo("msg"));
    }

    // ── Console output ────────────────────────────────────────────────────────

    [Test]
    public void WriteError_WhenConsoleOutputEnabled_WritesToConsole()
    {
        LocalLog.EnableConsoleOutput = true;
        var originalOut = Console.Out;
        var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);
            LocalLog.WriteError("Konsoleneintrag erwartet.");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.That(sw.ToString(), Does.Contain("Konsoleneintrag erwartet."));
    }

    [Test]
    public void WriteError_WhenConsoleOutputDisabled_DoesNotWriteToConsole()
    {
        LocalLog.EnableConsoleOutput = false;
        var originalOut = Console.Out;
        var sw = new StringWriter();
        try
        {
            Console.SetOut(sw);
            LocalLog.WriteError("Kein Konsoleneintrag.");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.That(sw.ToString(), Is.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string TodaysLogFile(string dir, string? causer = null) =>
        causer is null
            ? Path.Combine(dir, $"{DateTimeOffset.Now:yyyy-MM-dd}.log")
            : Path.Combine(dir, causer, $"{DateTimeOffset.Now:yyyy-MM-dd}.log");

    private static string ReadLog(string dir, string? causer = null) =>
        File.ReadAllText(TodaysLogFile(dir, causer));
}
