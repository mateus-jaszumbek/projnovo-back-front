namespace ServicosApp.Infrastructure.Services;

public sealed class ResendOptions
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Servicos App";
}
