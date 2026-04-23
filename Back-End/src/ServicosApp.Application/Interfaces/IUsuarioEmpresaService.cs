using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IUsuarioEmpresaService
{
    Task<UsuarioEmpresaDto> CriarAsync(
        CreateUsuarioEmpresaDto dto,
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default);

    Task<List<UsuarioEmpresaDto>> ListarAsync(
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default);

    Task<UsuarioEmpresaDto?> AtualizarAsync(
        Guid id,
        UpdateUsuarioEmpresaDto dto,
        Guid usuarioSolicitanteId,
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default);

    Task<bool> InativarAsync(
        Guid id,
        Guid usuarioSolicitanteId,
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default);
}
