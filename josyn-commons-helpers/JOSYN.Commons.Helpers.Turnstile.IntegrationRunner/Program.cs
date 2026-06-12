using System.Diagnostics;

const int iterations   = 10;
var processCount       = Environment.ProcessorCount;
var expectedLineCount  = processCount * iterations;
var lockId             = "TurnstileIntegrationTest";
var filePath           = Path.Combine(Path.GetTempPath(), $"josyn-turnstile-{Guid.NewGuid():N}.txt");

Console.WriteLine("=== Turnstile Integration Test ===");
Console.WriteLine($"Processes : {processCount}  (one per logical CPU core)");
Console.WriteLine($"Iterations: {iterations}  per process");
Console.WriteLine($"Expected  : {expectedLineCount}  lines");
Console.WriteLine($"Lock file : {filePath}");
Console.WriteLine();

try
{
    string workerPath;
    try
    {
        workerPath = FindWorkerPath();
        Console.WriteLine($"Worker    : {workerPath}");
        Console.WriteLine();
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine($"[FAIL] {ex.Message}");
        return 1;
    }

    File.WriteAllText(filePath, string.Empty);

    var processes = Enumerable
        .Range(0, processCount)
        .Select(_ =>
        {
            var psi = new ProcessStartInfo
            {
                FileName              = workerPath,
                Arguments             = $"\"{filePath}\" \"{lockId}\" {iterations}",
                UseShellExecute       = false,
                RedirectStandardError = true,
            };
            return Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start worker process.");
        })
        .ToList();

    Console.Write($"Waiting for {processCount} worker(s)");
    foreach (var p in processes)
    {
        p.WaitForExit();
        Console.Write(".");
    }
    Console.WriteLine(" done.");
    Console.WriteLine();

    // --- Assertions ---

    var failed = processes.Where(p => p.ExitCode != 0).ToList();
    if (failed.Count > 0)
    {
        Console.WriteLine($"[FAIL] {failed.Count} worker process(es) exited with non-zero code.");
        return 1;
    }

    var lines = File.ReadAllLines(filePath)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToArray();

    if (lines.Length != expectedLineCount)
    {
        Console.WriteLine($"[FAIL] Line count: expected {expectedLineCount}, got {lines.Length}.");
        Console.WriteLine("       A worker may have timed out waiting for the lock.");
        return 1;
    }

    var duplicates = lines.GroupBy(l => l).Where(g => g.Count() > 1).ToList();
    if (duplicates.Count > 0)
    {
        Console.WriteLine($"[FAIL] {duplicates.Count} duplicate line(s) detected — mutual exclusion failed.");
        foreach (var d in duplicates)
            Console.WriteLine($"       '{d.Key}'  ×{d.Count()}");
        return 1;
    }

    Console.WriteLine($"[PASS] {lines.Length} lines, no duplicates. Mutual exclusion held.");
    return 0;
}
finally
{
    if (File.Exists(filePath)) File.Delete(filePath);
}

// ---------------------------------------------------------------------------

static string FindWorkerPath()
{
    var exeName = OperatingSystem.IsWindows()
        ? "JOSYN.Commons.Helpers.Turnstile.IntegrationWorker.exe"
        : "JOSYN.Commons.Helpers.Turnstile.IntegrationWorker";

    // When both runner and worker are published to the same directory, the worker dll
    // is also present beside the runner. In a dev build MSBuild copies only the bootstrap
    // exe without the dll — skip that case and fall through to the sibling path.
    var beside    = Path.Combine(AppContext.BaseDirectory, exeName);
    var besideDll = beside + ".dll";  // ChangeExtension would mangle the dotted assembly name
    if (File.Exists(beside) && File.Exists(besideDll)) return beside;

    // Development fallback: sibling project output (AppendTargetFrameworkToOutputPath=false).
    foreach (var config in new[] { "Debug", "Release" })
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "JOSYN.Commons.Helpers.Turnstile.IntegrationWorker",
            "bin", config, exeName));

        if (File.Exists(path)) return path;
    }

    throw new FileNotFoundException(
        $"Worker binary '{exeName}' not found next to the runner or in sibling build output. " +
        $"Publish both runner and worker to the same directory, or build the solution first.");
}
