using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class DocumentoFiscalConsultaService : IDocumentoFiscalConsultaService
{
    private readonly AppDbContext _context;

    public DocumentoFiscalConsultaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DocumentoFiscalDto>> ListarAsync(
        Guid empresaId,
        string? tipoDocumento,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentosFiscais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(tipoDocumento))
        {
            if (!Enum.TryParse<TipoDocumentoFiscal>(tipoDocumento.Trim(), true, out var tipoEnum))
                throw new InvalidOperationException($"Tipo de documento fiscal inválido: {tipoDocumento}");

            query = query.Where(x => x.TipoDocumento == tipoEnum);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusDocumentoFiscal>(status.Trim(), true, out var statusEnum))
                throw new InvalidOperationException($"Status fiscal inválido: {status}");

            query = query.Where(x => x.Status == statusEnum);
        }

        var documentos = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return documentos.Select(DocumentoFiscalMapper.Map).ToList();
    }

    public async Task<DocumentoFiscalDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == documentoFiscalId, cancellationToken);

        return documento is null ? null : DocumentoFiscalMapper.Map(documento);
    }

    public async Task<TipoDocumentoFiscal?> ObterTipoDocumentoAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentosFiscais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == documentoFiscalId)
            .Select(x => (TipoDocumentoFiscal?)x.TipoDocumento)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
