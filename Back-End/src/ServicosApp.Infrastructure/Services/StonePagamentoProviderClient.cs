using ServicosApp.Application.DTOs.Pagamentos;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;

namespace ServicosApp.Infrastructure.Services;

// Esqueleto pronto para a Stone Connect ("Stone Conecta").
// Para ativar de verdade:
//   1. Cadastre-se como parceiro técnico no portal de desenvolvedores da Stone e obtenha
//      credenciais OAuth (sandbox + produção).
//   2. Implemente as chamadas HTTP reais nos métodos abaixo, usando o _httpClient já injetado.
//   3. Marque Implementado = true para "stone" em PagamentoProviderResolver.Catalogo.
public class StonePagamentoProviderClient : IPagamentoProviderClient
{
    private readonly HttpClient _httpClient;

    public StonePagamentoProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ProviderCode => PagamentoProviderCodes.Stone;

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
        => new("Integração com Stone ainda não implementada. Requer cadastro como parceiro Stone Conecta.");
}
