using ServicosApp.Application.DTOs;

public interface IOrdemServicoService
{
    Task<OrdemServicoDto> CriarAsync(Guid empresaId, CreateOrdemServicoDto dto, CancellationToken cancellationToken = default);
    Task<List<OrdemServicoDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<OrdemServicoDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<OrdemServicoDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateOrdemServicoDto dto, CancellationToken cancellationToken = default);
    Task<OrdemServicoDto?> AlterarStatusAsync(Guid empresaId, Guid id, AlterarStatusOrdemServicoDto dto, CancellationToken cancellationToken = default);

    Task<bool> CancelarAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid id,
        CancellationToken cancellationToken = default);
}