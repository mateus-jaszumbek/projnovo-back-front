using ServicosApp.Application.DTOs.Pagamentos;

namespace ServicosApp.Application.Interfaces;

public interface ICobrancaPagamentoService
{
    Task<CobrancaPagamentoDto> CriarAsync(
        Guid empresaId,
        Guid? usuarioId,
        CreateCobrancaPagamentoDto dto,
        string requestBaseUrl,
        CancellationToken cancellationToken = default);

    Task<CobrancaPagamentoDto?> ObterAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);

    Task<CobrancaPagamentoDto?> ConsultarStatusAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> CancelarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);

    Task ProcessarWebhookAsync(
        string providerCode,
        string webhookSecret,
        string rawPayload,
        CancellationToken cancellationToken = default);
}
