using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface ICaixaDiarioService
{
    Task<CaixaDiarioDto> AbrirAsync(Guid empresaId, Guid? usuarioId, CreateCaixaDiarioDto dto, CancellationToken cancellationToken = default);
    Task<List<CaixaDiarioDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<CaixaDiarioDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<CaixaDiarioDto?> FecharAsync(Guid empresaId, Guid id, Guid? usuarioId, FecharCaixaDiarioDto dto, CancellationToken cancellationToken = default);
}