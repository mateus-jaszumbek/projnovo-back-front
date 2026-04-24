using ServicosApp.Application.DTOs;

namespace ServicosApp.Application.Interfaces;

public interface ICredencialFiscalEmpresaService
{
    Task<CredencialFiscalEmpresaDto> CriarAsync(
        Guid empresaId,
        CreateCredencialFiscalEmpresaDto dto,
        CancellationToken cancellationToken = default);

    Task<List<CredencialFiscalEmpresaDto>> ListarAsync(
        Guid empresaId,
        string? tipoDocumentoFiscal,
        bool? ativo,
        CancellationToken cancellationToken = default);

    Task<CredencialFiscalEmpresaDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CredencialFiscalEmpresaDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateCredencialFiscalEmpresaDto dto,
        CancellationToken cancellationToken = default);
}
