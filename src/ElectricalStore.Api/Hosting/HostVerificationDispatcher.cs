using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Permixa.Application.Verification.Abstractions;
using Permixa.Application.Verification.Models;
using Permixa.Domain.Verification;
using Permixa.Infrastructure.Email;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Routes EmailChange deliveries through a host Arabic template + /change-email/confirm URL.
/// Other purposes delegate to Permixa's EmailVerificationDispatcher (confirmation / password reset).
/// </summary>
public sealed class HostVerificationDispatcher : IVerificationDispatcher
{
    private readonly EmailVerificationDispatcher _inner;
    private readonly IEmailSender _sender;
    private readonly EmailDeliveryHostOptions _email;
    private readonly ILogger<HostVerificationDispatcher> _logger;

    public HostVerificationDispatcher(
        EmailVerificationDispatcher inner,
        IEmailSender sender,
        IOptions<EmailDeliveryHostOptions> email,
        ILogger<HostVerificationDispatcher> logger)
    {
        _inner = inner;
        _sender = sender;
        _email = email.Value;
        _logger = logger;
    }

    public Task DispatchAsync(
        VerificationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Purpose == VerificationPurpose.EmailChange
            && request.Method == VerificationMethod.UrlToken)
        {
            return DispatchEmailChangeAsync(request, cancellationToken);
        }

        return _inner.DispatchAsync(request, cancellationToken);
    }

    private async Task DispatchEmailChangeAsync(
        VerificationDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var frontend = (_email.FrontendPublicUrl ?? string.Empty).Trim().TrimEnd('/');
        var token = Uri.EscapeDataString(request.RawVerificationValue ?? string.Empty);
        var url =
            $"{frontend}/change-email/confirm?challengeId={request.ChallengeId:D}&token={token}";

        var minutes = Math.Max(
            1,
            (int)Math.Ceiling((request.ExpiresAtUtc - DateTime.UtcNow).TotalMinutes));
        if (_email.UrlTokenLifetimeMinutes is > 0)
            minutes = _email.UrlTokenLifetimeMinutes.Value;

        var rendered = ArabicEmailChangeTemplates.RenderConfirmation(
            applicationName: string.IsNullOrWhiteSpace(_email.Branding.ApplicationName)
                ? "ElectricalStore"
                : _email.Branding.ApplicationName,
            companyName: _email.Branding.CompanyName,
            supportEmail: _email.Branding.SupportEmail,
            logoUrl: _email.Branding.LogoUrl,
            confirmationUrl: url,
            expirationMinutes: minutes);

        var fromEmail = string.IsNullOrWhiteSpace(_email.FromEmail)
            ? "noreply@localhost"
            : _email.FromEmail.Trim();

        await _sender.SendAsync(
            new EmailOutgoingMessage(
                To: request.Destination,
                From: fromEmail,
                Subject: rendered.Subject,
                HtmlBody: rendered.HtmlBody,
                TextBody: rendered.TextBody,
                IdempotencyKey: $"email-change:{request.ChallengeId:D}"),
            cancellationToken);

        _logger.LogInformation(
            "Dispatched email-change confirmation for challenge {ChallengeId}.",
            request.ChallengeId);
    }
}

