using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Services;

namespace ServicosApp.Tests;

public class FiscalPendingSyncServiceTests
{
    [Fact]
    public async Task SynchronizePendingAsync_DeveProcessarPendentesPorTipoEContabilizarResultados()
    {
        var (context, connection) = TestDbFactory.CreateContext();
        await using var _ = context;
        await using var __ = connection;

        var empresaA = CriarEmpresa("66666666000166", "Empresa Sync A");
        var empresaB = CriarEmpresa("77777777000177", "Empresa Sync B");

        var dfeAutorizado = CriarDocumentoFiscal(
            empresaA.Id,
            TipoDocumentoFiscal.Nfce,
            StatusDocumentoFiscal.PendenteEnvio,
            10);

        var nfsePendente = CriarDocumentoFiscal(
            empresaB.Id,
            TipoDocumentoFiscal.Nfse,
            StatusDocumentoFiscal.PendenteEnvio,
            11);

        var dfeComFalha = CriarDocumentoFiscal(
            empresaA.Id,
            TipoDocumentoFiscal.Nfe,
            StatusDocumentoFiscal.PendenteEnvio,
            12);

        var jaAutorizado = CriarDocumentoFiscal(
            empresaA.Id,
            TipoDocumentoFiscal.Nfce,
            StatusDocumentoFiscal.Autorizado,
            13);

        context.Empresas.AddRange(empresaA, empresaB);
        context.DocumentosFiscais.AddRange(dfeAutorizado, nfsePendente, dfeComFalha, jaAutorizado);
        await context.SaveChangesAsync();

        var dfeService = new SpyDfeVendaService([dfeComFalha.Id]);
        var nfseService = new SpyNfseService();
        var syncService = new FiscalPendingSyncService(
            context,
            dfeService,
            nfseService,
            Options.Create(new FiscalPendingSyncOptions
            {
                Enabled = true,
                BatchSize = 10,
                CooldownSeconds = 0
            }),
            NullLogger<FiscalPendingSyncService>.Instance);

        var result = await syncService.SynchronizePendingAsync();

        Assert.True(result.Enabled);
        Assert.Equal(3, result.ScannedCount);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(1, result.AuthorizedCount);
        Assert.Equal(1, result.StillPendingCount);
        Assert.Equal(0, result.CancelledCount);
        Assert.Equal(1, result.FailedCount);

        Assert.Contains(dfeAutorizado.Id, dfeService.ConsultedDocumentIds);
        Assert.Contains(dfeComFalha.Id, dfeService.ConsultedDocumentIds);
        Assert.Contains(nfsePendente.Id, nfseService.ConsultedDocumentIds);
        Assert.DoesNotContain(jaAutorizado.Id, dfeService.ConsultedDocumentIds);
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
        long numero)
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
            Numero = numero,
            Serie = 1,
            Status = status,
            Ambiente = AmbienteFiscal.Homologacao,
            ClienteNome = "Cliente Sync",
            DataEmissao = DateTime.UtcNow,
            ValorTotal = 100m
        };
    }

    private sealed class SpyDfeVendaService(HashSet<Guid> failingDocumentIds) : IDfeVendaService
    {
        public List<Guid> ConsultedDocumentIds { get; } = [];

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
            ConsultedDocumentIds.Add(documentoFiscalId);

            if (failingDocumentIds.Contains(documentoFiscalId))
                throw new InvalidOperationException("Falha simulada no provider.");

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
        public List<Guid> ConsultedDocumentIds { get; } = [];

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
            ConsultedDocumentIds.Add(documentoFiscalId);

            return Task.FromResult(new DocumentoFiscalDto
            {
                Id = documentoFiscalId,
                EmpresaId = empresaId,
                Status = nameof(StatusDocumentoFiscal.PendenteEnvio)
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
