using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FiscalContaReceberFlowTests
{
    [Fact]
    public async Task EmitirNfcePorVendaAsync_ComGerarContaReceber_DeveGerarContaEUsarCredencialDesprotegida()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var protector = new FiscalCredentialSecretProtector(new EphemeralDataProtectionProvider());
        var empresa = CriarEmpresa("11111111000111", "Empresa Fiscal");
        var cliente = CriarCliente(empresa.Id, "Cliente Fiscal");
        var peca = CriarPeca(empresa.Id, "Peca Fiscal");
        var venda = CriarVenda(empresa.Id, cliente.Id, "BOLETO", 150m);
        var itemVenda = CriarItemVenda(empresa.Id, venda.Id, peca.Id, "Peca Fiscal", 150m);
        var usuario = CriarUsuario("fiscal1@example.com");

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
            Provedor = "fake",
            ClientId = "client-id",
            ClientSecretEncrypted = protector.Protect("segredo-nfce"),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var provider = new SpyDfeProviderClient();
        var service = new DfeVendaService(
            context,
            new DocumentoFiscalBuilderService(context, new NumeracaoFiscalService(context)),
            new DfeProviderResolver([provider]),
            protector);

        var resultado = await service.EmitirNfcePorVendaAsync(
            empresa.Id,
            usuario.Id,
            venda.Id,
            new EmitirDfeVendaDto
            {
                GerarContaReceber = true,
                ValidarTributacaoCompleta = false
            });

        var conta = await context.ContasReceber.SingleAsync();

        Assert.Equal(StatusDocumentoFiscal.Autorizado.ToString(), resultado.Status);
        Assert.Equal("segredo-nfce", provider.LastClientSecret);
        Assert.Equal("VENDA", conta.OrigemTipo);
        Assert.Equal(venda.Id, conta.OrigemId);
        Assert.Equal(venda.ClienteId, conta.ClienteId);
        Assert.Equal(venda.ValorTotal, conta.Valor);
        Assert.Equal("BOLETO", conta.FormaPagamento);
        Assert.Equal(empresa.Id, conta.EmpresaId);
    }

    [Fact]
    public async Task EmitirNfcePorVendaAsync_ComContaExistente_NaoDeveDuplicarContaReceber()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var protector = new FiscalCredentialSecretProtector(new EphemeralDataProtectionProvider());
        var empresa = CriarEmpresa("22222222000122", "Empresa Venda");
        var cliente = CriarCliente(empresa.Id, "Cliente Venda");
        var peca = CriarPeca(empresa.Id, "Peca Venda");
        var venda = CriarVenda(empresa.Id, cliente.Id, "CARTAO_CREDITO", 90m);
        var itemVenda = CriarItemVenda(empresa.Id, venda.Id, peca.Id, "Peca Venda", 90m);
        var usuario = CriarUsuario("fiscal2@example.com");

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
            Provedor = "fake",
            ClientSecretEncrypted = protector.Protect("segredo-existente"),
            Ativo = true
        });
        context.ContasReceber.Add(new ContaReceber
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            ClienteId = cliente.Id,
            OrigemTipo = "VENDA",
            OrigemId = venda.Id,
            Descricao = "Conta prévia da venda",
            DataEmissao = DateOnly.FromDateTime(DateTime.UtcNow),
            DataVencimento = DateOnly.FromDateTime(DateTime.UtcNow),
            Valor = venda.ValorTotal,
            ValorRecebido = 0,
            Status = "PENDENTE",
            FormaPagamento = "CARTAO_CREDITO"
        });
        await context.SaveChangesAsync();

        var service = new DfeVendaService(
            context,
            new DocumentoFiscalBuilderService(context, new NumeracaoFiscalService(context)),
            new DfeProviderResolver([new SpyDfeProviderClient()]),
            protector);

        await service.EmitirNfcePorVendaAsync(
            empresa.Id,
            usuario.Id,
            venda.Id,
            new EmitirDfeVendaDto
            {
                GerarContaReceber = true,
                ValidarTributacaoCompleta = false
            });

        Assert.Equal(1, await context.ContasReceber.CountAsync());
    }

    [Fact]
    public async Task EmitirPorOrdemServicoAsync_ComGerarContaReceber_DeveGerarContaParaEmpresaDaOs()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var protector = new FiscalCredentialSecretProtector(new EphemeralDataProtectionProvider());
        var empresa = CriarEmpresa("33333333000133", "Empresa OS");
        var cliente = CriarCliente(empresa.Id, "Cliente OS");
        var usuario = CriarUsuario("fiscal3@example.com");
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
            NumeroOs = 321,
            ClienteId = cliente.Id,
            AparelhoId = aparelho.Id,
            Status = "FINALIZADA",
            DefeitoRelatado = "Nao liga",
            ValorMaoObra = 120m,
            ValorPecas = 0m,
            Desconto = 0m,
            ValorTotal = 120m
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
            ValorUnitario = 120m,
            ValorTotal = 120m
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
            Provedor = "fake",
            SenhaApiEncrypted = protector.Protect("senha-nfse"),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var service = new NfseService(
            context,
            new DocumentoFiscalBuilderService(context, new NumeracaoFiscalService(context)),
            new NfseProviderResolver([new NfseProviderClientFake()]),
            protector);

        var resultado = await service.EmitirPorOrdemServicoAsync(
            empresa.Id,
            usuario.Id,
            ordemServico.Id,
            new EmitirNfsePorOsDto
            {
                GerarContaReceber = true
            });

        var conta = await context.ContasReceber.SingleAsync();

        Assert.Equal(StatusDocumentoFiscal.Autorizado.ToString(), resultado.Status);
        Assert.Equal("ORDEM_SERVICO", conta.OrigemTipo);
        Assert.Equal(ordemServico.Id, conta.OrigemId);
        Assert.Equal(cliente.Id, conta.ClienteId);
        Assert.Equal(ordemServico.ValorTotal, conta.Valor);
        Assert.Equal(empresa.Id, conta.EmpresaId);
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
            Cidade = "Sao Paulo"
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
            CustoUnitario = 50m,
            PrecoVenda = 150m,
            EstoqueAtual = 10m
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
            CustoUnitario = 50m,
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
            SerieNfse = 1,
            ProximoNumeroNfe = 1,
            ProximoNumeroNfce = 1,
            ProximoNumeroNfse = 1,
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

    private sealed class SpyDfeProviderClient : IDfeProviderClient
    {
        public string ProviderCode => FiscalProviderCodes.Fake;

        public string? LastClientSecret { get; private set; }

        public Task<NfseProviderResult> EmitirAsync(
            ConfiguracaoFiscal configuracaoFiscal,
            CredencialFiscalEmpresa? credencial,
            DocumentoFiscal documento,
            CancellationToken cancellationToken = default)
        {
            LastClientSecret = credencial?.ClientSecretEncrypted;

            return Task.FromResult(new NfseProviderResult
            {
                Sucesso = true,
                Status = "AUTORIZADO",
                NumeroExterno = documento.Numero.ToString(),
                ChaveAcesso = $"CHAVE-{documento.Id:N}",
                Protocolo = $"PROTO-{documento.Id:N}",
                CodigoVerificacao = "CODIGOFAKE",
                LinkConsulta = "/mock/dfe",
                Lote = "LOTE-FAKE",
                XmlConteudo = "<xml />",
                XmlUrl = "/mock/xml",
                PdfUrl = "/mock/pdf",
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
            throw new NotSupportedException();
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
