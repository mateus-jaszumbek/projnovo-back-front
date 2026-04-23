using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IClienteService
{
    Task<ClienteDto> CriarAsync(Guid empresaId, CreateClienteDto dto, CancellationToken cancellationToken = default);
    Task<List<ClienteDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<ClienteDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<ClienteDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateClienteDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}
