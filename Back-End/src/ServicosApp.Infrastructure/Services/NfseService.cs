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
    private readonly INfseProviderResolver _providerResolver;
    private readonly IFiscalCredentialSecretProtector _secretProtector;

    public NfseService(
        AppDbContext context,
        IDocumentoFiscalBuilderService builder,
        INfseProviderResolver providerResolver,
        IFiscalCredentialSecretProtector secretProtector)
    {
        _context = context;
        _builder = builder;
        _providerResolver = providerResolver;
        _secretProtector = secretProtector;
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

        var credencialEntity = await _context.Set<CredencialFiscalEmpresa>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencialEntity is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var credencial = _secretProtector.CloneForUse(credencialEntity);
        var providerClient = _providerResolver.Resolve(config, credencial);

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

        documento.GerarContaReceberQuandoAutorizar = dto.GerarContaReceber;

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

        var providerResult = await providerClient.EmitirAsync(
            config,
            credencial,
            documento,
            cancellationToken);

        job.RequestPayload = providerResult.RequestPayload;
        job.ResponsePayload = providerResult.ResponsePayload;
        job.ProcessadoEm = DateTime.UtcNow;

        if (providerResult.Sucesso)
        {
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

            if (IsStatusAutorizado(providerResult.Status))
            {
                documento.Status = StatusDocumentoFiscal.Autorizado;
                documento.DataAutorizacao = DateTime.UtcNow;
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
                documento.Status = StatusDocumentoFiscal.PendenteEnvio;
                job.Status = "PROCESSANDO";

                await RegistrarEventoAsync(
                    empresaId,
                    documento.Id,
                    TipoEventoFiscal.Emissao,
                    StatusEventoFiscal.Processando,
                    "NFS-e enviada e aguardando autorização no provedor.",
                    usuarioId,
                    cancellationToken);
            }
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
        await GerarContaReceberSeConfiguradoAsync(empresaId, documento, cancellationToken);
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

        var credencialEntity = await _context.Set<CredencialFiscalEmpresa>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencialEntity is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var credencial = _secretProtector.CloneForUse(credencialEntity);
        var providerClient = _providerResolver.Resolve(config, credencial);

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

        var providerResult = await providerClient.ConsultarAsync(
            config,
            credencial,
            documento,
            cancellationToken);

        job.RequestPayload = providerResult.RequestPayload;
        job.ResponsePayload = providerResult.ResponsePayload;
        job.ProcessadoEm = DateTime.UtcNow;

        if (providerResult.Sucesso)
        {
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

            if (IsStatusAutorizado(providerResult.Status))
            {
                documento.Status = StatusDocumentoFiscal.Autorizado;
                documento.DataAutorizacao ??= DateTime.UtcNow;
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
            else if (IsStatusCancelado(providerResult.Status))
            {
                documento.Status = StatusDocumentoFiscal.Cancelado;
                documento.DataCancelamento ??= DateTime.UtcNow;
                job.Status = "SUCESSO";

                await RegistrarEventoAsync(
                    empresaId,
                    documento.Id,
                    TipoEventoFiscal.Consulta,
                    StatusEventoFiscal.Sucesso,
                    "Documento fiscal já consta como cancelado no provedor.",
                    documento.CreatedBy,
                    cancellationToken);
            }
            else
            {
                documento.Status = StatusDocumentoFiscal.PendenteEnvio;
                job.Status = "PROCESSANDO";

                await RegistrarEventoAsync(
                    empresaId,
                    documento.Id,
                    TipoEventoFiscal.Consulta,
                    StatusEventoFiscal.Processando,
                    "Documento fiscal ainda está em processamento no provedor.",
                    documento.CreatedBy,
                    cancellationToken);
            }
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
        await GerarContaReceberSeConfiguradoAsync(empresaId, documento, cancellationToken);
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

        var credencialEntity = await _context.Set<CredencialFiscalEmpresa>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencialEntity is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var credencial = _secretProtector.CloneForUse(credencialEntity);
        var providerClient = _providerResolver.Resolve(config, credencial);

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

        var providerResult = await providerClient.CancelarAsync(
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

    public async Task<DocumentoFiscalWebhookReplayDto> SolicitarReenvioWebhookAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await _context.DocumentosFiscais
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == documentoFiscalId &&
                x.TipoDocumento == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (documento is null)
            throw new KeyNotFoundException("Documento fiscal não encontrado.");

        var config = await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken);

        if (config is null)
            throw new InvalidOperationException("Configuração fiscal não encontrada.");

        var credencialEntity = await _context.Set<CredencialFiscalEmpresa>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == TipoDocumentoFiscal.Nfse,
                cancellationToken);

        if (credencialEntity is null)
            throw new InvalidOperationException("Credencial fiscal da NFS-e não encontrada.");

        var credencial = _secretProtector.CloneForUse(credencialEntity);
        var providerClient = _providerResolver.Resolve(config, credencial);

        var job = new IntegracaoFiscalJob
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documento.Id,
            TipoOperacao = "REENVIAR_WEBHOOK",
            Status = "PROCESSANDO",
            Tentativas = 1
        };

        _context.Set<IntegracaoFiscalJob>().Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        var providerResult = await providerClient.SolicitarReenvioWebhookAsync(
            config,
            credencial,
            documento,
            cancellationToken);

        job.RequestPayload = providerResult.RequestPayload;
        job.ResponsePayload = providerResult.ResponsePayload;
        job.ProcessadoEm = DateTime.UtcNow;
        job.Status = providerResult.Sucesso ? "SUCESSO" : "ERRO";
        job.MensagemErro = providerResult.Sucesso ? null : providerResult.MensagemErro;

        await RegistrarEventoAsync(
            empresaId,
            documento.Id,
            TipoEventoFiscal.Sincronizacao,
            providerResult.Sucesso ? StatusEventoFiscal.Sucesso : StatusEventoFiscal.Erro,
            providerResult.Sucesso
                ? "Reenvio do webhook solicitado ao provedor."
                : providerResult.MensagemErro ?? "Falha ao solicitar reenvio do webhook.",
            usuarioId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new DocumentoFiscalWebhookReplayDto
        {
            DocumentoFiscalId = documento.Id,
            TipoDocumento = documento.TipoDocumento.ToString(),
            ProviderCode = providerClient.ProviderCode,
            NumeroExterno = providerResult.NumeroExterno ?? documento.NumeroExterno ?? documento.Id.ToString("N"),
            StatusAtual = documento.Status.ToString(),
            ReenvioAceito = providerResult.Sucesso,
            Mensagem = providerResult.Sucesso
                ? "Reenvio do webhook solicitado. Se o hook estiver cadastrado corretamente, o status deve atualizar em instantes."
                : providerResult.MensagemErro ?? "Falha ao solicitar reenvio do webhook."
        };
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
        return GerarContaReceberInternoAsync(empresaId, ordemServicoId, documento, cancellationToken);
    }

    private async Task GerarContaReceberInternoAsync(
        Guid empresaId,
        Guid ordemServicoId,
        DocumentoFiscal documento,
        CancellationToken cancellationToken)
    {
        if (documento.ValorTotal <= 0)
            return;

        var jaGerouReceber = await _context.ContasReceber
            .AsNoTracking()
            .AnyAsync(
                x => x.EmpresaId == empresaId &&
                     x.OrigemTipo == "ORDEM_SERVICO" &&
                     x.OrigemId == ordemServicoId,
                cancellationToken);

        if (jaGerouReceber)
            return;

        var ordemServico = await _context.OrdensServico
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == ordemServicoId, cancellationToken);

        if (ordemServico is null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada.");

        var dataBase = DateOnly.FromDateTime(documento.DataAutorizacao ?? documento.DataEmissao);

        _context.ContasReceber.Add(new ContaReceber
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            ClienteId = ordemServico.ClienteId,
            OrigemTipo = "ORDEM_SERVICO",
            OrigemId = ordemServico.Id,
            Descricao = $"NFS-e da OS #{ordemServico.NumeroOs}",
            DataEmissao = dataBase,
            DataVencimento = dataBase,
            Valor = documento.ValorTotal,
            ValorRecebido = 0,
            Status = "PENDENTE",
            Observacoes = $"Gerado automaticamente ao autorizar NFS-e. Documento #{documento.Numero}.",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task GerarContaReceberSeConfiguradoAsync(
        Guid empresaId,
        DocumentoFiscal documento,
        CancellationToken cancellationToken)
    {
        if (documento.Status != StatusDocumentoFiscal.Autorizado ||
            !documento.GerarContaReceberQuandoAutorizar)
        {
            return;
        }

        await GerarContaReceberInternoAsync(
            empresaId,
            documento.OrigemId,
            documento,
            cancellationToken);

        documento.GerarContaReceberQuandoAutorizar = false;
        await _context.SaveChangesAsync(cancellationToken);
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
            GerarContaReceberQuandoAutorizar = x.GerarContaReceberQuandoAutorizar,
            XmlUrl = x.XmlUrl,
            PdfUrl = x.PdfUrl,
            CodigoRejeicao = x.CodigoRejeicao,
            MensagemRejeicao = x.MensagemRejeicao,
            MotivoCancelamento = x.MotivoCancelamento,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static bool IsStatusAutorizado(string? providerStatus)
        => string.Equals(
            NormalizeProviderStatus(providerStatus),
            "AUTORIZADO",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsStatusCancelado(string? providerStatus)
        => string.Equals(
            NormalizeProviderStatus(providerStatus),
            "CANCELADO",
            StringComparison.OrdinalIgnoreCase);

    private static string NormalizeProviderStatus(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
            return string.Empty;

        return providerStatus
            .Trim()
            .Replace('-', '_')
            .Replace(' ', '_')
            .ToUpperInvariant();
    }
}
