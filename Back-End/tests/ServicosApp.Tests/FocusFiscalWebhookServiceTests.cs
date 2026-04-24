using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FocusFiscalWebhookServiceTests
{
    [Fact]
    public async Task ProcessDfeAsync_DeveLocalizarPorReferenciaEConsultarDocumento()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresa = CriarEmpresa("88888888000188", "Empresa Hook");
        var documento = CriarDocumentoFiscal(
            empresa.Id,
            TipoDocumentoFiscal.Nfe,
            StatusDocumentoFiscal.PendenteEnvio,
            "focus-ref-001");

        context.Empresas.Add(empresa);
        context.DocumentosFiscais.Add(documento);
        await context.SaveChangesAsync();

        var dfeService = new SpyDfeVendaService();
        var nfseService = new SpyNfseService();
        var webhookService = new FocusFiscalWebhookService(
            context,
            dfeService,
            nfseService,
            Options.Create(new FocusWebhookOptions
            {
                Enabled = true,
                Secret = "segredo-focus"
            }),
            NullLogger<FocusFiscalWebhookService>.Instance);

        using var json = JsonDocument.Parse(
            """
            {
              "referencia": "focus-ref-001",
              "status": "autorizado"
            }
            """);

        var result = await webhookService.ProcessDfeAsync(json.RootElement);

        Assert.True(result.Processed);
        Assert.Equal(documento.Id, result.DocumentoFiscalId);
        Assert.Equal(empresa.Id, result.EmpresaId);
        Assert.Equal(nameof(StatusDocumentoFiscal.PendenteEnvio), result.StatusBefore);
        Assert.Equal(nameof(StatusDocumentoFiscal.Autorizado), result.StatusAfter);
        Assert.Equal(documento.Id, dfeService.LastDocumentId);
        Assert.Null(nfseService.LastDocumentId);
    }

    [Fact]
    public async Task ProcessNfseAsync_SemReferencia_DeveIgnorarSemConsultar()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var webhookService = new FocusFiscalWebhookService(
            context,
            new SpyDfeVendaService(),
            new SpyNfseService(),
            Options.Create(new FocusWebhookOptions
            {
                Enabled = true,
                Secret = "segredo-focus"
            }),
            NullLogger<FocusFiscalWebhookService>.Instance);

        using var json = JsonDocument.Parse("""{ "status": "autorizado" }""");
        var result = await webhookService.ProcessNfseAsync(json.RootElement);

        Assert.False(result.Processed);
        Assert.Equal("Payload recebido sem referencia do documento.", result.IgnoredReason);
    }

    [Fact]
    public void IsRequestAuthorized_DeveValidarSegredoConfigurado()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        using var _ = context;
        using var __ = connection;

        var webhookService = new FocusFiscalWebhookService(
            context,
            new SpyDfeVendaService(),
            new SpyNfseService(),
            Options.Create(new FocusWebhookOptions
            {
                Enabled = true,
                Secret = "segredo-focus"
            }),
            NullLogger<FocusFiscalWebhookService>.Instance);

        Assert.True(webhookService.IsRequestAuthorized("segredo-focus"));
        Assert.False(webhookService.IsRequestAuthorized("segredo-invalido"));
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

    private static DocumentoFiscal CriarDocumentoFiscal(
        Guid empresaId,
        TipoDocumentoFiscal tipoDocumento,
        StatusDocumentoFiscal status,
        string reference)
    {
        return new DocumentoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            TipoDocumento = tipoDocumento,
            OrigemTipo = tipoDocumento == TipoDocumentoFiscal.Nfse
                ? OrigemDocumentoFiscal.OrdemServico
                : OrigemDocumentoFiscal.Venda,
            OrigemId = Guid.NewGuid(),
            Numero = 1,
            Serie = 1,
            Status = status,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente Hook",
            DataEmissao = DateTime.UtcNow,
            ValorTotal = 10m,
            NumeroExterno = reference
        };
    }

    private sealed class SpyDfeVendaService : IDfeVendaService
    {
        public Guid? LastDocumentId { get; private set; }

        public Task<DocumentoFiscalDto> EmitirNfePorVendaAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid vendaId,
            EmitirDfeVendaDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentoFiscalDto> EmitirNfcePorVendaAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid vendaId,
            EmitirDfeVendaDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentoFiscalDto> ConsultarAsync(
            Guid empresaId,
            Guid documentoFiscalId,
            CancellationToken cancellationToken = default)
        {
            LastDocumentId = documentoFiscalId;

            return Task.FromResult(new DocumentoFiscalDto
            {
                Id = documentoFiscalId,
                EmpresaId = empresaId,
                Status = nameof(StatusDocumentoFiscal.Autorizado)
            });
        }

        public Task<DocumentoFiscalDto> CancelarAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid documentoFiscalId,
            CancelarDocumentoFiscalDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentoFiscalWebhookReplayDto> SolicitarReenvioWebhookAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid documentoFiscalId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class SpyNfseService : INfseService
    {
        public Guid? LastDocumentId { get; private set; }

        public Task<DocumentoFiscalDto> EmitirPorOrdemServicoAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid ordemServicoId,
            EmitirNfsePorOsDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<DocumentoFiscalDto>> ListarAsync(
            Guid empresaId,
            string? status,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentoFiscalDto?> ObterPorIdAsync(
            Guid empresaId,
            Guid documentoFiscalId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentoFiscalDto> ConsultarAsync(
            Guid empresaId,
            Guid documentoFiscalId,
            CancellationToken cancellationToken = default)
        {
            LastDocumentId = documentoFiscalId;

            return Task.FromResult(new DocumentoFiscalDto
            {
                Id = documentoFiscalId,
                EmpresaId = empresaId,
                Status = nameof(StatusDocumentoFiscal.Autorizado)
            });
        }

        public Task<DocumentoFiscalDto> CancelarAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid documentoFiscalId,
            CancelarDocumentoFiscalDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentoFiscalWebhookReplayDto> SolicitarReenvioWebhookAsync(
            Guid empresaId,
            Guid usuarioId,
            Guid documentoFiscalId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
