using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IGestaoService
{
    Task RegistrarCompraPecaAsync(Guid empresaId, Guid? usuarioId, CompraPecaDto dto, CancellationToken cancellationToken = default);
    Task<DreGerencialDto> ObterDreAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
    Task<List<ComissaoDto>> ListarComissoesAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, decimal percentualVendas, decimal percentualServicos, CancellationToken cancellationToken = default);
    Task<List<AuditoriaFinanceiraDto>> ListarAuditoriaFinanceiraAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
}
