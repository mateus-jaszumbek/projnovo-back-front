using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IModuloPersonalizadoService
{
    Task<List<ModuloPersonalizadoDto>> ListarModulosAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<ModuloPersonalizadoDto?> ObterModuloAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken = default);
    Task<ModuloPersonalizadoDto?> ObterModuloPorChaveAsync(Guid empresaId, string chave, CancellationToken cancellationToken = default);
    Task<ModuloPersonalizadoDto> CriarModuloAsync(Guid empresaId, CreateModuloPersonalizadoDto dto, CancellationToken cancellationToken = default);
    Task<ModuloPersonalizadoDto> GarantirModuloSistemaAsync(Guid empresaId, EnsureModuloSistemaDto dto, CancellationToken cancellationToken = default);
    Task<ModuloPersonalizadoDto?> AtualizarModuloAsync(Guid empresaId, Guid moduloId, UpdateModuloPersonalizadoDto dto, CancellationToken cancellationToken = default);

    Task<CampoPersonalizadoDto> CriarCampoAsync(Guid empresaId, Guid moduloId, CreateCampoPersonalizadoDto dto, CancellationToken cancellationToken = default);
    Task<CampoPersonalizadoDto?> AtualizarCampoAsync(Guid empresaId, Guid moduloId, Guid campoId, UpdateCampoPersonalizadoDto dto, CancellationToken cancellationToken = default);
    Task<bool> ExcluirCampoAsync(Guid empresaId, Guid moduloId, Guid campoId, CancellationToken cancellationToken = default);
    Task<bool> ReordenarCamposAsync(Guid empresaId, Guid moduloId, List<CampoLayoutDto> campos, CancellationToken cancellationToken = default);
    Task<List<CampoModuloLayoutDto>> ListarLayoutAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken = default);
    Task<bool> SalvarLayoutAsync(Guid empresaId, Guid moduloId, List<CampoModuloLayoutDto> campos, CancellationToken cancellationToken = default);

    Task<List<RegistroPersonalizadoDto>> ListarRegistrosAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken = default);
    Task<RegistroPersonalizadoDto> CriarRegistroAsync(Guid empresaId, Guid moduloId, CreateRegistroPersonalizadoDto dto, CancellationToken cancellationToken = default);
    Task<RegistroPersonalizadoDto> SalvarRegistroOrigemAsync(Guid empresaId, Guid moduloId, Guid origemId, CreateRegistroPersonalizadoDto dto, CancellationToken cancellationToken = default);
}
