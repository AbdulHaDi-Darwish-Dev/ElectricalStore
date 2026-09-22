using System.Net;
using System.Text;
using Permixa.Infrastructure.Email;
using Permixa.Infrastructure.Email.Templates;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Arabic-first email-safe HTML/text templates for Permixa verification delivery.
/// Replaces the English embedded Permixa renderer for this host.
/// </summary>
public sealed class ArabicEmailTemplateRenderer : IEmailTemplateRenderer
{
    public RenderedEmail RenderEmailConfirmationLink(EmailConfirmationLinkTemplateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var app = Encode(model.ApplicationName);
        var company = Encode(model.CompanyName);
        var support = Encode(model.SupportEmail);
        var url = EncodeAttr(model.VerificationUrl);
        var urlText = Encode(model.VerificationUrl);
        var minutes = model.ExpirationMinutes;

        var companyBlock = string.IsNullOrWhiteSpace(model.CompanyName)
            ? string.Empty
            : $"<p style=\"margin:8px 0 0;font-size:13px;color:#555;\">{company}</p>";

        var supportBlock = string.IsNullOrWhiteSpace(model.SupportEmail)
            ? string.Empty
            : $"<p style=\"margin:20px 0 0;font-size:12px;color:#666;\">للمساعدة: {support}</p>";

        var logoBlock = string.IsNullOrWhiteSpace(model.LogoUrl)
            ? string.Empty
            : $"<img src=\"{EncodeAttr(model.LogoUrl)}\" alt=\"\" width=\"120\" style=\"display:block;margin:0 0 16px;border:0;\" />";

        var html = $"""
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>تأكيد البريد الإلكتروني</title>
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
                          <p style="margin:0 0 20px;font-size:15px;line-height:1.7;">يرجى تأكيد بريدك الإلكتروني بالضغط على الزر أدناه.</p>
                          <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:0 0 20px;">
                            <tr>
                              <td align="center" style="background-color:#111111;border-radius:4px;">
                                <a href="{url}" style="display:inline-block;padding:12px 22px;font-size:15px;font-weight:700;color:#ffffff;text-decoration:none;">تأكيد البريد الإلكتروني</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:0 0 12px;font-size:13px;line-height:1.6;color:#555;">أو انسخ الرابط التالي وافتحه في المتصفح:</p>
                          <p style="margin:0 0 16px;font-size:12px;line-height:1.5;word-break:break-all;color:#333;">{urlText}</p>
                          <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#555;">ينتهي صلاحية هذا الرابط خلال {minutes} دقيقة.</p>
                          <p style="margin:0;font-size:13px;line-height:1.6;color:#666;">إذا لم تُنشئ حساباً لدينا، يمكنك تجاهل هذه الرسالة بأمان.</p>
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

        var companyLine = string.IsNullOrWhiteSpace(model.CompanyName) ? string.Empty : model.CompanyName + Environment.NewLine;
        var supportLine = string.IsNullOrWhiteSpace(model.SupportEmail)
            ? string.Empty
            : Environment.NewLine + "للمساعدة: " + model.SupportEmail;

        var text = new StringBuilder()
            .AppendLine(model.ApplicationName)
            .Append(companyLine)
            .AppendLine("يرجى تأكيد بريدك الإلكتروني عبر الرابط التالي:")
            .AppendLine()
            .AppendLine(model.VerificationUrl)
            .AppendLine()
            .AppendLine($"ينتهي صلاحية هذا الرابط خلال {minutes} دقيقة.")
            .AppendLine()
            .AppendLine("إذا لم تُنشئ حساباً لدينا، يمكنك تجاهل هذه الرسالة بأمان.")
            .Append(supportLine)
            .ToString();

        return new RenderedEmail(
            Subject: "تأكيد البريد الإلكتروني",
            HtmlBody: html,
            TextBody: text);
    }

    public RenderedEmail RenderEmailConfirmationOtp(EmailConfirmationOtpTemplateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        // Host registration uses UrlToken; keep a safe Arabic OTP fallback for Permixa completeness.
        var app = Encode(model.ApplicationName);
        var code = Encode(model.VerificationCode);
        var html = $"""
            <!DOCTYPE html><html lang="ar" dir="rtl"><body style="font-family:Tahoma,Arial,sans-serif;">
            <h1>{app}</h1>
            <p>رمز التأكيد: <strong>{code}</strong></p>
            <p>ينتهي خلال {model.ExpirationMinutes} دقيقة.</p>
            </body></html>
            """;
        var text = $"{model.ApplicationName}\nرمز التأكيد: {model.VerificationCode}\nينتهي خلال {model.ExpirationMinutes} دقيقة.";
        return new RenderedEmail("تأكيد البريد الإلكتروني", html, text);
    }

    public RenderedEmail RenderPasswordReset(PasswordResetTemplateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        // Deferred product feature — keep brand-safe Arabic shell for Permixa completeness.
        var app = Encode(model.ApplicationName);
        var url = EncodeAttr(model.ResetUrl);
        var html = $"""
            <!DOCTYPE html><html lang="ar" dir="rtl"><body style="font-family:Tahoma,Arial,sans-serif;">
            <h1>{app}</h1>
            <p><a href="{url}">إعادة تعيين كلمة المرور</a></p>
            </body></html>
            """;
        return new RenderedEmail("إعادة تعيين كلمة المرور", html, model.ResetUrl);
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string EncodeAttr(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
