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

        if (entity.Ambiente == AmbienteFiscal.Producao)
        {
            var empresa = await _context.Empresas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == empresaId && x.Ativo, cancellationToken)
                ?? throw new AppValidationException("Empresa ativa nao encontrada para ativar producao fiscal.");

            var activeCredentials = await _context.CredenciaisFiscaisEmpresas
                .AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Ativo)
                .ToListAsync(cancellationToken);

            var productionBlockers = BuildProductionSaveBlockers(
                empresa,
                entity,
                activeCredentials);

            if (productionBlockers.Count > 0)
            {
                throw new AppValidationException(
                    $"A empresa ainda nao esta pronta para producao fiscal: {string.Join("; ", productionBlockers)}.");
            }
        }

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

    public async Task<FiscalReadinessDto> ObterChecklistAsync(
        Guid empresaId,
        string? requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId && x.Ativo, cancellationToken)
            ?? throw new AppNotFoundException("Empresa ativa nao encontrada para montar o checklist fiscal.");

        var configuracao = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        var providerCode = FiscalProviderCodeNormalizer.NormalizeOrNull(configuracao?.ProvedorFiscal);
        var focusProviderSelected = string.Equals(
            providerCode,
            FiscalProviderCodes.FocusNfe,
            StringComparison.OrdinalIgnoreCase);

        var activeCredentials = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .ToListAsync(cancellationToken);

        var providerCredentials = string.IsNullOrWhiteSpace(providerCode)
            ? []
            : activeCredentials
                .Where(x => string.Equals(
                    FiscalProviderCodeNormalizer.NormalizeOrNull(x.Provedor),
                    providerCode,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        var usableProviderCredentials = providerCredentials
            .Where(HasUsableCredentialForProvider)
            .ToList();

        var result = new FiscalReadinessDto
        {
            EmpresaId = empresaId,
            Ambiente = (configuracao?.Ambiente ?? AmbienteFiscal.Homologacao).ToString(),
            ProviderCode = providerCode,
            FocusProviderSelected = focusProviderSelected
        };

        void AddItem(
            string key,
            string title,
            string scope,
            string status,
            string detail,
            bool blocksHomologacao = false,
            bool blocksProducao = false)
        {
            result.Items.Add(new FiscalChecklistItemDto
            {
                Key = key,
                Title = title,
                Scope = scope,
                Status = status,
                Detail = detail,
                BlocksHomologacao = blocksHomologacao,
                BlocksProducao = blocksProducao
            });

            if (blocksHomologacao)
            {
                result.MissingForHomologacao.Add(title);
            }

            if (blocksProducao)
            {
                result.MissingForProducao.Add(title);
            }
        }

        if (configuracao is null)
        {
            AddItem(
                "config-base",
                "Configuracao fiscal base",
                "empresa",
                "error",
                "Salve a configuracao fiscal inicial da empresa antes de emitir ou validar notas.",
                blocksHomologacao: true,
                blocksProducao: true);
        }
        else
        {
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(configuracao.RegimeTributario))
                issues.Add("regime tributario");
            if (configuracao.SerieNfce <= 0 || configuracao.SerieNfe <= 0 || configuracao.SerieNfse <= 0)
                issues.Add("series fiscais");
            if (configuracao.ProximoNumeroNfce <= 0 || configuracao.ProximoNumeroNfe <= 0 || configuracao.ProximoNumeroNfse <= 0)
                issues.Add("proxima numeracao");

            if (issues.Count > 0)
            {
                AddItem(
                    "config-base",
                    "Configuracao fiscal base",
                    "empresa",
                    "error",
                    $"Corrija estes pontos da configuracao fiscal: {string.Join(", ", issues)}.",
                    blocksHomologacao: true,
                    blocksProducao: true);
            }
            else
            {
                AddItem(
                    "config-base",
                    "Configuracao fiscal base",
                    "empresa",
                    "ok",
                    "Regime, series e numeracao fiscal estao preenchidos.");
            }
        }

        if (string.IsNullOrWhiteSpace(providerCode))
        {
            AddItem(
                "provider",
                "Provider fiscal",
                "empresa",
                "error",
                "Selecione um provider fiscal. O modulo hoje suporta fake e focusnfe.",
                blocksHomologacao: true,
                blocksProducao: true);
        }
        else if (FiscalProviderCodeNormalizer.IsFake(providerCode))
        {
            AddItem(
                "provider",
                "Provider fiscal",
                "empresa",
                "warning",
                "A empresa ainda esta em modo fake. Homologacao fica liberada, mas producao real continua bloqueada ate trocar para um provider real.",
                blocksProducao: true);
            result.NextSteps.Add("Troque o provider fake por focusnfe quando quiser fechar a emissao real.");
        }
        else if (focusProviderSelected)
        {
            AddItem(
                "provider",
                "Provider fiscal",
                "empresa",
                "ok",
                "Focus NFe esta selecionada como provider fiscal da empresa.");
        }
        else
        {
            AddItem(
                "provider",
                "Provider fiscal",
                "empresa",
                "error",
                $"O provider '{providerCode}' ainda nao esta integrado neste ambiente.",
                blocksHomologacao: true,
                blocksProducao: true);
        }

        var missingBaseFields = new List<string>();
        if (string.IsNullOrWhiteSpace(empresa.RazaoSocial)) missingBaseFields.Add("razao social");
        if (string.IsNullOrWhiteSpace(empresa.Cnpj)) missingBaseFields.Add("CNPJ");
        if (string.IsNullOrWhiteSpace(empresa.Cidade)) missingBaseFields.Add("cidade");
        if (string.IsNullOrWhiteSpace(empresa.Uf)) missingBaseFields.Add("UF");

        if (missingBaseFields.Count > 0)
        {
            AddItem(
                "empresa-base",
                "Cadastro da empresa",
                "empresa",
                "error",
                $"Complete o cadastro da empresa: {string.Join(", ", missingBaseFields)}.",
                blocksHomologacao: true,
                blocksProducao: true);
        }
        else
        {
            AddItem(
                "empresa-base",
                "Cadastro da empresa",
                "empresa",
                "ok",
                "A base do cadastro da empresa ja esta preenchida para seguir no fiscal.");
        }

        if (focusProviderSelected)
        {
            if (providerCredentials.Count == 0)
            {
                AddItem(
                    "credenciais",
                    "Credenciais do provider",
                    "provider",
                    "error",
                    "Cadastre pelo menos uma credencial ativa da Focus para NFS-e, NF-e ou NFC-e.",
                    blocksHomologacao: true,
                    blocksProducao: true);
                result.NextSteps.Add("Cadastre uma credencial ativa da Focus para o primeiro fluxo que a empresa vai emitir.");
            }
            else
            {
                var missingSecretTypes = providerCredentials
                    .Where(x => !HasAnyCredentialSecret(x))
                    .Select(x => ToDocumentLabel(x.TipoDocumentoFiscal))
                    .Distinct()
                    .ToList();

                var expiredTokenTypes = providerCredentials
                    .Where(IsTokenExpired)
                    .Select(x => ToDocumentLabel(x.TipoDocumentoFiscal))
                    .Distinct()
                    .ToList();

                var expiringSoonTypes = providerCredentials
                    .Where(IsTokenExpiringSoon)
                    .Select(x => ToDocumentLabel(x.TipoDocumentoFiscal))
                    .Distinct()
                    .ToList();

                if (usableProviderCredentials.Count == 0)
                {
                    var details = new List<string>();
                    if (missingSecretTypes.Count > 0)
                        details.Add($"sem token/segredo em {string.Join(", ", missingSecretTypes)}");
                    if (expiredTokenTypes.Count > 0)
                        details.Add($"token expirado em {string.Join(", ", expiredTokenTypes)}");

                    AddItem(
                        "credenciais",
                        "Credenciais do provider",
                        "provider",
                        "error",
                        $"Nenhuma credencial ativa esta pronta para uso na Focus: {string.Join("; ", details)}.",
                        blocksHomologacao: true,
                        blocksProducao: true);
                }
                else if (missingSecretTypes.Count > 0 || expiredTokenTypes.Count > 0 || expiringSoonTypes.Count > 0)
                {
                    var details = new List<string>
                    {
                        $"Credenciais prontas: {DescribeCredentialTypes(usableProviderCredentials)}."
                    };

                    if (missingSecretTypes.Count > 0)
                        details.Add($"Pendencia de token/segredo: {string.Join(", ", missingSecretTypes)}.");
                    if (expiredTokenTypes.Count > 0)
                        details.Add($"Token expirado: {string.Join(", ", expiredTokenTypes)}.");
                    if (expiringSoonTypes.Count > 0)
                        details.Add($"Token perto do vencimento: {string.Join(", ", expiringSoonTypes)}.");

                    AddItem(
                        "credenciais",
                        "Credenciais do provider",
                        "provider",
                        "warning",
                        string.Join(" ", details));
                }
                else
                {
                    AddItem(
                        "credenciais",
                        "Credenciais do provider",
                        "provider",
                        "ok",
                        $"Credenciais ativas e prontas: {DescribeCredentialTypes(usableProviderCredentials)}.");
                }
            }

            var nfseCredentials = providerCredentials
                .Where(x => x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse)
                .ToList();
            var usableNfseCredentials = usableProviderCredentials
                .Where(x => x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse)
                .ToList();

            if (nfseCredentials.Count == 0)
            {
                AddItem(
                    "nfse",
                    "NFS-e",
                    "documento",
                    "info",
                    "Nenhuma credencial de NFS-e esta ativa. Cadastre uma quando a empresa quiser emitir notas de servico.");
            }
            else
            {
                var nfseIssues = new List<string>();
                if (usableNfseCredentials.Count == 0)
                    nfseIssues.Add("credencial ativa sem token ou segredo valido");
                if (string.IsNullOrWhiteSpace(empresa.InscricaoMunicipal))
                    nfseIssues.Add("inscricao municipal da empresa");
                if (string.IsNullOrWhiteSpace(configuracao?.MunicipioCodigo))
                    nfseIssues.Add("codigo IBGE do municipio");
                if (string.IsNullOrWhiteSpace(configuracao?.ItemListaServico))
                    nfseIssues.Add("item da lista de servico");
                if (string.IsNullOrWhiteSpace(configuracao?.CnaePrincipal))
                    nfseIssues.Add("CNAE principal");

                FocusNfseMunicipioValidacaoDto? municipioValidacao = null;
                if (usableNfseCredentials.Count > 0)
                {
                    municipioValidacao = await _focusNfseMunicipioService.ValidarAsync(
                        empresaId,
                        cancellationToken);

                    nfseIssues.AddRange(municipioValidacao.Errors);
                }

                if (nfseIssues.Count > 0)
                {
                    AddItem(
                        "nfse",
                        "NFS-e",
                        "documento",
                        "error",
                        $"Ajuste estes pontos para NFS-e: {string.Join("; ", nfseIssues.Distinct())}.",
                        blocksHomologacao: true,
                        blocksProducao: true);
                    result.NextSteps.Add("Feche as pendencias municipais e cadastrais da NFS-e antes de testar emissao real.");
                }
                else if (municipioValidacao?.Warnings.Count > 0)
                {
                    AddItem(
                        "nfse",
                        "NFS-e",
                        "documento",
                        "warning",
                        $"NFS-e pronta para emitir, mas ainda vale revisar: {string.Join("; ", municipioValidacao.Warnings)}.");
                }
                else
                {
                    AddItem(
                        "nfse",
                        "NFS-e",
                        "documento",
                        "ok",
                        "NFS-e esta pronta para usar com a Focus na empresa atual.");
                }
            }

            var dfeCredentials = providerCredentials
                .Where(x => x.TipoDocumentoFiscal is TipoDocumentoFiscal.Nfe or TipoDocumentoFiscal.Nfce)
                .ToList();
            var usableDfeCredentials = usableProviderCredentials
                .Where(x => x.TipoDocumentoFiscal is TipoDocumentoFiscal.Nfe or TipoDocumentoFiscal.Nfce)
                .ToList();

            if (dfeCredentials.Count == 0)
            {
                AddItem(
                    "dfe",
                    "NF-e/NFC-e",
                    "documento",
                    "info",
                    "Nenhuma credencial de NF-e ou NFC-e esta ativa. Cadastre uma quando a empresa quiser emitir venda ou consumidor final.");
            }
            else
            {
                var dfeIssues = new List<string>();
                if (usableDfeCredentials.Count == 0)
                    dfeIssues.Add("credencial ativa sem token ou segredo valido");
                if (string.IsNullOrWhiteSpace(empresa.InscricaoEstadual))
                    dfeIssues.Add("inscricao estadual da empresa");
                if (string.IsNullOrWhiteSpace(empresa.Logradouro))
                    dfeIssues.Add("logradouro");
                if (string.IsNullOrWhiteSpace(empresa.Numero))
                    dfeIssues.Add("numero");
                if (string.IsNullOrWhiteSpace(empresa.Bairro))
                    dfeIssues.Add("bairro");
                if (string.IsNullOrWhiteSpace(empresa.Cidade))
                    dfeIssues.Add("cidade");
                if (string.IsNullOrWhiteSpace(empresa.Uf))
                    dfeIssues.Add("UF");
                if (string.IsNullOrWhiteSpace(configuracao?.MunicipioCodigo))
                    dfeIssues.Add("codigo IBGE do municipio");

                if (dfeIssues.Count > 0)
                {
                    AddItem(
                        "dfe",
                        "NF-e/NFC-e",
                        "documento",
                        "error",
                        $"Ajuste estes pontos para NF-e/NFC-e: {string.Join(", ", dfeIssues.Distinct())}.",
                        blocksHomologacao: true,
                        blocksProducao: true);
                    result.NextSteps.Add("Complete os dados cadastrais e fiscais da empresa antes de emitir NF-e ou NFC-e.");
                }
                else
                {
                    AddItem(
                        "dfe",
                        "NF-e/NFC-e",
                        "documento",
                        "ok",
                        $"NF-e/NFC-e estao prontas com estas credenciais: {DescribeCredentialTypes(usableDfeCredentials)}.");
                }
            }

            var webhookSetup = await ObterFocusWebhookSetupAsync(
                empresaId,
                requestBaseUrl,
                cancellationToken);

            if (!webhookSetup.Enabled || !webhookSetup.SecretConfigured || !webhookSetup.UrlsReady)
            {
                AddItem(
                    "webhook",
                    "Webhook Focus",
                    "integracao",
                    "warning",
                    "O webhook ainda nao esta totalmente pronto. Isso nao trava a emissao porque existe consulta manual e sincronizacao pendente, mas vale fechar antes de producao.");
                result.NextSteps.Add("Finalize o webhook da Focus para reduzir consulta manual e deixar a sincronizacao automatica.");
            }
            else if (!webhookSetup.BaseUrlLooksPublic)
            {
                AddItem(
                    "webhook",
                    "Webhook Focus",
                    "integracao",
                    "warning",
                    "As URLs do webhook foram montadas, mas a base atual ainda parece local ou privada.");
            }
            else
            {
                AddItem(
                    "webhook",
                    "Webhook Focus",
                    "integracao",
                    "ok",
                    "Webhook da Focus esta configurado localmente e pronto para cadastro remoto.");
            }
        }
        else if (FiscalProviderCodeNormalizer.IsFake(providerCode))
        {
            AddItem(
                "credenciais",
                "Credenciais do provider",
                "provider",
                "warning",
                "Enquanto a empresa estiver no provider fake, use homologacao para validar fluxo e numeraçao. Credenciais reais continuam pendentes para producao.",
                blocksProducao: true);
        }

        result.MissingForHomologacao = result.MissingForHomologacao
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        result.MissingForProducao = result.MissingForProducao
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        result.NextSteps = result.NextSteps
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.OkCount = result.Items.Count(x => string.Equals(x.Status, "ok", StringComparison.OrdinalIgnoreCase));
        result.WarningCount = result.Items.Count(x => string.Equals(x.Status, "warning", StringComparison.OrdinalIgnoreCase));
        result.ErrorCount = result.Items.Count(x => string.Equals(x.Status, "error", StringComparison.OrdinalIgnoreCase));

        result.HomologacaoReady = !result.Items.Any(x => x.BlocksHomologacao);
        result.ProducaoReady = !result.Items.Any(x => x.BlocksProducao);

        if (result.ProducaoReady)
        {
            result.Summary = "Producao pronta para o provider atual da empresa.";
        }
        else if (result.HomologacaoReady)
        {
            result.Summary = focusProviderSelected
                ? "Homologacao liberada. Falta fechar os pontos que ainda seguram a producao real."
                : "Homologacao liberada. Use homologacao enquanto o provider fiscal real e as credenciais finais nao estiverem prontas.";
        }
        else
        {
            result.Summary = "Ainda existem bloqueios para homologacao e producao nesta empresa.";
        }

        if (result.NextSteps.Count == 0)
        {
            result.NextSteps.Add("Revise os itens em aviso para deixar a operacao mais redonda antes de ir para producao.");
        }

        return result;
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

    private static List<string> BuildProductionSaveBlockers(
        Empresa empresa,
        ConfiguracaoFiscal configuracao,
        IReadOnlyCollection<CredencialFiscalEmpresa> activeCredentials)
    {
        var blockers = new List<string>();
        var providerCode = FiscalProviderCodeNormalizer.NormalizeOrNull(configuracao.ProvedorFiscal);

        if (!configuracao.Ativo)
            blockers.Add("ative a configuracao fiscal");

        if (string.IsNullOrWhiteSpace(providerCode) || FiscalProviderCodeNormalizer.IsFake(providerCode))
            blockers.Add("configure um provider fiscal real");

        if (!string.IsNullOrWhiteSpace(providerCode) &&
            !FiscalProviderCodeNormalizer.IsFake(providerCode) &&
            !string.Equals(providerCode, FiscalProviderCodes.FocusNfe, StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add($"o provider '{providerCode}' ainda nao esta integrado");
        }

        if (string.IsNullOrWhiteSpace(empresa.RazaoSocial))
            blockers.Add("preencha a razao social da empresa");
        if (string.IsNullOrWhiteSpace(empresa.Cnpj))
            blockers.Add("preencha o CNPJ da empresa");
        if (string.IsNullOrWhiteSpace(empresa.Cidade))
            blockers.Add("preencha a cidade da empresa");
        if (string.IsNullOrWhiteSpace(empresa.Uf))
            blockers.Add("preencha a UF da empresa");

        if (!string.Equals(providerCode, FiscalProviderCodes.FocusNfe, StringComparison.OrdinalIgnoreCase))
            return blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var providerCredentials = activeCredentials
            .Where(x => string.Equals(
                FiscalProviderCodeNormalizer.NormalizeOrNull(x.Provedor),
                providerCode,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (providerCredentials.Count == 0)
        {
            blockers.Add("cadastre pelo menos uma credencial ativa da Focus");
            return blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        var usableProviderCredentials = providerCredentials
            .Where(HasUsableCredentialForProvider)
            .ToList();

        if (usableProviderCredentials.Count == 0)
        {
            blockers.Add("garanta token ou segredo valido em ao menos uma credencial ativa da Focus");
            return blockers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        var hasNfseCredential = providerCredentials.Any(x => x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse);
        var hasDfeCredential = providerCredentials.Any(
            x => x.TipoDocumentoFiscal is TipoDocumentoFiscal.Nfe or TipoDocumentoFiscal.Nfce);

        if (hasNfseCredential)
        {
            if (string.IsNullOrWhiteSpace(empresa.InscricaoMunicipal))
                blockers.Add("preencha a inscricao municipal para NFS-e");
            if (string.IsNullOrWhiteSpace(configuracao.MunicipioCodigo))
                blockers.Add("configure o codigo IBGE do municipio para NFS-e");
            if (string.IsNullOrWhiteSpace(configuracao.ItemListaServico))
                blockers.Add("configure o item da lista de servico para NFS-e");
            if (string.IsNullOrWhiteSpace(configuracao.CnaePrincipal))
                blockers.Add("configure o CNAE principal para NFS-e");
        }

        if (hasDfeCredential)
        {
            if (string.IsNullOrWhiteSpace(empresa.InscricaoEstadual))
                blockers.Add("preencha a inscricao estadual para NF-e/NFC-e");
            if (string.IsNullOrWhiteSpace(empresa.Logradouro))
                blockers.Add("preencha o logradouro da empresa");
            if (string.IsNullOrWhiteSpace(empresa.Numero))
                blockers.Add("preencha o numero do endereco da empresa");
            if (string.IsNullOrWhiteSpace(empresa.Bairro))
                blockers.Add("preencha o bairro da empresa");
            if (string.IsNullOrWhiteSpace(empresa.Cidade))
                blockers.Add("preencha a cidade da empresa");
            if (string.IsNullOrWhiteSpace(empresa.Uf))
                blockers.Add("preencha a UF da empresa");
            if (string.IsNullOrWhiteSpace(configuracao.MunicipioCodigo))
                blockers.Add("configure o codigo IBGE do municipio para NF-e/NFC-e");
        }

        return blockers
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasUsableCredentialForProvider(CredencialFiscalEmpresa credential)
        => HasAnyCredentialSecret(credential) && !IsTokenExpired(credential);

    private static bool HasAnyCredentialSecret(CredencialFiscalEmpresa credential)
    {
        return !string.IsNullOrWhiteSpace(credential.TokenAcesso)
            || !string.IsNullOrWhiteSpace(credential.ClientSecretEncrypted)
            || !string.IsNullOrWhiteSpace(credential.UsuarioApi);
    }

    private static bool IsTokenExpired(CredencialFiscalEmpresa credential)
    {
        if (string.IsNullOrWhiteSpace(credential.TokenAcesso) || !credential.TokenExpiraEm.HasValue)
            return false;

        return credential.TokenExpiraEm.Value <= DateTime.UtcNow;
    }

    private static bool IsTokenExpiringSoon(CredencialFiscalEmpresa credential)
    {
        if (string.IsNullOrWhiteSpace(credential.TokenAcesso) || !credential.TokenExpiraEm.HasValue)
            return false;

        var now = DateTime.UtcNow;
        return credential.TokenExpiraEm.Value > now
            && credential.TokenExpiraEm.Value <= now.AddDays(7);
    }

    private static string DescribeCredentialTypes(IEnumerable<CredencialFiscalEmpresa> credentials)
    {
        var labels = credentials
            .Select(x => ToDocumentLabel(x.TipoDocumentoFiscal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return labels.Count == 0 ? "nenhuma" : string.Join(", ", labels);
    }

    private static string ToDocumentLabel(TipoDocumentoFiscal tipoDocumentoFiscal)
    {
        return tipoDocumentoFiscal switch
        {
            TipoDocumentoFiscal.Nfse => "NFS-e",
            TipoDocumentoFiscal.Nfe => "NF-e",
            TipoDocumentoFiscal.Nfce => "NFC-e",
            _ => tipoDocumentoFiscal.ToString()
        };
    }
}
