using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioDto> CriarAsync(
        CreateUsuarioDto dto,
        Guid? empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default);

    Task<List<UsuarioDto>> ListarAsync(
        Guid? empresaId,
        bool incluirTodasEmpresas,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto?> ObterPorIdAsync(
        Guid id,
        Guid? empresaId,
        bool incluirTodasEmpresas,
        CancellationToken cancellationToken = default);

    Task<bool> InativarAsync(
        Guid id,
        Guid usuarioSolicitanteId,
        Guid? empresaId,
        bool incluirTodasEmpresas,
        CancellationToken cancellationToken = default);
}
