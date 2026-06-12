using JOSYN.Commons.Helpers;

// Args: <filePath> <lockId> <iterations>
if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: worker <filePath> <lockId> <iterations>");
    return 1;
}

var filePath = args[0];
var lockId   = args[1];

if (!int.TryParse(args[2], out var iterations))
{
    Console.Error.WriteLine($"Invalid iterations value: '{args[2]}'");
    return 1;
}

var pid = Environment.ProcessId;

for (var i = 0; i < iterations; i++)
{
    Task.Delay(Random.Shared.Next(0, 101)).Wait();

    var result = Turnstile.Run(lockId, () =>
    {
        File.AppendAllText(filePath, $"pid={pid} iter={i}{Environment.NewLine}");
    });

    if (!result.Succeeded)
    {
        Console.Error.WriteLine($"Turnstile failed on iteration {i}: {result.ErrorMessage}");
        return 1;
    }
}

return 0;
