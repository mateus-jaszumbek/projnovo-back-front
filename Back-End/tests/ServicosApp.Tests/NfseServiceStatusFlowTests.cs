using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class NfseServiceStatusFlowTests
{
    [Fact]
    public async Task EmitirEConsultarNfse_ComProviderAssincrono_DeveGerarContaAoAutorizarDepois()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var protector = new FiscalCredentialSecretProtector(new EphemeralDataProtectionProvider());
        var empresa = CriarEmpresa("55555555000155", "Empresa Nfse Async");
        var cliente = CriarCliente(empresa.Id, "Cliente OS");
        var usuario = CriarUsuario("nfse-async@example.com");
        var aparelho = new Aparelho
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            ClienteId = cliente.Id,
            Marca = "Marca",
            Modelo = "Modelo"
        };
        var ordemServico = new OrdemServico
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            NumeroOs = 654,
            ClienteId = cliente.Id,
            AparelhoId = aparelho.Id,
            Status = "FINALIZADA",
            DefeitoRelatado = "Nao liga",
            ValorMaoObra = 180m,
            ValorPecas = 0m,
            Desconto = 0m,
            ValorTotal = 180m
        };
        var itemOs = new OrdemServicoItem
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            OrdemServicoId = ordemServico.Id,
            TipoItem = "SERVICO",
            Ordem = 1,
            Descricao = "Reparo",
            Quantidade = 1,
            ValorUnitario = 180m,
            ValorTotal = 180m
        };

        context.Empresas.Add(empresa);
        context.Clientes.Add(cliente);
        context.Usuarios.Add(usuario);
        context.Aparelhos.Add(aparelho);
        context.OrdensServico.Add(ordemServico);
        context.Set<OrdemServicoItem>().Add(itemOs);
        context.ConfiguracoesFiscais.Add(new ConfiguracaoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            RegimeTributario = "SimplesNacional",
            SerieNfse = 1,
            ProximoNumeroNfse = 1,
            CnaePrincipal = "9511800",
            ItemListaServico = "14.01",
            AliquotaIssPadrao = 5m,
            Ativo = true
        });
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = FiscalProviderCodes.Fake,
            SenhaApiEncrypted = protector.Protect("senha-nfse"),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new NfseService(
            context,
            new DocumentoFiscalBuilderService(context, new NumeracaoFiscalService(context)),
            new NfseProviderResolver([new AsyncSpyNfseProviderClient()]),
            protector);

        var emitido = await service.EmitirPorOrdemServicoAsync(
            empresa.Id,
            usuario.Id,
            ordemServico.Id,
            new EmitirNfsePorOsDto
            {
                GerarContaReceber = true
            });

        Assert.Equal(StatusDocumentoFiscal.PendenteEnvio.ToString(), emitido.Status);
        Assert.Equal(0, await context.ContasReceber.CountAsync());

        var autorizado = await service.ConsultarAsync(empresa.Id, emitido.Id);

        Assert.Equal(StatusDocumentoFiscal.Autorizado.ToString(), autorizado.Status);
        Assert.Equal(1, await context.ContasReceber.CountAsync());

        var documento = await context.DocumentosFiscais.SingleAsync();
        Assert.Equal(StatusDocumentoFiscal.Autorizado, documento.Status);
        Assert.NotNull(documento.DataAutorizacao);
        Assert.Equal("NFSE-CHAVE", documento.ChaveAcesso);
        Assert.False(documento.GerarContaReceberQuandoAutorizar);

        var conta = await context.ContasReceber.SingleAsync();
        Assert.Equal("ORDEM_SERVICO", conta.OrigemTipo);
        Assert.Equal(ordemServico.Id, conta.OrigemId);
        Assert.Equal(cliente.Id, conta.ClienteId);
        Assert.Equal(ordemServico.ValorTotal, conta.Valor);
    }

    private static Empresa CriarEmpresa(string cnpj, string nomeFantasia)
    {
        return new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = $"{nomeFantasia} Ltda",
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            Uf = "SP",
            Cidade = "Sao Paulo",
            Ativo = true
        };
    }

    private static Cliente CriarCliente(Guid empresaId, string nome)
    {
        return new Cliente
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nome = nome,
            TipoPessoa = "FISICA",
            CpfCnpj = "12345678901",
            Cidade = "Sao Paulo",
            Uf = "SP"
        };
    }

    private static Usuario CriarUsuario(string email)
    {
        return new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = email.Split('@')[0],
            Email = email,
            SenhaHash = "hash-teste"
        };
    }

    private sealed class AsyncSpyNfseProviderClient : INfseProviderClient
    {
        public string ProviderCode => FiscalProviderCodes.Fake;

        public Task<NfseProviderResult> EmitirAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new NfseProviderResult
            {
                Sucesso = true,
                Status = "PROCESSANDO_AUTORIZACAO",
                NumeroExterno = documento.Id.ToString("N"),
                RequestPayload = "{}",
                ResponsePayload = "{}"
            });
        }

        public Task<NfseProviderResult> ConsultarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new NfseProviderResult
            {
                Sucesso = true,
                Status = "AUTORIZADO",
                NumeroExterno = documento.NumeroExterno ?? documento.Id.ToString("N"),
                ChaveAcesso = "NFSE-CHAVE",
                Protocolo = "NFSE-PROTO",
                CodigoVerificacao = "NFSE-CODIGO",
                XmlUrl = "/mock/nfse.xml",
                PdfUrl = "/mock/nfse.pdf",
                RequestPayload = "{}",
                ResponsePayload = "{}"
            });
        }

        public Task<NfseProviderResult> CancelarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<NfseProviderResult> SolicitarReenvioWebhookAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
