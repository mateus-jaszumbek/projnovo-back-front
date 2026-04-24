using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class FocusFiscalWebhookService : IFocusFiscalWebhookService
{
    private readonly AppDbContext _context;
    private readonly IDfeVendaService _dfeVendaService;
    private readonly INfseService _nfseService;
    private readonly IOptions<FocusWebhookOptions> _options;
    private readonly ILogger<FocusFiscalWebhookService> _logger;

    public FocusFiscalWebhookService(
        AppDbContext context,
        IDfeVendaService dfeVendaService,
        INfseService nfseService,
        IOptions<FocusWebhookOptions> options,
        ILogger<FocusFiscalWebhookService> logger)
    {
        _context = context;
        _dfeVendaService = dfeVendaService;
        _nfseService = nfseService;
        _options = options;
        _logger = logger;
    }

    public bool IsRequestAuthorized(string? providedSecret)
    {
        var configuredSecret = _options.Value.Secret?.Trim();

        if (!_options.Value.Enabled || string.IsNullOrWhiteSpace(configuredSecret))
            return false;

        var provided = providedSecret?.Trim();
        if (string.IsNullOrWhiteSpace(provided))
            return false;

        var configuredBytes = Encoding.UTF8.GetBytes(configuredSecret);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (configuredBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes);
    }

    public Task<FocusWebhookProcessResultDto> ProcessDfeAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        return ProcessAsync(
            payload,
            isDfe: true,
            cancellationToken);
    }

    public Task<FocusWebhookProcessResultDto> ProcessNfseAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        return ProcessAsync(
            payload,
            isDfe: false,
            cancellationToken);
    }

    private async Task<FocusWebhookProcessResultDto> ProcessAsync(
        JsonElement payload,
        bool isDfe,
        CancellationToken cancellationToken)
    {
        var result = new FocusWebhookProcessResultDto
        {
            Enabled = _options.Value.Enabled,
            ProviderStatus = ExtractString(payload, "status", "situacao")
        };

        if (!_options.Value.Enabled)
        {
            result.IgnoredReason = "Webhook da Focus esta desabilitado.";
            return result;
        }

        var reference = ExtractReference(payload);
        result.Reference = reference;

        if (string.IsNullOrWhiteSpace(reference))
        {
            result.IgnoredReason = "Payload recebido sem referencia do documento.";
            return result;
        }

        var documento = await FindDocumentAsync(reference, isDfe, cancellationToken);
        if (documento is null)
        {
            result.IgnoredReason = "Nenhum documento fiscal correspondente foi encontrado.";
            return result;
        }

        result.DocumentoFiscalId = documento.Id;
        result.EmpresaId = documento.EmpresaId;
        result.TipoDocumento = documento.TipoDocumento.ToString();
        result.StatusBefore = documento.Status.ToString();

        DocumentoFiscalDto documentoAtualizado;

        try
        {
            documentoAtualizado = isDfe
                ? await _dfeVendaService.ConsultarAsync(documento.EmpresaId, documento.Id, cancellationToken)
                : await _nfseService.ConsultarAsync(documento.EmpresaId, documento.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao processar webhook da Focus para o documento {DocumentoFiscalId}.",
                documento.Id);

            result.IgnoredReason = "Falha ao consultar o documento fiscal no provedor.";
            return result;
        }

        result.Processed = true;
        result.StatusAfter = documentoAtualizado.Status;
        return result;
    }

    private async Task<WebhookDocumentRef?> FindDocumentAsync(
        string reference,
        bool isDfe,
        CancellationToken cancellationToken)
    {
        var query = _context.DocumentosFiscais
            .AsNoTracking()
            .Where(x => isDfe
                ? x.TipoDocumento == TipoDocumentoFiscal.Nfe || x.TipoDocumento == TipoDocumentoFiscal.Nfce
                : x.TipoDocumento == TipoDocumentoFiscal.Nfse);

        if (TryParseReferenceGuid(reference, out var documentId))
        {
            return await query
                .Where(x => x.Id == documentId || x.NumeroExterno == reference)
                .Select(x => new WebhookDocumentRef(x.Id, x.EmpresaId, x.TipoDocumento, x.Status))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await query
            .Where(x => x.NumeroExterno == reference)
            .Select(x => new WebhookDocumentRef(x.Id, x.EmpresaId, x.TipoDocumento, x.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ExtractReference(JsonElement payload)
    {
        var direct = ExtractString(payload, "referencia", "ref", "reference", "numero_externo");
        if (!string.IsNullOrWhiteSpace(direct))
            return direct.Trim();

        if (TryGetProperty(payload, "documento", out var documentoNode))
        {
            var nested = ExtractString(documentoNode, "referencia", "ref", "reference", "numero_externo");
            if (!string.IsNullOrWhiteSpace(nested))
                return nested.Trim();
        }

        return null;
    }

    private static string? ExtractString(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(payload, name, out var property) ||
                property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => property.GetRawText()
            };
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static bool TryParseReferenceGuid(string reference, out Guid documentId)
    {
        if (Guid.TryParse(reference, out documentId))
            return true;

        if (reference.Length == 32)
            return Guid.TryParseExact(reference, "N", out documentId);

        return false;
    }

    private sealed record WebhookDocumentRef(
        Guid Id,
        Guid EmpresaId,
        TipoDocumentoFiscal TipoDocumento,
        StatusDocumentoFiscal Status);
}
