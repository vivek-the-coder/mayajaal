using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MayaJaal.Shared.Models;

namespace MayaJaal.Infrastructure.Reporting;

/// <summary>
/// Generates PDF incident reports (QuestPDF Community license).
/// </summary>
public sealed class ReportGenerator
{
    static ReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> WriteIncidentPdfAsync(Incident incident, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, $"incident-{Sanitize(incident.Number)}.pdf");

        var bytes = await Task.Run(() => BuildPdf(incident), cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        return path;
    }

    private static byte[] BuildPdf(Incident incident)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text("MayaJaal Incident Report")
                    .FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Incident: {incident.Number}");
                    col.Item().Text($"Started (UTC): {incident.StartTime:yyyy-MM-dd HH:mm:ss}");
                    col.Item().Text($"Level: {incident.Level}");
                    col.Item().Text($"Risk: {incident.RiskScore}");
                    col.Item().Text($"Confidence: {incident.Confidence:P0}");
                    col.Item().Text($"Status: {incident.Status}");
                    col.Item().Text($"Response: {incident.Action}");
                    col.Item().Text($"Primary signal: {incident.PrimarySignal}");

                    col.Item().PaddingTop(12).Text("Timeline").SemiBold();
                    foreach (var entry in incident.Timeline.Take(40))
                    {
                        col.Item().Text($"• {entry.Timestamp:HH:mm:ss} — {entry.Description}");
                    }

                    col.Item().PaddingTop(12).Text($"Evidence items: {incident.Evidence.Count}");
                    col.Item().Text($"Events captured: {incident.Events.Count}");
                    if (!string.IsNullOrWhiteSpace(incident.ResolutionNotes))
                        col.Item().Text($"Resolution: {incident.ResolutionNotes}");
                });

                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Generated ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();
    }

    private static string Sanitize(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return Guid.NewGuid().ToString("N")[..8];
        foreach (var c in Path.GetInvalidFileNameChars())
            number = number.Replace(c, '_');
        return number;
    }
}
