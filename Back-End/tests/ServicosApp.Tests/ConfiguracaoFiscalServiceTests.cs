using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class ConfiguracaoFiscalServiceTests
{
    [Fact]
    public async Task ObterFocusWebhookSetupAsync_DeveMontarUrlsQuandoFocusEstiverConfigurado()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        context.Empresas.Add(CriarEmpresa(empresaId, "12345678000190", "Empresa Focus"));
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
            ProvedorFiscal = "focusnfe",
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(),
            Options.Create(new FocusWebhookOptions
            {
                Enabled = true,
                Secret = "segredo-webhook",
                PublicBaseUrl = "https://api.exemplo.com.br"
            }));

        var result = await service.ObterFocusWebhookSetupAsync(
            empresaId,
            "http://localhost:5221");

        Assert.True(result.FocusProviderSelected);
        Assert.True(result.SecretConfigured);
        Assert.True(result.Enabled);
        Assert.True(result.BaseUrlLooksPublic);
        Assert.True(result.UrlsReady);
        Assert.Equal(
            "https://api.exemplo.com.br/api/fiscal/webhooks/focus/dfe/segredo-webhook",
            result.DfeWebhookUrl);
        Assert.Equal(
            "https://api.exemplo.com.br/api/fiscal/webhooks/focus/nfse/segredo-webhook",
            result.NfseWebhookUrl);
    }

    [Fact]
    public async Task ObterFocusWebhookSetupAsync_DeveAlertarQuandoBaseUrlForLocalOuSegredoFaltar()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        context.Empresas.Add(CriarEmpresa(empresaId, "10987654000190", "Empresa Local"));
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
            ProvedorFiscal = "fake",
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(),
            Options.Create(new FocusWebhookOptions
            {
                Enabled = false,
                Secret = "",
                PublicBaseUrl = ""
            }));

        var result = await service.ObterFocusWebhookSetupAsync(
            empresaId,
            "http://localhost:5221");

        Assert.False(result.FocusProviderSelected);
        Assert.False(result.SecretConfigured);
        Assert.False(result.Enabled);
        Assert.False(result.BaseUrlLooksPublic);
        Assert.False(result.UrlsReady);
        Assert.Null(result.DfeWebhookUrl);
        Assert.Contains(
            result.Warnings,
            item => item.Contains("focusnfe", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Warnings,
            item => item.Contains("FocusWebhook:Secret", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Warnings,
            item => item.Contains("URL atual parece local", StringComparison.OrdinalIgnoreCase));
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

    private static Empresa CriarEmpresa(Guid empresaId, string cnpj, string nomeFantasia)
    {
        return new Empresa
        {
            Id = empresaId,
            RazaoSocial = $"{nomeFantasia} Ltda",
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            Uf = "SP",
            Cidade = "Sao Paulo",
            Ativo = true
        };
    }
}
