using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ConfiguracaoFiscalService : IConfiguracaoFiscalService
{
    private readonly AppDbContext _context;
    private readonly IFocusNfseMunicipioService _focusNfseMunicipioService;
    private readonly IOptions<FocusWebhookOptions> _focusWebhookOptions;

    public ConfiguracaoFiscalService(
        AppDbContext context,
        IFocusNfseMunicipioService focusNfseMunicipioService,
        IOptions<FocusWebhookOptions> focusWebhookOptions)
    {
        _context = context;
        _focusNfseMunicipioService = focusNfseMunicipioService;
        _focusWebhookOptions = focusWebhookOptions;
    }

    public async Task<ConfiguracaoFiscalDto> SalvarAsync(
        Guid empresaId,
        UpdateConfiguracaoFiscalDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarDto(dto);

        var entity = await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        if (entity is null)
        {
            entity = new ConfiguracaoFiscal
            {
                EmpresaId = empresaId
            };

            _context.ConfiguracoesFiscais.Add(entity);
        }

        entity.Ambiente = ParseAmbiente(dto.Ambiente);
        entity.RegimeTributario = dto.RegimeTributario.Trim();

        entity.SerieNfce = dto.SerieNfce;
        entity.SerieNfe = dto.SerieNfe;
        entity.SerieNfse = dto.SerieNfse;

        entity.ProximoNumeroNfce = dto.ProximoNumeroNfce;
        entity.ProximoNumeroNfe = dto.ProximoNumeroNfe;
        entity.ProximoNumeroNfse = dto.ProximoNumeroNfse;

        entity.ProvedorFiscal = FiscalProviderCodeNormalizer.NormalizeOrNull(dto.ProvedorFiscal);

        entity.MunicipioCodigo = string.IsNullOrWhiteSpace(dto.MunicipioCodigo)
            ? null
            : dto.MunicipioCodigo.Trim();

        entity.CnaePrincipal = string.IsNullOrWhiteSpace(dto.CnaePrincipal)
            ? null
            : dto.CnaePrincipal.Trim();

        entity.ItemListaServico = string.IsNullOrWhiteSpace(dto.ItemListaServico)
            ? null
            : dto.ItemListaServico.Trim();

        entity.CodigoTributarioMunicipio = string.IsNullOrWhiteSpace(dto.CodigoTributarioMunicipio)
            ? null
            : dto.CodigoTributarioMunicipio.Trim();

        entity.NaturezaOperacaoPadrao = string.IsNullOrWhiteSpace(dto.NaturezaOperacaoPadrao)
            ? null
            : dto.NaturezaOperacaoPadrao.Trim();

        entity.IssRetidoPadrao = dto.IssRetidoPadrao;
        entity.AliquotaIssPadrao = dto.AliquotaIssPadrao;
        entity.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ConfiguracaoFiscalDto?> ObterAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public Task<FocusNfseMunicipioValidacaoDto> ValidarMunicipioFocusNfseAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        return _focusNfseMunicipioService.ValidarAsync(empresaId, cancellationToken);
    }

    public async Task<FocusWebhookSetupDto> ObterFocusWebhookSetupAsync(
        Guid empresaId,
        string? requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var configuracao = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        var providerCode = FiscalProviderCodeNormalizer.NormalizeOrNull(configuracao?.ProvedorFiscal);
        var secret = _focusWebhookOptions.Value.Secret?.Trim();
        var publicBaseUrl = ResolvePublicBaseUrl(
            _focusWebhookOptions.Value.PublicBaseUrl,
            requestBaseUrl);

        var result = new FocusWebhookSetupDto
        {
            ProviderCode = providerCode,
            FocusProviderSelected = string.Equals(
                providerCode,
                FiscalProviderCodes.FocusNfe,
                StringComparison.OrdinalIgnoreCase),
            Enabled = _focusWebhookOptions.Value.Enabled,
            SecretConfigured = !string.IsNullOrWhiteSpace(secret),
            PublicBaseUrl = publicBaseUrl,
            BaseUrlLooksPublic = LooksPublicBaseUrl(publicBaseUrl)
        };

        if (result.SecretConfigured && !string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            result.DfeWebhookUrl = BuildWebhookUrl(publicBaseUrl, "dfe", secret!);
            result.NfseWebhookUrl = BuildWebhookUrl(publicBaseUrl, "nfse", secret!);
            result.UrlsReady = true;
        }

        if (!result.FocusProviderSelected)
        {
            result.Warnings.Add("Selecione o provedor focusnfe para usar webhook fiscal com a Focus.");
        }

        if (!result.SecretConfigured)
        {
            result.Warnings.Add("Configure FocusWebhook:Secret no backend antes de cadastrar as URLs na Focus.");
        }

        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            result.Warnings.Add("Nao foi possivel montar a URL publica do backend para o webhook.");
        }
        else if (!result.BaseUrlLooksPublic)
        {
            result.Warnings.Add("A URL atual parece local ou privada. Use um dominio publico para receber webhooks da Focus.");
        }

        if (!result.Enabled)
        {
            result.Warnings.Add("O receiver do webhook esta desabilitado. Ative FocusWebhook:Enabled no backend.");
        }

        if (result.UrlsReady)
        {
            result.NextSteps.Add("Cadastre a URL de DF-e na Focus para NF-e e NFC-e.");
            result.NextSteps.Add("Cadastre a URL de NFS-e na Focus para notas de servico.");
            result.NextSteps.Add("Emita em homologacao e confirme que o status muda sozinho sem consulta manual.");
        }
        else
        {
            result.NextSteps.Add("Ajuste os avisos acima primeiro.");
            result.NextSteps.Add("Depois copie as URLs do webhook e cadastre na Focus.");
        }

        return result;
    }

    private static ConfiguracaoFiscalDto Map(ConfiguracaoFiscal entity)
    {
        return new ConfiguracaoFiscalDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Ambiente = entity.Ambiente.ToString(),
            RegimeTributario = entity.RegimeTributario,
            SerieNfce = entity.SerieNfce,
            SerieNfe = entity.SerieNfe,
            SerieNfse = entity.SerieNfse,
            ProximoNumeroNfce = entity.ProximoNumeroNfce,
            ProximoNumeroNfe = entity.ProximoNumeroNfe,
            ProximoNumeroNfse = entity.ProximoNumeroNfse,
            ProvedorFiscal = entity.ProvedorFiscal,
            MunicipioCodigo = entity.MunicipioCodigo,
            CnaePrincipal = entity.CnaePrincipal,
            ItemListaServico = entity.ItemListaServico,
            CodigoTributarioMunicipio = entity.CodigoTributarioMunicipio,
            NaturezaOperacaoPadrao = entity.NaturezaOperacaoPadrao,
            IssRetidoPadrao = entity.IssRetidoPadrao,
            AliquotaIssPadrao = entity.AliquotaIssPadrao,
            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static AmbienteFiscal ParseAmbiente(string? ambiente)
    {
        if (string.IsNullOrWhiteSpace(ambiente))
            return AmbienteFiscal.Homologacao;

        if (Enum.TryParse<AmbienteFiscal>(ambiente.Trim(), true, out var result))
            return result;

        throw new AppValidationException("Ambiente fiscal invalido. Use Homologacao ou Producao.");
    }

    private static void ValidarDto(UpdateConfiguracaoFiscalDto dto)
    {
        var ambiente = ParseAmbiente(dto.Ambiente);

        if (string.IsNullOrWhiteSpace(dto.RegimeTributario))
            throw new AppValidationException("Regime tributario e obrigatorio.");

        if (dto.SerieNfce <= 0 || dto.SerieNfe <= 0 || dto.SerieNfse <= 0)
            throw new AppValidationException("As series fiscais devem ser maiores que zero.");

        if (dto.ProximoNumeroNfce <= 0 || dto.ProximoNumeroNfe <= 0 || dto.ProximoNumeroNfse <= 0)
            throw new AppValidationException("A proxima numeracao fiscal deve ser maior que zero.");

        if (dto.AliquotaIssPadrao is < 0 or > 100)
            throw new AppValidationException("A aliquota de ISS deve estar entre 0 e 100.");

        var provedor = FiscalProviderCodeNormalizer.NormalizeOrNull(dto.ProvedorFiscal);
        if (ambiente == AmbienteFiscal.Producao &&
            (string.IsNullOrWhiteSpace(provedor) || FiscalProviderCodeNormalizer.IsFake(provedor)))
        {
            throw new AppValidationException("Producao fiscal exige provedor real configurado. Use Homologacao enquanto estiver em modo fake.");
        }
    }

    private static string? ResolvePublicBaseUrl(string? configuredBaseUrl, string? requestBaseUrl)
    {
        var candidate = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? requestBaseUrl
            : configuredBaseUrl;

        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        var normalized = candidate.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return null;

        var builder = new UriBuilder(uri);
        builder.Path = builder.Path.EndsWith("/api", StringComparison.OrdinalIgnoreCase)
            ? builder.Path[..^4]
            : builder.Path;

        return builder.Uri.ToString().TrimEnd('/');
    }

    private static string BuildWebhookUrl(string publicBaseUrl, string documentType, string secret)
    {
        return $"{publicBaseUrl}/api/fiscal/webhooks/focus/{documentType}/{Uri.EscapeDataString(secret)}";
    }

    private static bool LooksPublicBaseUrl(string? publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(publicBaseUrl) ||
            !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return false;

        if (uri.IsLoopback)
            return false;

        return !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}
