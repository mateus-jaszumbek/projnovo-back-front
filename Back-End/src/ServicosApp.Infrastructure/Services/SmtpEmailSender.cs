using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptionsMonitor<SmtpOptions> _optionsMonitor;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptionsMonitor<SmtpOptions> optionsMonitor, ILogger<SmtpEmailSender> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Host))
        {
            _logger.LogWarning(
                "Envio de e-mail desabilitado ou Smtp:Host nao configurado. E-mail para {ToEmail} com assunto '{Subject}' nao foi enviado.",
                toEmail,
                subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient
        {
            // Alguns provedores de hospedagem restringem/atrasam conexoes de saida na porta SMTP;
            // sem um timeout curto, uma conexao bloqueada trava a requisicao por ~100s (padrao do MailKit).
            Timeout = 15_000,
        };

        try
        {
            // Auto detecta o modo certo pela porta (587 -> StartTls, 465 -> SSL direto).
            var socketOptions = options.UseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await client.ConnectAsync(options.Host, options.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.User))
                await client.AuthenticateAsync(options.User, options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);

            if (client.IsConnected)
                await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nao propaga: falha no envio de e-mail nao deve derrubar o fluxo (ex.: esqueci-senha)
            // nem revelar ao chamador se o envio funcionou ou nao.
            _logger.LogError(ex, "Falha ao enviar e-mail para {ToEmail} via SMTP {Host}:{Port}.", toEmail, options.Host, options.Port);
        }
    }
}
