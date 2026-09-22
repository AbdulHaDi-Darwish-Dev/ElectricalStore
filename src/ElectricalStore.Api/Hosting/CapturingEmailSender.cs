using System.Collections.Concurrent;
using Permixa.Infrastructure.Email;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// In-memory email capture for Development / automated tests. Never used in Production.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<CapturedOutboundEmail> _sent = new();

    public IReadOnlyList<CapturedOutboundEmail> Snapshot() => _sent.ToArray();

    public CapturedOutboundEmail? FindById(Guid id) =>
        _sent.FirstOrDefault(e => e.Id == id);

    public void Clear()
    {
        while (_sent.TryDequeue(out _))
        {
        }
    }

    public Task SendAsync(EmailOutgoingMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new CapturedOutboundEmail(
            Id: Guid.NewGuid(),
            CapturedAtUtc: DateTimeOffset.UtcNow,
            To: message.To,
            From: message.From,
            Subject: message.Subject,
            HtmlBody: message.HtmlBody,
            TextBody: message.TextBody));
        return Task.CompletedTask;
    }
}

public sealed record CapturedOutboundEmail(
    Guid Id,
    DateTimeOffset CapturedAtUtc,
    string To,
    string From,
    string Subject,
    string HtmlBody,
    string TextBody);
