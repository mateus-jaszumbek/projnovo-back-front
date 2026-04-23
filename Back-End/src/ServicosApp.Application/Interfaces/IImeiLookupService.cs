using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IImeiLookupService
{
    Task<ImeiLookupDto> ConsultarAsync(string imei, CancellationToken cancellationToken);
}
