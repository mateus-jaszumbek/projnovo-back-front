using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FocusNfseMunicipioServiceTests
{
    [Fact]
    public async Task ValidarAsync_DeveSinalizarCodigoTributarioQuandoMunicipioExigir()
    {
        using var protectorDirectory = new TempDirectory();
        var dataProtectionProvider = BuildDataProtectionProvider(protectorDirectory.Path);

        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Municipio Ltda",
            NomeFantasia = "Empresa Municipio",
            Cnpj = "55555555000155"
        };

        context.Empresas.Add(empresa);
        context.ConfiguracoesFiscais.Add(new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            ProvedorFiscal = FiscalProviderCodes.FocusNfe,
            MunicipioCodigo = "3542602",
            CnaePrincipal = "9511800",
            ItemListaServico = "14.01",
            Ativo = true
        });
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "focus-token",
            Ativo = true
        });
        await context.SaveChangesAsync();

        var secretProtector = new FiscalCredentialSecretProtector(dataProtectionProvider);
        var service = new FocusNfseMunicipioService(
            context,
            secretProtector,
            new HttpClient(new StubHttpMessageHandler(
                """
                {
                  "codigo_municipio": "3542602",
                  "nome_municipio": "Registro",
                  "sigla_uf": "SP",
                  "status_nfse": "ativo",
                  "codigo_tributario_municipio_obrigatorio_nfse": true
                }
                """)));

        var result = await service.ValidarAsync(empresa.Id);

        Assert.True(result.RemoteValidationAvailable);
        Assert.False(result.PodeEmitirNfse);
        Assert.True(result.CodigoTributarioMunicipioObrigatorio);
        Assert.Contains(result.Errors, item => item.Contains("código tributário municipal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidarAsync_DeveLiberarEmissaoQuandoConfiguracaoEstiverCompleta()
    {
        using var protectorDirectory = new TempDirectory();
        var dataProtectionProvider = BuildDataProtectionProvider(protectorDirectory.Path);

        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Pronta Ltda",
            NomeFantasia = "Empresa Pronta",
            Cnpj = "66666666000166"
        };

        context.Empresas.Add(empresa);
        context.ConfiguracoesFiscais.Add(new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            ProvedorFiscal = FiscalProviderCodes.FocusNfe,
            MunicipioCodigo = "3542602",
            CnaePrincipal = "9511800",
            ItemListaServico = "14.01",
            CodigoTributarioMunicipio = "1401",
            Ativo = true
        });
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "focus-token",
            Ativo = true
        });
        await context.SaveChangesAsync();

        var secretProtector = new FiscalCredentialSecretProtector(dataProtectionProvider);
        var service = new FocusNfseMunicipioService(
            context,
            secretProtector,
            new HttpClient(new StubHttpMessageHandler(
                """
                {
                  "codigo_municipio": "3542602",
                  "nome_municipio": "Registro",
                  "sigla_uf": "SP",
                  "status_nfse": "ativo",
                  "codigo_tributario_municipio_obrigatorio_nfse": true
                }
                """)));

        var result = await service.ValidarAsync(empresa.Id);

        Assert.True(result.PodeEmitirNfse);
        Assert.Empty(result.Errors);
        Assert.Equal("Registro", result.MunicipioNome);
    }

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }

    private static IDataProtectionProvider BuildDataProtectionProvider(string directory)
    {
        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(directory));

        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }
}
