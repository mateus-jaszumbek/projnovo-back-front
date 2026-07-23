using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ResendOptions> _optionsMonitor;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient httpClient,
        IOptionsMonitor<ResendOptions> optionsMonitor,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.FromEmail))
        {
            _logger.LogWarning(
                "Envio de e-mail desabilitado ou Resend nao configurado. E-mail para {ToEmail} com assunto '{Subject}' nao foi enviado.",
                toEmail,
                subject);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Content = JsonContent.Create(new
            {
                from = $"{options.FromName} <{options.FromEmail}>",
                to = new[] { toEmail },
                subject,
                html = htmlBody,
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Falha ao enviar e-mail para {ToEmail} via Resend. Status {StatusCode}: {Body}",
                    toEmail,
                    (int)response.StatusCode,
                    body);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nao propaga: falha no envio de e-mail nao deve derrubar o fluxo (ex.: esqueci-senha)
            // nem revelar ao chamador se o envio funcionou ou nao.
            _logger.LogError(ex, "Falha ao enviar e-mail para {ToEmail} via Resend.", toEmail);
        }
    }
}
