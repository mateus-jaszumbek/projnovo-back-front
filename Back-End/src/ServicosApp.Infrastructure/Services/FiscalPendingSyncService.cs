using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class FiscalPendingSyncService : IFiscalPendingSyncService
{
    private readonly AppDbContext _context;
    private readonly IDfeVendaService _dfeVendaService;
    private readonly INfseService _nfseService;
    private readonly IOptions<FiscalPendingSyncOptions> _options;
    private readonly ILogger<FiscalPendingSyncService> _logger;

    public FiscalPendingSyncService(
        AppDbContext context,
        IDfeVendaService dfeVendaService,
        INfseService nfseService,
        IOptions<FiscalPendingSyncOptions> options,
        ILogger<FiscalPendingSyncService> logger)
    {
        _context = context;
        _dfeVendaService = dfeVendaService;
        _nfseService = nfseService;
        _options = options;
        _logger = logger;
    }

    public async Task<FiscalPendingSyncResultDto> SynchronizePendingAsync(
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        var result = new FiscalPendingSyncResultDto
        {
            Enabled = options.Enabled
        };

        if (!options.Enabled)
            return result;

        var batchSize = Math.Max(1, options.BatchSize);
        var cooldown = TimeSpan.FromSeconds(Math.Max(0, options.CooldownSeconds));
        var cutoff = DateTime.UtcNow.Subtract(cooldown);

        var pendingDocuments = await _context.DocumentosFiscais
            .AsNoTracking()
            .Where(x =>
                x.Status == StatusDocumentoFiscal.PendenteEnvio &&
                x.UpdatedAt <= cutoff)
            .OrderBy(x => x.UpdatedAt)
            .Take(batchSize)
            .Select(x => new PendingFiscalDocumentRef(
                x.Id,
                x.EmpresaId,
                x.TipoDocumento))
            .ToListAsync(cancellationToken);

        result.ScannedCount = pendingDocuments.Count;

        foreach (var document in pendingDocuments)
        {
            try
            {
                var documentoAtualizado = document.TipoDocumento == TipoDocumentoFiscal.Nfse
                    ? await _nfseService.ConsultarAsync(document.EmpresaId, document.Id, cancellationToken)
                    : await _dfeVendaService.ConsultarAsync(document.EmpresaId, document.Id, cancellationToken);

                result.ProcessedCount++;
                CountStatus(result, documentoAtualizado.Status);
            }
            catch (Exception ex)
            {
                result.FailedCount++;

                _logger.LogError(
                    ex,
                    "Falha ao sincronizar documento fiscal pendente {DocumentoFiscalId} da empresa {EmpresaId}.",
                    document.Id,
                    document.EmpresaId);
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }

        return result;
    }

    private static void CountStatus(FiscalPendingSyncResultDto result, string? status)
    {
        if (string.Equals(status, nameof(StatusDocumentoFiscal.Autorizado), StringComparison.OrdinalIgnoreCase))
        {
            result.AuthorizedCount++;
            return;
        }

        if (string.Equals(status, nameof(StatusDocumentoFiscal.Cancelado), StringComparison.OrdinalIgnoreCase))
        {
            result.CancelledCount++;
            return;
        }

        result.StillPendingCount++;
    }

    private sealed record PendingFiscalDocumentRef(
        Guid Id,
        Guid EmpresaId,
        TipoDocumentoFiscal TipoDocumento);
}
