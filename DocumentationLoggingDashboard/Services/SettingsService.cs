using System.Text.Json;

namespace DocumentationLoggingDashboard.Services;

/// <summary>
/// Loads and saves dashboard settings.
/// </summary>
public sealed class SettingsService
{
    public const string DefaultDocumentationRootFolder = "DocumentationLogs";

    private const string UserSettingsFileName = "user-settings.json";
    private const string AppSettingsFileName = "appsettings.json";

    public string GetDocumentationRootFolder()
    {
        string configuredPath = ReadDocumentationRootFolderSetting();

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = DefaultDocumentationRootFolder;
        }

        try
        {
            return ResolveDocumentationRootFolder(configuredPath);
        }
        catch (ArgumentException)
        {
            return ResolveDocumentationRootFolder(DefaultDocumentationRootFolder);
        }
        catch (NotSupportedException)
        {
            return ResolveDocumentationRootFolder(DefaultDocumentationRootFolder);
        }
        catch (PathTooLongException)
        {
            return ResolveDocumentationRootFolder(DefaultDocumentationRootFolder);
        }
    }

    public string ResolveDocumentationRootFolder(string configuredPath)
    {
        configuredPath = string.IsNullOrWhiteSpace(configuredPath)
            ? DefaultDocumentationRootFolder
            : configuredPath.Trim();

        string resolvedPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        return Path.GetFullPath(resolvedPath);
    }

    public void UpdateDocumentationRootFolder(string documentationRootFolder)
    {
        string configuredPath = string.IsNullOrWhiteSpace(documentationRootFolder)
            ? DefaultDocumentationRootFolder
            : documentationRootFolder.Trim();

        UserSettings settings = new()
        {
            DocumentationRootFolder = configuredPath
        };

        string settingsJson = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        // Runtime changes are stored beside the app so we do not modify the
        // build-copied appsettings.json that acts as the project default.
        File.WriteAllText(GetUserSettingsPath(), settingsJson);
    }

    private static string ReadDocumentationRootFolderSetting()
    {
        string? userSetting = TryReadDocumentationRootFolderSetting(GetUserSettingsPath());

        if (!string.IsNullOrWhiteSpace(userSetting))
        {
            return userSetting;
        }

        string? appSetting = TryReadDocumentationRootFolderSetting(GetAppSettingsPath());

        if (!string.IsNullOrWhiteSpace(appSetting))
        {
            return appSetting;
        }

        return DefaultDocumentationRootFolder;
    }

    private static string? TryReadDocumentationRootFolderSetting(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(settingsPath));

            if (document.RootElement.TryGetProperty("DocumentationRootFolder", out JsonElement rootFolderElement)
                && rootFolderElement.ValueKind == JsonValueKind.String)
            {
                return rootFolderElement.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static string GetUserSettingsPath()
    {
        return Path.Combine(AppContext.BaseDirectory, UserSettingsFileName);
    }

    private static string GetAppSettingsPath()
    {
        return Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
    }

    private sealed class UserSettings
    {
        public string DocumentationRootFolder { get; init; } = DefaultDocumentationRootFolder;
    }
}
