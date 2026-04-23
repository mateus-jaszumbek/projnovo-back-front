using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface ITecnicoService
{
    Task<TecnicoDto> CriarAsync(Guid empresaId, CreateTecnicoDto dto, CancellationToken cancellationToken = default);
    Task<List<TecnicoDto>> ListarAsync(Guid empresaId, bool? ativo = null, string? busca = null, CancellationToken cancellationToken = default);
    Task<TecnicoDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<TecnicoDto> AtualizarAsync(Guid empresaId, Guid id, UpdateTecnicoDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> AtivarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}