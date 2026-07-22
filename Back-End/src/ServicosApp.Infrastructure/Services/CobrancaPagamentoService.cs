using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class CobrancaPagamentoService : ICobrancaPagamentoService
{
    private readonly AppDbContext _context;
    private readonly IPagamentoProviderResolver _resolver;
    private readonly IPagamentoCredentialSecretProtector _protector;
    private readonly IVendaService _vendaService;

    public CobrancaPagamentoService(
        AppDbContext context,
        IPagamentoProviderResolver resolver,
        IPagamentoCredentialSecretProtector protector,
        IVendaService vendaService)
    {
        _context = context;
        _resolver = resolver;
        _protector = protector;
        _vendaService = vendaService;
    }

    public async Task<CobrancaPagamentoDto> CriarAsync(
        Guid empresaId,
        Guid? usuarioId,
        CreateCobrancaPagamentoDto dto,
        string requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (dto.Valor <= 0)
            throw new InvalidOperationException("Informe um valor maior que zero para gerar a cobrança.");

        var canal = (dto.Canal ?? string.Empty).Trim().ToUpperInvariant();
        if (canal != "PIX" && canal != "MAQUININHA")
            throw new InvalidOperationException("Canal de cobrança inválido. Use PIX ou MAQUININHA.");

        var configuracao = await _context.ConfiguracoesPagamento
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken)
            ?? throw new InvalidOperationException("Configure um provedor de pagamento antes de gerar cobranças.");

        if (canal == "PIX" && !configuracao.SuportaPix)
            throw new InvalidOperationException("O provedor configurado não tem Pix habilitado.");

        if (canal == "MAQUININHA" && !configuracao.SuportaMaquininha)
            throw new InvalidOperationException("O provedor configurado não tem cobrança por maquininha habilitada.");

        var provider = _resolver.Resolve(configuracao.Provider);

        var cobranca = new CobrancaPagamento
        {
            EmpresaId = empresaId,
            Provider = configuracao.Provider,
            Canal = canal,
            OrigemTipo = dto.OrigemTipo,
            OrigemId = dto.OrigemId,
            Valor = dto.Valor,
            Descricao = dto.Descricao,
            Status = "PENDENTE",
            CreatedBy = usuarioId
        };

        _context.CobrancasPagamento.Add(cobranca);
        await _context.SaveChangesAsync(cancellationToken);

        var webhookUrl = $"{requestBaseUrl.TrimEnd('/')}/api/public/pagamentos/webhooks/{configuracao.Provider}/{configuracao.WebhookSecret}";
        var configuracaoParaUso = CloneParaUso(configuracao);

        var resultado = canal == "PIX"
            ? await provider.CriarCobrancaPixAsync(configuracaoParaUso, cobranca, webhookUrl, cancellationToken)
            : await provider.CriarCobrancaMaquininhaAsync(configuracaoParaUso, cobranca, webhookUrl, cancellationToken);

        cobranca.ExternalId = resultado.ExternalId;
        cobranca.QrCodeBase64 = resultado.QrCodeBase64;
        cobranca.QrCodePayload = resultado.QrCodePayload;
        cobranca.Status = resultado.Sucesso ? (resultado.Status ?? "PENDENTE") : "RECUSADA";
        cobranca.MensagemErro = resultado.Sucesso ? null : resultado.MensagemErro;

        await _context.SaveChangesAsync(cancellationToken);

        if (!resultado.Sucesso)
            throw new InvalidOperationException(resultado.MensagemErro ?? "Não foi possível gerar a cobrança no provedor de pagamento.");

        return Map(cobranca);
    }

    public async Task<CobrancaPagamentoDto?> ObterAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.CobrancasPagamento
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<CobrancaPagamentoDto?> ConsultarStatusAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var cobranca = await _context.CobrancasPagamento
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (cobranca is null)
            return null;

        if (cobranca.Status is "APROVADA" or "RECUSADA" or "CANCELADA")
            return Map(cobranca);

        var configuracao = await _context.ConfiguracoesPagamento
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        if (configuracao is null)
            return Map(cobranca);

        await AtualizarStatusAsync(configuracao, cobranca, cancellationToken);
        return Map(cobranca);
    }

    public async Task<bool> CancelarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var cobranca = await _context.CobrancasPagamento
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (cobranca is null)
            return false;

        if (cobranca.Status != "PENDENTE")
            return true;

        var configuracao = await _context.ConfiguracoesPagamento
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        if (configuracao is not null)
        {
            var provider = _resolver.Resolve(configuracao.Provider);
            await provider.CancelarAsync(CloneParaUso(configuracao), cobranca, cancellationToken);
        }

        cobranca.Status = "CANCELADA";
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ProcessarWebhookAsync(
        string providerCode,
        string webhookSecret,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = PagamentoProviderCodeNormalizer.Normalize(providerCode);

        var configuracao = await _context.ConfiguracoesPagamento
            .FirstOrDefaultAsync(
                x => x.Provider == normalizedProvider && x.WebhookSecret == webhookSecret,
                cancellationToken);

        if (configuracao is null)
            return;

        var externalId = ExtrairExternalId(rawPayload);
        if (string.IsNullOrWhiteSpace(externalId))
            return;

        var cobranca = await _context.CobrancasPagamento
            .FirstOrDefaultAsync(
                x => x.EmpresaId == configuracao.EmpresaId && x.ExternalId == externalId,
                cancellationToken);

        if (cobranca is null || cobranca.Status is "APROVADA" or "RECUSADA" or "CANCELADA")
            return;

        await AtualizarStatusAsync(configuracao, cobranca, cancellationToken);
    }

    private async Task AtualizarStatusAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken)
    {
        var provider = _resolver.Resolve(configuracao.Provider);
        var resultado = await provider.ConsultarAsync(CloneParaUso(configuracao), cobranca, cancellationToken);

        if (!resultado.Sucesso)
            return;

        var statusAnterior = cobranca.Status;
        cobranca.Status = resultado.Status ?? cobranca.Status;

        if (cobranca.Status == "APROVADA" && statusAnterior != "APROVADA")
        {
            cobranca.PagoEm = DateTime.UtcNow;

            if (cobranca.OrigemTipo == "VENDA")
            {
                try
                {
                    await _vendaService.FinalizarAsync(cobranca.EmpresaId, cobranca.CreatedBy, cobranca.OrigemId, cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    // Venda já finalizada/cancelada por outro caminho - o pagamento em si já foi confirmado.
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtrairExternalId(string rawPayload)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId))
                return dataId.ToString();

            if (root.TryGetProperty("id", out var id))
                return id.ToString();
        }
        catch (JsonException)
        {
            // payload fora do formato esperado - ignorado, o webhook simplesmente não encontra a cobrança.
        }

        return null;
    }

    private ConfiguracaoPagamento CloneParaUso(ConfiguracaoPagamento entity)
    {
        return new ConfiguracaoPagamento
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Provider = entity.Provider,
            AccessTokenEncrypted = _protector.Unprotect(entity.AccessTokenEncrypted),
            PublicKey = entity.PublicKey,
            PosId = entity.PosId,
            UserIdExterno = entity.UserIdExterno,
            WebhookSecret = entity.WebhookSecret,
            SuportaMaquininha = entity.SuportaMaquininha,
            SuportaPix = entity.SuportaPix,
            Ativo = entity.Ativo
        };
    }

    private static CobrancaPagamentoDto Map(CobrancaPagamento entity)
    {
        return new CobrancaPagamentoDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Provider = entity.Provider,
            Canal = entity.Canal,
            OrigemTipo = entity.OrigemTipo,
            OrigemId = entity.OrigemId,
            Valor = entity.Valor,
            Status = entity.Status,
            Descricao = entity.Descricao,
            QrCodeBase64 = entity.QrCodeBase64,
            QrCodePayload = entity.QrCodePayload,
            MensagemErro = entity.MensagemErro,
            CreatedAt = entity.CreatedAt,
            PagoEm = entity.PagoEm
        };
    }
}
