using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface ICategoriaPecaService
{
    Task<CategoriaPecaDto> CriarAsync(Guid empresaId, CreateCategoriaPecaDto dto, CancellationToken cancellationToken = default);
    Task<List<CategoriaPecaDto>> ListarAsync(Guid empresaId, bool? ativo = null, string? busca = null, CancellationToken cancellationToken = default);
    Task<CategoriaPecaDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<CategoriaPecaDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateCategoriaPecaDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> AtivarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}
