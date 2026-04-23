using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IContaReceberService
{
    Task<ContaReceberDto> CriarAsync(Guid empresaId, CreateContaReceberDto dto, CancellationToken cancellationToken = default);
    Task<List<ContaReceberDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<ContaReceberDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<ContaReceberDto?> ReceberAsync(Guid empresaId, Guid? usuarioId, Guid id, ReceberContaReceberDto dto, CancellationToken cancellationToken = default);
}
