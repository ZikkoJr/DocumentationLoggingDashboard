using System.Globalization;
using System.Text;
using DocumentationLoggingDashboard.Models;
using DocumentationLoggingDashboard.Services;

namespace DocumentationLoggingDashboard.DocumentationLogs;

public sealed class DocumentationLogPreviewFormatter
{
    private readonly LogTemplateService templateService;

    public DocumentationLogPreviewFormatter(LogTemplateService? templateService = null)
    {
        this.templateService = templateService ?? new LogTemplateService();
    }

    public string Format(DocumentationLogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        StringBuilder builder = new();
        builder.AppendLine($"{templateService.GetDisplayName(logEvent.LogType)} — Excel Entry Preview");
        builder.AppendLine($"Log ID: {logEvent.LogId}");
        builder.AppendLine(
            $"Date/Time: {logEvent.Timestamp.DateTime.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture)}");
        builder.AppendLine();

        if (logEvent.LogType == LogType.DebuggingLog)
        {
            DocumentationLogHotel hotel = logEvent.Hotels.Single();
            Append(builder, "Hotel Name", hotel.HotelName);
            Append(builder, "Hotel ID", hotel.HotelId);
            Append(builder, "PMS", hotel.Pms.PmsName);
            Append(builder, "Error Shown On Ticket", logEvent.GetValue(DocumentationLogFieldKeys.ErrorShownOnTicket));
            Append(builder, "Root Cause", logEvent.GetValue(DocumentationLogFieldKeys.RootCause));
            Append(builder, "Fix Applied", logEvent.GetValue(DocumentationLogFieldKeys.FixApplied));
        }
        else
        {
            Append(builder, "Hotel IDs", logEvent.CanonicalHotelIds);
            Append(builder, "Script Name", logEvent.GetValue(DocumentationLogFieldKeys.ScriptName));

            if (logEvent.LogType == LogType.ScriptEditingLog)
            {
                Append(builder, "Reason For Edit", logEvent.GetValue(DocumentationLogFieldKeys.ReasonForEdit));
                Append(builder, "Changes Made", logEvent.GetValue(DocumentationLogFieldKeys.ChangesMade));
            }
            else
            {
                Append(builder, "Reason For Creation", logEvent.GetValue(DocumentationLogFieldKeys.ReasonForCreation));
                Append(builder, "Script Purpose / What It Does", logEvent.GetValue(DocumentationLogFieldKeys.ScriptPurpose));
            }
        }

        Append(builder, "Created By", logEvent.GetValue(DocumentationLogFieldKeys.CreatedBy));
        Append(builder, "Notes / Follow-up", logEvent.GetValue(DocumentationLogFieldKeys.NotesFollowUp));
        return builder.ToString().TrimEnd();
    }

    private static void Append(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"{label}:");
        builder.AppendLine(value);
        builder.AppendLine();
    }
}
