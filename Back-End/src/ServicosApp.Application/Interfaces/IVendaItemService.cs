using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IVendaItemService
{
    Task<VendaItemDto> AdicionarAsync(Guid empresaId, Guid vendaId, CreateVendaItemDto dto, CancellationToken cancellationToken = default);
    Task<List<VendaItemDto>> ListarAsync(Guid empresaId, Guid vendaId, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(Guid empresaId, Guid vendaId, Guid itemId, CancellationToken cancellationToken = default);
}