using System.Text;
using JOSYN.Commons.Helpers;
using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.Log;

/// <inheritdoc cref="ILocalLog"/>
public static class LocalLog
{
    /// <inheritdoc cref="ILocalLog.LogDirectory"/>
    public static string LogDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "logs");

    /// <inheritdoc cref="ILocalLog.EnableConsoleOutput"/>
    public static bool EnableConsoleOutput { get; set; } = false;

    /// <inheritdoc cref="ILocalLog.WriteError(string, string?, string?)"/>
    public static void WriteError(string message, string? callStack = null, string? exceptionDetails = null)
    {
        var entry = FormatEntry(levelError, message, callStack, exceptionDetails);
        AppendEntry(LogDirectory, entry);
        if (EnableConsoleOutput)
            PrintEntry(entry, ConsoleColor.Red);
    }

    /// <inheritdoc cref="ILocalLog.WriteError(Result)"/>
    public static void WriteError(Result result) =>
        WriteError(result.ErrorMessage ?? string.Empty, result.CallStackAsString, result.Exception?.ToString());

    /// <inheritdoc cref="ILocalLog.WriteError(string, string, string?, string?)"/>
    public static void WriteError(string causer, string message, string? callStack = null, string? exceptionDetails = null)
    {
        var entry = FormatEntry(levelError, message, callStack, exceptionDetails);
        AppendEntry(Path.Combine(LogDirectory, causer), entry);
        if (EnableConsoleOutput)
            PrintEntry(entry, ConsoleColor.Red);
    }

    /// <inheritdoc cref="ILocalLog.WriteError(string, Result)"/>
    public static void WriteError(string causer, Result result) =>
        WriteError(causer, result.ErrorMessage ?? string.Empty, result.CallStackAsString, result.Exception?.ToString());

    /// <inheritdoc cref="ILocalLog.WriteInfo(string)"/>
    public static void WriteInfo(string message)
    {
        var entry = FormatEntry(levelInfo, message);
        AppendEntry(LogDirectory, entry);
        if (EnableConsoleOutput)
            PrintEntry(entry, ConsoleColor.Gray);
    }

    /// <inheritdoc cref="ILocalLog.WriteInfo(string, string)"/>
    public static void WriteInfo(string causer, string message)
    {
        var entry = FormatEntry(levelInfo, message);
        AppendEntry(Path.Combine(LogDirectory, causer), entry);
        if (EnableConsoleOutput)
            PrintEntry(entry, ConsoleColor.Gray);
    }

    // -------------------------------------------------------------------------

    private const string levelError = "ERROR";
    private const string levelInfo  = "INFO";

    internal static string FormatEntry(string level, string message, string? callStack = null, string? exceptionDetails = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] [{level}]");
        sb.AppendLine(message);
        if (!string.IsNullOrEmpty(callStack))
        {
            sb.AppendLine("--- CallStack ---");
            sb.AppendLine(callStack);
        }
        if (!string.IsNullOrEmpty(exceptionDetails))
        {
            sb.AppendLine("--- Exception ---");
            sb.AppendLine(exceptionDetails);
        }
        sb.AppendLine(new string('-', 80));
        return sb.ToString();
    }

    private static void AppendEntry(string directory, string entry)
    {
        try
        {
            WorldAccess.EnsureFolder(directory);
            var path = Path.Combine(directory, $"{DateTimeOffset.Now:yyyy-MM-dd}.log");
            File.AppendAllText(path, entry, Encoding.UTF8);
            WorldAccess.SetFileWorldWritable(path);
        }
        catch
        {
            // Write failures must not endanger the host process.
        }
    }

    private static void PrintEntry(string entry, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(entry);
        Console.ResetColor();
    }
}
