using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Services;

// Implementado conforme a documentação pública do Mercado Pago (Payments API + Point Integration API).
// Não foi testado com credenciais reais - qualquer store precisa gerar seu próprio access token
// em https://www.mercadopago.com.br/developers e cadastrar o device (Point Smart) para a maquininha.
public class MercadoPagoPagamentoProviderClient : IPagamentoProviderClient
{
    private const string BaseUrl = "https://api.mercadopago.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;

    public MercadoPagoPagamentoProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderCode => PagamentoProviderCodes.MercadoPago;

    public async Task<PagamentoProviderResult> CriarCobrancaPixAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        string webhookUrl,
        CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject
        {
            ["transaction_amount"] = cobranca.Valor,
            ["description"] = string.IsNullOrWhiteSpace(cobranca.Descricao)
                ? $"{cobranca.OrigemTipo} #{cobranca.OrigemId}"
                : cobranca.Descricao,
            ["payment_method_id"] = "pix",
            ["external_reference"] = $"{cobranca.OrigemTipo}:{cobranca.OrigemId}:{cobranca.Id}",
            ["notification_url"] = webhookUrl,
            ["payer"] = new JsonObject
            {
                ["email"] = $"cliente-{cobranca.EmpresaId:N}@servicosapp.com.br"
            }
        };

        using var request = BuildRequest(HttpMethod.Post, $"{BaseUrl}/v1/payments", configuracao, payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Falha(responseJson);

        var node = JsonNode.Parse(responseJson);
        var transactionData = node?["point_of_interaction"]?["transaction_data"];

        return new PagamentoProviderResult
        {
            Sucesso = true,
            ExternalId = node?["id"]?.ToString(),
            Status = MapStatus(node?["status"]?.GetValue<string>()),
            QrCodeBase64 = transactionData?["qr_code_base64"]?.GetValue<string>(),
            QrCodePayload = transactionData?["qr_code"]?.GetValue<string>(),
            ResponsePayload = responseJson
        };
    }

    public async Task<PagamentoProviderResult> CriarCobrancaMaquininhaAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        string webhookUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configuracao.PosId))
            return Falha("Configure o ID do dispositivo (Point Smart) antes de cobrar na maquininha.");

        var payload = new JsonObject
        {
            ["amount"] = (long)Math.Round(cobranca.Valor * 100, MidpointRounding.AwayFromZero),
            ["additional_info"] = new JsonObject
            {
                ["external_reference"] = $"{cobranca.OrigemTipo}:{cobranca.OrigemId}:{cobranca.Id}",
                ["print_on_terminal"] = true
            },
            ["notification_url"] = webhookUrl
        };

        using var request = BuildRequest(
            HttpMethod.Post,
            $"{BaseUrl}/point/integration-api/devices/{Uri.EscapeDataString(configuracao.PosId)}/payment-intents",
            configuracao,
            payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Falha(responseJson);

        var node = JsonNode.Parse(responseJson);

        return new PagamentoProviderResult
        {
            Sucesso = true,
            ExternalId = node?["id"]?.ToString(),
            Status = MapStatus(node?["state"]?.GetValue<string>()),
            ResponsePayload = responseJson
        };
    }

    public async Task<PagamentoProviderResult> ConsultarAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cobranca.ExternalId))
            return Falha("Cobrança sem identificador externo.");

        var url = cobranca.Canal == "MAQUININHA"
            ? $"{BaseUrl}/point/integration-api/payment-intents/{cobranca.ExternalId}"
            : $"{BaseUrl}/v1/payments/{cobranca.ExternalId}";

        using var request = BuildRequest(HttpMethod.Get, url, configuracao, body: null);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Falha(responseJson);

        var node = JsonNode.Parse(responseJson);

        if (cobranca.Canal == "MAQUININHA")
        {
            var estado = node?["state"]?.GetValue<string>();
            var paymentId = node?["payment"]?["id"]?.ToString();

            return new PagamentoProviderResult
            {
                Sucesso = true,
                ExternalId = paymentId ?? cobranca.ExternalId,
                Status = MapStatus(estado),
                ResponsePayload = responseJson
            };
        }

        return new PagamentoProviderResult
        {
            Sucesso = true,
            ExternalId = cobranca.ExternalId,
            Status = MapStatus(node?["status"]?.GetValue<string>()),
            ResponsePayload = responseJson
        };
    }

    public async Task<bool> CancelarAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cobranca.ExternalId))
            return true;

        if (cobranca.Canal == "MAQUININHA")
        {
            using var request = BuildRequest(
                HttpMethod.Delete,
                $"{BaseUrl}/point/integration-api/payment-intents/{cobranca.ExternalId}",
                configuracao,
                body: null);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        var payload = new JsonObject { ["status"] = "cancelled" };
        using var cancelRequest = BuildRequest(
            HttpMethod.Put,
            $"{BaseUrl}/v1/payments/{cobranca.ExternalId}",
            configuracao,
            payload);

        using var cancelResponse = await _httpClient.SendAsync(cancelRequest, cancellationToken);
        return cancelResponse.IsSuccessStatusCode;
    }

    private static HttpRequestMessage BuildRequest(
        HttpMethod method,
        string url,
        ConfiguracaoPagamento configuracao,
        JsonObject? body)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracao.AccessTokenEncrypted);
        request.Headers.TryAddWithoutValidation("X-Idempotency-Key", Guid.NewGuid().ToString("N"));

        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string MapStatus(string? providerStatus)
    {
        return providerStatus?.ToLowerInvariant() switch
        {
            "approved" or "finished" => "APROVADA",
            "rejected" or "canceled" or "cancelled" => "RECUSADA",
            "in_process" or "pending" or "open" => "PENDENTE",
            _ => "PENDENTE"
        };
    }

    private static PagamentoProviderResult Falha(string mensagem)
    {
        return new PagamentoProviderResult
        {
            Sucesso = false,
            MensagemErro = mensagem,
            ResponsePayload = mensagem
        };
    }
}
