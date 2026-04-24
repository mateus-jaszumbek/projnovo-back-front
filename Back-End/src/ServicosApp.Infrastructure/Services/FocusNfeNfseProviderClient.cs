using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Infrastructure.Data;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Application.Interfaces;

namespace ServicosApp.Infrastructure.Services;

public class FocusNfeNfseProviderClient : INfseProviderClient
{
    private const string HomologacaoBaseUrl = "https://homologacao.focusnfe.com.br";
    private const string ProducaoBaseUrl = "https://api.focusnfe.com.br";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IFocusNfseMunicipioService _municipioService;

    public FocusNfeNfseProviderClient(
        HttpClient httpClient,
        AppDbContext context,
        IFocusNfseMunicipioService municipioService)
    {
        _httpClient = httpClient;
        _context = context;
        _municipioService = municipioService;
    }

    public string ProviderCode => FiscalProviderCodes.FocusNfe;

    public async Task<NfseProviderResult> EmitirAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var validacaoMunicipio = await _municipioService.ValidarAsync(documento.EmpresaId, cancellationToken);
        if (validacaoMunicipio.Errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", validacaoMunicipio.Errors));

        var empresa = await ObterEmpresaAsync(documento.EmpresaId, cancellationToken);
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        var reference = ResolveReference(documento);
        var payload = BuildEmitPayload(configuracaoFiscal, empresa, documento);
        var requestJson = payload.ToJsonString(JsonOptions);

        using var request = BuildRequest(
            HttpMethod.Post,
            new Uri(baseUri, $"/v2/nfse?ref={Uri.EscapeDataString(reference)}"),
            credencial,
            requestJson);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = ParseResponse(response.StatusCode, responseJson, baseUri, reference);
        result.RequestPayload = requestJson;
        result.ResponsePayload = responseJson;
        return result;
    }

    public async Task<NfseProviderResult> ConsultarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        var reference = ResolveReference(documento);

