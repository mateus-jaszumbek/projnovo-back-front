using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class FocusNfeDfeProviderClient : IDfeProviderClient
{
    private const string HomologacaoBaseUrl = "https://homologacao.focusnfe.com.br";
    private const string ProducaoBaseUrl = "https://api.focusnfe.com.br";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;

    public FocusNfeDfeProviderClient(HttpClient httpClient, AppDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    public string ProviderCode => FiscalProviderCodes.FocusNfe;

    public async Task<NfseProviderResult> EmitirAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var credencialObrigatoria = RequireCredential(credencial);
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencialObrigatoria);
        var reference = ResolveReference(documento);
        var payload = await BuildEmitPayloadAsync(configuracaoFiscal, documento, cancellationToken);
        var requestJson = payload.ToJsonString(JsonOptions);

        using var request = BuildRequest(
            HttpMethod.Post,
            BuildEmitUri(baseUri, documento.TipoDocumento, reference),
            credencialObrigatoria,
            requestJson);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = ParseResponse(documento.TipoDocumento, response.StatusCode, responseJson, baseUri, reference);

        if (!result.Sucesso && ShouldRecoverWithConsult(result.MensagemErro))
        {
            result = await ConsultarInternalAsync(
                configuracaoFiscal,
                credencialObrigatoria,
                documento,
                cancellationToken);
        }

        result.RequestPayload = requestJson;
        result.ResponsePayload ??= responseJson;
        return result;
    }

    public Task<NfseProviderResult> ConsultarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        return ConsultarInternalAsync(
            configuracaoFiscal,
            RequireCredential(credencial),
            documento,
            cancellationToken);
    }

    public async Task<NfseProviderResult> CancelarAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var credencialObrigatoria = RequireCredential(credencial);
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencialObrigatoria);
        var reference = ResolveReference(documento);
        var payload = new JsonObject
        {
            ["justificativa"] = motivo.Trim()
        };
        var requestJson = payload.ToJsonString(JsonOptions);

        using var request = BuildRequest(
            HttpMethod.Delete,
            BuildConsultOrCancelUri(baseUri, documento.TipoDocumento, reference),
            credencialObrigatoria,
            requestJson);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = ParseResponse(documento.TipoDocumento, response.StatusCode, responseJson, baseUri, reference);

        if (!result.Sucesso && IsAlreadyCancelledMessage(result.MensagemErro))
        {
            result = await ConsultarInternalAsync(
                configuracaoFiscal,
                credencialObrigatoria,
                documento,
                cancellationToken);

            if (!IsStatusCancelado(result.Status))
            {
                result = new NfseProviderResult
                {
                    Sucesso = true,
                    Status = "CANCELADO",
                    NumeroExterno = reference,
                    ChaveAcesso = documento.ChaveAcesso,
                    Protocolo = documento.Protocolo,
                    XmlUrl = documento.XmlUrl,
                    PdfUrl = documento.PdfUrl
                };
            }
        }

        result.RequestPayload = requestJson;
        result.ResponsePayload ??= responseJson;
        return result;
    }

    public async Task<NfseProviderResult> SolicitarReenvioWebhookAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa? credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken = default)
    {
        var credencialObrigatoria = RequireCredential(credencial);

        if (documento.TipoDocumento == TipoDocumentoFiscal.Nfce)
        {
            throw new InvalidOperationException(
                "A Focus nao documenta reenvio automatico de webhook para NFC-e. Use a consulta do documento para validar a sincronizacao.");
        }

        var baseUri = ResolveBaseUri(configuracaoFiscal, credencialObrigatoria);
        var reference = ResolveReference(documento);

        using var request = BuildRequest(
            HttpMethod.Post,
            new Uri(baseUri, $"/v2/nfe/{Uri.EscapeDataString(reference)}/hook"),
            credencialObrigatoria);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if ((int)response.StatusCode < 400)
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
                mensagemErro = GetString(root, "mensagem", "message", "erro")
                    ?? GetArrayMessages(root, "erros", "errors")
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
            Status = response.StatusCode.ToString().ToUpperInvariant(),
            NumeroExterno = reference,
            CodigoErro = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture),
            MensagemErro = string.IsNullOrWhiteSpace(mensagemErro)
                ? "Falha ao solicitar reenvio do webhook na Focus."
                : mensagemErro,
            RequestPayload = $$"""{"ref":"{{reference}}"}""",
            ResponsePayload = responseJson
        };
    }

    private async Task<NfseProviderResult> ConsultarInternalAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        CredencialFiscalEmpresa credencial,
        DocumentoFiscal documento,
        CancellationToken cancellationToken)
    {
        var baseUri = ResolveBaseUri(configuracaoFiscal, credencial);
        var reference = ResolveReference(documento);

        using var request = BuildRequest(
            HttpMethod.Get,
            BuildConsultOrCancelUri(baseUri, documento.TipoDocumento, reference, includeCompleteQuery: true),
            credencial);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = ParseResponse(documento.TipoDocumento, response.StatusCode, responseJson, baseUri, reference);
        result.RequestPayload = $$"""{"ref":"{{reference}}"}""";
        result.ResponsePayload = responseJson;
        return result;
    }

    private async Task<JsonObject> BuildEmitPayloadAsync(
        ConfiguracaoFiscal configuracaoFiscal,
        DocumentoFiscal documento,
        CancellationToken cancellationToken)
    {
        var empresa = await ObterEmpresaAsync(documento.EmpresaId, cancellationToken);
        var venda = await ObterVendaAsync(documento.EmpresaId, documento.OrigemId, cancellationToken);
        var pecasPorId = await ObterPecasPorIdAsync(documento, cancellationToken);

        ValidateDocumento(configuracaoFiscal, empresa, documento);

        var naturezaOperacao = ResolveNaturezaOperacao(configuracaoFiscal, documento.TipoDocumento);
        var localDestino = ResolveLocalDestino(empresa.Uf, documento.ClienteUf);
        var municipioEmitenteCodigo = RequireDigits(
            configuracaoFiscal.MunicipioCodigo,
            "Configure o codigo do municipio da empresa antes de emitir NF-e/NFC-e pela Focus NFe.");

        var root = new JsonObject
        {
            ["natureza_operacao"] = naturezaOperacao,
            ["data_emissao"] = FormatFocusDate(documento.DataEmissao),
            ["tipo_documento"] = 1,
            ["local_destino"] = localDestino,
            ["consumidor_final"] = 1,
            ["cnpj_emitente"] = DigitsOnly(empresa.Cnpj),
            ["nome_emitente"] = empresa.RazaoSocial.Trim(),
            ["nome_fantasia_emitente"] = EmptyToNull(empresa.NomeFantasia),
            ["logradouro_emitente"] = RequireValue(empresa.Logradouro, "A empresa precisa ter logradouro para emitir NF-e/NFC-e."),
            ["numero_emitente"] = RequireValue(empresa.Numero, "A empresa precisa ter numero do endereco para emitir NF-e/NFC-e."),
            ["complemento_emitente"] = EmptyToNull(empresa.Complemento),
            ["bairro_emitente"] = RequireValue(empresa.Bairro, "A empresa precisa ter bairro para emitir NF-e/NFC-e."),
            ["codigo_municipio_emitente"] = municipioEmitenteCodigo,
            ["municipio_emitente"] = RequireValue(empresa.Cidade, "A empresa precisa ter municipio para emitir NF-e/NFC-e."),
            ["uf_emitente"] = RequireValue(empresa.Uf, "A empresa precisa ter UF para emitir NF-e/NFC-e.").ToUpperInvariant(),
            ["cep_emitente"] = DigitsOnlyOrNull(empresa.Cep),
            ["telefone_emitente"] = DigitsOnlyOrNull(empresa.Telefone),
            ["inscricao_estadual_emitente"] = RequireValue(
                empresa.InscricaoEstadual,
                "A empresa precisa ter inscricao estadual para emitir NF-e/NFC-e."),
            ["regime_tributario_emitente"] = ResolveRegimeTributarioEmitente(
                configuracaoFiscal.RegimeTributario,
                empresa.RegimeTributario),
            ["municipio"] = municipioEmitenteCodigo,
            ["modalidade_frete"] = documento.TipoDocumento == TipoDocumentoFiscal.Nfce ? "9" : 9,
            ["items"] = BuildItemsArray(documento, pecasPorId)
        };

        if (!string.IsNullOrWhiteSpace(empresa.InscricaoMunicipal) &&
            !string.IsNullOrWhiteSpace(configuracaoFiscal.CnaePrincipal))
        {
            root["inscricao_municipal_emitente"] = empresa.InscricaoMunicipal.Trim();
            root["cnae_fiscal_emitente"] = DigitsOnly(configuracaoFiscal.CnaePrincipal);
        }

        if (!string.IsNullOrWhiteSpace(documento.PayloadEnvio))
            root["informacoes_adicionais_contribuinte"] = documento.PayloadEnvio.Trim();

        if (documento.TipoDocumento == TipoDocumentoFiscal.Nfe)
        {
            root["data_entrada_saida"] = FormatFocusDate(documento.DataEmissao);
            root["finalidade_emissao"] = 1;
            root["presenca_comprador"] = 1;
            root["valor_frete"] = 0m;
            root["valor_seguro"] = 0m;
            root["valor_desconto"] = documento.Desconto;
            root["valor_outras_despesas"] = 0m;
            root["valor_total"] = documento.ValorTotal;
            root["valor_produtos"] = documento.ValorProdutos;
            root["numero"] = documento.Numero;
            root["serie"] = documento.Serie;
        }
        else
        {
            root["presenca_comprador"] = "1";
            root["numero"] = documento.Numero.ToString(CultureInfo.InvariantCulture);
            root["serie"] = documento.Serie.ToString(CultureInfo.InvariantCulture);
            root["formas_pagamento"] = BuildPaymentArray(venda, documento.ValorTotal);
        }

        var destinatario = BuildDestinatario(documento, requireFullAddress: documento.TipoDocumento == TipoDocumentoFiscal.Nfe);
        foreach (var pair in destinatario)
            root[pair.Key] = pair.Value?.DeepClone();

        return root;
    }

    private JsonArray BuildItemsArray(
        DocumentoFiscal documento,
        IReadOnlyDictionary<Guid, Peca> pecasPorId)
    {
        var items = new JsonArray();

        for (var index = 0; index < documento.Itens.Count; index++)
        {
            var item = documento.Itens[index];
            pecasPorId.TryGetValue(item.PecaId ?? Guid.Empty, out var peca);

            var unidade = ResolveUnidade(peca?.Unidade);
            var icmsSituacao = RequireValue(item.CstCsosn, $"Item {item.Descricao}: informe CST/CSOSN.");
            var itemJson = new JsonObject
            {
                ["numero_item"] = index + 1,
                ["codigo_produto"] = ResolveCodigoProduto(item, peca),
                ["descricao"] = ResolveDescricaoItem(item.Descricao, documento.Ambiente),
                ["codigo_ncm"] = ResolveNcm(item.Ncm),
                ["cfop"] = RequireDigits(item.Cfop, $"Item {item.Descricao}: informe CFOP."),
                ["unidade_comercial"] = unidade,
                ["quantidade_comercial"] = item.Quantidade,
                ["valor_unitario_comercial"] = item.ValorUnitario,
                ["valor_bruto"] = item.ValorTotal,
                ["unidade_tributavel"] = unidade,
                ["quantidade_tributavel"] = item.Quantidade,
                ["valor_unitario_tributavel"] = item.ValorUnitario,
                ["icms_origem"] = ResolveIcmsOrigem(item.OrigemMercadoria),
                ["icms_situacao_tributaria"] = icmsSituacao,
                ["pis_situacao_tributaria"] = ResolvePisSituacaoTributaria(item),
                ["cofins_situacao_tributaria"] = ResolveCofinsSituacaoTributaria(item)
            };

            if (!string.IsNullOrWhiteSpace(item.Cest))
                itemJson["cest"] = DigitsOnly(item.Cest);

            if (ShouldSendIcmsValues(icmsSituacao))
            {
                itemJson["icms_modalidade_base_calculo"] = 3;
                itemJson["icms_base_calculo"] = item.BaseIcms ?? item.ValorTotal;

                if ((item.AliquotaIcms ?? 0m) > 0m)
                    itemJson["icms_aliquota"] = item.AliquotaIcms.GetValueOrDefault();

                if ((item.ValorIcms ?? 0m) > 0m)
                    itemJson["icms_valor"] = item.ValorIcms.GetValueOrDefault();
            }

            if ((item.AliquotaPis ?? 0m) > 0m || (item.ValorPis ?? 0m) > 0m)
            {
                itemJson["pis_base_calculo"] = item.BasePis ?? item.ValorTotal;
                itemJson["pis_aliquota_porcentual"] = item.AliquotaPis ?? 0m;
                itemJson["pis_valor"] = item.ValorPis ?? 0m;
            }

            if ((item.AliquotaCofins ?? 0m) > 0m || (item.ValorCofins ?? 0m) > 0m)
            {
                itemJson["cofins_base_calculo"] = item.BaseCofins ?? item.ValorTotal;
                itemJson["cofins_aliquota_porcentual"] = item.AliquotaCofins ?? 0m;
                itemJson["cofins_valor"] = item.ValorCofins ?? 0m;
            }

            items.Add(itemJson);
        }

        return items;
    }

    private static JsonObject BuildDestinatario(DocumentoFiscal documento, bool requireFullAddress)
    {
        var destinatario = new JsonObject();
        var documentoTomador = DigitsOnlyOrNull(documento.ClienteCpfCnpj);

        if (!string.IsNullOrWhiteSpace(documentoTomador))
        {
            if (documentoTomador.Length <= 11)
                destinatario["cpf_destinatario"] = documentoTomador;
            else
                destinatario["cnpj_destinatario"] = documentoTomador;
        }

        if (requireFullAddress || !string.IsNullOrWhiteSpace(documento.ClienteNome))
            destinatario["nome_destinatario"] = RequireValue(documento.ClienteNome, "Informe o nome do destinatario.");

        if (requireFullAddress)
        {
            destinatario["logradouro_destinatario"] = RequireValue(documento.ClienteLogradouro, "NF-e exige logradouro do destinatario.");
            destinatario["numero_destinatario"] = RequireValue(documento.ClienteNumero, "NF-e exige numero do destinatario.");
            destinatario["complemento_destinatario"] = EmptyToNull(documento.ClienteComplemento);
            destinatario["bairro_destinatario"] = RequireValue(documento.ClienteBairro, "NF-e exige bairro do destinatario.");
            destinatario["municipio_destinatario"] = RequireValue(documento.ClienteCidade, "NF-e exige municipio do destinatario.");
            destinatario["uf_destinatario"] = RequireValue(documento.ClienteUf, "NF-e exige UF do destinatario.").ToUpperInvariant();
            destinatario["cep_destinatario"] = DigitsOnlyOrNull(documento.ClienteCep);
            destinatario["codigo_municipio_destinatario"] = DigitsOnlyOrNull(documento.ClienteMunicipioCodigo);
            destinatario["pais_destinatario"] = "Brasil";
        }
        else
        {
            AddIfPresent(destinatario, "nome_destinatario", documento.ClienteNome);
            AddIfPresent(destinatario, "municipio_destinatario", documento.ClienteCidade);
            AddIfPresent(destinatario, "uf_destinatario", NormalizeUf(documento.ClienteUf));
        }

        AddIfPresent(destinatario, "telefone_destinatario", DigitsOnlyOrNull(documento.ClienteTelefone));
        AddIfPresent(destinatario, "email_destinatario", documento.ClienteEmail);
        destinatario["indicador_inscricao_estadual_destinatario"] = "9";

        return destinatario;
    }

    private static JsonArray BuildPaymentArray(Venda venda, decimal valorTotal)
    {
        var formaPagamento = string.IsNullOrWhiteSpace(venda.FormaPagamento)
            ? "DINHEIRO"
            : venda.FormaPagamento.Trim().ToUpperInvariant();

        var payment = new JsonObject
        {
            ["indicador_pagamento"] = IsPagamentoPrazo(formaPagamento) ? "1" : "0",
            ["forma_pagamento"] = ResolveFormaPagamento(formaPagamento),
            ["valor_pagamento"] = valorTotal
        };

        if (string.Equals(payment["forma_pagamento"]?.GetValue<string>(), "99", StringComparison.Ordinal))
            payment["descricao_pagamento"] = formaPagamento;

        return new JsonArray { payment };
    }

    private async Task<Empresa> ObterEmpresaAsync(Guid empresaId, CancellationToken cancellationToken)
    {
        return await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId && x.Ativo, cancellationToken)
            ?? throw new InvalidOperationException("Empresa emitente nao encontrada.");
    }

    private async Task<Venda> ObterVendaAsync(Guid empresaId, Guid vendaId, CancellationToken cancellationToken)
    {
        return await _context.Vendas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == vendaId, cancellationToken)
            ?? throw new InvalidOperationException("Venda fiscal nao encontrada.");
    }

    private async Task<IReadOnlyDictionary<Guid, Peca>> ObterPecasPorIdAsync(
        DocumentoFiscal documento,
        CancellationToken cancellationToken)
    {
        var pecaIds = documento.Itens
            .Where(x => x.PecaId.HasValue)
            .Select(x => x.PecaId!.Value)
            .Distinct()
            .ToArray();

        if (pecaIds.Length == 0)
            return new Dictionary<Guid, Peca>();

        return await _context.Pecas
            .AsNoTracking()
            .Where(x => x.EmpresaId == documento.EmpresaId && pecaIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private static void ValidateDocumento(
        ConfiguracaoFiscal configuracaoFiscal,
        Empresa empresa,
        DocumentoFiscal documento)
    {
        if (documento.TipoDocumento != TipoDocumentoFiscal.Nfe &&
            documento.TipoDocumento != TipoDocumentoFiscal.Nfce)
        {
            throw new InvalidOperationException("O provedor Focus NFe de DF-e suporta apenas NF-e e NFC-e.");
        }

        if (documento.Itens.Count == 0)
            throw new InvalidOperationException("Nao e possivel emitir documento fiscal sem itens.");

        if (string.IsNullOrWhiteSpace(empresa.Cnpj))
            throw new InvalidOperationException("A empresa precisa ter CNPJ para emitir NF-e/NFC-e.");

        if (string.IsNullOrWhiteSpace(empresa.InscricaoEstadual))
            throw new InvalidOperationException("A empresa precisa ter inscricao estadual para emitir NF-e/NFC-e.");

        if (documento.TipoDocumento == TipoDocumentoFiscal.Nfce)
        {
            var now = DateTimeOffset.UtcNow;
            var emissao = documento.DataEmissao.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(documento.DataEmissao, DateTimeKind.Utc))
                : new DateTimeOffset(documento.DataEmissao.ToUniversalTime());

            if (Math.Abs((now - emissao).TotalMinutes) > 5)
            {
                throw new InvalidOperationException(
                    "A NFC-e precisa ter data/hora de emissao com diferenca maxima de 5 minutos do horario atual.");
            }
        }

        if (documento.TipoDocumento == TipoDocumentoFiscal.Nfe)
        {
            if (string.IsNullOrWhiteSpace(documento.ClienteCpfCnpj))
                throw new InvalidOperationException("NF-e exige CPF/CNPJ do destinatario.");

            if (string.IsNullOrWhiteSpace(documento.ClienteCidade))
                throw new InvalidOperationException("NF-e exige municipio do destinatario.");

            if (string.IsNullOrWhiteSpace(documento.ClienteUf))
                throw new InvalidOperationException("NF-e exige UF do destinatario.");
        }

        foreach (var item in documento.Itens)
        {
            if (string.IsNullOrWhiteSpace(item.Ncm))
                throw new InvalidOperationException($"Item {item.Descricao}: informe NCM.");

            if (string.IsNullOrWhiteSpace(item.Cfop))
                throw new InvalidOperationException($"Item {item.Descricao}: informe CFOP.");

            if (string.IsNullOrWhiteSpace(item.CstCsosn))
                throw new InvalidOperationException($"Item {item.Descricao}: informe CST/CSOSN.");

            if (string.IsNullOrWhiteSpace(item.OrigemMercadoria))
                throw new InvalidOperationException($"Item {item.Descricao}: informe origem da mercadoria.");
        }
    }

    private static CredencialFiscalEmpresa RequireCredential(CredencialFiscalEmpresa? credencial)
    {
        return credencial
            ?? throw new InvalidOperationException(
                "Credencial fiscal da Focus NFe nao encontrada para este documento.");
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

    private static Uri BuildEmitUri(Uri baseUri, TipoDocumentoFiscal tipoDocumento, string reference)
    {
        var path = tipoDocumento == TipoDocumentoFiscal.Nfce
            ? $"/v2/nfce?ref={Uri.EscapeDataString(reference)}&completa=1"
            : $"/v2/nfe?ref={Uri.EscapeDataString(reference)}";

        return new Uri(baseUri, path);
    }

    private static Uri BuildConsultOrCancelUri(
        Uri baseUri,
        TipoDocumentoFiscal tipoDocumento,
        string reference,
        bool includeCompleteQuery = false)
    {
        var route = tipoDocumento == TipoDocumentoFiscal.Nfce ? "nfce" : "nfe";
        var suffix = includeCompleteQuery ? "?completa=1" : string.Empty;
        return new Uri(baseUri, $"/v2/{route}/{Uri.EscapeDataString(reference)}{suffix}");
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

    private static NfseProviderResult ParseResponse(
        TipoDocumentoFiscal tipoDocumento,
        HttpStatusCode statusCode,
        string responseJson,
        Uri baseUri,
        string reference)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return new NfseProviderResult
            {
                Sucesso = statusCode is HttpStatusCode.Created or HttpStatusCode.Accepted or HttpStatusCode.OK,
                Status = statusCode switch
                {
                    HttpStatusCode.Accepted => "PROCESSANDO_AUTORIZACAO",
                    HttpStatusCode.Created or HttpStatusCode.OK => "AUTORIZADO",
                    _ => statusCode.ToString().ToUpperInvariant()
                },
                NumeroExterno = reference,
                CodigoErro = (int)statusCode >= 400 ? ((int)statusCode).ToString(CultureInfo.InvariantCulture) : null,
                MensagemErro = (int)statusCode >= 400 ? "O provedor retornou uma resposta vazia." : null
            };
        }

        try
        {
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            var providerStatus = GetString(root, "status", "situacao");
            var normalizedStatus = NormalizeStatus(providerStatus);
            var success = IsSuccessfulProviderResponse(statusCode, normalizedStatus);

            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                normalizedStatus = statusCode switch
                {
                    HttpStatusCode.Accepted => "PROCESSANDO_AUTORIZACAO",
                    HttpStatusCode.Created or HttpStatusCode.OK => success ? "AUTORIZADO" : "ERRO_AUTORIZACAO",
                    _ => statusCode.ToString().ToUpperInvariant()
                };
            }

            return new NfseProviderResult
            {
                Sucesso = success,
                Status = normalizedStatus,
                NumeroExterno = reference,
                ChaveAcesso = GetString(root, "chave_nfe", "chave", "chave_acesso"),
                Protocolo = GetString(root, "protocolo", "numero_protocolo"),
                CodigoVerificacao = null,
                LinkConsulta = NormalizeRemoteUrl(
                    baseUri,
                    GetString(root, "url", "url_consulta", "url_sefaz")),
                Lote = GetString(root, "lote"),
                XmlConteudo = null,
                XmlUrl = NormalizeRemoteUrl(
                    baseUri,
                    GetString(root, "url_xml", "caminho_xml_nota_fiscal", "url_xml_nota_fiscal")),
                PdfUrl = NormalizeRemoteUrl(
                    baseUri,
                    tipoDocumento == TipoDocumentoFiscal.Nfce
                        ? GetString(root, "url_danfce", "caminho_danfce", "url_danfe", "caminho_danfe")
                        : GetString(root, "url_danfe", "caminho_danfe", "url_danfce", "caminho_danfce")),
                CodigoErro = success ? null : GetString(root, "codigo", "error_code") ?? ((int)statusCode).ToString(CultureInfo.InvariantCulture),
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
                Status = statusCode.ToString().ToUpperInvariant(),
                NumeroExterno = reference,
                CodigoErro = ((int)statusCode).ToString(CultureInfo.InvariantCulture),
                MensagemErro = responseJson
            };
        }
    }

    private static bool IsSuccessfulProviderResponse(HttpStatusCode statusCode, string? normalizedStatus)
    {
        if ((int)statusCode >= 400)
            return false;

        if (string.IsNullOrWhiteSpace(normalizedStatus))
            return statusCode is HttpStatusCode.Accepted or HttpStatusCode.Created or HttpStatusCode.OK;

        return normalizedStatus is
            "AUTORIZADO" or
            "AUTORIZADA" or
            "CANCELADO" or
            "CANCELADA" or
            "PROCESSANDO_AUTORIZACAO" or
            "EM_PROCESSAMENTO" or
            "PROCESSANDO";
    }

    private static string GetErrorMessage(
        JsonElement root,
        string? providerStatus,
        HttpStatusCode statusCode)
    {
        var errors = GetArrayMessages(root, "erros", "errors");
        if (!string.IsNullOrWhiteSpace(errors))
            return errors;

        return GetString(root, "mensagem", "mensagem_sefaz", "message", "erro")
            ?? providerStatus
            ?? $"Falha ao comunicar com a Focus NFe ({(int)statusCode}).";
    }

    private static string? GetArrayMessages(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var errorsElement) ||
                errorsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
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

                var mensagem = GetString(item, "mensagem", "erro", "message");
                if (!string.IsNullOrWhiteSpace(mensagem))
                    messages.Add(mensagem.Trim());
            }

            if (messages.Count > 0)
                return string.Join(" | ", messages.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        return null;
    }

    private static string? NormalizeRemoteUrl(Uri baseUri, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        return Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : new Uri(baseUri, url.TrimStart('/')).ToString();
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property) ||
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

    private static bool ShouldRecoverWithConsult(string? mensagemErro)
    {
        var normalized = NormalizeForMatch(mensagemErro);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        return normalized.Contains("PROCESSAMENTO", StringComparison.Ordinal) ||
               normalized.Contains("PROCESSANDO", StringComparison.Ordinal) ||
               normalized.Contains("OPERACAO PENDENTE", StringComparison.Ordinal) ||
               normalized.Contains("REFERENCIA EM PROCESSAMENTO", StringComparison.Ordinal) ||
               normalized.Contains("JA AUTORIZADA", StringComparison.Ordinal) ||
               normalized.Contains("REFERENCIA JA UTILIZADA", StringComparison.Ordinal);
    }

    private static bool IsAlreadyCancelledMessage(string? mensagemErro)
    {
        var normalized = NormalizeForMatch(mensagemErro);
        return normalized.Contains("JA CANCELADA", StringComparison.Ordinal) ||
               normalized.Contains("JA CANCELADO", StringComparison.Ordinal);
    }

    private static bool IsStatusCancelado(string? providerStatus)
        => string.Equals(NormalizeStatus(providerStatus), "CANCELADO", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(NormalizeStatus(providerStatus), "CANCELADA", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToUpperInvariant(ch));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ResolveNaturezaOperacao(
        ConfiguracaoFiscal configuracaoFiscal,
        TipoDocumentoFiscal tipoDocumento)
    {
        var configurada = configuracaoFiscal.NaturezaOperacaoPadrao?.Trim();
        if (!string.IsNullOrWhiteSpace(configurada) &&
            !IsNaturezaOperacaoNfse(configurada))
        {
            return configurada;
        }

        return tipoDocumento == TipoDocumentoFiscal.Nfce
            ? "VENDA AO CONSUMIDOR"
            : "VENDA DE MERCADORIA";
    }

    private static bool IsNaturezaOperacaoNfse(string value)
    {
        return value is
            "TributacaoNoMunicipio" or
            "TributacaoForaDoMunicipio" or
            "Isencao" or
            "Imune" or
            "ExigibilidadeSuspensaJudicial" or
            "ExigibilidadeSuspensaAdministrativa";
    }

    private static int ResolveRegimeTributarioEmitente(string? regimeConfigurado, string? regimeEmpresa)
    {
        var normalized = (regimeConfigurado ?? regimeEmpresa ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        return normalized switch
        {
            "SIMPLESNACIONAL" => 1,
            "SIMPLESNACIONALEXCESSODESUBLIMITE" => 2,
            "REGIMENORMAL" => 3,
            "MEI" => 1,
            _ => 1
        };
    }

    private static int ResolveLocalDestino(string? ufOrigem, string? ufDestino)
    {
        var origem = NormalizeUf(ufOrigem);
        var destino = NormalizeUf(ufDestino);

        if (string.IsNullOrWhiteSpace(destino))
            return 1;

        return string.Equals(origem, destino, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    }

    private static string ResolveFormaPagamento(string formaPagamento)
    {
        return formaPagamento switch
        {
            "DINHEIRO" => "01",
            "CHEQUE" => "02",
            "CARTAO_CREDITO" => "03",
            "CARTAO_DEBITO" => "04",
            "BOLETO" => "15",
            "PIX" => "17",
            "TRANSFERENCIA" => "18",
            "CREDIARIO" => "91",
            _ => "99"
        };
    }

    private static bool IsPagamentoPrazo(string formaPagamento)
    {
        return formaPagamento is "BOLETO" or "CARTAO_CREDITO" or "CREDIARIO";
    }

    private static string ResolveCodigoProduto(DocumentoFiscalItem item, Peca? peca)
    {
        if (!string.IsNullOrWhiteSpace(peca?.CodigoInterno))
            return peca.CodigoInterno.Trim();

        if (!string.IsNullOrWhiteSpace(peca?.Sku))
            return peca.Sku.Trim();

        if (!string.IsNullOrWhiteSpace(item.Cfop))
            return $"CFOP{DigitsOnly(item.Cfop)}";

        return item.Id.ToString("N")[..12].ToUpperInvariant();
    }

    private static string ResolveDescricaoItem(string descricao, AmbienteFiscal ambiente)
    {
        var value = RequireValue(descricao, "Descricao do item nao informada.");

        if (ambiente == AmbienteFiscal.Homologacao &&
            !value.Contains("HOMOLOGACAO", StringComparison.OrdinalIgnoreCase))
        {
            return $"{value} - HOMOLOGACAO";
        }

        return value;
    }

    private static string ResolveNcm(string? ncm)
    {
        var digits = RequireDigits(ncm, "Informe o NCM do item.");
        return digits.Length switch
        {
            >= 8 => digits[..8],
            >= 2 => digits,
            _ => throw new InvalidOperationException("O NCM do item precisa ter pelo menos 2 digitos.")
        };
    }

    private static string ResolveUnidade(string? unidade)
    {
        var value = string.IsNullOrWhiteSpace(unidade) ? "UN" : unidade.Trim().ToUpperInvariant();
        return value.Length > 6 ? value[..6] : value;
    }

    private static string ResolveIcmsOrigem(string? origemMercadoria)
    {
        var digits = RequireDigits(origemMercadoria, "Informe a origem da mercadoria.");
        return digits[..1];
    }

    private static bool ShouldSendIcmsValues(string icmsSituacao)
    {
        var normalized = icmsSituacao.Trim().ToUpperInvariant();

        return normalized is "00" or "20" or "40" or "41" or "51" or "60" or "90" or "900";
    }

    private static string ResolvePisSituacaoTributaria(DocumentoFiscalItem item)
    {
        return (item.AliquotaPis ?? 0m) > 0m || (item.ValorPis ?? 0m) > 0m
            ? "01"
            : "07";
    }

    private static string ResolveCofinsSituacaoTributaria(DocumentoFiscalItem item)
    {
        return (item.AliquotaCofins ?? 0m) > 0m || (item.ValorCofins ?? 0m) > 0m
            ? "01"
            : "07";
    }

    private static void AddIfPresent(JsonObject json, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            json[key] = value.Trim();
    }

    private static string? NormalizeUf(string? uf)
        => string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();

    private static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());

    private static string? DigitsOnlyOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : DigitsOnly(value.Trim());

    private static string RequireDigits(string? value, string message)
    {
        var digits = DigitsOnlyOrNull(value);
        if (string.IsNullOrWhiteSpace(digits))
            throw new InvalidOperationException(message);

        return digits;
    }

    private static string RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(message);

        return value.Trim();
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatFocusDate(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        return new DateTimeOffset(utc).ToString("yyyy-MM-ddTHH:mm:sszzz");
    }
}
