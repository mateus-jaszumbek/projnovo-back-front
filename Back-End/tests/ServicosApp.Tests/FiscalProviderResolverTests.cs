using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FiscalProviderResolverTests
{
    [Fact]
    public void DfeResolver_DevePriorizarProvedorDaCredencial()
    {
        var resolver = new DfeProviderResolver(
        [
            new StubDfeProviderClient("fake"),
            new StubDfeProviderClient("focusnfe")
        ]);

        var provider = resolver.Resolve(
            new ConfiguracaoFiscal
            {
                ProvedorFiscal = "fake"
            },
            new CredencialFiscalEmpresa
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe,
                Provedor = "focusnfe"
            });

        Assert.Equal("focusnfe", provider.ProviderCode);
    }

    [Fact]
    public void DfeResolver_DeveUsarFakeQuandoSomenteFakeEstiverRegistrado()
    {
        var resolver = new DfeProviderResolver([new StubDfeProviderClient(FiscalProviderCodes.Fake)]);

        var provider = resolver.Resolve(new ConfiguracaoFiscal(), null);

        Assert.Equal(FiscalProviderCodes.Fake, provider.ProviderCode);
    }

    [Fact]
    public void DfeResolver_DeveManterFakeComoFallbackQuandoExistiremOutrosProviders()
    {
        var resolver = new DfeProviderResolver(
        [
            new StubDfeProviderClient(FiscalProviderCodes.Fake),
            new StubDfeProviderClient(FiscalProviderCodes.FocusNfe)
        ]);

        var provider = resolver.Resolve(new ConfiguracaoFiscal(), null);

        Assert.Equal(FiscalProviderCodes.Fake, provider.ProviderCode);
    }

    [Fact]
    public void DfeResolver_DeveFalharQuandoProvedorNaoEstiverRegistrado()
    {
        var resolver = new DfeProviderResolver([new StubDfeProviderClient(FiscalProviderCodes.Fake)]);

        var action = () => resolver.Resolve(
            new ConfiguracaoFiscal
            {
                ProvedorFiscal = "focusnfe"
            },
            null);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("focusnfe", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NfseResolver_DevePriorizarProvedorDaCredencial()
    {
        var resolver = new NfseProviderResolver(
        [
            new StubNfseProviderClient("fake"),
            new StubNfseProviderClient("abrasf")
        ]);

        var provider = resolver.Resolve(
            new ConfiguracaoFiscal
            {
                ProvedorFiscal = "fake"
            },
            new CredencialFiscalEmpresa
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
                Provedor = "abrasf"
            });

        Assert.Equal("abrasf", provider.ProviderCode);
    }

    [Fact]
    public void NfseResolver_DeveUsarFakeQuandoSomenteFakeEstiverRegistrado()
    {
        var resolver = new NfseProviderResolver([new StubNfseProviderClient(FiscalProviderCodes.Fake)]);

        var provider = resolver.Resolve(
            new ConfiguracaoFiscal(),
            new CredencialFiscalEmpresa
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
                Provedor = string.Empty
            });

        Assert.Equal(FiscalProviderCodes.Fake, provider.ProviderCode);
    }

    [Fact]
    public void NfseResolver_DeveNormalizarCodigoDoProvider()
    {
        var resolver = new NfseProviderResolver(
        [
            new StubNfseProviderClient(FiscalProviderCodes.Fake),
            new StubNfseProviderClient(FiscalProviderCodes.FocusNfe)
        ]);

        var provider = resolver.Resolve(
            new ConfiguracaoFiscal
            {
                ProvedorFiscal = "Focus NFe"
            },
            new CredencialFiscalEmpresa
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
                Provedor = string.Empty
            });

        Assert.Equal(FiscalProviderCodes.FocusNfe, provider.ProviderCode);
    }

    [Fact]
    public void NfseResolver_DeveManterFakeComoFallbackQuandoExistiremOutrosProviders()
    {
        var resolver = new NfseProviderResolver(
        [
            new StubNfseProviderClient(FiscalProviderCodes.Fake),
            new StubNfseProviderClient(FiscalProviderCodes.FocusNfe)
        ]);

        var provider = resolver.Resolve(
            new ConfiguracaoFiscal(),
            new CredencialFiscalEmpresa
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
                Provedor = string.Empty
            });

        Assert.Equal(FiscalProviderCodes.Fake, provider.ProviderCode);
    }

    private sealed class StubDfeProviderClient(string providerCode) : IDfeProviderClient
    {
        public string ProviderCode => providerCode;

        public Task<NfseProviderResult> EmitirAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NfseProviderResult> ConsultarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NfseProviderResult> CancelarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            string motivo,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NfseProviderResult> SolicitarReenvioWebhookAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubNfseProviderClient(string providerCode) : INfseProviderClient
    {
        public string ProviderCode => providerCode;

        public Task<NfseProviderResult> EmitirAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NfseProviderResult> ConsultarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NfseProviderResult> CancelarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            string motivo,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NfseProviderResult> SolicitarReenvioWebhookAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
