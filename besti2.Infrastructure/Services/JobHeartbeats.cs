using System.Collections.Concurrent;

namespace besti2.Infrastructure.Services;

// "Systemstatus" in /admin: when each background job last finished a run, and whether it went well.
// One API process, so a static list is enough (it starts empty after every deploy)
public static class JobHeartbeats
{
    private static readonly ConcurrentDictionary<string, (DateTime LastRunUtc, bool Ok)> runs = new();

    public static void Ran(string job, bool ok) => runs[job] = (DateTime.UtcNow, ok);

    public static IReadOnlyList<(string Job, DateTime LastRunUtc, bool Ok)> All() =>
        runs.OrderBy(r => r.Key).Select(r => (r.Key, r.Value.LastRunUtc, r.Value.Ok)).ToList();
}
