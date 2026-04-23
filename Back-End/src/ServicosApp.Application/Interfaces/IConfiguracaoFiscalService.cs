using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IConfiguracaoFiscalService
{
    Task<ConfiguracaoFiscalDto> SalvarAsync(Guid empresaId, UpdateConfiguracaoFiscalDto dto, CancellationToken cancellationToken = default);
    Task<ConfiguracaoFiscalDto?> ObterAsync(Guid empresaId, CancellationToken cancellationToken = default);
}