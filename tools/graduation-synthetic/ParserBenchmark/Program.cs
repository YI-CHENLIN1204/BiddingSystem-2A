using System.Diagnostics;
using System.Security.Cryptography;
using 崴正.Services;

if (args.Length == 0)
    throw new ArgumentException("Provide workbook paths.");
foreach (var path in args.Order())
{
    var bytes = File.ReadAllBytes(path);
    var name = Path.GetFileName(path);
    var parser = new TenderExcelParser();
    int Parse()
    {
        var sheets = parser.GetSheetNames(new MemoryStream(bytes), name)
            .Where(sheet => sheet != "工程總表").ToArray();
        return parser.Parse(new MemoryStream(bytes), name, sheets).Count;
    }

    int rows = Parse();
    if (rows <= 0)
        throw new InvalidOperationException($"No detail rows: {name}");
    double[] times = new double[20];
    long[] allocations = new long[times.Length];
    using var process = Process.GetCurrentProcess();
    process.Refresh();
    long workingSetBefore = process.WorkingSet64;
    for (int i = 0; i < times.Length; i++)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        if (Parse() != rows)
            throw new InvalidOperationException($"Inconsistent detail count: {name}");
        times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        allocations[i] = GC.GetAllocatedBytesForCurrentThread() - before;
    }
    process.Refresh();
    Array.Sort(times);
    Array.Sort(allocations);
    Console.WriteLine(FormattableString.Invariant(
        $"{name},{bytes.Length},{rows},{(times[9] + times[10]) / 2:F3},{times[18]:F3},{(allocations[9] + allocations[10]) / 2},{workingSetBefore},{process.PeakWorkingSet64},{Convert.ToHexString(SHA256.HashData(bytes))}"));
}
