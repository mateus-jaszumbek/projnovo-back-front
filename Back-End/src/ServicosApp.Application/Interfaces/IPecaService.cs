
using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IPecaService
{
    Task<PecaDto> CriarAsync(Guid empresaId, CreatePecaDto dto, CancellationToken cancellationToken = default);
    Task<List<PecaDto>> ListarAsync(Guid empresaId, bool? ativo = null, string? busca = null, CancellationToken cancellationToken = default);
    Task<PecaDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<PecaDto?> AtualizarAsync(Guid empresaId, Guid id, UpdatePecaDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> AtivarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}