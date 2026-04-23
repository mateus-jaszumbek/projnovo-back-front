using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class NfseService : INfseService
{
    private readonly AppDbContext _context;
    private readonly IDocumentoFiscalBuilderService _builder;
    private readonly INfseProviderClient _providerClient;

    public NfseService(
        AppDbContext context,
        IDocumentoFiscalBuilderService builder,
        INfseProviderClient providerClient)
    {
        _context = context;
        _builder = builder;
        _providerClient = providerClient;
    }

    public async Task<DocumentoFiscalDto> EmitirPorOrdemServicoAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid ordemServicoId,
        EmitirNfsePorOsDto dto,
        CancellationToken cancellationToken = default)
    {
        var config = await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (config is null)
            throw new InvalidOperationException("Configuração fiscal não encontrada.");

        var credencial = await _context.Set<CredencialFiscalEmpresa>()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencial is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var jaExiste = await _context.DocumentosFiscais
            .AsNoTracking()
            .AnyAsync(
                x => x.EmpresaId == empresaId &&
                     x.TipoDocumento == TipoDocumentoFiscal.Nfse &&
                     x.OrigemTipo == OrigemDocumentoFiscal.OrdemServico &&
                     x.OrigemId == ordemServicoId &&
                     x.Status != StatusDocumentoFiscal.Cancelado,
                cancellationToken);

        if (jaExiste)
            throw new InvalidOperationException("Já existe NFS-e ativa para essa ordem de serviço.");

        var documento = await _builder.CriarNfsePorOrdemServicoAsync(
            empresaId,
            usuarioId,
            ordemServicoId,
            dto.DataCompetencia,
            dto.ObservacoesNota,
            cancellationToken);

        _context.DocumentosFiscais.Add(documento);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqliteException sqliteEx &&
            sqliteEx.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Já existe NFS-e ativa para essa ordem de serviço.");
        }

        await RegistrarEventoAsync(
            empresaId,
            documento.Id,
            TipoEventoFiscal.Emissao,
            StatusEventoFiscal.Processando,
            "Documento fiscal criado e aguardando envio.",
            usuarioId,
            cancellationToken);

        var job = new IntegracaoFiscalJob
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documento.Id,
            TipoOperacao = "EMITIR",
            Status = "PROCESSANDO",
            Tentativas = 1
        };

        _context.Set<IntegracaoFiscalJob>().Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        var providerResult = await _providerClient.EmitirAsync(
            config,
            credencial,
            documento,
            cancellationToken);

        job.RequestPayload = providerResult.RequestPayload;
        job.ResponsePayload = providerResult.ResponsePayload;
        job.ProcessadoEm = DateTime.UtcNow;

        if (providerResult.Sucesso)
        {
            documento.Status = StatusDocumentoFiscal.Autorizado;
            documento.DataAutorizacao = DateTime.UtcNow;
            documento.ChaveAcesso = providerResult.ChaveAcesso;
            documento.Protocolo = providerResult.Protocolo;
            documento.CodigoVerificacao = providerResult.CodigoVerificacao;
            documento.LinkConsulta = providerResult.LinkConsulta;
            documento.NumeroExterno = providerResult.NumeroExterno;
            documento.Lote = providerResult.Lote;
            documento.XmlConteudo = providerResult.XmlConteudo;
            documento.XmlUrl = providerResult.XmlUrl;
            documento.PdfUrl = providerResult.PdfUrl;
            documento.PayloadEnvio = providerResult.RequestPayload;
            documento.PayloadRetorno = providerResult.ResponsePayload;

            job.Status = "SUCESSO";

            await RegistrarEventoAsync(
                empresaId,
                documento.Id,
                TipoEventoFiscal.Emissao,
                StatusEventoFiscal.Sucesso,
                "NFS-e autorizada com sucesso.",
                usuarioId,
                cancellationToken);
        }
        else
        {
            documento.Status = StatusDocumentoFiscal.Rejeitado;
            documento.CodigoRejeicao = providerResult.CodigoErro;
            documento.MensagemRejeicao = providerResult.MensagemErro;
            documento.PayloadEnvio = providerResult.RequestPayload;
            documento.PayloadRetorno = providerResult.ResponsePayload;

            job.Status = "ERRO";
            job.MensagemErro = providerResult.MensagemErro;

            await RegistrarEventoAsync(
                empresaId,
                documento.Id,
                TipoEventoFiscal.Rejeicao,
                StatusEventoFiscal.Erro,
                providerResult.MensagemErro ?? "Falha na emissão da NFS-e.",
                usuarioId,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (providerResult.Sucesso && dto.GerarContaReceber)
        {
            await GerarContaReceberSeNecessarioAsync(
                empresaId,
                ordemServicoId,
                documento,
                usuarioId,
                cancellationToken);
        }

        return Map(documento);
    }

    public async Task<List<DocumentoFiscalDto>> ListarAsync(
        Guid empresaId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentosFiscais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.TipoDocumento == TipoDocumentoFiscal.Nfse);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<StatusDocumentoFiscal>(status.Trim(), true, out var statusEnum))
                throw new InvalidOperationException($"Status fiscal inválido: {status}");

            query = query.Where(x => x.Status == statusEnum);
        }

        var documentos = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return documentos.Select(Map).ToList();
    }

    public async Task<DocumentoFiscalDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == documentoFiscalId, cancellationToken);

        return documento is null ? null : Map(documento);
    }

    public async Task<DocumentoFiscalDto> ConsultarAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == documentoFiscalId, cancellationToken);

        if (documento is null)
            throw new KeyNotFoundException("Documento fiscal não encontrado.");

        var config = await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (config is null)
            throw new InvalidOperationException("Configuração fiscal não encontrada.");

        var credencial = await _context.Set<CredencialFiscalEmpresa>()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencial is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var job = new IntegracaoFiscalJob
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documento.Id,
            TipoOperacao = "CONSULTAR",
            Status = "PROCESSANDO",
            Tentativas = 1
        };

        _context.Set<IntegracaoFiscalJob>().Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        var providerResult = await _providerClient.ConsultarAsync(
            config,
            credencial,
            documento,
            cancellationToken);

        job.RequestPayload = providerResult.RequestPayload;
        job.ResponsePayload = providerResult.ResponsePayload;
        job.ProcessadoEm = DateTime.UtcNow;

        if (providerResult.Sucesso)
        {
            documento.Status = StatusDocumentoFiscal.Autorizado;
            documento.DataAutorizacao ??= DateTime.UtcNow;
            documento.ChaveAcesso = providerResult.ChaveAcesso ?? documento.ChaveAcesso;
            documento.Protocolo = providerResult.Protocolo ?? documento.Protocolo;
            documento.CodigoVerificacao = providerResult.CodigoVerificacao ?? documento.CodigoVerificacao;
            documento.LinkConsulta = providerResult.LinkConsulta ?? documento.LinkConsulta;
            documento.NumeroExterno = providerResult.NumeroExterno ?? documento.NumeroExterno;
            documento.Lote = providerResult.Lote ?? documento.Lote;
            documento.XmlConteudo = providerResult.XmlConteudo ?? documento.XmlConteudo;
            documento.XmlUrl = providerResult.XmlUrl ?? documento.XmlUrl;
            documento.PdfUrl = providerResult.PdfUrl ?? documento.PdfUrl;
            documento.PayloadEnvio = providerResult.RequestPayload;
            documento.PayloadRetorno = providerResult.ResponsePayload;

            job.Status = "SUCESSO";

            await RegistrarEventoAsync(
                empresaId,
                documento.Id,
                TipoEventoFiscal.Consulta,
                StatusEventoFiscal.Sucesso,
                "Consulta realizada com sucesso.",
                documento.CreatedBy,
                cancellationToken);
        }
        else
        {
            job.Status = "ERRO";
            job.MensagemErro = providerResult.MensagemErro;

            await RegistrarEventoAsync(
                empresaId,
                documento.Id,
                TipoEventoFiscal.Consulta,
                StatusEventoFiscal.Erro,
                providerResult.MensagemErro ?? "Falha na consulta.",
                documento.CreatedBy,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Map(documento);
    }

    public async Task<DocumentoFiscalDto> CancelarAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid documentoFiscalId,
        CancelarDocumentoFiscalDto dto,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == documentoFiscalId, cancellationToken);

        if (documento is null)
            throw new KeyNotFoundException("Documento fiscal não encontrado.");

        if (documento.Status == StatusDocumentoFiscal.Cancelado)
            throw new InvalidOperationException("Documento fiscal já está cancelado.");

        if (documento.Status != StatusDocumentoFiscal.Autorizado)
            throw new InvalidOperationException("Somente documento autorizado pode ser cancelado.");

        var config = await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (config is null)
            throw new InvalidOperationException("Configuração fiscal não encontrada.");

        var credencial = await _context.Set<CredencialFiscalEmpresa>()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencial is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var job = new IntegracaoFiscalJob
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documento.Id,
            TipoOperacao = "CANCELAR",
            Status = "PROCESSANDO",
            Tentativas = 1
        };

        _context.Set<IntegracaoFiscalJob>().Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        var providerResult = await _providerClient.CancelarAsync(
            config,
            credencial,
            documento,
            dto.Motivo,
            cancellationToken);

        job.RequestPayload = providerResult.RequestPayload;
        job.ResponsePayload = providerResult.ResponsePayload;
        job.ProcessadoEm = DateTime.UtcNow;

        if (!providerResult.Sucesso)
        {
            job.Status = "ERRO";
            job.MensagemErro = providerResult.MensagemErro;

            await RegistrarEventoAsync(
                empresaId,
                documento.Id,
                TipoEventoFiscal.Cancelamento,
                StatusEventoFiscal.Erro,
                providerResult.MensagemErro ?? "Falha no cancelamento.",
                usuarioId,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException(providerResult.MensagemErro ?? "Falha no cancelamento da NFS-e.");
        }

        documento.Status = StatusDocumentoFiscal.Cancelado;
        documento.DataCancelamento = DateTime.UtcNow;
        documento.MotivoCancelamento = dto.Motivo;
        documento.PayloadEnvio = providerResult.RequestPayload;
        documento.PayloadRetorno = providerResult.ResponsePayload;

        job.Status = "SUCESSO";

        await RegistrarEventoAsync(
            empresaId,
            documento.Id,
            TipoEventoFiscal.Cancelamento,
            StatusEventoFiscal.Sucesso,
            "Documento fiscal cancelado com sucesso.",
            usuarioId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Map(documento);
    }

    private async Task RegistrarEventoAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        TipoEventoFiscal tipoEvento,
        StatusEventoFiscal status,
        string mensagem,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        var evento = new EventoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documentoFiscalId,
            TipoEvento = tipoEvento,
            Status = status,
            Mensagem = mensagem,
            CreatedBy = usuarioId
        };

        _context.EventosFiscais.Add(evento);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task GerarContaReceberSeNecessarioAsync(
        Guid empresaId,
        Guid ordemServicoId,
        DocumentoFiscal documento,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static DocumentoFiscalDto Map(DocumentoFiscal x)
    {
        return new DocumentoFiscalDto
        {
            Id = x.Id,
            EmpresaId = x.EmpresaId,
            TipoDocumento = x.TipoDocumento.ToString(),
            OrigemTipo = x.OrigemTipo.ToString(),
            OrigemId = x.OrigemId,
            Numero = x.Numero,
            Serie = x.Serie,
            SerieRps = x.SerieRps,
            NumeroRps = x.NumeroRps,
            Status = x.Status.ToString(),
            Ambiente = x.Ambiente.ToString(),
            ClienteId = x.ClienteId,
            ClienteNome = x.ClienteNome,
            ClienteCpfCnpj = x.ClienteCpfCnpj,
            DataEmissao = x.DataEmissao,
            DataCompetencia = x.DataCompetencia,
            DataAutorizacao = x.DataAutorizacao,
            DataCancelamento = x.DataCancelamento,
            ChaveAcesso = x.ChaveAcesso,
            Protocolo = x.Protocolo,
            CodigoVerificacao = x.CodigoVerificacao,
            LinkConsulta = x.LinkConsulta,
            NumeroExterno = x.NumeroExterno,
            Lote = x.Lote,
            ValorServicos = x.ValorServicos,
            ValorProdutos = x.ValorProdutos,
            Desconto = x.Desconto,
            ValorTotal = x.ValorTotal,
            XmlUrl = x.XmlUrl,
            PdfUrl = x.PdfUrl,
            CodigoRejeicao = x.CodigoRejeicao,
            MensagemRejeicao = x.MensagemRejeicao,
            MotivoCancelamento = x.MotivoCancelamento,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}