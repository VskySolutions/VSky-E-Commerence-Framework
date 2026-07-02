using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Recaptcha;

/// <summary>Per-form reCAPTCHA enable flags for the eight protected form types (AC-TEN-007.4).</summary>
public class RecaptchaFormSettings
{
    public bool Register { get; set; }
    public bool Login { get; set; }
    public bool PasswordReset { get; set; }
    public bool GuestCheckout { get; set; }
    public bool Contact { get; set; }
    public bool Newsletter { get; set; }
    public bool ReviewSubmit { get; set; }
    public bool QaSubmit { get; set; }

    public static RecaptchaFormSettings From(RecaptchaConfig c) => new()
    {
        Register = c.ProtectRegister,
        Login = c.ProtectLogin,
        PasswordReset = c.ProtectPasswordReset,
        GuestCheckout = c.ProtectGuestCheckout,
        Contact = c.ProtectContact,
        Newsletter = c.ProtectNewsletter,
        ReviewSubmit = c.ProtectReviewSubmit,
        QaSubmit = c.ProtectQaSubmit,
    };
}

/// <summary>Admin view of the reCAPTCHA config. The Secret Key is masked, never returned in full.</summary>
public class RecaptchaConfigDto
{
    public string? SiteKey { get; set; }
    public RecaptchaVersion Version { get; set; }
    public decimal ScoreThreshold { get; set; }
    public RecaptchaFailBehaviour FailBehaviour { get; set; }
    public RecaptchaFormSettings PerFormSettings { get; set; } = new();
    public bool HasSecretKey { get; set; }
    public string? SecretKeyMasked { get; set; }

    public static RecaptchaConfigDto From(RecaptchaConfig c) => new()
    {
        SiteKey = c.SiteKey,
        Version = c.Version,
        ScoreThreshold = c.ScoreThreshold,
        FailBehaviour = c.FailBehaviour,
        PerFormSettings = RecaptchaFormSettings.From(c),
        HasSecretKey = !string.IsNullOrWhiteSpace(c.SecretKeyEncrypted),
        SecretKeyMasked = string.IsNullOrWhiteSpace(c.SecretKeyLast4) ? null : $"••••{c.SecretKeyLast4}",
    };
}

/// <summary>Public config for frontend widget initialisation — Site Key + version + per-form flags only (AC-TEN-007.5).</summary>
public class PublicRecaptchaConfigDto
{
    public string? SiteKey { get; set; }
    public RecaptchaVersion Version { get; set; }
    public RecaptchaFormSettings PerFormSettings { get; set; } = new();

    public static PublicRecaptchaConfigDto From(RecaptchaConfig c) => new()
    {
        SiteKey = c.SiteKey,
        Version = c.Version,
        PerFormSettings = RecaptchaFormSettings.From(c),
    };
}
