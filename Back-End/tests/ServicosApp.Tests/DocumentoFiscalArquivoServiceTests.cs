using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ServicosApp.Tests;

public class DocumentoFiscalArquivoServiceTests
{
    [Fact]
    public async Task ObterXmlAsync_DeveRespeitarEscopoDaEmpresa()
    {
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

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaA.Id,
            TipoDocumento = TipoDocumentoFiscal.Nfse,
            OrigemTipo = OrigemDocumentoFiscal.OrdemServico,
            OrigemId = Guid.NewGuid(),
            Numero = 10,
            Serie = 1,
            Status = StatusDocumentoFiscal.Autorizado,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente A",
            DataEmissao = DateTime.UtcNow,
            XmlConteudo = "<xml>empresa-a</xml>",
            ValorTotal = 100m
        };

        context.Empresas.AddRange(empresaA, empresaB);
        context.DocumentosFiscais.Add(documento);
        await context.SaveChangesAsync();

        var service = new DocumentoFiscalArquivoService(context);

        var resultadoEmpresaCorreta = await service.ObterXmlAsync(empresaA.Id, documento.Id);
        var resultadoEmpresaErrada = await service.ObterXmlAsync(empresaB.Id, documento.Id);

        Assert.NotNull(resultadoEmpresaCorreta);
        Assert.Equal("<xml>empresa-a</xml>", resultadoEmpresaCorreta!.Conteudo);
        Assert.Null(resultadoEmpresaErrada);
    }

    [Fact]
    public async Task ObterXmlAsync_DeveBaixarXmlOficialQuandoNaoEstiverPersistido()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Real Ltda",
            NomeFantasia = "Empresa Real",
            Cnpj = "33333333000133"
        };

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            TipoDocumento = TipoDocumentoFiscal.Nfse,
            OrigemTipo = OrigemDocumentoFiscal.OrdemServico,
            OrigemId = Guid.NewGuid(),
            Numero = 11,
            Serie = 1,
            Status = StatusDocumentoFiscal.Autorizado,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente Real",
            DataEmissao = DateTime.UtcNow,
            XmlUrl = "https://files.example.com/documento.xml",
            ValorTotal = 150m
        };

        context.Empresas.Add(empresa);
        context.DocumentosFiscais.Add(documento);
        await context.SaveChangesAsync();

        var httpClient = new HttpClient(new StubHttpMessageHandler(
            "https://files.example.com/documento.xml",
            "<xml>oficial</xml>"));

        var service = new DocumentoFiscalArquivoService(context, httpClient);

        var resultado = await service.ObterXmlAsync(empresa.Id, documento.Id);

        Assert.NotNull(resultado);
        Assert.Equal("<xml>oficial</xml>", resultado!.Conteudo);
        Assert.Equal("application/xml", resultado.ContentType);
    }

    [Fact]
    public async Task ObterImpressaoAsync_DeveExporPdfOficialQuandoDisponivel()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = "Empresa Impressao Ltda",
            NomeFantasia = "Empresa Impressao",
            Cnpj = "44444444000144"
        };

        var documento = new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresa.Id,
            TipoDocumento = TipoDocumentoFiscal.Nfse,
            OrigemTipo = OrigemDocumentoFiscal.OrdemServico,
            OrigemId = Guid.NewGuid(),
            Numero = 12,
            Serie = 1,
            Status = StatusDocumentoFiscal.Autorizado,
            Ambiente = AmbienteFiscal.Producao,
            ClienteNome = "Cliente PDF",
            DataEmissao = DateTime.UtcNow,
            PdfUrl = "https://files.example.com/oficial.pdf",
            ValorTotal = 250m
        };

        context.Empresas.Add(empresa);
        context.DocumentosFiscais.Add(documento);
        await context.SaveChangesAsync();

        var service = new DocumentoFiscalArquivoService(context);

        var resultado = await service.ObterImpressaoAsync(empresa.Id, documento.Id);

        Assert.NotNull(resultado);
        Assert.Equal("https://files.example.com/oficial.pdf", resultado!.OfficialPdfUrl);
    }

    private sealed class StubHttpMessageHandler(string expectedUrl, string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(expectedUrl, request.RequestUri?.ToString());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/xml")
            });
        }
    }
}
