using ServicosApp.Application.DTOs;

public interface IEstoqueMovimentoService
{
    Task<EstoqueMovimentoDto> RegistrarEntradaAsync(Guid empresaId, CreateEstoqueEntradaDto dto, CancellationToken cancellationToken = default);
    Task<EstoqueMovimentoDto> RegistrarSaidaAsync(Guid empresaId, CreateEstoqueSaidaDto dto, CancellationToken cancellationToken = default);
    Task<EstoqueMovimentoDto> RegistrarConsumoOrdemServicoAsync(Guid empresaId, CreateConsumoOrdemServicoDto dto, CancellationToken cancellationToken = default);
    Task<List<EstoqueMovimentoDto>> ListarPorPecaAsync(Guid empresaId, Guid pecaId, CancellationToken cancellationToken = default);
}