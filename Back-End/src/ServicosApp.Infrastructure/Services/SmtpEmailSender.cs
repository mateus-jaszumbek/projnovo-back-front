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

        using var client = new SmtpClient();

        try
        {
            var socketOptions = options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(options.Host, options.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.User))
                await client.AuthenticateAsync(options.User, options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
