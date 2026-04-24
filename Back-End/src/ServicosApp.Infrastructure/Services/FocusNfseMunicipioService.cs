using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class FocusNfseMunicipioService : IFocusNfseMunicipioService
{
    private readonly AppDbContext _context;
    private readonly IFiscalCredentialSecretProtector _secretProtector;
    private readonly HttpClient _httpClient;

    public FocusNfseMunicipioService(
        AppDbContext context,
        IFiscalCredentialSecretProtector secretProtector,
        HttpClient httpClient)
    {
        _context = context;
        _secretProtector = secretProtector;
        _httpClient = httpClient;
    }

    public async Task<FocusNfseMunicipioValidacaoDto> ValidarAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (config is null)
        {
            return new FocusNfseMunicipioValidacaoDto
            {
                ProviderCode = string.Empty,
                PodeEmitirNfse = false,
                Errors = ["Configuração fiscal não encontrada."]
            };
        }

        var credencialEntity = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        var credencial = credencialEntity is null
            ? null
            : _secretProtector.CloneForUse(credencialEntity);

        var providerCode = FiscalProviderCodeNormalizer.NormalizeOrNull(
            credencial?.Provedor ?? config.ProvedorFiscal) ?? string.Empty;

        var result = new FocusNfseMunicipioValidacaoDto
        {
            ProviderCode = providerCode,
            MunicipioCodigo = config.MunicipioCodigo,
            RemoteValidationAvailable = false,
            PodeEmitirNfse = false,
            ItemListaServicoConfigurado = !string.IsNullOrWhiteSpace(config.ItemListaServico),
            CnaePrincipalConfigurado = !string.IsNullOrWhiteSpace(config.CnaePrincipal),
            CodigoTributarioMunicipioConfigurado = !string.IsNullOrWhiteSpace(config.CodigoTributarioMunicipio)
        };

        AddLocalValidation(config, result);

        if (!string.Equals(providerCode, FiscalProviderCodes.FocusNfe, StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add("A validação remota desta etapa está disponível apenas para o provider focusnfe.");
            result.PodeEmitirNfse = result.Errors.Count == 0;
            return result;
        }

        var token = credencial?.TokenAcesso?.Trim()
            ?? credencial?.ClientSecretEncrypted?.Trim()
            ?? credencial?.UsuarioApi?.Trim();

        if (string.IsNullOrWhiteSpace(config.MunicipioCodigo))
        {
            result.PodeEmitirNfse = false;
            return result;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            result.Warnings.Add("Credencial Focus sem token de acesso. A validação remota do município não pôde ser executada.");
            result.PodeEmitirNfse = result.Errors.Count == 0;
            return result;
        }

        var baseUri = ResolveBaseUri(config, credencial);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(baseUri, $"/v2/municipios/{Uri.EscapeDataString(config.MunicipioCodigo.Trim())}"));

        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{token}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                result.Warnings.Add($"A Focus retornou {(int)response.StatusCode} ao consultar o município fiscal.");
                result.PodeEmitirNfse = result.Errors.Count == 0;
                return result;
            }

            ApplyRemoteValidation(responseJson, result);
        }
        catch (HttpRequestException)
        {
            result.Warnings.Add("Não foi possível consultar a Focus para validar o município agora.");
        }
        catch (TaskCanceledException)
        {
            result.Warnings.Add("A consulta ao município fiscal expirou antes de concluir.");
        }

        result.PodeEmitirNfse = result.Errors.Count == 0 &&
                                (!result.RemoteValidationAvailable ||
                                 string.Equals(result.StatusNfse, "ativo", StringComparison.OrdinalIgnoreCase));

        return result;
    }

    private static void AddLocalValidation(
        ConfiguracaoFiscal config,
        FocusNfseMunicipioValidacaoDto result)
    {
        if (string.IsNullOrWhiteSpace(config.MunicipioCodigo))
            result.Errors.Add("Configure o código IBGE do município para a NFS-e.");

        if (string.IsNullOrWhiteSpace(config.ItemListaServico))
            result.Errors.Add("Configure o item da lista de serviço antes de emitir NFS-e.");

        if (string.IsNullOrWhiteSpace(config.CnaePrincipal))
            result.Errors.Add("Configure o CNAE principal antes de emitir NFS-e.");
    }

    private static void ApplyRemoteValidation(
        string responseJson,
        FocusNfseMunicipioValidacaoDto result)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        result.RemoteValidationAvailable = true;
        result.MunicipioCodigo ??= GetString(root, "codigo_municipio") ?? GetString(root, "codigo");
        result.MunicipioNome = GetString(root, "nome_municipio") ?? GetString(root, "nome");
        result.Uf = GetString(root, "sigla_uf") ?? GetString(root, "uf");
        result.StatusNfse = GetString(root, "status_nfse");
        result.CodigoTributarioMunicipioObrigatorio = GetBool(
            root,
            "codigo_tributario_municipio_obrigatorio_nfse");

        if (result.CodigoTributarioMunicipioObrigatorio == true &&
            !result.CodigoTributarioMunicipioConfigurado)
        {
            result.Errors.Add("Este município exige código tributário municipal para a NFS-e.");
        }

        if (!string.IsNullOrWhiteSpace(result.StatusNfse) &&
            !string.Equals(result.StatusNfse, "ativo", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(
                $"O município está com status NFSe '{result.StatusNfse}' na Focus e não está pronto para emissão.");
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            _ => property.GetRawText()
        };
    }

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static Uri ResolveBaseUri(ConfiguracaoFiscal configuracaoFiscal, CredencialFiscalEmpresa? credencial)
    {
        var customBaseUrl = credencial?.UrlBase?.Trim();
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
            return new Uri(customBaseUrl.EndsWith("/") ? customBaseUrl : $"{customBaseUrl}/", UriKind.Absolute);

        var defaultBaseUrl = configuracaoFiscal.Ambiente == AmbienteFiscal.Producao
            ? "https://api.focusnfe.com.br"
            : "https://homologacao.focusnfe.com.br";

        return new Uri($"{defaultBaseUrl}/", UriKind.Absolute);
    }
}
