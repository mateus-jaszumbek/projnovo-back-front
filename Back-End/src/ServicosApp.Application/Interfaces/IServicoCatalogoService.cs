using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IServicoCatalogoService
{
    Task<ServicoCatalogoDto> CriarAsync(Guid empresaId, CreateServicoCatalogoDto dto, CancellationToken cancellationToken = default);
    Task<List<ServicoCatalogoDto>> ListarAsync(Guid empresaId, bool? ativo = null, string? busca = null, CancellationToken cancellationToken = default);
    Task<ServicoCatalogoDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<ServicoCatalogoDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateServicoCatalogoDto dto, CancellationToken cancellationToken = default);
    Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> AtivarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}