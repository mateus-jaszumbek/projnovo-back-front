using System.Net;
using System.Net.Http;
using System.Text;
using ServicosApp.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FocusNfeNfseProviderClientTests
{
    [Fact]
    public async Task EmitirAsync_DeveMapearUrlsEStatusDoFocus()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Focus Ltda",
            NomeFantasia = "Empresa Focus",
            Cnpj = "12345678000190",
            InscricaoMunicipal = "998877",
            RegimeTributario = "SimplesNacional",
            Ativo = true
        };

        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            TipoDocumento = TipoDocumentoFiscal.Nfse,
            OrigemTipo = OrigemDocumentoFiscal.OrdemServico,
            OrigemId = Guid.NewGuid(),
            Numero = 42,
            Serie = 1,
            Status = StatusDocumentoFiscal.PendenteEnvio,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente Focus",
            ClienteCpfCnpj = "12345678901",
            DataEmissao = DateTime.UtcNow,
            ValorServicos = 150m,
            ValorTotal = 150m
        };

        var configuracao = new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            MunicipioCodigo = "3550308",
            CnaePrincipal = "9511800",
            ItemListaServico = "14.01",
            CodigoTributarioMunicipio = "1401",
            AliquotaIssPadrao = 2.5m,
            RegimeTributario = "SimplesNacional"
        };

        var credencial = new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-focus"
        };

        var handler = new StubHttpMessageHandler((request, body) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://homologacao.focusnfe.com.br/v2/nfse?ref=" + documento.Id.ToString("N"), request.RequestUri?.ToString());
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            Assert.Contains("\"prestador\"", body);
            Assert.Contains("\"servico\"", body);
            Assert.Contains("\"codigo_tributario_municipio\":\"1401\"", body);

            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(
                    """
                    {
                      "status": "processando_autorizacao",
                      "codigo_verificacao": "ABC12345",
                      "url": "https://api.focusnfe.com.br/nfse/espelho/42",
                      "url_danfse": "https://files.focusnfe.com.br/notas/42.pdf",
                      "caminho_xml_nota_fiscal": "/arquivos/notas/42.xml"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new FocusNfeNfseProviderClient(
            new HttpClient(handler),
            context,
            new StubFocusNfseMunicipioService());

        var resultado = await client.EmitirAsync(configuracao, credencial, documento);

        Assert.True(resultado.Sucesso);
        Assert.Equal("PROCESSANDO_AUTORIZACAO", resultado.Status);
        Assert.Equal(documento.Id.ToString("N"), resultado.NumeroExterno);
        Assert.Equal("ABC12345", resultado.CodigoVerificacao);
        Assert.Equal("https://files.focusnfe.com.br/notas/42.pdf", resultado.PdfUrl);
        Assert.Equal("https://homologacao.focusnfe.com.br/arquivos/notas/42.xml", resultado.XmlUrl);
    }

    [Fact]
    public async Task SolicitarReenvioWebhookAsync_DeveChamarEndpointDaFocus()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Focus Ltda",
            NomeFantasia = "Empresa Focus",
            Cnpj = "12345678000190",
            InscricaoMunicipal = "998877",
            RegimeTributario = "SimplesNacional",
            Ativo = true
        };

        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            TipoDocumento = TipoDocumentoFiscal.Nfse,
            OrigemTipo = OrigemDocumentoFiscal.OrdemServico,
            OrigemId = Guid.NewGuid(),
            Numero = 42,
            Serie = 1,
            NumeroExterno = "nfse-ref-42",
            Status = StatusDocumentoFiscal.PendenteEnvio,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente Focus",
            DataEmissao = DateTime.UtcNow,
            ValorServicos = 150m,
            ValorTotal = 150m
        };

        var configuracao = new ConfiguracaoFiscal
        {
            EmpresaId = empresa.Id,
            Ambiente = AmbienteFiscal.Homologacao,
            MunicipioCodigo = "3550308",
            CnaePrincipal = "9511800",
            ItemListaServico = "14.01",
            CodigoTributarioMunicipio = "1401",
            AliquotaIssPadrao = 2.5m,
            RegimeTributario = "SimplesNacional"
        };

        var credencial = new CredencialFiscalEmpresa
        {
            EmpresaId = empresa.Id,
            TipoDocumentoFiscal = TipoDocumentoFiscal.Nfse,
            Provedor = FiscalProviderCodes.FocusNfe,
            TokenAcesso = "token-focus"
        };

        var handler = new StubHttpMessageHandler((request, body) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "https://homologacao.focusnfe.com.br/v2/nfse/nfse-ref-42/hook",
                request.RequestUri?.ToString());
            Assert.Equal(string.Empty, body);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {
                        "id": "hook-1",
                        "event": "nfse",
                        "url": "https://api.exemplo.com.br/api/fiscal/webhooks/focus/nfse/segredo-webhook"
                      }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new FocusNfeNfseProviderClient(
            new HttpClient(handler),
            context,
            new StubFocusNfseMunicipioService());

        var resultado = await client.SolicitarReenvioWebhookAsync(configuracao, credencial, documento);

        Assert.True(resultado.Sucesso);
        Assert.Equal("REENVIO_SOLICITADO", resultado.Status);
        Assert.Equal("nfse-ref-42", resultado.NumeroExterno);
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

    private sealed class StubFocusNfseMunicipioService : IFocusNfseMunicipioService
    {
        public Task<FocusNfseMunicipioValidacaoDto> ValidarAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FocusNfseMunicipioValidacaoDto
            {
                ProviderCode = FiscalProviderCodes.FocusNfe,
                PodeEmitirNfse = true
            });
        }
    }
}
