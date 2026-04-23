using ServicosApp.Application.DTOs;

public interface IOrdemServicoItemService
{
    Task<OrdemServicoItemDto> AdicionarAsync(Guid empresaId, Guid ordemServicoId, CreateOrdemServicoItemDto dto, CancellationToken cancellationToken = default);
    Task<List<OrdemServicoItemDto>> ListarAsync(Guid empresaId, Guid ordemServicoId, CancellationToken cancellationToken = default);
    Task<OrdemServicoItemDto?> AtualizarAsync(Guid empresaId, Guid ordemServicoId, Guid itemId, UpdateOrdemServicoItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(Guid empresaId, Guid ordemServicoId, Guid itemId, CancellationToken cancellationToken = default);
    Task<bool> ReordenarAsync(Guid empresaId, Guid ordemServicoId, List<ReordenarOrdemServicoItemDto> itens, CancellationToken cancellationToken = default);
}
