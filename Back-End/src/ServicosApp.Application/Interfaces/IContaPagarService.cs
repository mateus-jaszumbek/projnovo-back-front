using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IContaPagarService
{
    Task<ContaPagarDto> CriarAsync(Guid empresaId, CreateContaPagarDto dto, CancellationToken cancellationToken = default);
    Task<List<ContaPagarDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<ContaPagarDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<ContaPagarDto?> PagarAsync(Guid empresaId, Guid? usuarioId, Guid id, PagarContaPagarDto dto, CancellationToken cancellationToken = default);
}
