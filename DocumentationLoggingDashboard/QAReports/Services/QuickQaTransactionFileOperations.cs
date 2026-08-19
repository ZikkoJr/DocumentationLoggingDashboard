using System.Security.Cryptography;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Provides the injectable filesystem boundary used by the Quick QA paired
/// transaction and deterministic failure tests.
/// </summary>
public interface IQuickQaTransactionFileOperations
{
    void CreateDirectory(string path);

    bool FileExists(string path);

    byte[] ReadAllBytes(string path);

    void WriteNewAndFlush(string path, ReadOnlyMemory<byte> content);

    long GetFileLength(string path);

    byte[] ComputeSha256(string path);

    DateTime GetLastWriteTimeUtc(string path);

    void VerifyExclusiveAccess(string path);

    IQuickQaExclusiveReadLease OpenExclusiveReadLease(string path);

    void Move(string sourcePath, string destinationPath);

    void Delete(string path);
}

/// <summary>
/// Retains a no-sharing read handle so a rollback baseline cannot be changed,
/// replaced, or removed while the paired commit is in progress.
/// </summary>
public interface IQuickQaExclusiveReadLease : IDisposable
{
    long Length { get; }

    byte[] ComputeSha256();
}

public sealed class QuickQaTransactionFileOperations
    : IQuickQaTransactionFileOperations
{
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.SequentialScan);
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    public void WriteNewAndFlush(
        string path,
        ReadOnlyMemory<byte> content)
    {
        bool created = false;

        try
        {
            using FileStream stream = new(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.SequentialScan);
            created = true;
            stream.Write(content.Span);
            stream.Flush(flushToDisk: true);
        }
        catch (Exception writeException)
        {
            if (!created)
            {
                throw;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception cleanupException)
            {
                throw new QuickQaOwnedTemporaryCleanupException(
                    path,
                    writeException,
                    cleanupException);
            }

            throw;
        }
    }

    public long GetFileLength(string path)
    {
        return new FileInfo(path).Length;
    }

    public byte[] ComputeSha256(string path)
    {
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.SequentialScan);
        return SHA256.HashData(stream);
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        return File.GetLastWriteTimeUtc(path);
    }

    public void VerifyExclusiveAccess(string path)
    {
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 1,
            FileOptions.RandomAccess);
    }

    public IQuickQaExclusiveReadLease OpenExclusiveReadLease(string path)
    {
        return new ExclusiveReadLease(path);
    }

    public void Move(string sourcePath, string destinationPath)
    {
        File.Move(sourcePath, destinationPath, overwrite: false);
    }

    public void Delete(string path)
    {
        File.Delete(path);
    }

    private sealed class ExclusiveReadLease : IQuickQaExclusiveReadLease
    {
        private readonly FileStream stream;
        private bool disposed;

        public ExclusiveReadLease(string path)
        {
            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.SequentialScan);
        }

        public long Length
        {
            get
            {
                ThrowIfDisposed();
                return stream.Length;
            }
        }

        public byte[] ComputeSha256()
        {
            ThrowIfDisposed();
            stream.Position = 0;
            return SHA256.HashData(stream);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            stream.Dispose();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }
    }
}

public sealed class QuickQaOwnedTemporaryCleanupException : IOException
{
    public QuickQaOwnedTemporaryCleanupException(
        string temporaryPath,
        Exception writeException,
        Exception cleanupException)
        : base(
            "A Quick QA temporary write failed and its owned temporary file could not be removed.",
            new AggregateException(writeException, cleanupException))
    {
        TemporaryPath = temporaryPath;
    }

    public string TemporaryPath { get; }
}
