namespace ServicosApp.Application.DTOs;

public class FocusWebhookRemoteStatusDto
{
    public string Event { get; set; } = string.Empty;
    public string? CredentialTipoDocumento { get; set; }
    public bool CredentialConfigured { get; set; }
    public bool CheckedRemotely { get; set; }
    public bool Registered { get; set; }
    public string? HookId { get; set; }
    public string? RemoteUrl { get; set; }
}
