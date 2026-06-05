using JOSYN.Foundation.ResultPattern;

namespace JOSYN.Commons.Log;

/// <summary>
/// Contract definition for the process-local file logger.
/// Writes entries to <c>&lt;LogDirectory&gt;\&lt;yyyy-MM-dd&gt;.log</c>.
/// Overloads with a <c>causer</c> parameter write to a subdirectory of the same name.
/// </summary>
public interface ILocalLog
{
    /// <summary>
    /// Root path for all log files of this process.
    /// Must be set before the first log call if a different storage location is desired.
    /// </summary>
    static abstract string LogDirectory { get; set; }

    /// <summary>
    /// Controls whether log entries are additionally written to the console.
    /// </summary>
    static abstract bool EnableConsoleOutput { get; set; }

    /// <summary>Writes an error entry.</summary>
    static abstract void WriteError(string message, string? callStack = null, string? exceptionDetails = null);

    /// <summary>Writes an error entry from a <see cref="Result"/>.</summary>
    static abstract void WriteError(Result result);

    /// <summary>Writes an error entry to the subdirectory of the specified causer.</summary>
    static abstract void WriteError(string causer, string message, string? callStack = null, string? exceptionDetails = null);

    /// <summary>Writes an error entry from a <see cref="Result"/> to the subdirectory of the specified causer.</summary>
    static abstract void WriteError(string causer, Result result);

    /// <summary>Writes an info entry.</summary>
    static abstract void WriteInfo(string message);

    /// <summary>Writes an info entry to the subdirectory of the specified causer.</summary>
    static abstract void WriteInfo(string causer, string message);
}
