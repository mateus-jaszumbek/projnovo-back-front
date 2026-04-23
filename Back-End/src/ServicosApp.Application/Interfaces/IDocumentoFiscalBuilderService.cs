using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;

namespace ServicosApp.Application.Interfaces;

public interface IDocumentoFiscalBuilderService
{
    Task<DocumentoFiscal> CriarNfsePorOrdemServicoAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid ordemServicoId,
        DateTime? dataCompetencia,
        string? observacoesNota,
        CancellationToken cancellationToken = default);

    Task<DocumentoFiscal> CriarDfePorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        TipoDocumentoFiscal tipoDocumento,
        DateTime? dataEmissao,
        string? observacoesNota,
        bool validarTributacaoCompleta,
        CancellationToken cancellationToken = default);
}
