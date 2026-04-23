using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface IRegraFiscalProdutoService
{
    Task<RegraFiscalProdutoDto> CriarAsync(
        Guid empresaId,
        CreateRegraFiscalProdutoDto dto,
        CancellationToken cancellationToken = default);

    Task<List<RegraFiscalProdutoDto>> ListarAsync(
        Guid empresaId,
        string? tipoDocumentoFiscal,
        bool? ativo,
        CancellationToken cancellationToken = default);

    Task<RegraFiscalProdutoDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RegraFiscalProdutoDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateRegraFiscalProdutoDto dto,
        CancellationToken cancellationToken = default);
}
