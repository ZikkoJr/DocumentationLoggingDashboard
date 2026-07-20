using System.Security.Cryptography;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Keeps the paired transaction's low-level filesystem boundary small enough
/// for deterministic failure verification without changing storage policy.
/// </summary>
internal interface IQaReportTransactionFileOperations
{
    void CreateDirectory(string path);

    bool FileExists(string path);

    void WriteNewAndFlush(string path, ReadOnlyMemory<byte> content);

    long GetFileLength(string path);

    byte[] ComputeSha256(string path);

    void Move(string sourcePath, string destinationPath);

    void Delete(string path);
}

internal sealed class QaReportTransactionFileOperations
    : IQaReportTransactionFileOperations
{
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public void WriteNewAndFlush(
        string path,
        ReadOnlyMemory<byte> content)
    {
        bool fileCreated = false;

        try
        {
            using FileStream stream = new(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.SequentialScan);
            fileCreated = true;
            stream.Write(content.Span);
            stream.Flush(flushToDisk: true);
        }
        catch (Exception writeException)
        {
            if (!fileCreated)
            {
                // CreateNew did not establish ownership, so a colliding file
                // must never be removed by this transaction.
                throw;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception cleanupException)
            {
                throw new QaReportOwnedTemporaryCleanupException(
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

    public void Move(string sourcePath, string destinationPath)
    {
        File.Move(sourcePath, destinationPath, overwrite: false);
    }

    public void Delete(string path)
    {
        File.Delete(path);
    }
}

internal sealed class QaReportOwnedTemporaryCleanupException : IOException
{
    public QaReportOwnedTemporaryCleanupException(
        string temporaryPath,
        Exception writeException,
        Exception cleanupException)
        : base(
            "A QA report temporary write failed and its owned temporary file could not be removed.",
            new AggregateException(writeException, cleanupException))
    {
        TemporaryPath = temporaryPath;
    }

    public string TemporaryPath { get; }
}