        using var request = BuildRequest(
            HttpMethod.Get,
            new Uri(baseUri, $"/v2/nfse/{Uri.EscapeDataString(reference)}"),
            credencial);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = ParseResponse(response.StatusCode, responseJson, baseUri, reference);
        result.RequestPayload = $$"""{"ref":"{{reference}}"}""";
        result.ResponsePayload = responseJson;
        return result;
    }

    public async Task<NfseProviderResult> CancelarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        var reference = ResolveReference(documento);
        var payload = new JsonObject
        {
            ["justificativa"] = motivo.Trim()
        };
        var requestJson = payload.ToJsonString(JsonOptions);

        using var request = BuildRequest(
            HttpMethod.Delete,
            new Uri(baseUri, $"/v2/nfse/{Uri.EscapeDataString(reference)}"),
            credencial,
            requestJson);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = ParseResponse(response.StatusCode, responseJson, baseUri, reference);
        result.RequestPayload = requestJson;
        result.ResponsePayload = responseJson;
        return result;
    }

    public async Task<NfseProviderResult> SolicitarReenvioWebhookAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        var reference = ResolveReference(documento);

        using var request = BuildRequest(
            HttpMethod.Post,
            new Uri(baseUri, $"/v2/nfse/{Uri.EscapeDataString(reference)}/hook"),
            credencial);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseReplayWebhookResponse(response.StatusCode, responseJson, reference);
    }

    private async Task<Empresa> ObterEmpresaAsync(Guid empresaId, CancellationToken cancellationToken)
    {
        return await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId && x.Ativo, cancellationToken)
            ?? throw new InvalidOperationException("Empresa emitente não encontrada.");
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

    private static HttpRequestMessage BuildRequest(
        HttpMethod method,
        Uri uri,
        CredencialFiscalEmpresa credencial,
        string? jsonContent = null)
    {
        var token = ResolveApiToken(credencial);
        var request = new HttpRequestMessage(method, uri);
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{token}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (jsonContent is not null)
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        return request;
    }

    private static string ResolveApiToken(CredencialFiscalEmpresa credencial)
    {
        return credencial.TokenAcesso?.Trim()
            ?? credencial.ClientSecretEncrypted?.Trim()
            ?? credencial.UsuarioApi?.Trim()
            ?? throw new InvalidOperationException(
                "Credencial fiscal da Focus NFe sem token de acesso configurado.");
    }

    private static string ResolveReference(DocumentoFiscal documento)
    {
        return string.IsNullOrWhiteSpace(documento.NumeroExterno)
            ? documento.Id.ToString("N")
            : documento.NumeroExterno.Trim();
    }

    private static JsonObject BuildEmitPayload(
        ConfiguracaoFiscal configuracaoFiscal,
        Empresa empresa,
        DocumentoFiscal documento)
    {
        var municipioPrestador = RequireValue(
            configuracaoFiscal.MunicipioCodigo,
            "Configure o código IBGE do município antes de emitir NFS-e via Focus NFe.");

        var inscricaoMunicipal = RequireValue(
            empresa.InscricaoMunicipal,
            "A empresa precisa ter inscrição municipal para emitir NFS-e via Focus NFe.");

        var itemListaServico = RequireValue(
            configuracaoFiscal.ItemListaServico,
            "Configure o item da lista de serviço antes de emitir NFS-e via Focus NFe.");

        var cnaePrincipal = RequireValue(
            configuracaoFiscal.CnaePrincipal,
            "Configure o CNAE principal antes de emitir NFS-e via Focus NFe.");

        var aliquotaIss = documento.Itens
            .Select(x => x.AliquotaIss)
            .FirstOrDefault(x => x > 0);

        if (aliquotaIss <= 0)
            aliquotaIss = configuracaoFiscal.AliquotaIssPadrao ?? 0m;

        var valorIss = documento.Itens.Sum(x => x.ValorIss);
        var issRetido = documento.Itens.Any(x => x.IssRetido) || configuracaoFiscal.IssRetidoPadrao;
        var valorIssRetido = issRetido ? valorIss : 0m;
        var discriminacao = BuildDiscriminacao(documento, configuracaoFiscal.Ambiente);

        var root = new JsonObject
        {
            ["data_emissao"] = FormatFocusDate(documento.DataEmissao),
            ["natureza_operacao"] = ResolveNaturezaOperacao(configuracaoFiscal.NaturezaOperacaoPadrao),
            ["optante_simples_nacional"] = IsOptanteSimples(configuracaoFiscal.RegimeTributario, empresa.RegimeTributario),
            ["incentivador_cultural"] = false
        };

        root["prestador"] = new JsonObject
        {
            ["cnpj"] = DigitsOnly(empresa.Cnpj),
            ["codigo_municipio"] = DigitsOnly(municipioPrestador),
            ["inscricao_municipal"] = DigitsOnly(inscricaoMunicipal)
        };

        root["servico"] = new JsonObject
        {
            ["valor_servicos"] = documento.ValorServicos,
            ["valor_deducoes"] = 0m,
            ["iss_retido"] = issRetido,
            ["valor_iss"] = valorIss,
            ["valor_iss_retido"] = valorIssRetido,
            ["base_calculo"] = documento.ValorServicos,
            ["aliquota"] = aliquotaIss,
            ["desconto_incondicionado"] = documento.Desconto,
            ["desconto_condicionado"] = 0m,
            ["item_lista_servico"] = itemListaServico,
            ["codigo_cnae"] = DigitsOnly(cnaePrincipal),
            ["discriminacao"] = discriminacao,
            ["codigo_municipio"] = DigitsOnly(municipioPrestador),
            ["valor_liquido"] = documento.ValorTotal > valorIssRetido
                ? documento.ValorTotal - valorIssRetido
                : 0m
        };

        if (!string.IsNullOrWhiteSpace(configuracaoFiscal.CodigoTributarioMunicipio))
        {
            root["servico"]!["codigo_tributario_municipio"] =
                configuracaoFiscal.CodigoTributarioMunicipio.Trim();
        }

        var tomador = BuildTomador(documento);
        if (tomador is not null)
            root["tomador"] = tomador;

        return root;
    }

    private static JsonObject? BuildTomador(DocumentoFiscal documento)
    {
        var tomador = new JsonObject();
        var documentoTomador = DigitsOnlyOrNull(documento.ClienteCpfCnpj);

        if (!string.IsNullOrWhiteSpace(documentoTomador))
        {
            if (documentoTomador.Length <= 11)
                tomador["cpf"] = documentoTomador;
            else
                tomador["cnpj"] = documentoTomador;
        }

        AddIfNotEmpty(tomador, "razao_social", documento.ClienteNome);
        AddIfNotEmpty(tomador, "telefone", DigitsOnlyOrNull(documento.ClienteTelefone));
        AddIfNotEmpty(tomador, "email", documento.ClienteEmail);

        var endereco = new JsonObject();
        AddIfNotEmpty(endereco, "logradouro", documento.ClienteLogradouro);
        AddIfNotEmpty(endereco, "tipo_logradouro", BuildTipoLogradouro(documento.ClienteLogradouro));
        AddIfNotEmpty(endereco, "numero", documento.ClienteNumero);
        AddIfNotEmpty(endereco, "complemento", documento.ClienteComplemento);
        AddIfNotEmpty(endereco, "bairro", documento.ClienteBairro);
        AddIfNotEmpty(endereco, "codigo_municipio", DigitsOnlyOrNull(documento.ClienteMunicipioCodigo));
        AddIfNotEmpty(endereco, "uf", NormalizeUf(documento.ClienteUf));
        AddIfNotEmpty(endereco, "cep", DigitsOnlyOrNull(documento.ClienteCep));

        if (endereco.Count > 0)
            tomador["endereco"] = endereco;

        return tomador.Count == 0 ? null : tomador;
    }

    private static NfseProviderResult ParseResponse(
        System.Net.HttpStatusCode statusCode,
        string responseJson,
        Uri baseUri,
        string reference)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return new NfseProviderResult
            {
                Sucesso = false,
                Status = statusCode.ToString(),
                NumeroExterno = reference,
                CodigoErro = ((int)statusCode).ToString(),
                MensagemErro = "O provedor retornou uma resposta vazia."
            };
        }

        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            var providerStatus = GetString(root, "status");
            var normalizedStatus = NormalizeStatus(providerStatus);
            var success = IsSuccessfulProviderResponse(statusCode, normalizedStatus);

            return new NfseProviderResult
            {
                Sucesso = success,
                Status = string.IsNullOrWhiteSpace(normalizedStatus)
                    ? statusCode.ToString().ToUpperInvariant()
                    : normalizedStatus,
                NumeroExterno = reference,
                ChaveAcesso = GetString(root, "chave_nfe"),
                Protocolo = GetString(root, "protocolo"),
                CodigoVerificacao = GetString(root, "codigo_verificacao"),
                LinkConsulta = NormalizeRemoteUrl(baseUri, GetString(root, "url")),
                Lote = GetString(root, "lote"),
                XmlConteudo = null,
                XmlUrl = NormalizeRemoteUrl(
                    baseUri,
                    GetString(root, "url_xml") ?? GetString(root, "caminho_xml_nota_fiscal")),
                PdfUrl = NormalizeRemoteUrl(
                    baseUri,
                    GetString(root, "url_danfse") ?? GetString(root, "caminho_danfse")),
                CodigoErro = success ? null : GetString(root, "codigo") ?? ((int)statusCode).ToString(),
                MensagemErro = success
                    ? null
                    : GetErrorMessage(root, providerStatus, statusCode)
            };
        }
        catch (JsonException)
        {
            return new NfseProviderResult
            {
                Sucesso = false,
                Status = statusCode.ToString(),
                NumeroExterno = reference,
                CodigoErro = ((int)statusCode).ToString(),
                MensagemErro = responseJson
            };
        }
    }

    private static bool IsSuccessfulProviderResponse(System.Net.HttpStatusCode statusCode, string? normalizedStatus)
    {
        if ((int)statusCode >= 400)
            return false;

        if (string.IsNullOrWhiteSpace(normalizedStatus))
            return true;

        return normalizedStatus is "AUTORIZADO" or "CANCELADO" or "PROCESSANDO_AUTORIZACAO" or "EM_PROCESSAMENTO";
    }

    private static string GetErrorMessage(
        JsonElement root,
        string? providerStatus,
        System.Net.HttpStatusCode statusCode)
    {
        var errors = GetArrayMessages(root, "erros");
        if (!string.IsNullOrWhiteSpace(errors))
            return errors;

        return GetString(root, "mensagem")
            ?? GetString(root, "mensagem_sefaz")
            ?? providerStatus
            ?? $"Falha ao comunicar com a Focus NFe ({(int)statusCode}).";
    }

    private static string? GetArrayMessages(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var errorsElement) ||
            errorsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var messages = new List<string>();
        foreach (var item in errorsElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    messages.Add(value.Trim());
                continue;
            }

            var mensagem = GetString(item, "mensagem") ?? GetString(item, "erro");
            if (!string.IsNullOrWhiteSpace(mensagem))
                messages.Add(mensagem.Trim());
        }

        return messages.Count == 0 ? null : string.Join(" | ", messages.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static NfseProviderResult ParseReplayWebhookResponse(
        System.Net.HttpStatusCode statusCode,
        string responseJson,
        string reference)
    {
        if ((int)statusCode < 400)
        {
            return new NfseProviderResult
            {
                Sucesso = true,
                Status = "REENVIO_SOLICITADO",
                NumeroExterno = reference,
                RequestPayload = $$"""{"ref":"{{reference}}"}""",
                ResponsePayload = responseJson
            };
        }

        var mensagemErro = responseJson;
        if (!string.IsNullOrWhiteSpace(responseJson))
        {
            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;
                mensagemErro = GetString(root, "mensagem")
                    ?? GetString(root, "message")
                    ?? GetArrayMessages(root, "erros")
                    ?? responseJson;
            }
            catch (JsonException)
            {
                // Mantém payload bruto como mensagem.
            }
        }

        return new NfseProviderResult
        {
            Sucesso = false,
            Status = statusCode.ToString().ToUpperInvariant(),
            NumeroExterno = reference,
            CodigoErro = ((int)statusCode).ToString(),
            MensagemErro = string.IsNullOrWhiteSpace(mensagemErro)
                ? "Falha ao solicitar reenvio do webhook na Focus."
                : mensagemErro,
            RequestPayload = $$"""{"ref":"{{reference}}"}""",
            ResponsePayload = responseJson
        };
    }

    private static string? NormalizeRemoteUrl(Uri baseUri, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        return Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : new Uri(baseUri, url.TrimStart('/')).ToString();
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
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => property.GetRawText()
        };
    }

    private static string NormalizeStatus(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
            return string.Empty;

        return providerStatus
            .Trim()
            .Replace('-', '_')
            .Replace(' ', '_')
            .ToUpperInvariant();
    }

    private static string BuildDiscriminacao(DocumentoFiscal documento, AmbienteFiscal ambiente)
    {
        var itens = documento.Itens
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Descricao)
            .Select(x => $"{x.Descricao} - Qtd {x.Quantidade:0.##} - Valor {x.ValorTotal:0.00}")
            .ToList();

        if (itens.Count == 0)
            itens.Add($"Serviços prestados - Documento {documento.Numero}/{documento.Serie}");

        if (ambiente == AmbienteFiscal.Homologacao)
            itens.Add("NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL");

        return string.Join(" | ", itens);
    }

    private static string FormatFocusDate(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        return new DateTimeOffset(utc).ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    private static string ResolveNaturezaOperacao(string? naturezaOperacaoPadrao)
    {
        if (string.IsNullOrWhiteSpace(naturezaOperacaoPadrao))
            return "1";

        var normalized = naturezaOperacaoPadrao.Trim();
        return normalized switch
        {
            "TributacaoNoMunicipio" => "1",
            "TributacaoForaDoMunicipio" => "2",
            "Isencao" => "3",
            "Imune" => "4",
            "ExigibilidadeSuspensaJudicial" => "5",
            "ExigibilidadeSuspensaAdministrativa" => "6",
            _ => normalized
        };
    }

    private static bool IsOptanteSimples(string? regimeConfigurado, string? regimeEmpresa)
    {
        var normalized = (regimeConfigurado ?? regimeEmpresa ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        return normalized is "simplesnacional" or "mei";
    }

    private static void AddIfNotEmpty(JsonObject json, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            json[key] = value.Trim();
    }

    private static string? BuildTipoLogradouro(string? logradouro)
    {
        if (string.IsNullOrWhiteSpace(logradouro))
            return null;

        var normalized = logradouro.Trim();
        return normalized.Length <= 3 ? normalized : normalized[..3];
    }

    private static string? NormalizeUf(string? uf)
        => string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();

    private static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());

    private static string? DigitsOnlyOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : DigitsOnly(value.Trim());

    private static string RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(message);

        return value.Trim();
    }
}
