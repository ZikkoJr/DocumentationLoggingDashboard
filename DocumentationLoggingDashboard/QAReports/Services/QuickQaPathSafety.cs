namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Rejects existing reparse-point components below the app-controlled QAReports
/// boundary so a lexical Quick QA destination cannot be redirected elsewhere.
/// The configured documentation root itself remains the trusted administrator
/// boundary and is intentionally not inspected here.
/// </summary>
internal static class QuickQaPathSafety
{
    public static void EnsureNoReparsePoints(
        string qaReportsRootPath,
        string candidatePath)
    {
        string root = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(qaReportsRootPath));
        string candidate = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(candidatePath));
        string rootPrefix = root + Path.DirectorySeparatorChar;

        if (!candidate.Equals(root, StringComparison.OrdinalIgnoreCase)
            && !candidate.StartsWith(
                rootPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The Quick QA path is outside the trusted QAReports boundary.");
        }

        EnsureComponentIsNotReparsePoint(root);

        string relative = Path.GetRelativePath(root, candidate);
        if (relative == ".")
        {
            return;
        }

        string current = root;
        foreach (string component in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            EnsureComponentIsNotReparsePoint(current);
        }
    }

    private static void EnsureComponentIsNotReparsePoint(string path)
    {
        FileAttributes attributes;

        try
        {
            attributes = File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or NotSupportedException
                or System.Security.SecurityException)
        {
            throw new InvalidOperationException(
                "Quick QA could not validate the storage path for reparse-point safety.",
                exception);
        }

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException(
                $"Quick QA storage cannot use the reparse-point path component '{Path.GetFileName(path)}'.");
        }
    }
}
