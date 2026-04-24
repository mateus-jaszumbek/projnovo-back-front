using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FocusWebhookRegistrationServiceTests
{
    [Fact]
    public async Task ObterStatusAsync_DeveReconhecerHooksRemotosJaCadastrados()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        SeedEmpresaComFocus(context, empresaId);

        var handler = new FocusWebhookHttpHandler();
        handler.Seed(
            "token-dfe",
            new FocusRemoteHookPayload(
                "hook-dfe",
                "nfe",
                "https://api.exemplo.com.br/api/fiscal/webhooks/focus/dfe/segredo-webhook"));
        handler.Seed(
            "token-nfse",
            new FocusRemoteHookPayload(
                "hook-nfse",
                "nfse",
                "https://api.exemplo.com.br/api/fiscal/webhooks/focus/nfse/segredo-webhook"));

        var service = CreateService(context, handler);

        var result = await service.ObterStatusAsync(empresaId, "http://localhost:5221");

        Assert.True(result.CanRegisterRemotely);
        Assert.True(result.CheckedRemotely);
        Assert.True(result.DfeRemoteStatus.Registered);
        Assert.True(result.NfseRemoteStatus.Registered);
        Assert.Equal("hook-dfe", result.DfeRemoteStatus.HookId);
        Assert.Equal("hook-nfse", result.NfseRemoteStatus.HookId);
        Assert.Empty(result.ActionsTaken);
    }

    [Fact]
    public async Task SincronizarAsync_DeveCriarHooksAusentesNaFocus()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        SeedEmpresaComFocus(context, empresaId);

        var handler = new FocusWebhookHttpHandler();
        handler.Seed("token-dfe");
        handler.Seed("token-nfse");

        var service = CreateService(context, handler);

        var result = await service.SincronizarAsync(empresaId, "http://localhost:5221");

        Assert.True(result.DfeRemoteStatus.Registered);
        Assert.True(result.NfseRemoteStatus.Registered);
        Assert.Equal(2, handler.CreateCount);
        Assert.Contains(
            result.ActionsTaken,
            item => item.Contains("DF-e", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.ActionsTaken,
            item => item.Contains("NFS-e", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SincronizarAsync_NaoDeveDuplicarHookQuandoEventoJaApontarParaOutraUrl()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        SeedEmpresaComFocus(context, empresaId);

        var handler = new FocusWebhookHttpHandler();
        handler.Seed(
            "token-dfe",
            new FocusRemoteHookPayload(
                "hook-dfe-antigo",
                "nfe",
                "https://legado.exemplo.com.br/fiscal/nfe"));
        handler.Seed(
            "token-nfse",
            new FocusRemoteHookPayload(
                "hook-nfse",
                "nfse",
                "https://api.exemplo.com.br/api/fiscal/webhooks/focus/nfse/segredo-webhook"));

        var service = CreateService(context, handler);

        var result = await service.SincronizarAsync(empresaId, "http://localhost:5221");

        Assert.False(result.DfeRemoteStatus.Registered);
        Assert.Equal("hook-dfe-antigo", result.DfeRemoteStatus.HookId);
        Assert.Equal(0, handler.CreateCount);
        Assert.Contains(
            result.Warnings,
            item => item.Contains("outra URL", StringComparison.OrdinalIgnoreCase));
    }

    private static FocusWebhookRegistrationService CreateService(
        Infrastructure.Data.AppDbContext context,
        FocusWebhookHttpHandler handler)
    {
        var configService = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(),
            Options.Create(new FocusWebhookOptions
            {
                Enabled = true,
                Secret = "segredo-webhook",
                PublicBaseUrl = "https://api.exemplo.com.br"
            }));

        var protector = new FiscalCredentialSecretProtector(
            new EphemeralDataProtectionProvider());

        return new FocusWebhookRegistrationService(
            new HttpClient(handler),
            context,
            configService,
            protector,
            LoggerFactory.Create(builder => { }).CreateLogger<FocusWebhookRegistrationService>());
    }

    private static void SeedEmpresaComFocus(Infrastructure.Data.AppDbContext context, Guid empresaId)
    {
        context.Empresas.Add(new Empresa
        {
            Id = empresaId,
            RazaoSocial = "Empresa Focus Ltda",
            NomeFantasia = "Empresa Focus",
            Cnpj = "12345678000190",
            Uf = "SP",
            Cidade = "Sao Paulo",
            Ativo = true
        });

        context.ConfiguracoesFiscais.Add(new ConfiguracaoFiscal
        {
            EmpresaId = empresaId,
            Ambiente = AmbienteFiscal.Homologacao,
            RegimeTributario = "SimplesNacional",
            SerieNfce = 1,
            SerieNfe = 1,
            SerieNfse = 1,
            ProximoNumeroNfce = 1,
            ProximoNumeroNfe = 1,
            ProximoNumeroNfse = 1,
            ProvedorFiscal = FiscalProviderCodes.FocusNfe,
            Ativo = true
        });

        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-dfe",
            Ativo = true
        });

        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-nfse",
            Ativo = true
        });

        context.SaveChanges();
    }

    private sealed class StubFocusNfseMunicipioService : IFocusNfseMunicipioService
    {
        public Task<FocusNfseMunicipioValidacaoDto> ValidarAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FocusNfseMunicipioValidacaoDto());
        }
    }

    private sealed class FocusWebhookHttpHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, List<FocusRemoteHookPayload>> _hooksByToken =
            new(StringComparer.Ordinal);

        public int CreateCount { get; private set; }

        public void Seed(string token, params FocusRemoteHookPayload[] hooks)
        {
            _hooksByToken[token] = hooks.ToList();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = ExtractToken(request.Headers.Authorization);
            if (!_hooksByToken.TryGetValue(token, out var hooks))
            {
                hooks = [];
                _hooksByToken[token] = hooks;
            }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (request.Method == HttpMethod.Get && string.Equals(path, "/v2/hooks", StringComparison.Ordinal))
            {
                return JsonResponse(hooks);
            }

            if (request.Method == HttpMethod.Post && string.Equals(path, "/v2/hooks", StringComparison.Ordinal))
            {
                var body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var hook = new FocusRemoteHookPayload(
                    Guid.NewGuid().ToString("N"),
                    root.GetProperty("event").GetString() ?? string.Empty,
                    root.GetProperty("url").GetString() ?? string.Empty);

                hooks.Add(hook);
                CreateCount++;
                return JsonResponse(hook);
            }

            throw new InvalidOperationException($"Requisição inesperada para {request.Method} {request.RequestUri}");
        }

        private static string ExtractToken(AuthenticationHeaderValue? authorization)
        {
            Assert.NotNull(authorization);
            Assert.Equal("Basic", authorization?.Scheme);

            var parameter = authorization?.Parameter ?? string.Empty;
            var decoded = Encoding.ASCII.GetString(Convert.FromBase64String(parameter));
            return decoded.Split(':', 2)[0];
        }

        private static HttpResponseMessage JsonResponse(object payload)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed record FocusRemoteHookPayload(
        string Id,
        string Event,
        string Url);
}
