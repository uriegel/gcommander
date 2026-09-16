using System.Collections.Concurrent;
using System.Threading.Channels;

class MetaFileData : IDisposable
{
    public MetaFileData()
    {
        for (int i = 0; i < 4; i++)
            _ = MetadataWorker(cts.Token);
    }

    public void QueueMetadata(FileItem item, string path)
    {
        if (!metadataPending.TryAdd(path, 0))
            return;
        metadataQueue.Writer.TryWrite(new(path, item));
    }

    async Task MetadataWorker(CancellationToken cancellationToken)
    {
        await foreach (var job in metadataQueue.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessMetadata(job, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Metadata error for {job.Path}: {ex}");
            }
            finally
            {
                metadataPending.TryRemove(job.Path, out _);
            }
        }
    }

    async static Task ProcessMetadata(Job job, CancellationToken cancellationToken)
    {
        if (!await WaitUntilStable(job.Path, cancellationToken))
            return;
        if (!File.Exists(job.Path))
            return;
        var extension = Path.GetExtension(job.Path);

        if (!extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
            return;

        var exifData = await Task.Run(() => ExifReader.GetExifData(job.Path), cancellationToken);
        if (exifData != null)
            job.Item.ExifData = exifData;
    }

    static async Task<bool> WaitUntilStable(string path, CancellationToken cancellationToken)
    {
        const int interval = 150;

        long lastSize = -1;
        DateTime lastWrite = default;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var info = new FileInfo(path);

                if (!info.Exists)
                {
                    await Task.Delay(interval, cancellationToken);
                    continue;
                }

                var size = info.Length;
                var write = info.LastWriteTimeUtc;

                if (size == lastSize && write == lastWrite)
                    return true;

                lastSize = size;
                lastWrite = write;
            }
            catch (IOException)
            {
                // File is currently unavailable.
            }
            catch (UnauthorizedAccessException)
            {
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    readonly Channel<Job> metadataQueue = Channel.CreateUnbounded<Job>();
    readonly ConcurrentDictionary<string, byte> metadataPending = new(StringComparer.OrdinalIgnoreCase);
    readonly CancellationTokenSource cts = new();

    record Job(string Path, FileItem Item);

    #region IDisposable

    public void Dispose()
    {
        // Ändere diesen Code nicht. Füge Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                // Verwalteten Zustand (verwaltete Objekte) bereinigen
                cts.Cancel();

            // Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~MetaFileData()
    // {
    //     // Ändere diesen Code nicht. Füge Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
    //     Dispose(disposing: false);
    // }

    bool disposedValue;

    #endregion
}

