using System.Net;
using System.Text;
using ElectricalStore.Api.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElectricalStore.Api.Endpoints;

/// <summary>
/// Development-only outbox for CapturingEmailSender (Playwright / local verification).
/// Never mapped in Production.
/// </summary>
public static class DevEmailOutboxEndpoints
{
    public static IEndpointRouteBuilder MapDevEmailOutboxIfEnabled(this IEndpointRouteBuilder endpoints)
    {
        var env = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment())
            return endpoints;

        var capturing = endpoints.ServiceProvider.GetService<CapturingEmailSender>();
        if (capturing is null)
            return endpoints;

        endpoints.MapGet("/dev/email-outbox", (HttpRequest request, CapturingEmailSender mailbox) =>
            {
                var items = mailbox.Snapshot();
                if (WantsHtmlList(request))
                    return Results.Content(BuildHtmlIndex(items), "text/html; charset=utf-8");

                return Results.Json(items.Select(ToJsonDto));
            })
            .AllowAnonymous()
            .WithTags("Development")
            .WithName("DevEmailOutbox")
            .WithSummary("Development-only captured outbound emails (JSON for clients; HTML list for browsers)");

        endpoints.MapGet("/dev/email-outbox/{id:guid}/preview", (
                Guid id,
                CapturingEmailSender mailbox) =>
            {
                var message = mailbox.FindById(id);
                if (message is null)
                    return Results.NotFound();

                // Exact captured HTML body — same payload Resend would receive.
                return Results.Content(message.HtmlBody, "text/html; charset=utf-8");
            })
            .AllowAnonymous()
            .WithTags("Development")
            .WithName("DevEmailOutboxPreview")
            .WithSummary("Development-only browser preview of captured HTML email body");

        endpoints.MapGet("/dev/email-outbox/{id:guid}/text", (
                Guid id,
                CapturingEmailSender mailbox) =>
            {
                var message = mailbox.FindById(id);
                if (message is null)
                    return Results.NotFound();

                return Results.Text(message.TextBody, "text/plain; charset=utf-8");
            })
            .AllowAnonymous()
            .WithTags("Development")
            .WithName("DevEmailOutboxText")
            .WithSummary("Development-only plain-text body of a captured email");

        endpoints.MapDelete("/dev/email-outbox", (CapturingEmailSender mailbox) =>
            {
                mailbox.Clear();
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags("Development")
            .WithName("DevEmailOutboxClear");

        return endpoints;
    }

    /// <summary>
    /// Browsers send text/html; Playwright/fetch typically send */* or application/json → keep JSON.
    /// </summary>
    private static bool WantsHtmlList(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(accept) || accept.Contains("*/*", StringComparison.Ordinal))
        {
            // Explicit JSON wins; bare */* stays JSON for automation.
            if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return false;
            if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
               && !accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static object ToJsonDto(CapturedOutboundEmail e) => new
    {
        id = e.Id,
        capturedAtUtc = e.CapturedAtUtc,
        to = e.To,
        from = e.From,
        subject = e.Subject,
        textBody = e.TextBody,
        htmlBody = e.HtmlBody,
        previewUrl = $"/dev/email-outbox/{e.Id:D}/preview",
        textUrl = $"/dev/email-outbox/{e.Id:D}/text"
    };

    private static string BuildHtmlIndex(IReadOnlyList<CapturedOutboundEmail> items)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Dev email outbox</title>
              <style>
                body { font-family: system-ui, Segoe UI, sans-serif; margin: 24px; color: #111; background: #fafafa; }
                h1 { font-size: 1.25rem; margin: 0 0 8px; }
                .meta { color: #555; font-size: 0.875rem; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #e5e5e5; }
                th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid #eee; vertical-align: top; font-size: 0.875rem; }
                th { background: #f4f4f5; font-weight: 600; }
                a { color: #0b57d0; }
                .empty { padding: 24px; color: #666; border: 1px dashed #ccc; background: #fff; }
                code { font-size: 0.8rem; }
              </style>
            </head>
            <body>
              <h1>Development email outbox</h1>
              <p class="meta">Captured messages only — not mapped in Production. Preview renders the exact HTML body that would be sent via Resend.</p>
            """);

        if (items.Count == 0)
        {
            sb.Append("<p class=\"empty\">No messages captured yet.</p></body></html>");
            return sb.ToString();
        }

        sb.Append("<table><thead><tr><th>Captured (UTC)</th><th>To</th><th>Subject</th><th>Actions</th></tr></thead><tbody>");
        foreach (var e in items.Reverse())
        {
            var to = WebUtility.HtmlEncode(e.To);
            var subject = WebUtility.HtmlEncode(e.Subject);
            var when = WebUtility.HtmlEncode(e.CapturedAtUtc.ToString("u"));
            var preview = $"/dev/email-outbox/{e.Id:D}/preview";
            var text = $"/dev/email-outbox/{e.Id:D}/text";
            sb.Append("<tr>")
                .Append("<td><code>").Append(when).Append("</code></td>")
                .Append("<td dir=\"ltr\">").Append(to).Append("</td>")
                .Append("<td>").Append(subject).Append("</td>")
                .Append("<td><a href=\"").Append(preview).Append("\" target=\"_blank\" rel=\"noopener\">Open HTML preview</a>")
                .Append(" · <a href=\"").Append(text).Append("\" target=\"_blank\" rel=\"noopener\">Plain text</a></td>")
                .Append("</tr>");
        }

        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }
}
