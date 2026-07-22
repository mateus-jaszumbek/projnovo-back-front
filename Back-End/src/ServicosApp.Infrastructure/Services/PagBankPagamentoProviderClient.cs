using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Services;

// Esqueleto pronto para o PagBank (Ready2/Smart API).
// Para ativar de verdade:
//   1. Crie uma conta de desenvolvedor no PagBank e registre um aplicativo para obter
//      client id/secret e credenciais OAuth.
//   2. Implemente as chamadas HTTP reais nos métodos abaixo, usando o _httpClient já injetado.
//   3. Marque Implementado = true para "pagbank" em PagamentoProviderResolver.Catalogo.
public class PagBankPagamentoProviderClient : IPagamentoProviderClient
{
    private readonly HttpClient _httpClient;

    public PagBankPagamentoProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderCode => PagamentoProviderCodes.PagBank;

    public Task<PagamentoProviderResult> CriarCobrancaMaquininhaAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        string webhookUrl,
        CancellationToken cancellationToken = default)
        => throw NaoImplementado();

    public Task<PagamentoProviderResult> CriarCobrancaPixAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        string webhookUrl,
        CancellationToken cancellationToken = default)
        => throw NaoImplementado();

    public Task<PagamentoProviderResult> ConsultarAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken = default)
        => throw NaoImplementado();

    public Task<bool> CancelarAsync(
        ConfiguracaoPagamento configuracao,
        CobrancaPagamento cobranca,
        CancellationToken cancellationToken = default)
        => throw NaoImplementado();

    private static NotSupportedException NaoImplementado()
        => new("Integração com PagBank ainda não implementada. Requer conta de desenvolvedor PagBank.");
}
