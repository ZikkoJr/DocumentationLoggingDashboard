using System.Text;
using DocumentationLoggingDashboard.QAReports.Models;
using DocumentationLoggingDashboard.QAReports.Pdf;
using DocumentationLoggingDashboard.QAReports.Validation;
using MigraDoc;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace DocumentationLoggingDashboard.QAReports.Services;

/// <summary>
/// Generates one reusable, in-memory PDF payload from a current ready QA report.
/// </summary>
public sealed class QaPdfGenerationService
{
    private static readonly object RenderingLock = new();

    public byte[] GeneratePdf(
        QaReport report,
        QaReportValidationResult validationResult,
        DateTimeOffset generatedAt)
    {
        if (report is null)
        {
            throw new QaPdfGenerationException(
                "A QA report is required for PDF generation.");
        }

        if (validationResult is null)
        {
            throw new QaPdfGenerationException(
                "A QA report readiness result is required for PDF generation.");
        }

        QaPdfInputValidator.Validate(report, validationResult);

        try
        {
            // MigraDoc and PDFsharp use process-wide font state. Serializing this
            // small rendering boundary keeps the required error-font choice stable.
            lock (RenderingLock)
            {
                PredefinedFontsAndChars.ErrorFontName = QaPdfStyles.FontFamily;

                Document document = new QaPdfDocumentBuilder().Build(
                    report,
                    validationResult,
                    generatedAt);
                PdfDocumentRenderer renderer = new()
                {
                    Document = document
                };

                renderer.RenderDocument();
                renderer.PdfDocument.Info.CreationDate = generatedAt.UtcDateTime;
                renderer.PdfDocument.Info.ModificationDate = generatedAt.UtcDateTime;

                using MemoryStream stream = new();
                renderer.Save(stream, closeStream: false);
                byte[] payload = stream.ToArray();
                ValidatePayload(payload);
                return payload;
            }
        }
        catch (QaPdfGenerationException)
        {
            throw;
        }
        catch (Exception exception) when (IsFontResolutionFailure(exception))
        {
            throw new QaPdfGenerationException(
                $"The PDF renderer could not resolve the required font '{QaPdfStyles.FontFamily}'.",
                exception);
        }
        catch (Exception exception)
        {
            throw new QaPdfGenerationException(
                "The PDF library failed while rendering the QA report.",
                exception);
        }
    }

    private static void ValidatePayload(byte[] payload)
    {
        if (payload.Length < 16
            || payload[0] != (byte)'%'
            || payload[1] != (byte)'P'
            || payload[2] != (byte)'D'
            || payload[3] != (byte)'F'
            || payload[4] != (byte)'-')
        {
            throw new QaPdfGenerationException(
                "The PDF renderer did not produce a nonempty valid PDF payload.");
        }

        int markerWindowStart = Math.Max(0, payload.Length - 2048);
        string ending = Encoding.ASCII.GetString(
            payload,
            markerWindowStart,
            payload.Length - markerWindowStart);
        if (!ending.Contains("%%EOF", StringComparison.Ordinal))
        {
            throw new QaPdfGenerationException(
                "The PDF renderer produced a payload without a plausible PDF EOF marker.");
        }
    }

    private static bool IsFontResolutionFailure(Exception exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            string typeName = current.GetType().FullName ?? string.Empty;
            string message = current.Message;

            if (typeName.Contains("Font", StringComparison.OrdinalIgnoreCase)
                || message.Contains("font", StringComparison.OrdinalIgnoreCase)
                || message.Contains("typeface", StringComparison.OrdinalIgnoreCase)
                || message.Contains("glyph", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
