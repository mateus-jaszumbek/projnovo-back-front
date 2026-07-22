using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IGestaoService
{
    Task RegistrarCompraPecaAsync(Guid empresaId, Guid? usuarioId, CompraPecaDto dto, CancellationToken cancellationToken = default);
    Task<DreGerencialDto> ObterDreAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
    Task<List<ComissaoDto>> ListarComissoesAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, decimal percentualVendas, decimal percentualServicos, CancellationToken cancellationToken = default);
    Task<List<AuditoriaFinanceiraDto>> ListarAuditoriaFinanceiraAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
    Task<List<DespesaCategoriaDto>> ListarDespesasPorCategoriaAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default);
    Task<List<ResumoMensalDto>> ListarResumoMensalAsync(Guid empresaId, int meses, CancellationToken cancellationToken = default);
    Task<List<AniversarianteDto>> ListarAniversariantesAsync(Guid empresaId, int? mes, CancellationToken cancellationToken = default);
    Task<List<ClienteInativoDto>> ListarClientesInativosAsync(Guid empresaId, int mesesMin, int mesesMax, CancellationToken cancellationToken = default);
}
