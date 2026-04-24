using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class DfeVendaServiceStatusFlowTests
{
    [Fact]
    public async Task EmitirEConsultarNfce_ComProviderAssincrono_DeveAtualizarDePendenteParaAutorizado()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var protector = new FiscalCredentialSecretProtector(new EphemeralDataProtectionProvider());
        var empresa = CriarEmpresa("44444444000144", "Empresa Async");
        var cliente = CriarCliente(empresa.Id, "Cliente Async");
        var peca = CriarPeca(empresa.Id, "Peca Async");
        var venda = CriarVenda(empresa.Id, cliente.Id, "PIX", 120m);
        var itemVenda = CriarItemVenda(empresa.Id, venda.Id, peca.Id, "Peca Async", 120m);
        var usuario = CriarUsuario("async@example.com");

        context.Empresas.Add(empresa);
        context.Clientes.Add(cliente);
        context.Pecas.Add(peca);
        context.Vendas.Add(venda);
        context.VendaItens.Add(itemVenda);
        context.Usuarios.Add(usuario);
        context.ConfiguracoesFiscais.Add(CriarConfiguracaoFiscal(empresa.Id));
        context.CredenciaisFiscaisEmpresas.Add(new CredencialFiscalEmpresa
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfce,
            Provedor = FiscalProviderCodes.Fake,
            ClientSecretEncrypted = protector.Protect("segredo-async"),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var provider = new AsyncSpyDfeProviderClient();
        var service = new DfeVendaService(
            context,
            new DocumentoFiscalBuilderService(context, new NumeracaoFiscalService(context)),
            new DfeProviderResolver([provider]),
            protector);

        var emitido = await service.EmitirNfcePorVendaAsync(
            empresa.Id,
            usuario.Id,
            venda.Id,
            new EmitirDfeVendaDto
            {
                GerarContaReceber = true,
                ValidarTributacaoCompleta = false
            });

        Assert.Equal(StatusDocumentoFiscal.PendenteEnvio.ToString(), emitido.Status);
        Assert.Equal(0, await context.ContasReceber.CountAsync());

        var autorizado = await service.ConsultarAsync(empresa.Id, emitido.Id);

        Assert.Equal(StatusDocumentoFiscal.Autorizado.ToString(), autorizado.Status);
        Assert.Equal(1, await context.ContasReceber.CountAsync());

        var documento = await context.DocumentosFiscais.SingleAsync();
        Assert.Equal(StatusDocumentoFiscal.Autorizado, documento.Status);
        Assert.NotNull(documento.DataAutorizacao);
        Assert.Equal("CHAVE-CONSULTA", documento.ChaveAcesso);
        Assert.Equal("PROTO-CONSULTA", documento.Protocolo);
        Assert.False(documento.GerarContaReceberQuandoAutorizar);

        var conta = await context.ContasReceber.SingleAsync();
        Assert.Equal("VENDA", conta.OrigemTipo);
        Assert.Equal(venda.Id, conta.OrigemId);
        Assert.Equal(venda.ClienteId, conta.ClienteId);
        Assert.Equal(venda.ValorTotal, conta.Valor);
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
            Logradouro = "Rua A",
            Numero = "10",
            Bairro = "Centro",
            InscricaoEstadual = "123456789",
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

    private static Peca CriarPeca(Guid empresaId, string nome)
    {
        return new Peca
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nome = nome,
            Unidade = "UN",
            Ncm = "84733049",
            CfopPadraoNfe = "5102",
            CfopPadraoNfce = "5102",
            CstCsosn = "102",
            OrigemMercadoria = "0",
            CustoUnitario = 40m,
            PrecoVenda = 120m,
            EstoqueAtual = 5m
        };
    }

    private static Venda CriarVenda(Guid empresaId, Guid clienteId, string formaPagamento, decimal valorTotal)
    {
        return new Venda
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            ClienteId = clienteId,
            Status = "FECHADA",
            FormaPagamento = formaPagamento,
            Subtotal = valorTotal,
            Desconto = 0m,
            ValorTotal = valorTotal
        };
    }

    private static VendaItem CriarItemVenda(Guid empresaId, Guid vendaId, Guid pecaId, string descricao, decimal valorTotal)
    {
        return new VendaItem
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            VendaId = vendaId,
            PecaId = pecaId,
            Descricao = descricao,
            Quantidade = 1,
            CustoUnitario = 40m,
            ValorUnitario = valorTotal,
            Desconto = 0m,
            ValorTotal = valorTotal
        };
    }

    private static ConfiguracaoFiscal CriarConfiguracaoFiscal(Guid empresaId)
    {
        return new ConfiguracaoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Ambiente = AmbienteFiscal.Homologacao,
            RegimeTributario = "SimplesNacional",
            SerieNfe = 1,
            SerieNfce = 1,
            ProximoNumeroNfe = 1,
            ProximoNumeroNfce = 1,
            MunicipioCodigo = "3550308",
            Ativo = true
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

    private sealed class AsyncSpyDfeProviderClient : IDfeProviderClient
    {
        public string ProviderCode => FiscalProviderCodes.Fake;

        public Task<NfseProviderResult> EmitirAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
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
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new NfseProviderResult
            {
                Sucesso = true,
                Status = "AUTORIZADO",
                NumeroExterno = documento.NumeroExterno ?? documento.Id.ToString("N"),
                ChaveAcesso = "CHAVE-CONSULTA",
                Protocolo = "PROTO-CONSULTA",
                XmlUrl = "/mock/xml",
                PdfUrl = "/mock/pdf",
                RequestPayload = "{}",
                ResponsePayload = "{}"
            });
        }

        public Task<NfseProviderResult> CancelarAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<NfseProviderResult> SolicitarReenvioWebhookAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
