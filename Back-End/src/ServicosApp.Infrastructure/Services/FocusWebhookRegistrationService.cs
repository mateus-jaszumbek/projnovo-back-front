using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class FocusWebhookRegistrationService : IFocusWebhookRegistrationService
{
    private const string HomologacaoBaseUrl = "https://homologacao.focusnfe.com.br";
    private const string ProducaoBaseUrl = "https://api.focusnfe.com.br";

    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IConfiguracaoFiscalService _configuracaoFiscalService;
    private readonly IFiscalCredentialSecretProtector _secretProtector;
    private readonly ILogger<FocusWebhookRegistrationService> _logger;

    public FocusWebhookRegistrationService(
        HttpClient httpClient,
        AppDbContext context,
        IConfiguracaoFiscalService configuracaoFiscalService,
        IFiscalCredentialSecretProtector secretProtector,
        ILogger<FocusWebhookRegistrationService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _configuracaoFiscalService = configuracaoFiscalService;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    public Task<FocusWebhookSetupDto> ObterStatusAsync(
        Guid empresaId,
        string? requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        return BuildStatusAsync(empresaId, requestBaseUrl, syncMissingHooks: false, cancellationToken);
    }

    public Task<FocusWebhookSetupDto> SincronizarAsync(
        Guid empresaId,
        string? requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        return BuildStatusAsync(empresaId, requestBaseUrl, syncMissingHooks: true, cancellationToken);
    }

    private async Task<FocusWebhookSetupDto> BuildStatusAsync(
        Guid empresaId,
        string? requestBaseUrl,
        bool syncMissingHooks,
        CancellationToken cancellationToken)
    {
        var result = await _configuracaoFiscalService.ObterFocusWebhookSetupAsync(
            empresaId,
            requestBaseUrl,
            cancellationToken);

        result.DfeRemoteStatus ??= new FocusWebhookRemoteStatusDto
        {
            Event = "nfe"
        };
        result.NfseRemoteStatus ??= new FocusWebhookRemoteStatusDto
        {
            Event = "nfse"
        };
        result.ActionsTaken ??= [];
        result.CanRegisterRemotely = CanRegisterRemotely(result);
        result.CheckedRemotely = false;
        result.SyncedRemotely = false;

        if (!result.CanRegisterRemotely)
        {
            AddNextStep(
                result,
                "Quando a URL estiver pronta e a credencial ativa da Focus existir, use a verificação remota para validar o cadastro na Focus.");
            return result;
        }

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId && x.Ativo, cancellationToken);

        if (empresa is null)
        {
            AddWarning(result, "Empresa ativa não encontrada para validar o webhook na Focus.");
            return result;
        }

        var cnpjCpf = DigitsOnlyOrNull(empresa.Cnpj);
        if (string.IsNullOrWhiteSpace(cnpjCpf) || (cnpjCpf.Length != 11 && cnpjCpf.Length != 14))
        {
            AddWarning(result, "A empresa precisa ter CNPJ ou CPF válido para cadastrar webhook na Focus.");
            return result;
        }

        var configuracaoFiscal = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        if (configuracaoFiscal is null)
        {
            AddWarning(result, "Salve a configuração fiscal da empresa antes de sincronizar webhook na Focus.");
            return result;
        }

        var credenciais = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .ToListAsync(cancellationToken);

        var focusCredenciais = credenciais
            .Where(x => string.Equals(
                FiscalProviderCodeNormalizer.NormalizeOrNull(x.Provedor),
                FiscalProviderCodes.FocusNfe,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        var dfeCredencial = focusCredenciais
            .FirstOrDefault(x => x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfe)
            ?? focusCredenciais.FirstOrDefault(x => x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfce);

        var nfseCredencial = focusCredenciais
            .FirstOrDefault(x => x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse);

        await PopulateRemoteStatusAsync(
            result,
            empresa,
            configuracaoFiscal,
            result.DfeRemoteStatus,
            result.DfeWebhookUrl,
            "nfe",
            "DF-e",
            dfeCredencial,
            syncMissingHooks,
            cnpjCpf,
            cancellationToken);

        await PopulateRemoteStatusAsync(
            result,
            empresa,
            configuracaoFiscal,
            result.NfseRemoteStatus,
            result.NfseWebhookUrl,
            "nfse",
            "NFS-e",
            nfseCredencial,
            syncMissingHooks,
            cnpjCpf,
            cancellationToken);

        result.CheckedRemotely = result.DfeRemoteStatus.CheckedRemotely || result.NfseRemoteStatus.CheckedRemotely;
        result.SyncedRemotely = syncMissingHooks;

        if (result.DfeRemoteStatus.Registered && result.NfseRemoteStatus.Registered)
        {
            AddNextStep(result, "Os dois webhooks da Focus já estão cadastrados para esta empresa.");
            AddNextStep(result, "Emita uma nota em homologação e confirme se o status atualiza sozinho sem consulta manual.");
        }
        else if (syncMissingHooks)
        {
            AddNextStep(result, "Revise os avisos restantes antes de seguir para produção fiscal.");
        }

        return result;
    }

    private async Task PopulateRemoteStatusAsync(
        FocusWebhookSetupDto result,
        Empresa empresa,
        ConfiguracaoFiscal configuracaoFiscal,
        FocusWebhookRemoteStatusDto remoteStatus,
        string? desiredUrl,
        string eventName,
        string scopeLabel,
        CredencialFiscalEmpresa? credencial,
        bool syncMissingHooks,
        string cnpjCpf,
        CancellationToken cancellationToken)
    {
        remoteStatus.Event = eventName;

        if (credencial is null)
        {
            AddWarning(result, $"Cadastre uma credencial ativa da Focus para {scopeLabel} antes de verificar o webhook remoto.");
            return;
        }

        var credencialUso = _secretProtector.CloneForUse(credencial);
        remoteStatus.CredentialTipoDocumento = credencialUso.TipoDocumentoFiscal.ToString();
        remoteStatus.CredentialConfigured = true;

        if (string.IsNullOrWhiteSpace(desiredUrl))
        {
            AddWarning(result, $"A URL de {scopeLabel} ainda não está pronta para cadastro na Focus.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ResolveApiToken(credencialUso)))
        {
            AddWarning(result, $"A credencial de {scopeLabel} da Focus está sem token de acesso configurado.");
            return;
        }

        try
        {
            var hooks = await ListHooksAsync(configuracaoFiscal, credencialUso, cancellationToken);
            remoteStatus.CheckedRemotely = true;

            var exactMatch = hooks.FirstOrDefault(x =>
                EventMatches(x.Event, eventName) &&
                UrlMatches(x.Url, desiredUrl));

            if (exactMatch is not null)
            {
                remoteStatus.Registered = true;
                remoteStatus.HookId = exactMatch.Id;
                remoteStatus.RemoteUrl = exactMatch.Url;
                return;
            }

            var conflictingHook = hooks.FirstOrDefault(x =>
                EventMatches(x.Event, eventName));

            if (conflictingHook is not null)
            {
                remoteStatus.HookId = conflictingHook.Id;
                remoteStatus.RemoteUrl = conflictingHook.Url;
                AddWarning(
                    result,
                    $"A Focus já possui um webhook de {scopeLabel} apontando para outra URL. Revise esse hook antes de cadastrar um novo automaticamente.");
                return;
            }

            if (!syncMissingHooks)
                return;

            var createdHook = await CreateHookAsync(
                empresa,
                configuracaoFiscal,
                credencialUso,
                eventName,
                desiredUrl,
                cnpjCpf,
                cancellationToken);

            remoteStatus.Registered = true;
            remoteStatus.HookId = createdHook.Id;
            remoteStatus.RemoteUrl = createdHook.Url ?? desiredUrl;
            result.ActionsTaken.Add($"Webhook de {scopeLabel} cadastrado na Focus.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao sincronizar webhook da Focus para empresa {EmpresaId} no escopo {ScopeLabel}.",
                empresa.Id,
                scopeLabel);

            AddWarning(result, $"Não foi possível validar o webhook remoto de {scopeLabel} agora.");
        }
    }

    private async Task<List<FocusRemoteHook>> ListHooksAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        CancellationToken cancellationToken)
    {
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        using var request = BuildRequest(
            HttpMethod.Get,
            new Uri(baseUri, "/v2/hooks"),
            credencial);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();
        return ParseHooks(responseJson);
    }

    private async Task<FocusRemoteHook> CreateHookAsync(
        Empresa empresa,
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        string eventName,
        string url,
        string cnpjCpf,
        CancellationToken cancellationToken)
    {
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        var payload = new JsonObject
        {
            ["event"] = eventName,
            ["url"] = url
        };

        if (cnpjCpf.Length == 11)
            payload["cpf"] = cnpjCpf;
        else
            payload["cnpj"] = cnpjCpf;

        var requestJson = payload.ToJsonString();

        using var request = BuildRequest(
            HttpMethod.Post,
            new Uri(baseUri, "/v2/hooks"),
            credencial,
            requestJson);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        var createdHook = ParseHooks(responseJson).FirstOrDefault();
        return createdHook ?? new FocusRemoteHook(
            Id: null,
            Event: eventName,
            Url: url);
    }

    private static HttpRequestMessage BuildRequest(
        HttpMethod method,
        Uri uri,
        CredencialFiscalEmpresa credencial,
        string? jsonContent = null)
    {
        var token = ResolveApiToken(credencial);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Credencial fiscal da Focus sem token de acesso configurado para webhook.");
        }

        var request = new HttpRequestMessage(method, uri);
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{token}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (jsonContent is not null)
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        return request;
    }

    private static Uri ResolveBaseUri(ConfiguracaoFiscal configuracaoFiscal, CredencialFiscalEmpresa credencial)
    {
        var customBaseUrl = credencial.UrlBase?.Trim();
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
            return new Uri(customBaseUrl.EndsWith("/") ? customBaseUrl : $"{customBaseUrl}/", UriKind.Absolute);

        var defaultBaseUrl = configuracaoFiscal.Ambiente == AmbienteFiscal.Producao
            ? ProducaoBaseUrl
            : HomologacaoBaseUrl;

        return new Uri($"{defaultBaseUrl}/", UriKind.Absolute);
    }

    private static string? ResolveApiToken(CredencialFiscalEmpresa credencial)
    {
        return credencial.TokenAcesso?.Trim()
            ?? credencial.ClientSecretEncrypted?.Trim()
            ?? credencial.UsuarioApi?.Trim();
    }

    private static List<FocusRemoteHook> ParseHooks(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
            return [];

        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var hooks = new List<FocusRemoteHook>();
            CollectHooks(document.RootElement, hooks);
            return hooks;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void CollectHooks(JsonElement element, List<FocusRemoteHook> hooks)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                AddHook(item, hooks);

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        if (LooksLikeHook(element))
            AddHook(element, hooks);

        foreach (var collectionName in new[] { "hooks", "items", "data", "results", "gatilhos" })
        {
            if (!TryGetProperty(element, collectionName, out var nested) ||
                nested.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in nested.EnumerateArray())
                AddHook(item, hooks);

            return;
        }
    }

    private static void AddHook(JsonElement element, List<FocusRemoteHook> hooks)
    {
        var hook = new FocusRemoteHook(
            GetString(element, "id", "hook_id"),
            GetString(element, "event", "evento"),
            GetString(element, "url", "callback"));

        if (string.IsNullOrWhiteSpace(hook.Id) &&
            string.IsNullOrWhiteSpace(hook.Event) &&
            string.IsNullOrWhiteSpace(hook.Url))
        {
            return;
        }

        hooks.Add(hook);
    }

    private static bool LooksLikeHook(JsonElement element)
    {
        return !string.IsNullOrWhiteSpace(GetString(element, "event", "evento")) ||
               !string.IsNullOrWhiteSpace(GetString(element, "url", "callback")) ||
               !string.IsNullOrWhiteSpace(GetString(element, "id", "hook_id"));
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

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, propertyName, out var property) ||
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

    private static bool EventMatches(string? remoteEvent, string expectedEvent)
    {
        return string.Equals(
            remoteEvent?.Trim(),
            expectedEvent,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool UrlMatches(string? remoteUrl, string expectedUrl)
    {
        return string.Equals(
            NormalizeUrl(remoteUrl),
            NormalizeUrl(expectedUrl),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().TrimEnd('/');
    }

    private static string? DigitsOnlyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static bool CanRegisterRemotely(FocusWebhookSetupDto setup)
    {
        return setup.FocusProviderSelected &&
               setup.Enabled &&
               setup.SecretConfigured &&
               setup.UrlsReady &&
               setup.BaseUrlLooksPublic;
    }

    private static void AddWarning(FocusWebhookSetupDto result, string warning)
    {
        if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
            result.Warnings.Add(warning);
    }

    private static void AddNextStep(FocusWebhookSetupDto result, string nextStep)
    {
        if (!result.NextSteps.Contains(nextStep, StringComparer.OrdinalIgnoreCase))
            result.NextSteps.Add(nextStep);
    }

    private sealed record FocusRemoteHook(
        string? Id,
        string? Event,
        string? Url);
}