internal static class ArabicEmailChangeTemplates
{
    public static (string Subject, string HtmlBody, string TextBody) RenderConfirmation(
        string applicationName,
        string? companyName,
        string? supportEmail,
        string? logoUrl,
        string confirmationUrl,
        int expirationMinutes)
    {
        var app = Encode(applicationName);
        var company = Encode(companyName);
        var support = Encode(supportEmail);
        var url = EncodeAttr(confirmationUrl);
        var urlText = Encode(confirmationUrl);
        var minutes = expirationMinutes;

        var companyBlock = string.IsNullOrWhiteSpace(companyName)
            ? string.Empty
            : $"<p style=\"margin:8px 0 0;font-size:13px;color:#555;\">{company}</p>";
        var supportBlock = string.IsNullOrWhiteSpace(supportEmail)
            ? string.Empty
            : $"<p style=\"margin:20px 0 0;font-size:12px;color:#666;\">للمساعدة: {support}</p>";
        var logoBlock = string.IsNullOrWhiteSpace(logoUrl)
            ? string.Empty
            : $"<img src=\"{EncodeAttr(logoUrl)}\" alt=\"\" width=\"120\" style=\"display:block;margin:0 0 16px;border:0;\" />";

        var html = $"""
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>تأكيد تغيير البريد الإلكتروني</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:Tahoma,Arial,Helvetica,sans-serif;color:#222;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f4f4f5;padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:560px;background-color:#ffffff;border:1px solid #e5e5e5;" dir="rtl">
                      <tr>
                        <td style="padding:28px 28px 8px;text-align:right;">
                          {logoBlock}
                          <h1 style="margin:0;font-size:22px;line-height:1.3;font-weight:700;">{app}</h1>
                          {companyBlock}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 28px 28px;text-align:right;">
                          <p style="margin:0 0 20px;font-size:15px;line-height:1.7;">تم طلب تغيير البريد الإلكتروني لحسابك. اضغط على الزر أدناه لتأكيد العنوان الجديد. لن يتغيّر بريد حسابك الحالي إلا بعد التأكيد.</p>
                          <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:0 0 20px;">
                            <tr>
                              <td align="center" style="background-color:#111111;border-radius:4px;">
                                <a href="{url}" style="display:inline-block;padding:12px 22px;font-size:15px;font-weight:700;color:#ffffff;text-decoration:none;">تأكيد تغيير البريد الإلكتروني</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:0 0 12px;font-size:13px;line-height:1.6;color:#555;">أو انسخ الرابط التالي وافتحه في المتصفح:</p>
                          <p style="margin:0 0 16px;font-size:12px;line-height:1.5;word-break:break-all;color:#333;">{urlText}</p>
                          <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#555;">ينتهي صلاحية هذا الرابط خلال {minutes} دقيقة.</p>
                          <p style="margin:0;font-size:13px;line-height:1.6;color:#666;">إذا لم تطلب هذا التغيير، لا تضغط على الرابط وقم بتأمين حسابك فوراً.</p>
                          {supportBlock}
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        var companyLine = string.IsNullOrWhiteSpace(companyName) ? string.Empty : companyName + Environment.NewLine;
        var supportLine = string.IsNullOrWhiteSpace(supportEmail)
            ? string.Empty
            : Environment.NewLine + "للمساعدة: " + supportEmail;

        var text =
            applicationName + Environment.NewLine
            + companyLine
            + "تم طلب تغيير البريد الإلكتروني. افتح الرابط لتأكيد العنوان الجديد:"
            + Environment.NewLine + Environment.NewLine
            + confirmationUrl + Environment.NewLine + Environment.NewLine
            + $"ينتهي صلاحية هذا الرابط خلال {minutes} دقيقة."
            + Environment.NewLine + Environment.NewLine
            + "إذا لم تطلب هذا التغيير، لا تفتح الرابط وقم بتأمين حسابك."
            + supportLine;

        return ("تأكيد تغيير البريد الإلكتروني", html, text);
    }

    public static (string Subject, string HtmlBody, string TextBody) RenderSecurityNotice(
        string applicationName,
        string? companyName,
        string? supportEmail,
        string? logoUrl)
    {
        var app = Encode(applicationName);
        var company = Encode(companyName);
        var support = Encode(supportEmail);
        var companyBlock = string.IsNullOrWhiteSpace(companyName)
            ? string.Empty
            : $"<p style=\"margin:8px 0 0;font-size:13px;color:#555;\">{company}</p>";
        var supportBlock = string.IsNullOrWhiteSpace(supportEmail)
            ? string.Empty
            : $"<p style=\"margin:20px 0 0;font-size:12px;color:#666;\">للمساعدة: {support}</p>";
        var logoBlock = string.IsNullOrWhiteSpace(logoUrl)
            ? string.Empty
            : $"<img src=\"{EncodeAttr(logoUrl)}\" alt=\"\" width=\"120\" style=\"display:block;margin:0 0 16px;border:0;\" />";

        var html = $"""
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head><meta charset="utf-8" /><title>تم تغيير البريد الإلكتروني</title></head>
            <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:Tahoma,Arial,Helvetica,sans-serif;color:#222;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f4f4f5;padding:24px 12px;">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:560px;background-color:#ffffff;border:1px solid #e5e5e5;" dir="rtl">
                    <tr><td style="padding:28px;text-align:right;">
                      {logoBlock}
                      <h1 style="margin:0 0 12px;font-size:22px;">{app}</h1>
                      {companyBlock}
                      <p style="margin:16px 0;font-size:15px;line-height:1.7;">تم تغيير البريد الإلكتروني لحسابك بنجاح.</p>
                      <p style="margin:0;font-size:13px;line-height:1.6;color:#666;">إذا لم تقم بهذا التغيير، أعد تعيين كلمة المرور فوراً وتواصل مع الدعم.</p>
                      {supportBlock}
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;

        var text =
            applicationName + Environment.NewLine
            + (string.IsNullOrWhiteSpace(companyName) ? string.Empty : companyName + Environment.NewLine)
            + "تم تغيير البريد الإلكتروني لحسابك بنجاح."
            + Environment.NewLine
            + "إذا لم تقم بهذا التغيير، أعد تعيين كلمة المرور وتواصل مع الدعم."
            + (string.IsNullOrWhiteSpace(supportEmail) ? string.Empty : Environment.NewLine + "للمساعدة: " + supportEmail);

        return ("تم تغيير البريد الإلكتروني لحسابك", html, text);
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string EncodeAttr(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
