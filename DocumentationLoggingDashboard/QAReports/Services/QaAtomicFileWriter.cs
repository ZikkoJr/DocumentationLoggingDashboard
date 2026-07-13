namespace DocumentationLoggingDashboard.QAReports.Services;

internal static class QaAtomicFileWriter
{
    internal static void WriteNew(string destinationPath, ReadOnlySpan<byte> content)
    {
        WriteTemporaryAndCommit(destinationPath, content, replaceExisting: false);
    }

    internal static void Replace(string destinationPath, ReadOnlySpan<byte> content)
    {
        if (!File.Exists(destinationPath))
        {
            throw new FileNotFoundException(
                "The metadata destination must exist before it can be replaced.",
                destinationPath);
        }

        WriteTemporaryAndCommit(destinationPath, content, replaceExisting: true);
    }

    private static void WriteTemporaryAndCommit(
        string destinationPath,
        ReadOnlySpan<byte> content,
        bool replaceExisting)
    {
        string? destinationDirectory = Path.GetDirectoryName(destinationPath);

        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new ArgumentException(
                "The destination must include a directory.",
                nameof(destinationPath));
        }

        string temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
        bool temporaryFileCreated = false;

        try
        {
            using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.SequentialScan))
            {
                temporaryFileCreated = true;
                stream.Write(content);
                stream.Flush(flushToDisk: true);
            }

            if (replaceExisting)
            {
                File.Replace(temporaryPath, destinationPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, destinationPath);
            }
        }
        finally
        {
            if (temporaryFileCreated)
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (IOException)
        {
            // Preserve the original write or replacement exception.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the original write or replacement exception.
        }
    }
}
