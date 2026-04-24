using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IFocusNfseMunicipioService
{
    Task<FocusNfseMunicipioValidacaoDto> ValidarAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);
}
