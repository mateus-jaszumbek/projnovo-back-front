using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegistrarEmpresaAsync(
        RegistrarEmpresaDto dto,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default);
}