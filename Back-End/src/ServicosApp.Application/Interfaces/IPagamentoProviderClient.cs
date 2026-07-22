using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Application.Interfaces;

public interface IPagamentoProviderClient
{
    string ProviderCode { get; }

    Task<PagamentoProviderResult> CriarCobrancaMaquininhaAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        string webhookUrl,
        CancellationToken cancellationToken = default);

    Task<PagamentoProviderResult> CriarCobrancaPixAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        string webhookUrl,
        CancellationToken cancellationToken = default);

    Task<PagamentoProviderResult> ConsultarAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken = default);

    Task<bool> CancelarAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken = default);
}
