namespace ServicosApp.Application.DTOs.Fiscal;

public class FocusWebhookOptions
{
    public bool Enabled { get; set; } = false;
    public string? Secret { get; set; }
    public string? PublicBaseUrl { get; set; }
}
