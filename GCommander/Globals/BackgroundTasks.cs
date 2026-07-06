using System.Collections.Concurrent;

static class BackgroundTasks
{
    public static async Task CancelAllAsync()
    {
        cancellation.Cancel();
        await Task.WhenAll(tasks.Values);
    }
    public static int GetId() => Interlocked.Increment(ref idSeed);
    public static CancellationToken GetCancellationToken(CancellationToken token)
        => CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, token).Token;
    public static void Add(int id, Task task) => tasks.TryAdd(id, task);
    public static void Remove(int id) => tasks.TryRemove(id, out _);
    static readonly ConcurrentDictionary<int, Task> tasks = [];
    static readonly CancellationTokenSource cancellation = new();
    static int idSeed;
}