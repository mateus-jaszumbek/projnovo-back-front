namespace ServicosApp.Application.DTOs;

public class FocusWebhookSetupDto
{
    public string? ProviderCode { get; set; }
    public bool FocusProviderSelected { get; set; }
    public bool Enabled { get; set; }
    public bool SecretConfigured { get; set; }
    public string? PublicBaseUrl { get; set; }
    public bool BaseUrlLooksPublic { get; set; }
    public string? DfeWebhookUrl { get; set; }
    public string? NfseWebhookUrl { get; set; }
    public bool UrlsReady { get; set; }
    public bool CanRegisterRemotely { get; set; }
    public bool CheckedRemotely { get; set; }
    public bool SyncedRemotely { get; set; }
    public FocusWebhookRemoteStatusDto DfeRemoteStatus { get; set; } = new()
    {
        Event = "nfe"
    };
    public FocusWebhookRemoteStatusDto NfseRemoteStatus { get; set; } = new()
    {
        Event = "nfse"
    };
    public List<string> ActionsTaken { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> NextSteps { get; set; } = [];
}
