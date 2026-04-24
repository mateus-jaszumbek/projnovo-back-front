using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace ServicosApp.Tests;

public class CredencialFiscalEmpresaServiceTests
{
    [Fact]
    public async Task ListarAsync_DeveRetornarApenasCredenciaisDaEmpresaInformada()
    {
        using var protectorDirectory = new TempDirectory();
        var dataProtectionProvider = BuildDataProtectionProvider(protectorDirectory.Path);

        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaA = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa A Ltda",
            NomeFantasia = "Empresa A",
            Cnpj = "11111111000111"
        };

        var empresaB = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa B Ltda",
            NomeFantasia = "Empresa B",
            Cnpj = "22222222000122"
        };

        context.Empresas.AddRange(empresaA, empresaB);
        await context.SaveChangesAsync();

        var secretProtector = new FiscalCredentialSecretProtector(dataProtectionProvider);
        var service = new CredencialFiscalEmpresaService(context, secretProtector);

        await service.CriarAsync(
            empresaA.Id,
            new CreateCredencialFiscalEmpresaDto
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse.ToString(),
                Provedor = "PrefeituraA",
                UsuarioApi = "empresa-a",
                SenhaApi = "senha-a",
                Ativo = true
            });

        await service.CriarAsync(
            empresaB.Id,
            new CreateCredencialFiscalEmpresaDto
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse.ToString(),
                Provedor = "PrefeituraB",
                UsuarioApi = "empresa-b",
                SenhaApi = "senha-b",
                Ativo = true
            });

        var resultado = await service.ListarAsync(empresaA.Id, null, null);

        Assert.Single(resultado);
        Assert.Equal(
            FiscalProviderCodeNormalizer.Normalize("PrefeituraA"),
            resultado[0].Provedor);
        Assert.Equal(TipoDocumentoFiscal.Nfse.ToString(), resultado[0].TipoDocumentoFiscal);
    }

    [Fact]
    public async Task CriarAsync_DevePersistirSegredosProtegidos()
    {
        using var protectorDirectory = new TempDirectory();
        var dataProtectionProvider = BuildDataProtectionProvider(protectorDirectory.Path);

        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Segura Ltda",
            NomeFantasia = "Empresa Segura",
            Cnpj = "33333333000133"
        };

        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();

        var secretProtector = new FiscalCredentialSecretProtector(dataProtectionProvider);
        var service = new CredencialFiscalEmpresaService(context, secretProtector);

        await service.CriarAsync(
            empresa.Id,
            new CreateCredencialFiscalEmpresaDto
            {
                TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe.ToString(),
                Provedor = "Focus",
                ClientId = "client-id",
                ClientSecret = "segredo-super-sensivel",
                TokenAcesso = "token-sensivel"
            });

        var entity = context.CredenciaisFiscaisEmpresas.Single();

        Assert.NotEqual("segredo-super-sensivel", entity.ClientSecretEncrypted);
        Assert.NotEqual("token-sensivel", entity.TokenAcesso);
        Assert.Equal("segredo-super-sensivel", secretProtector.Unprotect(entity.ClientSecretEncrypted));
        Assert.Equal("token-sensivel", secretProtector.Unprotect(entity.TokenAcesso));
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
