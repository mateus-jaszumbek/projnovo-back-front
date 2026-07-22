using ServicosApp.Application.DTOs.Pagamentos;

namespace ServicosApp.Application.Interfaces;

public interface IConfiguracaoPagamentoService
{
    Task<ConfiguracaoPagamentoDto?> ObterAsync(Guid empresaId, string requestBaseUrl, CancellationToken cancellationToken = default);

    Task<ConfiguracaoPagamentoDto> SalvarAsync(
        Guid empresaId,
        UpdateConfiguracaoPagamentoDto dto,
        string requestBaseUrl,
        CancellationToken cancellationToken = default);

    IReadOnlyCollection<PagamentoProviderInfoDto> ListarProvedoresDisponiveis();
}
