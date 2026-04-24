using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class ConfiguracaoFiscalServiceTests
{
    [Fact]
    public async Task SalvarAsync_DeveBloquearProducaoQuandoFocusNaoTiverCredenciaisProntas()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        context.Empresas.Add(CriarEmpresaCompleta(empresaId, "22345678000190", "Empresa Sem Credencial"));
        await context.SaveChangesAsync();

        var service = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(),
            Options.Create(new FocusWebhookOptions()));

        var dto = CriarDtoBase();
        dto.Ambiente = "Producao";
        dto.ProvedorFiscal = "focusnfe";
        dto.MunicipioCodigo = "4106902";
        dto.CnaePrincipal = "9511800";
        dto.ItemListaServico = "14.01";

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            service.SalvarAsync(empresaId, dto));

        Assert.Contains("credencial ativa da Focus", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SalvarAsync_DevePermitirProducaoQuandoFocusEstiverPronta()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        context.Empresas.Add(CriarEmpresaCompleta(empresaId, "32345678000190", "Empresa Produção"));
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            EmpresaId = empresaId,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = "focusnfe",
            TokenAcesso = "token-nfse",
            TokenExpiraEm = DateTime.UtcNow.AddDays(30),
            Ativo = true
        });
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            EmpresaId = empresaId,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe,
            Provedor = "focusnfe",
            TokenAcesso = "token-nfe",
            TokenExpiraEm = DateTime.UtcNow.AddDays(30),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(),
            Options.Create(new FocusWebhookOptions()));

        var dto = CriarDtoBase();
        dto.Ambiente = "Producao";
        dto.ProvedorFiscal = "focusnfe";
        dto.MunicipioCodigo = "4106902";
        dto.CnaePrincipal = "9511800";
        dto.ItemListaServico = "14.01";

        var result = await service.SalvarAsync(empresaId, dto);

        Assert.Equal("Producao", result.Ambiente);
        Assert.Equal("focusnfe", result.ProvedorFiscal);
    }

    [Fact]
    public async Task ObterChecklistAsync_DeveLiberarHomologacaoEManterProducaoPendenteQuandoProviderForFake()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        context.Empresas.Add(CriarEmpresaCompleta(empresaId, "12345678000190", "Empresa Homolog"));
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
            MunicipioCodigo = "3550308",
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(),
            Options.Create(new FocusWebhookOptions()));

        var result = await service.ObterChecklistAsync(
            empresaId,
            "http://localhost:5221");

        Assert.True(result.HomologacaoReady);
        Assert.False(result.ProducaoReady);
        Assert.Contains(
            result.MissingForProducao,
            item => item.Contains("Provider", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Items,
            item => item.Key == "provider" && item.Status == "warning");
    }

    [Fact]
    public async Task ObterChecklistAsync_DeveMarcarProducaoProntaQuandoFocusEFluxosEstiveremProntos()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaId = Guid.NewGuid();
        context.Empresas.Add(CriarEmpresaCompleta(empresaId, "10987654000190", "Empresa Focus"));
        context.ConfiguracoesFiscais.Add(new ConfiguracaoFiscal
        {
            EmpresaId = empresaId,
            Ambiente = AmbienteFiscal.Producao,
            RegimeTributario = "SimplesNacional",
            SerieNfce = 1,
            SerieNfe = 1,
            SerieNfse = 1,
            ProximoNumeroNfce = 10,
            ProximoNumeroNfe = 10,
            ProximoNumeroNfse = 10,
            ProvedorFiscal = "focusnfe",
            MunicipioCodigo = "3550308",
            CnaePrincipal = "9511800",
            ItemListaServico = "14.01",
            Ativo = true
        });
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            EmpresaId = empresaId,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = "focusnfe",
            TokenAcesso = "token-nfse",
            TokenExpiraEm = DateTime.UtcNow.AddDays(30),
            Ativo = true
        });
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            EmpresaId = empresaId,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe,
            Provedor = "focusnfe",
            TokenAcesso = "token-nfe",
            TokenExpiraEm = DateTime.UtcNow.AddDays(30),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new ConfiguracaoFiscalService(
            context,
            new StubFocusNfseMunicipioService(new FocusNfseMunicipioValidacaoDto
            {
                ProviderCode = "focusnfe",
                MunicipioCodigo = "3550308",
                PodeEmitirNfse = true,
                ItemListaServicoConfigurado = true,
                CnaePrincipalConfigurado = true
            }),
            Options.Create(new FocusWebhookOptions
            {
                Enabled = true,
                Secret = "segredo-webhook",
                PublicBaseUrl = "https://api.exemplo.com.br"
            }));

        var result = await service.ObterChecklistAsync(
            empresaId,
            "https://api.exemplo.com.br");

        Assert.True(result.HomologacaoReady);
        Assert.True(result.ProducaoReady);
        Assert.Equal("focusnfe", result.ProviderCode);
        Assert.Contains(
            result.Items,
            item => item.Key == "nfse" && item.Status == "ok");
        Assert.Contains(
            result.Items,
            item => item.Key == "dfe" && item.Status == "ok");
    }

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
        private readonly FocusNfseMunicipioValidacaoDto _result;

        public StubFocusNfseMunicipioService(FocusNfseMunicipioValidacaoDto? result = null)
        {
            _result = result ?? new FocusNfseMunicipioValidacaoDto();
        }

        public Task<FocusNfseMunicipioValidacaoDto> ValidarAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
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

    private static Empresa CriarEmpresaCompleta(Guid empresaId, string cnpj, string nomeFantasia)
    {
        return new Empresa
        {
            Id = empresaId,
            RazaoSocial = $"{nomeFantasia} Ltda",
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            InscricaoEstadual = "123456789",
            InscricaoMunicipal = "987654",
            Email = "financeiro@exemplo.com",
            Telefone = "41999999999",
            Cep = "80000000",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "Curitiba",
            Uf = "PR",
            Ativo = true
        };
    }

    private static UpdateConfiguracaoFiscalDto CriarDtoBase()
    {
        return new UpdateConfiguracaoFiscalDto
        {
            Ambiente = "Homologacao",
            RegimeTributario = "SimplesNacional",
            SerieNfce = 1,
            SerieNfe = 1,
            SerieNfse = 1,
            ProximoNumeroNfce = 1,
            ProximoNumeroNfe = 1,
            ProximoNumeroNfse = 1,
            Ativo = true
        };
    }
}
