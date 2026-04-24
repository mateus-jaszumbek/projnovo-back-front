using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.DTOs.Fiscal;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class DfeVendaService : IDfeVendaService
{
    private readonly AppDbContext _context;
    private readonly IDocumentoFiscalBuilderService _builder;
    private readonly IDfeProviderResolver _providerResolver;
    private readonly IFiscalCredentialSecretProtector _secretProtector;

    public DfeVendaService(
        AppDbContext context,
        IDocumentoFiscalBuilderService builder,
        IDfeProviderResolver providerResolver,
        IFiscalCredentialSecretProtector secretProtector)
    {
        _context = context;
        _builder = builder;
        _providerResolver = providerResolver;
        _secretProtector = secretProtector;
    }

    public Task<DocumentoFiscalDto> EmitirNfePorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        EmitirDfeVendaDto dto,
        CancellationToken cancellationToken = default)
    {
        return EmitirPorVendaAsync(
            empresaId,
            usuarioId,
            vendaId,
            TipoDocumentoFiscal.Nfe,
            dto,
            cancellationToken);
    }

    public Task<DocumentoFiscalDto> EmitirNfcePorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        EmitirDfeVendaDto dto,
        CancellationToken cancellationToken = default)
    {
        return EmitirPorVendaAsync(
            empresaId,
            usuarioId,
            vendaId,
            TipoDocumentoFiscal.Nfce,
            dto,
            cancellationToken);
    }

    public async Task<DocumentoFiscalDto> ConsultarAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await ObterDocumentoDfeAsync(empresaId, documentoFiscalId, cancellationToken);
        var config = await ObterConfiguracaoFiscalAsync(empresaId, cancellationToken);
        var credencial = await ObterCredencialAsync(empresaId, documento.TipoDocumento, cancellationToken);
        var providerClient = _providerResolver.Resolve(config, credencial);

        var job = CriarJob(empresaId, documento.Id, "CONSULTAR");
        _context.IntegracoesFiscaisJobs.Add(job);
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
        return DocumentoFiscalMapper.Map(documento);
    }

    public async Task<DocumentoFiscalDto> CancelarAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid documentoFiscalId,
        CancelarDocumentoFiscalDto dto,
        CancellationToken cancellationToken = default)
    {
        var documento = await ObterDocumentoDfeAsync(empresaId, documentoFiscalId, cancellationToken);

        if (documento.Status == StatusDocumentoFiscal.Cancelado)
            throw new InvalidOperationException("Documento fiscal já está cancelado.");

        if (documento.Status != StatusDocumentoFiscal.Autorizado)
            throw new InvalidOperationException("Somente documento autorizado pode ser cancelado.");

        var config = await ObterConfiguracaoFiscalAsync(empresaId, cancellationToken);
        var credencial = await ObterCredencialAsync(empresaId, documento.TipoDocumento, cancellationToken);
        var providerClient = _providerResolver.Resolve(config, credencial);

        var job = CriarJob(empresaId, documento.Id, "CANCELAR");
        _context.IntegracoesFiscaisJobs.Add(job);
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
            throw new InvalidOperationException(providerResult.MensagemErro ?? "Falha no cancelamento do documento fiscal.");
        }

        documento.Status = StatusDocumentoFiscal.Cancelado;
        documento.DataCancelamento = DateTime.UtcNow;
        documento.MotivoCancelamento = dto.Motivo;
        documento.PayloadEnvio = providerResult.RequestPayload;
        documento.PayloadRetorno = providerResult.ResponsePayload;
        documento.XmlConteudo = providerResult.XmlConteudo ?? documento.XmlConteudo;
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
        return DocumentoFiscalMapper.Map(documento);
    }

    public async Task<DocumentoFiscalWebhookReplayDto> SolicitarReenvioWebhookAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken = default)
    {
        var documento = await ObterDocumentoDfeAsync(empresaId, documentoFiscalId, cancellationToken);
        var config = await ObterConfiguracaoFiscalAsync(empresaId, cancellationToken);
        var credencial = await ObterCredencialAsync(empresaId, documento.TipoDocumento, cancellationToken);
        var providerClient = _providerResolver.Resolve(config, credencial);

        var job = CriarJob(empresaId, documento.Id, "REENVIAR_WEBHOOK");
        _context.IntegracoesFiscaisJobs.Add(job);
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

    private async Task<DocumentoFiscalDto> EmitirPorVendaAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid vendaId,
        TipoDocumentoFiscal tipoDocumento,
        EmitirDfeVendaDto dto,
        CancellationToken cancellationToken)
    {
        var jaExiste = await _context.DocumentosFiscais
            .AsNoTracking()
            .AnyAsync(
                x => x.EmpresaId == empresaId &&
                     x.TipoDocumento == tipoDocumento &&
                     x.OrigemTipo == OrigemDocumentoFiscal.Venda &&
                     x.OrigemId == vendaId &&
                     x.Status != StatusDocumentoFiscal.Cancelado,
                cancellationToken);

        if (jaExiste)
            throw new InvalidOperationException($"Já existe {tipoDocumento} ativa para essa venda.");

        var config = await ObterConfiguracaoFiscalAsync(empresaId, cancellationToken);
        var credencial = await ObterCredencialAsync(empresaId, tipoDocumento, cancellationToken);
        var providerClient = _providerResolver.Resolve(config, credencial);

        var documento = await _builder.CriarDfePorVendaAsync(
            empresaId,
            usuarioId,
            vendaId,
            tipoDocumento,
            dto.DataEmissao,
            dto.ObservacoesNota,
            dto.ValidarTributacaoCompleta,
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
            throw new InvalidOperationException($"Já existe {tipoDocumento} ativa para essa venda.");
        }

        await RegistrarEventoAsync(
            empresaId,
            documento.Id,
            TipoEventoFiscal.Emissao,
            StatusEventoFiscal.Processando,
            $"{tipoDocumento} criada e aguardando envio.",
            usuarioId,
            cancellationToken);

        var job = CriarJob(empresaId, documento.Id, "EMITIR");
        _context.IntegracoesFiscaisJobs.Add(job);
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
                    $"{tipoDocumento} autorizada com sucesso.",
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
                    $"{tipoDocumento} enviada e aguardando autorização no provedor.",
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
                providerResult.MensagemErro ?? $"Falha na emissão da {tipoDocumento}.",
                usuarioId,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await GerarContaReceberSeConfiguradoAsync(empresaId, documento, cancellationToken);
        return DocumentoFiscalMapper.Map(documento);
    }

    private async Task<DocumentoFiscal> ObterDocumentoDfeAsync(
        Guid empresaId,
        Guid documentoFiscalId,
        CancellationToken cancellationToken)
    {
        var documento = await _context.DocumentosFiscais
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == documentoFiscalId &&
                (x.TipoDocumento == TipoDocumentoFiscal.Nfe || x.TipoDocumento == TipoDocumentoFiscal.Nfce),
                cancellationToken);

        return documento ?? throw new KeyNotFoundException("Documento fiscal não encontrado.");
    }

    private async Task<ConfiguracaoFiscal> ObterConfiguracaoFiscalAsync(
        Guid empresaId,
        CancellationToken cancellationToken)
    {
        return await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Ativo, cancellationToken)
            ?? throw new InvalidOperationException("Configuração fiscal não encontrada.");
    }

    private async Task<CredencialFiscalEmpresa?> ObterCredencialAsync(
        Guid empresaId,
        TipoDocumentoFiscal tipoDocumento,
        CancellationToken cancellationToken)
    {
        var credencial = await _context.CredenciaisFiscaisEmpresas
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Ativo &&
                     x.TipoDocumentoFiscal == tipoDocumento,
                cancellationToken);

        return credencial is null ? null : _secretProtector.CloneForUse(credencial);
    }

    private static IntegracaoFiscalJob CriarJob(Guid empresaId, Guid documentoId, string operacao)
    {
        return new IntegracaoFiscalJob
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documentoId,
            TipoOperacao = operacao,
            Status = "PROCESSANDO",
            Tentativas = 1
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
        _context.EventosFiscais.Add(new EventoFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            DocumentoFiscalId = documentoFiscalId,
            TipoEvento = tipoEvento,
            Status = status,
            Mensagem = mensagem,
            CreatedBy = usuarioId
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task GerarContaReceberSeNecessarioAsync(
        Guid empresaId,
        Guid vendaId,
        DocumentoFiscal documento,
        CancellationToken cancellationToken)
    {
        if (documento.ValorTotal <= 0)
            return;

        var jaLancouCaixa = await _context.CaixaLancamentos
            .AsNoTracking()
            .AnyAsync(
                x => x.EmpresaId == empresaId &&
                     x.OrigemTipo == "VENDA" &&
                     x.OrigemId == vendaId,
                cancellationToken);

        var jaGerouReceber = await _context.ContasReceber
            .AsNoTracking()
            .AnyAsync(
                x => x.EmpresaId == empresaId &&
                     x.OrigemTipo == "VENDA" &&
                     x.OrigemId == vendaId,
                cancellationToken);

        if (jaLancouCaixa || jaGerouReceber)
            return;

        var venda = await _context.Vendas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == vendaId, cancellationToken);

        if (venda is null)
            throw new KeyNotFoundException("Venda não encontrada.");

        var dataBase = DateOnly.FromDateTime(documento.DataAutorizacao ?? documento.DataEmissao);
        var tipoDescricao = documento.TipoDocumento == TipoDocumentoFiscal.Nfe ? "NF-e" : "NFC-e";
        var formaPagamento = string.IsNullOrWhiteSpace(venda.FormaPagamento)
            ? null
            : venda.FormaPagamento.Trim().ToUpperInvariant();

        _context.ContasReceber.Add(new ContaReceber
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            ClienteId = venda.ClienteId,
            OrigemTipo = "VENDA",
            OrigemId = venda.Id,
            Descricao = $"{tipoDescricao} da venda #{venda.NumeroVenda}",
            DataEmissao = dataBase,
            DataVencimento = dataBase,
            Valor = documento.ValorTotal,
            ValorRecebido = 0,
            Status = "PENDENTE",
            FormaPagamento = formaPagamento,
            Observacoes = $"Gerado automaticamente ao autorizar {tipoDescricao}. Documento #{documento.Numero}.",
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

        await GerarContaReceberSeNecessarioAsync(
            empresaId,
            documento.OrigemId,
            documento,
            cancellationToken);

        documento.GerarContaReceberQuandoAutorizar = false;
        await _context.SaveChangesAsync(cancellationToken);
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

