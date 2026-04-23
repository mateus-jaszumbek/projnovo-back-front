using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IAparelhoService
{
    Task<AparelhoDto> CriarAsync(Guid empresaId, CreateAparelhoDto dto, CancellationToken cancellationToken = default);
    Task<List<AparelhoDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<List<AparelhoDto>> ListarPorClienteAsync(Guid empresaId, Guid clienteId, CancellationToken cancellationToken = default);
    Task<AparelhoDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<AparelhoDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateAparelhoDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}
