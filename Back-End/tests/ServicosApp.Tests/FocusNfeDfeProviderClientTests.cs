using System.Net;
using System.Net.Http;
using System.Text;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FocusNfeDfeProviderClientTests
{
    [Fact]
    public async Task EmitirNfeAsync_DeveMapearUrlsEStatusDoFocus()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = CriarEmpresa();
        var venda = CriarVenda(empresa.Id, "BOLETO", 150m);

        context.Empresas.Add(empresa);
        context.Vendas.Add(venda);
        await context.SaveChangesAsync();

        var documento = CriarDocumentoNfe(empresa.Id, venda.Id);

        var configuracao = new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            MunicipioCodigo = "3550308",
            RegimeTributario = "SimplesNacional"
        };

        var credencial = new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-focus"
        };

        var handler = new StubHttpMessageHandler((request, body) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "https://homologacao.focusnfe.com.br/v2/nfe?ref=" + documento.Id.ToString("N"),
                request.RequestUri?.ToString());
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            Assert.Contains("\"cnpj_emitente\":\"12345678000190\"", body);
            Assert.Contains("\"cpf_destinatario\":\"12345678901\"", body);
            Assert.Contains("\"items\"", body);
            Assert.Contains("\"finalidade_emissao\":1", body);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """
                    {
                      "status": "autorizado",
                      "chave_nfe": "35123456780001900000055000000000421000000042",
                      "protocolo": "135260000000001",
                      "url_sefaz": "https://sefaz.sp.gov.br/consulta",
                      "caminho_xml_nota_fiscal": "/arquivos/nfe/42.xml",
                      "url_danfe": "https://files.focusnfe.com.br/notas/42.pdf"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new FocusNfeDfeProviderClient(new HttpClient(handler), context);

        var resultado = await client.EmitirAsync(configuracao, credencial, documento);

        Assert.True(resultado.Sucesso);
        Assert.Equal("AUTORIZADO", resultado.Status);
        Assert.Equal(documento.Id.ToString("N"), resultado.NumeroExterno);
        Assert.Equal("35123456780001900000055000000000421000000042", resultado.ChaveAcesso);
        Assert.Equal("135260000000001", resultado.Protocolo);
        Assert.Equal("https://homologacao.focusnfe.com.br/arquivos/nfe/42.xml", resultado.XmlUrl);
        Assert.Equal("https://files.focusnfe.com.br/notas/42.pdf", resultado.PdfUrl);
        Assert.Equal("https://sefaz.sp.gov.br/consulta", resultado.LinkConsulta);
    }

    [Fact]
    public async Task EmitirNfceAsync_DeveEnviarPagamentoEMapearProcessamento()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = CriarEmpresa();
        var venda = CriarVenda(empresa.Id, "PIX", 89.9m);

        context.Empresas.Add(empresa);
        context.Vendas.Add(venda);
        await context.SaveChangesAsync();

        var documento = CriarDocumentoNfce(empresa.Id, venda.Id);

        var configuracao = new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            MunicipioCodigo = "3550308",
            RegimeTributario = "SimplesNacional"
        };

        var credencial = new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfce,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-focus"
        };

        var handler = new StubHttpMessageHandler((request, body) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "https://homologacao.focusnfe.com.br/v2/nfce?ref=" + documento.Id.ToString("N") + "&completa=1",
                request.RequestUri?.ToString());
            Assert.Contains("\"formas_pagamento\"", body);
            Assert.Contains("\"forma_pagamento\":\"17\"", body);
            Assert.Contains("\"presenca_comprador\":\"1\"", body);

            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(
                    """
                    {
                      "status": "processando_autorizacao",
                      "caminho_xml_nota_fiscal": "/arquivos/nfce/55.xml",
                      "caminho_danfce": "/arquivos/nfce/55.pdf"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new FocusNfeDfeProviderClient(new HttpClient(handler), context);

        var resultado = await client.EmitirAsync(configuracao, credencial, documento);

        Assert.True(resultado.Sucesso);
        Assert.Equal("PROCESSANDO_AUTORIZACAO", resultado.Status);
        Assert.Equal(documento.Id.ToString("N"), resultado.NumeroExterno);
        Assert.Equal("https://homologacao.focusnfe.com.br/arquivos/nfce/55.xml", resultado.XmlUrl);
        Assert.Equal("https://homologacao.focusnfe.com.br/arquivos/nfce/55.pdf", resultado.PdfUrl);
    }

    [Fact]
    public async Task SolicitarReenvioWebhookAsync_DeveChamarEndpointNfeDaFocus()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = CriarEmpresa();
        var venda = CriarVenda(empresa.Id, "BOLETO", 150m);

        context.Empresas.Add(empresa);
        context.Vendas.Add(venda);
        await context.SaveChangesAsync();

        var documento = CriarDocumentoNfe(empresa.Id, venda.Id);
        documento.NumeroExterno = "nfe-ref-42";

        var configuracao = new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            MunicipioCodigo = "3550308",
            RegimeTributario = "SimplesNacional"
        };

        var credencial = new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfe,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-focus"
        };

        var handler = new StubHttpMessageHandler((request, body) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "https://homologacao.focusnfe.com.br/v2/nfe/nfe-ref-42/hook",
                request.RequestUri?.ToString());
            Assert.Equal(string.Empty, body);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {
                        "id": "hook-1",
                        "event": "nfe",
                        "url": "https://api.exemplo.com.br/api/fiscal/webhooks/focus/dfe/segredo-webhook"
                      }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new FocusNfeDfeProviderClient(new HttpClient(handler), context);

        var resultado = await client.SolicitarReenvioWebhookAsync(configuracao, credencial, documento);

        Assert.True(resultado.Sucesso);
        Assert.Equal("REENVIO_SOLICITADO", resultado.Status);
        Assert.Equal("nfe-ref-42", resultado.NumeroExterno);
    }

    [Fact]
    public async Task SolicitarReenvioWebhookAsync_DeveFalharParaNfce()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = CriarEmpresa();
        var venda = CriarVenda(empresa.Id, "PIX", 89.9m);

        context.Empresas.Add(empresa);
        context.Vendas.Add(venda);
        await context.SaveChangesAsync();

        var documento = CriarDocumentoNfce(empresa.Id, venda.Id);
        var configuracao = new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            MunicipioCodigo = "3550308",
            RegimeTributario = "SimplesNacional"
        };

        var credencial = new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfce,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-focus"
        };

        var client = new FocusNfeDfeProviderClient(new HttpClient(new StubHttpMessageHandler((request, body) =>
        {
            throw new InvalidOperationException("Nao deveria chamar HTTP para NFC-e.");
        })), context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SolicitarReenvioWebhookAsync(configuracao, credencial, documento));

        Assert.Contains("NFC-e", ex.Message);
    }

    private static Empresa CriarEmpresa()
    {
        return new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Focus Ltda",
            NomeFantasia = "Empresa Focus",
            Cnpj = "12345678000190",
            InscricaoEstadual = "123456789",
            Logradouro = "Rua das Flores",
            Numero = "100",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Uf = "SP",
            Cep = "01001000",
            RegimeTributario = "SimplesNacional",
            Ativo = true
        };
    }

    private static Venda CriarVenda(Guid empresaId, string formaPagamento, decimal valorTotal)
    {
        return new Venda
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Status = "FECHADA",
            FormaPagamento = formaPagamento,
            Subtotal = valorTotal,
            Desconto = 0m,
            ValorTotal = valorTotal
        };
    }

    private static DocumentoFiscal CriarDocumentoNfe(Guid empresaId, Guid vendaId)
    {
        return new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumento = TipoDocumentoFiscal.Nfe,
            OrigemTipo = OrigemDocumentoFiscal.Venda,
            OrigemId = vendaId,
            Numero = 42,
            Serie = 1,
            Status = StatusDocumentoFiscal.PendenteEnvio,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente Focus",
            ClienteCpfCnpj = "12345678901",
            ClienteCep = "01001000",
            ClienteLogradouro = "Rua do Cliente",
            ClienteNumero = "200",
            ClienteBairro = "Centro",
            ClienteCidade = "Sao Paulo",
            ClienteUf = "SP",
            ClienteMunicipioCodigo = "3550308",
            DataEmissao = DateTime.UtcNow,
            ValorProdutos = 150m,
            ValorTotal = 150m,
            Itens =
            [
                new DocumentoFiscalItem
                {
                    Id = Guid.NewGuid(),
                    EmpresaId = empresaId,
                    TipoItem = TipoItemFiscal.Produto,
                    Descricao = "Peca Focus",
                    Quantidade = 1,
                    ValorUnitario = 150m,
                    ValorTotal = 150m,
                    Ncm = "84733049",
                    Cfop = "5102",
                    CstCsosn = "102",
                    OrigemMercadoria = "0"
                }
            ]
        };
    }

    private static DocumentoFiscal CriarDocumentoNfce(Guid empresaId, Guid vendaId)
    {
        return new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumento = TipoDocumentoFiscal.Nfce,
            OrigemTipo = OrigemDocumentoFiscal.Venda,
            OrigemId = vendaId,
            Numero = 55,
            Serie = 1,
            Status = StatusDocumentoFiscal.PendenteEnvio,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente NFCe",
            ClienteCpfCnpj = "12345678901",
            ClienteCidade = "Sao Paulo",
            ClienteUf = "SP",
            DataEmissao = DateTime.UtcNow,
            ValorProdutos = 89.9m,
            ValorTotal = 89.9m,
            Itens =
            [
                new DocumentoFiscalItem
                {
                    Id = Guid.NewGuid(),
                    EmpresaId = empresaId,
                    TipoItem = TipoItemFiscal.Produto,
                    Descricao = "Produto NFCe",
                    Quantidade = 1,
                    ValorUnitario = 89.9m,
                    ValorTotal = 89.9m,
                    Ncm = "84733049",
                    Cfop = "5102",
                    CstCsosn = "102",
                    OrigemMercadoria = "0"
                }
            ]
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, string, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responder(request, body);
        }
    }
}
