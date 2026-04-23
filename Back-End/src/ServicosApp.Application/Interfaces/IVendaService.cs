using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IVendaService
{
    Task<VendaDto> CriarAsync(Guid empresaId, Guid? usuarioId, CreateVendaDto dto, CancellationToken cancellationToken = default);
    Task<VendaDto> CriarComItensAsync(Guid empresaId, Guid? usuarioId, CreateVendaComItensDto dto, CancellationToken cancellationToken = default);
    Task<List<VendaDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<VendaDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
    Task<VendaDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateVendaDto dto, CancellationToken cancellationToken = default);
    Task<bool> FinalizarAsync(Guid empresaId, Guid? usuarioId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> CancelarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}
