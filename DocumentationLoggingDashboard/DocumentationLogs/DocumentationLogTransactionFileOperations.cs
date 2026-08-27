using System.Security.Cryptography;

namespace DocumentationLoggingDashboard.DocumentationLogs;

/// <summary>
/// Injectable filesystem boundary for deterministic multi-artifact transaction
/// tests and production lock/fingerprint checks.
/// </summary>
public interface IDocumentationLogTransactionFileOperations
{
    void CreateDirectory(string path);

    bool FileExists(string path);

    byte[] ReadAllBytes(string path);

    void WriteNewAndFlush(string path, ReadOnlyMemory<byte> content);

    long GetFileLength(string path);

    byte[] ComputeSha256(string path);

    void VerifyExclusiveAccess(string path);

    IDocumentationLogExclusiveReadLease OpenExclusiveReadLease(string path);

    void Move(string sourcePath, string destinationPath);

    void Delete(string path);
}
public interface IDocumentationLogExclusiveReadLease : IDisposable
{
    long Length { get; }

    byte[] ComputeSha256();
}

public sealed class DocumentationLogTransactionFileOperations
    : IDocumentationLogTransactionFileOperations
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

    public void WriteNewAndFlush(string path, ReadOnlyMemory<byte> content)
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
        catch
        {
            if (created)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Preserve the original staging failure. The transaction
                    // reports any residue during its cleanup pass.
                }
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

    public IDocumentationLogExclusiveReadLease OpenExclusiveReadLease(string path)
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

    private sealed class ExclusiveReadLease : IDocumentationLogExclusiveReadLease
    {
        private readonly FileStream stream;

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

        public long Length => stream.Length;

        public byte[] ComputeSha256()
        {
            stream.Position = 0;
            return SHA256.HashData(stream);
        }

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
