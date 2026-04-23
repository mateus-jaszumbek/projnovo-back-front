using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface ICaixaLancamentoService
{
    Task<CaixaLancamentoDto> LancarAsync(Guid empresaId, Guid? usuarioId, CreateCaixaLancamentoDto dto, CancellationToken cancellationToken = default);
    Task<List<CaixaLancamentoDto>> ListarPorCaixaAsync(Guid empresaId, Guid caixaDiarioId, CancellationToken cancellationToken = default);
}