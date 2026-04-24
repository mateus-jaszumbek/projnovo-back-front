using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IFiscalPendingSyncService
{
    Task<FiscalPendingSyncResultDto> SynchronizePendingAsync(
        CancellationToken cancellationToken = default);
}
