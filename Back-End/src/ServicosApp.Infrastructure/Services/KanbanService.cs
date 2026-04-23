using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class KanbanService : IKanbanService
{
    private readonly AppDbContext _context;

    public KanbanService(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // PÚBLICO
    // =========================

    public async Task<List<KanbanPublicoColunaDto>> ObterQuadroPublicoAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPublicoAsync(empresaId, cancellationToken);
        await GarantirCardsPublicosDasOrdensServicoAsync(empresaId, fluxo.Id, cancellationToken);
        await ArquivarFinalizadosAposPrazoAsync(empresaId, fluxo.Id, cancellationToken);

        var colunas = await _context.KanbanColunas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id && x.Ativa)
            .OrderBy(x => x.Ordem)
            .Select(x => new KanbanPublicoColunaDto
            {
                Id = x.Id,
                NomeInterno = x.NomeInterno,
                NomePublico = x.NomePublico,
                Cor = x.Cor,
                Ordem = x.Ordem,
                Sistema = x.Sistema,
                Ativa = x.Ativa,
                VisivelCliente = x.VisivelCliente,
                GeraEventoCliente = x.GeraEventoCliente,
                EtapaFinal = x.EtapaFinal,
                PermiteEnvioWhatsApp = x.PermiteEnvioWhatsApp,
                DescricaoPublica = x.DescricaoPublica
            })
            .ToListAsync(cancellationToken);

        var colunaIds = colunas.Select(x => x.Id).ToList();

        var cards = await _context.KanbanCards
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Ativo &&
                !x.OcultoDoQuadro &&
                colunaIds.Contains(x.KanbanColunaId))
            .OrderBy(x => x.Ordem)
            .Select(x => new KanbanPublicoCardDto
            {
                Id = x.Id,
                OrdemServicoId = x.OrdemServicoId,
                KanbanColunaId = x.KanbanColunaId,
                PublicTrackingToken = x.PublicTrackingToken,
                NumeroOs = x.OrdemServico != null ? x.OrdemServico.NumeroOs.ToString() : string.Empty,
                Cliente = x.OrdemServico != null && x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Nome : string.Empty,
                TelefoneCliente = x.OrdemServico != null && x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Telefone : null,
                Aparelho = x.OrdemServico != null && x.OrdemServico.Aparelho != null
                    ? ((x.OrdemServico.Aparelho.Marca ?? string.Empty) + " " + (x.OrdemServico.Aparelho.Modelo ?? string.Empty)).Trim()
                    : string.Empty,
                Defeito = x.OrdemServico != null ? x.OrdemServico.DefeitoRelatado : null,
                Tecnico = x.OrdemServico != null && x.OrdemServico.Tecnico != null ? x.OrdemServico.Tecnico.Nome : null,
                ValorTotal = x.OrdemServico != null ? x.OrdemServico.ValorTotal : null,
                StatusFinanceiro = null,
                StatusPeca = null,
                Atrasada = x.OrdemServico != null
                    && x.OrdemServico.DataPrevisao.HasValue
                    && x.OrdemServico.DataPrevisao.Value < DateTime.UtcNow
                    && !x.KanbanColuna!.EtapaFinal,
                Ordem = x.Ordem
            })
            .ToListAsync(cancellationToken);

        foreach (var coluna in colunas)
        {
            coluna.Cards = cards
                .Where(x => x.KanbanColunaId == coluna.Id)
                .OrderBy(x => x.Ordem)
                .ToList();
        }

        return colunas;
    }

    public async Task<List<KanbanPublicoCardDto>> ListarEncerradosPublicoAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPublicoAsync(empresaId, cancellationToken);
        await ArquivarFinalizadosAposPrazoAsync(empresaId, fluxo.Id, cancellationToken);

        var finais = await _context.KanbanColunas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id && x.Ativa && x.EtapaFinal)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (finais.Count == 0)
            return new List<KanbanPublicoCardDto>();

        return await _context.KanbanCards
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Ativo &&
                finais.Contains(x.KanbanColunaId))
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new KanbanPublicoCardDto
            {
                Id = x.Id,
                OrdemServicoId = x.OrdemServicoId,
                KanbanColunaId = x.KanbanColunaId,
                PublicTrackingToken = x.PublicTrackingToken,
                NumeroOs = x.OrdemServico != null ? x.OrdemServico.NumeroOs.ToString() : string.Empty,
                Cliente = x.OrdemServico != null && x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Nome : string.Empty,
                TelefoneCliente = x.OrdemServico != null && x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Telefone : null,
                Aparelho = x.OrdemServico != null && x.OrdemServico.Aparelho != null
                    ? ((x.OrdemServico.Aparelho.Marca ?? string.Empty) + " " + (x.OrdemServico.Aparelho.Modelo ?? string.Empty)).Trim()
                    : string.Empty,
                Defeito = x.OrdemServico != null ? x.OrdemServico.DefeitoRelatado : null,
                Tecnico = x.OrdemServico != null && x.OrdemServico.Tecnico != null ? x.OrdemServico.Tecnico.Nome : null,
                ValorTotal = x.OrdemServico != null ? x.OrdemServico.ValorTotal : null,
                StatusFinanceiro = null,
                StatusPeca = null,
                Atrasada = false,
                Ordem = x.Ordem
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KanbanConfiguracaoColunaDto>> ObterConfiguracaoPublicaAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPublicoAsync(empresaId, cancellationToken);

        return await _context.KanbanColunas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id)
            .OrderBy(x => x.Ordem)
            .Select(x => new KanbanConfiguracaoColunaDto
            {
                Id = x.Id,
                NomeInterno = x.NomeInterno,
                NomePublico = x.NomePublico,
                Cor = x.Cor,
                Ordem = x.Ordem,
                Sistema = x.Sistema,
                Ativa = x.Ativa,
                VisivelCliente = x.VisivelCliente,
                GeraEventoCliente = x.GeraEventoCliente,
                EtapaFinal = x.EtapaFinal,
                TipoFinalizacao = x.TipoFinalizacao,
                PermiteEnvioWhatsApp = x.PermiteEnvioWhatsApp,
                DescricaoPublica = x.DescricaoPublica
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<KanbanConfiguracaoColunaDto> CriarColunaPublicaAsync(Guid empresaId, CreateKanbanPublicoColunaDto dto, CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPublicoAsync(empresaId, cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.NomeInterno))
            throw new InvalidOperationException("Informe o nome interno da coluna.");

        var tipoFinalizacao = dto.EtapaFinal
            ? NormalizarTipoFinalizacao(dto.TipoFinalizacao) ?? "ENTREGUE"
            : null;

        if (dto.EtapaFinal)
        {
            var jaExisteMesmoTipo = await _context.KanbanColunas.AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == fluxo.Id &&
                x.Ativa &&
                x.EtapaFinal &&
                x.TipoFinalizacao == tipoFinalizacao,
                cancellationToken);

            if (jaExisteMesmoTipo)
                throw new InvalidOperationException($"Já existe uma coluna final do tipo {tipoFinalizacao}.");
        }

        var finais = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id && x.EtapaFinal)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        var ordemInsercao = finais.Count > 0
            ? finais.Min(x => x.Ordem)
            : (await _context.KanbanColunas
                .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id)
                .Select(x => (int?)x.Ordem)
                .MaxAsync(cancellationToken) ?? 0) + 1;

        var colunasPosteriores = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id && x.Ordem >= ordemInsercao)
            .ToListAsync(cancellationToken);

        foreach (var colunaExistente in colunasPosteriores)
        {
            colunaExistente.Ordem += 1;
            colunaExistente.UpdatedAt = DateTime.UtcNow;
        }

        var coluna = new KanbanColuna
        {
            EmpresaId = empresaId,
            KanbanFluxoId = fluxo.Id,
            NomeInterno = dto.NomeInterno.Trim(),
            NomePublico = Normalizar(dto.NomePublico),
            Cor = NormalizarCor(dto.Cor),
            Ordem = ordemInsercao,
            Sistema = false,
            Ativa = true,
            VisivelCliente = dto.VisivelCliente,
            GeraEventoCliente = dto.GeraEventoCliente,
            EtapaFinal = dto.EtapaFinal,
            TipoFinalizacao = tipoFinalizacao,
            PermiteEnvioWhatsApp = dto.PermiteEnvioWhatsApp,
            DescricaoPublica = Normalizar(dto.DescricaoPublica)
        };

        _context.KanbanColunas.Add(coluna);
        await _context.SaveChangesAsync(cancellationToken);
        await MoverColunasFinaisParaFimAsync(empresaId, fluxo.Id, cancellationToken);

        return MapConfiguracao(coluna);
    }

    public async Task<KanbanConfiguracaoColunaDto?> AtualizarColunaPublicaAsync(Guid empresaId, Guid colunaId, UpdateKanbanPublicoColunaDto dto, CancellationToken cancellationToken = default)
    {
        var coluna = await _context.KanbanColunas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == colunaId, cancellationToken);

        if (coluna is null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.NomeInterno))
            throw new InvalidOperationException("Informe o nome interno da coluna.");

        if (coluna.EtapaFinal && !dto.EtapaFinal)
        {
            var existeOutraFinal = await _context.KanbanColunas
                .AnyAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.KanbanFluxoId == coluna.KanbanFluxoId &&
                    x.Id != coluna.Id &&
                    x.Ativa &&
                    x.EtapaFinal,
                    cancellationToken);

            if (!existeOutraFinal)
                throw new InvalidOperationException("Deve existir pelo menos uma coluna final.");
        }

        var tipoFinalizacao = dto.EtapaFinal
            ? NormalizarTipoFinalizacao(dto.TipoFinalizacao) ?? coluna.TipoFinalizacao ?? "ENTREGUE"
            : null;

        if (dto.EtapaFinal)
        {
            var jaExisteMesmoTipo = await _context.KanbanColunas.AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == coluna.KanbanFluxoId &&
                x.Id != coluna.Id &&
                x.Ativa &&
                x.EtapaFinal &&
                x.TipoFinalizacao == tipoFinalizacao,
                cancellationToken);

            if (jaExisteMesmoTipo)
                throw new InvalidOperationException($"Já existe uma coluna final do tipo {tipoFinalizacao}.");
        }

        coluna.NomeInterno = dto.NomeInterno.Trim();
        coluna.NomePublico = Normalizar(dto.NomePublico);
        coluna.Cor = NormalizarCor(dto.Cor);
        coluna.Ativa = dto.Ativa;
        coluna.VisivelCliente = dto.VisivelCliente;
        coluna.GeraEventoCliente = dto.GeraEventoCliente;
        coluna.EtapaFinal = dto.EtapaFinal;
        coluna.TipoFinalizacao = tipoFinalizacao;
        coluna.PermiteEnvioWhatsApp = dto.PermiteEnvioWhatsApp;
        coluna.DescricaoPublica = Normalizar(dto.DescricaoPublica);
        coluna.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await MoverColunasFinaisParaFimAsync(empresaId, coluna.KanbanFluxoId, cancellationToken);

        return MapConfiguracao(coluna);
    }

    public async Task<bool> ReordenarColunaPublicaAsync(Guid empresaId, Guid colunaId, ReordenarKanbanColunaDto dto, CancellationToken cancellationToken = default)
    {
        var coluna = await _context.KanbanColunas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == colunaId && x.Ativa, cancellationToken);

        if (coluna is null)
            return false;

        var colunas = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == coluna.KanbanFluxoId && x.Ativa)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        colunas.RemoveAll(x => x.Id == coluna.Id);

        int insertIndex;
        if (coluna.EtapaFinal)
        {
            insertIndex = colunas.Count;
        }
        else
        {
            var finalIndex = colunas.FindIndex(x => x.EtapaFinal);
            var maxIndex = finalIndex >= 0 ? finalIndex : colunas.Count;
            insertIndex = Math.Clamp((dto.Ordem <= 0 ? maxIndex + 1 : dto.Ordem) - 1, 0, maxIndex);
        }

        colunas.Insert(insertIndex, coluna);

        for (var i = 0; i < colunas.Count; i++)
        {
            colunas[i].Ordem = i + 1;
            colunas[i].UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirColunaPublicaAsync(Guid empresaId, Guid colunaId, CancellationToken cancellationToken = default)
    {
        var coluna = await _context.KanbanColunas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == colunaId && x.Ativa, cancellationToken);

        if (coluna is null)
            return false;

        if (coluna.Sistema || coluna.EtapaFinal)
            throw new InvalidOperationException("Essa coluna não pode ser excluída.");

        var destino = await _context.KanbanColunas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == coluna.KanbanFluxoId &&
                x.Ativa &&
                x.Id != coluna.Id)
            .OrderByDescending(x => x.Ordem < coluna.Ordem ? x.Ordem : 0)
            .ThenBy(x => x.Ordem)
            .FirstOrDefaultAsync(cancellationToken);

        if (destino is null)
            throw new InvalidOperationException("Não foi encontrada uma coluna de destino.");

        var cardsDaColuna = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == coluna.Id && x.Ativo)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        var proximaOrdem = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == destino.Id && x.Ativo)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        foreach (var card in cardsDaColuna)
        {
            proximaOrdem++;
            card.KanbanColunaId = destino.Id;
            card.Ordem = proximaOrdem;
            card.UpdatedAt = DateTime.UtcNow;
        }

        coluna.Ativa = false;
        coluna.UpdatedAt = DateTime.UtcNow;

        var restantes = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == coluna.KanbanFluxoId && x.Ativa)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < restantes.Count; i++)
        {
            restantes[i].Ordem = i + 1;
            restantes[i].UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<KanbanPublicoCardDto?> MoverCardPublicoAsync(
        Guid empresaId,
        Guid ordemServicoId,
        MoveKanbanPublicoCardDto dto,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPublicoAsync(empresaId, cancellationToken);
        await GarantirCardsPublicosDasOrdensServicoAsync(empresaId, fluxo.Id, cancellationToken);

        var card = await _context.KanbanCards
            .Include(x => x.OrdemServico)
            .Include(x => x.KanbanColuna)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.OrdemServicoId == ordemServicoId && x.Ativo, cancellationToken);

        if (card is null)
            return null;

        var colunaDestino = await _context.KanbanColunas
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == dto.ColunaId &&
                x.KanbanFluxoId == fluxo.Id &&
                x.Ativa,
                cancellationToken);

        if (colunaDestino is null)
            return null;

        var nomeOrigem = card.KanbanColuna?.NomeInterno;
        var origemId = card.KanbanColunaId;

        await ReordenarCardsPublicosAsync(empresaId, card, colunaDestino.Id, dto.Ordem, cancellationToken);

        card.PublicTrackingToken = string.IsNullOrWhiteSpace(card.PublicTrackingToken)
            ? Guid.NewGuid().ToString("N")
            : card.PublicTrackingToken;

        if (card.OrdemServico is not null)
        {
            card.OrdemServico.KanbanColunaAtualId = colunaDestino.Id;
            card.OrdemServico.TrackingToken = string.IsNullOrWhiteSpace(card.OrdemServico.TrackingToken)
                ? card.PublicTrackingToken
                : card.OrdemServico.TrackingToken;

            if (colunaDestino.EtapaFinal && colunaDestino.TipoFinalizacao == "CANCELADA")
            {
                card.OrdemServico.Status = "CANCELADA";
                card.OrdemServico.DataConclusao ??= DateTime.UtcNow;
                card.OrdemServico.DataEntrega = null;
            }
            else if (colunaDestino.EtapaFinal && colunaDestino.TipoFinalizacao == "ENTREGUE")
            {
                card.OrdemServico.Status = "ENTREGUE";
                card.OrdemServico.DataConclusao ??= DateTime.UtcNow;
                card.OrdemServico.DataEntrega ??= DateTime.UtcNow;
            }

            card.OrdemServico.UpdatedAt = DateTime.UtcNow;
        }

        _context.OrdemServicoKanbanHistoricos.Add(new OrdemServicoKanbanHistorico
        {
            EmpresaId = empresaId,
            OrdemServicoId = ordemServicoId,
            ColunaOrigemId = origemId,
            ColunaDestinoId = colunaDestino.Id,
            UsuarioId = usuarioId,
            NomeColunaOrigem = nomeOrigem,
            NomeColunaDestino = colunaDestino.NomeInterno,
            HistoricoPublico = colunaDestino.GeraEventoCliente,
            TituloPublico = colunaDestino.NomePublico ?? colunaDestino.NomeInterno,
            DescricaoPublica = colunaDestino.DescricaoPublica,
            PublicTrackingToken = card.PublicTrackingToken,
            DataMovimentacao = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return await ObterCardPublicoPorOrdemServicoAsync(empresaId, ordemServicoId, cancellationToken);
    }

    public async Task<KanbanPublicoCardDto?> ReabrirCardPublicoAsync(
        Guid empresaId,
        Guid ordemServicoId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPublicoAsync(empresaId, cancellationToken);

        var colunaInicialId = await _context.KanbanColunas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == fluxo.Id &&
                x.Ativa &&
                !x.EtapaFinal)
            .OrderBy(x => x.Ordem)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        var proximaOrdem = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == colunaInicialId && x.Ativo)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        return await MoverCardPublicoAsync(
            empresaId,
            ordemServicoId,
            new MoveKanbanPublicoCardDto
            {
                ColunaId = colunaInicialId,
                Ordem = proximaOrdem + 1
            },
            usuarioId,
            cancellationToken);
    }

    // =========================
    // PRIVADO
    // =========================

    public async Task<List<KanbanPrivadoColunaDto>> ObterMeuKanbanPrivadoAsync(Guid empresaId, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPrivadoAsync(empresaId, usuarioId, cancellationToken);

        var colunas = await _context.KanbanColunas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id && x.Ativa)
            .OrderBy(x => x.Ordem)
            .Select(x => new KanbanPrivadoColunaDto
            {
                Id = x.Id,
                Nome = x.NomeInterno,
                Ordem = x.Ordem,
                Sistema = x.Sistema,
                Ativa = x.Ativa
            })
            .ToListAsync(cancellationToken);

        var colunaIds = colunas.Select(x => x.Id).ToList();

        var cards = await _context.KanbanTarefasPrivadas
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.UsuarioId == usuarioId &&
                x.Ativo &&
                colunaIds.Contains(x.KanbanColunaId))
            .OrderBy(x => x.Ordem)
            .Select(x => new KanbanPrivadoCardDto
            {
                Id = x.Id,
                KanbanColunaId = x.KanbanColunaId,
                OrdemServicoId = x.OrdemServicoId,
                Titulo = x.Titulo,
                Descricao = x.Descricao,
                Ordem = x.Ordem,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var coluna in colunas)
        {
            coluna.Cards = cards
                .Where(x => x.KanbanColunaId == coluna.Id)
                .OrderBy(x => x.Ordem)
                .ToList();
        }

        return colunas;
    }

    public async Task<KanbanPrivadoColunaDto> CriarColunaPrivadaAsync(Guid empresaId, Guid usuarioId, CreateKanbanPrivadoColunaDto dto, CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPrivadoAsync(empresaId, usuarioId, cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Informe o nome da coluna.");

        var proximaOrdem = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        var coluna = new KanbanColuna
        {
            EmpresaId = empresaId,
            KanbanFluxoId = fluxo.Id,
            NomeInterno = dto.Nome.Trim(),
            Cor = "#E2E8F0",
            Ordem = proximaOrdem + 1,
            Sistema = false,
            Ativa = true
        };

        _context.KanbanColunas.Add(coluna);
        await _context.SaveChangesAsync(cancellationToken);

        return new KanbanPrivadoColunaDto
        {
            Id = coluna.Id,
            Nome = coluna.NomeInterno,
            Ordem = coluna.Ordem,
            Sistema = coluna.Sistema,
            Ativa = coluna.Ativa
        };
    }

    public async Task<KanbanPrivadoColunaDto?> AtualizarColunaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid colunaId, UpdateKanbanPrivadoColunaDto dto, CancellationToken cancellationToken = default)
    {
        var coluna = await _context.KanbanColunas
            .Include(x => x.KanbanFluxo)
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == colunaId &&
                x.KanbanFluxo != null &&
                x.KanbanFluxo.Tipo == "PRIVADO" &&
                x.KanbanFluxo.UsuarioId == usuarioId,
                cancellationToken);

        if (coluna is null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Informe o nome da coluna.");

        coluna.NomeInterno = dto.Nome.Trim();
        coluna.Ativa = dto.Ativa;
        coluna.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new KanbanPrivadoColunaDto
        {
            Id = coluna.Id,
            Nome = coluna.NomeInterno,
            Ordem = coluna.Ordem,
            Sistema = coluna.Sistema,
            Ativa = coluna.Ativa
        };
    }

    public async Task<bool> ExcluirColunaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid colunaId, CancellationToken cancellationToken = default)
    {
        var coluna = await _context.KanbanColunas
            .Include(x => x.KanbanFluxo)
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == colunaId &&
                x.Ativa &&
                x.KanbanFluxo != null &&
                x.KanbanFluxo.Tipo == "PRIVADO" &&
                x.KanbanFluxo.UsuarioId == usuarioId,
                cancellationToken);

        if (coluna is null)
            return false;

        if (coluna.Sistema)
            throw new InvalidOperationException("As colunas padrão não podem ser excluídas.");

        var destino = await _context.KanbanColunas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == coluna.KanbanFluxoId &&
                x.Ativa &&
                x.Id != coluna.Id)
            .OrderBy(x => x.Ordem)
            .FirstOrDefaultAsync(cancellationToken);

        if (destino is null)
            throw new InvalidOperationException("Não foi encontrada uma coluna de destino.");

        var tarefas = await _context.KanbanTarefasPrivadas
            .Where(x => x.EmpresaId == empresaId && x.UsuarioId == usuarioId && x.KanbanColunaId == coluna.Id && x.Ativo)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        var proximaOrdem = await _context.KanbanTarefasPrivadas
            .Where(x => x.EmpresaId == empresaId && x.UsuarioId == usuarioId && x.KanbanColunaId == destino.Id && x.Ativo)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        foreach (var tarefa in tarefas)
        {
            proximaOrdem++;
            tarefa.KanbanColunaId = destino.Id;
            tarefa.Ordem = proximaOrdem;
            tarefa.UpdatedAt = DateTime.UtcNow;
        }

        coluna.Ativa = false;
        coluna.UpdatedAt = DateTime.UtcNow;

        var restantes = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == coluna.KanbanFluxoId && x.Ativa)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < restantes.Count; i++)
        {
            restantes[i].Ordem = i + 1;
            restantes[i].UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<KanbanPrivadoCardDto> CriarTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, CreateKanbanTarefaPrivadaDto dto, CancellationToken cancellationToken = default)
    {
        var fluxo = await GarantirFluxoPrivadoAsync(empresaId, usuarioId, cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("Informe o título da tarefa.");

        Guid colunaId;
        if (dto.KanbanColunaId.HasValue)
        {
            var colunaValida = await _context.KanbanColunas
                .AnyAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.Id == dto.KanbanColunaId.Value &&
                    x.KanbanFluxoId == fluxo.Id &&
                    x.Ativa,
                    cancellationToken);

            if (!colunaValida)
                throw new InvalidOperationException("Coluna inválida para o kanban privado.");

            colunaId = dto.KanbanColunaId.Value;
        }
        else
        {
            colunaId = await _context.KanbanColunas
                .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxo.Id && x.Ativa)
                .OrderBy(x => x.Ordem)
                .Select(x => x.Id)
                .FirstAsync(cancellationToken);
        }

        var proximaOrdem = await _context.KanbanTarefasPrivadas
            .Where(x => x.EmpresaId == empresaId && x.UsuarioId == usuarioId && x.KanbanColunaId == colunaId && x.Ativo)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        var tarefa = new KanbanTarefaPrivada
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            KanbanColunaId = colunaId,
            OrdemServicoId = dto.OrdemServicoId,
            Titulo = dto.Titulo.Trim(),
            Descricao = Normalizar(dto.Descricao),
            Ordem = proximaOrdem + 1,
            Ativo = true
        };

        _context.KanbanTarefasPrivadas.Add(tarefa);
        await _context.SaveChangesAsync(cancellationToken);

        return new KanbanPrivadoCardDto
        {
            Id = tarefa.Id,
            KanbanColunaId = tarefa.KanbanColunaId,
            OrdemServicoId = tarefa.OrdemServicoId,
            Titulo = tarefa.Titulo,
            Descricao = tarefa.Descricao,
            Ordem = tarefa.Ordem,
            CreatedAt = tarefa.CreatedAt,
            UpdatedAt = tarefa.UpdatedAt
        };
    }

    public async Task<KanbanPrivadoCardDto?> AtualizarTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid tarefaId, UpdateKanbanTarefaPrivadaDto dto, CancellationToken cancellationToken = default)
    {
        var tarefa = await _context.KanbanTarefasPrivadas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.UsuarioId == usuarioId && x.Id == tarefaId && x.Ativo, cancellationToken);

        if (tarefa is null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("Informe o título da tarefa.");

        tarefa.Titulo = dto.Titulo.Trim();
        tarefa.Descricao = Normalizar(dto.Descricao);
        tarefa.OrdemServicoId = dto.OrdemServicoId;
        tarefa.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new KanbanPrivadoCardDto
        {
            Id = tarefa.Id,
            KanbanColunaId = tarefa.KanbanColunaId,
            OrdemServicoId = tarefa.OrdemServicoId,
            Titulo = tarefa.Titulo,
            Descricao = tarefa.Descricao,
            Ordem = tarefa.Ordem,
            CreatedAt = tarefa.CreatedAt,
            UpdatedAt = tarefa.UpdatedAt
        };
    }

    public async Task<KanbanPrivadoCardDto?> MoverTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid tarefaId, MoveKanbanTarefaPrivadaDto dto, CancellationToken cancellationToken = default)
    {
        var tarefa = await _context.KanbanTarefasPrivadas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.UsuarioId == usuarioId && x.Id == tarefaId && x.Ativo, cancellationToken);

        if (tarefa is null)
            return null;

        var colunaValida = await _context.KanbanColunas
            .Include(x => x.KanbanFluxo)
            .AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == dto.ColunaId &&
                x.Ativa &&
                x.KanbanFluxo != null &&
                x.KanbanFluxo.Tipo == "PRIVADO" &&
                x.KanbanFluxo.UsuarioId == usuarioId,
                cancellationToken);

        if (!colunaValida)
            throw new InvalidOperationException("Coluna inválida para o kanban privado.");

        await ReordenarTarefasPrivadasAsync(empresaId, usuarioId, tarefa, dto.ColunaId, dto.Ordem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new KanbanPrivadoCardDto
        {
            Id = tarefa.Id,
            KanbanColunaId = tarefa.KanbanColunaId,
            OrdemServicoId = tarefa.OrdemServicoId,
            Titulo = tarefa.Titulo,
            Descricao = tarefa.Descricao,
            Ordem = tarefa.Ordem,
            CreatedAt = tarefa.CreatedAt,
            UpdatedAt = tarefa.UpdatedAt
        };
    }

    public async Task<bool> ExcluirTarefaPrivadaAsync(Guid empresaId, Guid usuarioId, Guid tarefaId, CancellationToken cancellationToken = default)
    {
        var tarefa = await _context.KanbanTarefasPrivadas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.UsuarioId == usuarioId && x.Id == tarefaId && x.Ativo, cancellationToken);

        if (tarefa is null)
            return false;

        var colunaId = tarefa.KanbanColunaId;

        tarefa.Ativo = false;
        tarefa.UpdatedAt = DateTime.UtcNow;

        var restantes = await _context.KanbanTarefasPrivadas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.UsuarioId == usuarioId &&
                x.KanbanColunaId == colunaId &&
                x.Ativo)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < restantes.Count; i++)
        {
            restantes[i].Ordem = i + 1;
            restantes[i].UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // =========================
    // TRACKING PÚBLICO
    // =========================

    public async Task<KanbanTrackingPublicoDto?> ObterTrackingPublicoAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var card = await _context.KanbanCards
            .AsNoTracking()
            .Where(x =>
                x.Ativo &&
                x.PublicTrackingToken == token &&
                x.OrdemServico != null &&
                x.OrdemServico.TrackingPublicoAtivo)
            .Select(x => new
            {
                x.EmpresaId,
                x.OrdemServicoId,
                x.PublicTrackingToken,
                x.KanbanColunaId,
                FluxoId = x.KanbanColuna!.KanbanFluxoId,
                NumeroOs = x.OrdemServico!.NumeroOs,
                Cliente = x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Nome : string.Empty,
                Aparelho = x.OrdemServico.Aparelho != null
                    ? ((x.OrdemServico.Aparelho.Marca ?? string.Empty) + " " + (x.OrdemServico.Aparelho.Modelo ?? string.Empty)).Trim()
                    : string.Empty,
                Defeito = x.OrdemServico.DefeitoRelatado,
                StatusAtual = x.KanbanColuna!.NomePublico ?? x.KanbanColuna.NomeInterno,
                ValorTotal = x.OrdemServico.ValorTotal,
                EmpresaLogoUrl = x.OrdemServico.Empresa != null ? x.OrdemServico.Empresa.LogoUrl : null,
                FotosJson = x.OrdemServico.FotosJson
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
            return null;

        var etapasBase = await _context.KanbanColunas
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == card.EmpresaId &&
                x.KanbanFluxoId == card.FluxoId &&
                x.Ativa &&
                x.VisivelCliente)
            .OrderBy(x => x.Ordem)
            .Select(x => new
            {
                x.Id,
                Nome = x.NomePublico ?? x.NomeInterno,
                x.Cor,
                x.Ordem
            })
            .ToListAsync(cancellationToken);

        var ordemAtual = etapasBase
            .FirstOrDefault(x => x.Id == card.KanbanColunaId)?
            .Ordem;

        var etapas = etapasBase
            .Select(x => new KanbanTrackingEtapaDto
            {
                ColunaId = x.Id,
                Nome = x.Nome,
                Cor = x.Cor,
                Ordem = x.Ordem,
                Atual = x.Id == card.KanbanColunaId,
                Concluida = ordemAtual.HasValue && x.Ordem < ordemAtual.Value
            })
            .ToList();

        var historico = await _context.OrdemServicoKanbanHistoricos
            .AsNoTracking()
            .Where(x => x.PublicTrackingToken == token && x.HistoricoPublico)
            .OrderBy(x => x.DataMovimentacao)
            .Select(x => new KanbanTrackingEventoDto
            {
                Titulo = x.TituloPublico ?? x.NomeColunaDestino,
                Descricao = x.DescricaoPublica,
                Data = x.DataMovimentacao
            })
            .ToListAsync(cancellationToken);

        var itens = await _context.OrdensServicoItens
            .AsNoTracking()
            .Where(x => x.OrdemServicoId == card.OrdemServicoId)
            .OrderBy(x => x.Ordem)
            .Select(x => new KanbanTrackingItemDto
            {
                TipoItem = x.TipoItem,
                Descricao = x.Descricao,
                Quantidade = x.Quantidade,
                ValorTotal = x.ValorTotal
            })
            .ToListAsync(cancellationToken);

        return new KanbanTrackingPublicoDto
        {
            PublicTrackingToken = card.PublicTrackingToken,
            NumeroOs = card.NumeroOs.ToString(),
            Cliente = card.Cliente,
            Aparelho = card.Aparelho,
            Defeito = card.Defeito,
            EmpresaLogoUrl = card.EmpresaLogoUrl,
            StatusAtual = card.StatusAtual,
            ColunaAtualId = card.KanbanColunaId,
            ValorTotal = card.ValorTotal,
            Etapas = etapas,
            Historico = historico,
            Itens = itens,
            Fotos = OrdemServicoFotoJson.Parse(card.FotosJson)
        };
    }

    // =========================
    // HELPERS
    // =========================

    private async Task ArquivarFinalizadosAposPrazoAsync(
        Guid empresaId,
        Guid fluxoId,
        CancellationToken cancellationToken)
    {
        var limite = DateTime.UtcNow.AddDays(-1);

        var finais = await _context.KanbanColunas
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == fluxoId &&
                x.Ativa &&
                x.EtapaFinal)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (finais.Count == 0)
            return;

        var cards = await _context.KanbanCards
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Ativo &&
                !x.OcultoDoQuadro &&
                finais.Contains(x.KanbanColunaId) &&
                x.DataEntradaColunaAtual.HasValue &&
                x.DataEntradaColunaAtual.Value <= limite)
            .ToListAsync(cancellationToken);

        if (cards.Count == 0)
            return;

        foreach (var card in cards)
        {
            card.OcultoDoQuadro = true;
            card.DataOcultado = DateTime.UtcNow;
            card.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<KanbanFluxo> GarantirFluxoPublicoAsync(Guid empresaId, CancellationToken cancellationToken)
    {
        var fluxo = await _context.KanbanFluxos
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Tipo == "PUBLICO" && x.UsuarioId == null && x.Ativo, cancellationToken);

        if (fluxo is null)
        {
            fluxo = new KanbanFluxo
            {
                EmpresaId = empresaId,
                Nome = "Kanban Público",
                Tipo = "PUBLICO",
                UsuarioId = null,
                Ativo = true
            };

            _context.KanbanFluxos.Add(fluxo);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await GarantirColunasPublicasAsync(empresaId, fluxo.Id, cancellationToken);
        return fluxo;
    }

    private async Task<KanbanFluxo> GarantirFluxoPrivadoAsync(Guid empresaId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var fluxo = await _context.KanbanFluxos
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Tipo == "PRIVADO" && x.UsuarioId == usuarioId && x.Ativo, cancellationToken);

        if (fluxo is null)
        {
            fluxo = new KanbanFluxo
            {
                EmpresaId = empresaId,
                Nome = "Meu Kanban",
                Tipo = "PRIVADO",
                UsuarioId = usuarioId,
                Ativo = true
            };

            _context.KanbanFluxos.Add(fluxo);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await GarantirColunasPrivadasAsync(empresaId, fluxo.Id, cancellationToken);
        return fluxo;
    }

    private async Task GarantirColunasPublicasAsync(Guid empresaId, Guid fluxoId, CancellationToken cancellationToken)
    {
        await NormalizarColunasFinaisLegadasAsync(empresaId, fluxoId, cancellationToken);
        await ConsolidarColunasFinaisDuplicadasAsync(empresaId, fluxoId, cancellationToken);

        var colunas = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxoId)
            .ToListAsync(cancellationToken);

        var alterou = false;

        if (!colunas.Any(x => x.Sistema && !x.EtapaFinal))
        {
            _context.KanbanColunas.Add(new KanbanColuna
            {
                EmpresaId = empresaId,
                KanbanFluxoId = fluxoId,
                NomeInterno = "Entrada",
                NomePublico = "Recebido",
                Cor = "#CBD5E1",
                Ordem = 1,
                Sistema = true,
                Ativa = true,
                VisivelCliente = true,
                GeraEventoCliente = true,
                EtapaFinal = false,
                TipoFinalizacao = null,
                PermiteEnvioWhatsApp = false,
                DescricaoPublica = "Seu aparelho foi recebido."
            });

            alterou = true;
        }

        if (!colunas.Any(x => x.Ativa && x.EtapaFinal && x.TipoFinalizacao == "ENTREGUE"))
        {
            var maiorOrdem = colunas.Count == 0 ? 1 : colunas.Max(x => x.Ordem);

            _context.KanbanColunas.Add(new KanbanColuna
            {
                EmpresaId = empresaId,
                KanbanFluxoId = fluxoId,
                NomeInterno = "Finalizado",
                NomePublico = "Finalizado",
                Cor = "#86EFAC",
                Ordem = maiorOrdem + 1,
                Sistema = true,
                Ativa = true,
                VisivelCliente = true,
                GeraEventoCliente = true,
                EtapaFinal = true,
                TipoFinalizacao = "ENTREGUE",
                PermiteEnvioWhatsApp = true,
                DescricaoPublica = "Seu atendimento foi finalizado."
            });

            alterou = true;
        }

        if (!colunas.Any(x => x.Ativa && x.EtapaFinal && x.TipoFinalizacao == "CANCELADA"))
        {
            var maiorOrdem = colunas.Count == 0 ? 1 : colunas.Max(x => x.Ordem);

            _context.KanbanColunas.Add(new KanbanColuna
            {
                EmpresaId = empresaId,
                KanbanFluxoId = fluxoId,
                NomeInterno = "Cancelada",
                NomePublico = "Cancelada",
                Cor = "#FCA5A5",
                Ordem = maiorOrdem + 1,
                Sistema = true,
                Ativa = true,
                VisivelCliente = true,
                GeraEventoCliente = true,
                EtapaFinal = true,
                TipoFinalizacao = "CANCELADA",
                PermiteEnvioWhatsApp = false,
                DescricaoPublica = "Seu atendimento foi cancelado."
            });

            alterou = true;
        }

        if (alterou)
            await _context.SaveChangesAsync(cancellationToken);

        await MoverColunasFinaisParaFimAsync(empresaId, fluxoId, cancellationToken);
    }

    private async Task GarantirColunasPrivadasAsync(Guid empresaId, Guid fluxoId, CancellationToken cancellationToken)
    {
        var colunas = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxoId)
            .ToListAsync(cancellationToken);

        if (colunas.Count > 0)
            return;

        _context.KanbanColunas.AddRange(
            new KanbanColuna
            {
                EmpresaId = empresaId,
                KanbanFluxoId = fluxoId,
                NomeInterno = "A Fazer",
                Cor = "#E2E8F0",
                Ordem = 1,
                Sistema = true,
                Ativa = true
            },
            new KanbanColuna
            {
                EmpresaId = empresaId,
                KanbanFluxoId = fluxoId,
                NomeInterno = "Em Andamento",
                Cor = "#BFDBFE",
                Ordem = 2,
                Sistema = true,
                Ativa = true
            },
            new KanbanColuna
            {
                EmpresaId = empresaId,
                KanbanFluxoId = fluxoId,
                NomeInterno = "Concluído",
                Cor = "#BBF7D0",
                Ordem = 3,
                Sistema = true,
                Ativa = true
            });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task GarantirCardsPublicosDasOrdensServicoAsync(Guid empresaId, Guid fluxoId, CancellationToken cancellationToken)
    {
        var colunas = await _context.KanbanColunas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxoId && x.Ativa)
            .OrderBy(x => x.Ordem)
            .Select(x => new
            {
                x.Id,
                x.Ordem,
                x.EtapaFinal,
                x.TipoFinalizacao
            })
            .ToListAsync(cancellationToken);

        var colunaInicialId = colunas
            .Where(x => !x.EtapaFinal)
            .OrderBy(x => x.Ordem)
            .Select(x => x.Id)
            .First();

        var colunaEntregueId = colunas
            .Where(x => x.EtapaFinal && x.TipoFinalizacao == "ENTREGUE")
            .OrderBy(x => x.Ordem)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();

        var colunaCanceladaId = colunas
            .Where(x => x.EtapaFinal && x.TipoFinalizacao == "CANCELADA")
            .OrderBy(x => x.Ordem)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();

        var ordens = await _context.OrdensServico
            .Where(x => x.EmpresaId == empresaId)
            .ToListAsync(cancellationToken);

        var cardsExistentes = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .ToDictionaryAsync(x => x.OrdemServicoId, cancellationToken);

        var maxPorColuna = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .GroupBy(x => x.KanbanColunaId)
            .Select(g => new { ColunaId = g.Key, MaxOrdem = g.Max(x => x.Ordem) })
            .ToDictionaryAsync(x => x.ColunaId, x => x.MaxOrdem, cancellationToken);

        var alterou = false;

        foreach (var os in ordens)
        {
            if (cardsExistentes.TryGetValue(os.Id, out var cardExistente))
            {
                if (string.IsNullOrWhiteSpace(cardExistente.PublicTrackingToken))
                {
                    cardExistente.PublicTrackingToken = string.IsNullOrWhiteSpace(os.TrackingToken)
                        ? Guid.NewGuid().ToString("N")
                        : os.TrackingToken;

                    cardExistente.UpdatedAt = DateTime.UtcNow;
                    alterou = true;
                }

                continue;
            }

            var colunaDestinoId = os.KanbanColunaAtualId ?? Guid.Empty;

            var colunaValida = colunaDestinoId != Guid.Empty && colunas.Any(x => x.Id == colunaDestinoId);

            if (!colunaValida)
            {
                colunaDestinoId =
                    os.Status == "CANCELADA" && colunaCanceladaId.HasValue ? colunaCanceladaId.Value :
                    os.Status == "ENTREGUE" && colunaEntregueId.HasValue ? colunaEntregueId.Value :
                    colunaInicialId;
            }

            var ordemAtual = maxPorColuna.TryGetValue(colunaDestinoId, out var max) ? max : 0;
            ordemAtual++;

            maxPorColuna[colunaDestinoId] = ordemAtual;

            var token = string.IsNullOrWhiteSpace(os.TrackingToken)
                ? Guid.NewGuid().ToString("N")
                : os.TrackingToken;

            os.KanbanColunaAtualId = colunaDestinoId;
            os.TrackingToken = token;
            os.UpdatedAt = DateTime.UtcNow;

            _context.KanbanCards.Add(new KanbanCard
            {
                EmpresaId = empresaId,
                OrdemServicoId = os.Id,
                KanbanColunaId = colunaDestinoId,
                PublicTrackingToken = token,
                Ordem = ordemAtual,
                Ativo = true,
                DataEntradaColunaAtual = DateTime.UtcNow,
                OcultoDoQuadro = false,
                DataOcultado = null
            });

            alterou = true;
        }

        if (alterou)
            await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ReordenarCardsPublicosAsync(
        Guid empresaId,
        KanbanCard card,
        Guid colunaDestinoId,
        int ordemDestino,
        CancellationToken cancellationToken)
    {
        if (card.KanbanColunaId == colunaDestinoId)
        {
            var cards = await _context.KanbanCards
                .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == card.KanbanColunaId && x.Ativo)
                .OrderBy(x => x.Ordem)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            cards.RemoveAll(x => x.Id == card.Id);

            var insertIndex = Math.Clamp((ordemDestino <= 0 ? cards.Count + 1 : ordemDestino) - 1, 0, cards.Count);
            cards.Insert(insertIndex, card);

            for (var i = 0; i < cards.Count; i++)
            {
                cards[i].KanbanColunaId = colunaDestinoId;
                cards[i].Ordem = i + 1;
                cards[i].UpdatedAt = DateTime.UtcNow;
            }

            return;
        }

        var origem = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == card.KanbanColunaId && x.Ativo && x.Id != card.Id)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < origem.Count; i++)
        {
            origem[i].Ordem = i + 1;
            origem[i].UpdatedAt = DateTime.UtcNow;
        }

        var destino = await _context.KanbanCards
            .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == colunaDestinoId && x.Ativo && x.Id != card.Id)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        card.KanbanColunaId = colunaDestinoId;
        card.PublicTrackingToken = string.IsNullOrWhiteSpace(card.PublicTrackingToken)
            ? Guid.NewGuid().ToString("N")
            : card.PublicTrackingToken;

        card.DataEntradaColunaAtual = DateTime.UtcNow;
        card.OcultoDoQuadro = false;
        card.DataOcultado = null;
        card.UpdatedAt = DateTime.UtcNow;

        var indexDestino = Math.Clamp((ordemDestino <= 0 ? destino.Count + 1 : ordemDestino) - 1, 0, destino.Count);
        destino.Insert(indexDestino, card);

        for (var i = 0; i < destino.Count; i++)
        {
            destino[i].KanbanColunaId = colunaDestinoId;
            destino[i].Ordem = i + 1;
            destino[i].UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task ReordenarTarefasPrivadasAsync(Guid empresaId, Guid usuarioId, KanbanTarefaPrivada tarefa, Guid colunaDestinoId, int ordemDestino, CancellationToken cancellationToken)
    {
        if (tarefa.KanbanColunaId == colunaDestinoId)
        {
            var tarefas = await _context.KanbanTarefasPrivadas
                .Where(x =>
                    x.EmpresaId == empresaId &&
                    x.UsuarioId == usuarioId &&
                    x.KanbanColunaId == tarefa.KanbanColunaId &&
                    x.Ativo)
                .OrderBy(x => x.Ordem)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            tarefas.RemoveAll(x => x.Id == tarefa.Id);

            var insertIndex = Math.Clamp((ordemDestino <= 0 ? tarefas.Count + 1 : ordemDestino) - 1, 0, tarefas.Count);
            tarefas.Insert(insertIndex, tarefa);

            for (var i = 0; i < tarefas.Count; i++)
            {
                tarefas[i].KanbanColunaId = colunaDestinoId;
                tarefas[i].Ordem = i + 1;
                tarefas[i].UpdatedAt = DateTime.UtcNow;
            }

            return;
        }

        var origem = await _context.KanbanTarefasPrivadas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.UsuarioId == usuarioId &&
                x.KanbanColunaId == tarefa.KanbanColunaId &&
                x.Ativo &&
                x.Id != tarefa.Id)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < origem.Count; i++)
        {
            origem[i].Ordem = i + 1;
            origem[i].UpdatedAt = DateTime.UtcNow;
        }

        var destino = await _context.KanbanTarefasPrivadas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.UsuarioId == usuarioId &&
                x.KanbanColunaId == colunaDestinoId &&
                x.Ativo &&
                x.Id != tarefa.Id)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        tarefa.KanbanColunaId = colunaDestinoId;

        var indexDestino = Math.Clamp((ordemDestino <= 0 ? destino.Count + 1 : ordemDestino) - 1, 0, destino.Count);
        destino.Insert(indexDestino, tarefa);

        for (var i = 0; i < destino.Count; i++)
        {
            destino[i].KanbanColunaId = colunaDestinoId;
            destino[i].Ordem = i + 1;
            destino[i].UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task MoverColunasFinaisParaFimAsync(Guid empresaId, Guid fluxoId, CancellationToken cancellationToken)
    {
        var colunas = await _context.KanbanColunas
            .Where(x => x.EmpresaId == empresaId && x.KanbanFluxoId == fluxoId && x.Ativa)
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        var naoFinais = colunas.Where(x => !x.EtapaFinal).ToList();
        var finais = colunas
            .Where(x => x.EtapaFinal)
            .OrderBy(x => x.TipoFinalizacao == "ENTREGUE" ? 1 : x.TipoFinalizacao == "CANCELADA" ? 2 : 99)
            .ThenBy(x => x.Ordem)
            .ToList();

        var ordenadas = naoFinais.Concat(finais).ToList();

        for (var i = 0; i < ordenadas.Count; i++)
        {
            ordenadas[i].Ordem = i + 1;
            ordenadas[i].UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task NormalizarColunasFinaisLegadasAsync(Guid empresaId, Guid fluxoId, CancellationToken cancellationToken)
    {
        var colunas = await _context.KanbanColunas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == fluxoId &&
                x.Ativa &&
                x.EtapaFinal &&
                string.IsNullOrWhiteSpace(x.TipoFinalizacao))
            .ToListAsync(cancellationToken);

        if (colunas.Count == 0)
            return;

        var alterou = false;

        foreach (var coluna in colunas)
        {
            var nome = $"{coluna.NomeInterno} {coluna.NomePublico}".Trim().ToUpperInvariant();

            if (nome.Contains("CANCEL"))
            {
                coluna.TipoFinalizacao = "CANCELADA";
                coluna.UpdatedAt = DateTime.UtcNow;
                alterou = true;
                continue;
            }

            if (nome.Contains("FINAL") || nome.Contains("ENTREG"))
            {
                coluna.TipoFinalizacao = "ENTREGUE";
                coluna.UpdatedAt = DateTime.UtcNow;
                alterou = true;
            }
        }

        if (alterou)
            await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ConsolidarColunasFinaisDuplicadasAsync(Guid empresaId, Guid fluxoId, CancellationToken cancellationToken)
    {
        var finais = await _context.KanbanColunas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanFluxoId == fluxoId &&
                x.Ativa &&
                x.EtapaFinal &&
                !string.IsNullOrWhiteSpace(x.TipoFinalizacao))
            .OrderBy(x => x.Ordem)
            .ToListAsync(cancellationToken);

        if (finais.Count == 0)
            return;

        foreach (var tipo in new[] { "ENTREGUE", "CANCELADA" })
        {
            var grupo = finais
                .Where(x => x.TipoFinalizacao == tipo)
                .OrderByDescending(x => x.Sistema)
                .ThenBy(x => x.Ordem)
                .ToList();

            if (grupo.Count <= 1)
                continue;

            var principal = grupo.First();
            var duplicadas = grupo.Skip(1).ToList();

            var proximaOrdem = await _context.KanbanCards
                .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == principal.Id && x.Ativo)
                .Select(x => (int?)x.Ordem)
                .MaxAsync(cancellationToken) ?? 0;

            foreach (var duplicada in duplicadas)
            {
                var cards = await _context.KanbanCards
                    .Where(x => x.EmpresaId == empresaId && x.KanbanColunaId == duplicada.Id && x.Ativo)
                    .OrderBy(x => x.Ordem)
                    .ToListAsync(cancellationToken);

                foreach (var card in cards)
                {
                    proximaOrdem++;
                    card.KanbanColunaId = principal.Id;
                    card.Ordem = proximaOrdem;
                    card.UpdatedAt = DateTime.UtcNow;
                }

                var ordens = await _context.OrdensServico
                    .Where(x => x.EmpresaId == empresaId && x.KanbanColunaAtualId == duplicada.Id)
                    .ToListAsync(cancellationToken);

                foreach (var ordem in ordens)
                {
                    ordem.KanbanColunaAtualId = principal.Id;
                    ordem.UpdatedAt = DateTime.UtcNow;
                }

                duplicada.Ativa = false;
                duplicada.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static KanbanConfiguracaoColunaDto MapConfiguracao(KanbanColuna coluna)
    {
        return new KanbanConfiguracaoColunaDto
        {
            Id = coluna.Id,
            NomeInterno = coluna.NomeInterno,
            NomePublico = coluna.NomePublico,
            Cor = coluna.Cor,
            Ordem = coluna.Ordem,
            Sistema = coluna.Sistema,
            Ativa = coluna.Ativa,
            VisivelCliente = coluna.VisivelCliente,
            GeraEventoCliente = coluna.GeraEventoCliente,
            EtapaFinal = coluna.EtapaFinal,
            TipoFinalizacao = coluna.TipoFinalizacao,
            PermiteEnvioWhatsApp = coluna.PermiteEnvioWhatsApp,
            DescricaoPublica = coluna.DescricaoPublica
        };
    }

    private async Task<KanbanPublicoCardDto?> ObterCardPublicoPorOrdemServicoAsync(
        Guid empresaId,
        Guid ordemServicoId,
        CancellationToken cancellationToken)
    {
        return await _context.KanbanCards
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.OrdemServicoId == ordemServicoId && x.Ativo)
            .Select(x => new KanbanPublicoCardDto
            {
                Id = x.Id,
                OrdemServicoId = x.OrdemServicoId,
                KanbanColunaId = x.KanbanColunaId,
                PublicTrackingToken = x.PublicTrackingToken,
                NumeroOs = x.OrdemServico != null ? x.OrdemServico.NumeroOs.ToString() : string.Empty,
                Cliente = x.OrdemServico != null && x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Nome : string.Empty,
                TelefoneCliente = x.OrdemServico != null && x.OrdemServico.Cliente != null ? x.OrdemServico.Cliente.Telefone : null,
                Aparelho = x.OrdemServico != null && x.OrdemServico.Aparelho != null
                    ? ((x.OrdemServico.Aparelho.Marca ?? string.Empty) + " " + (x.OrdemServico.Aparelho.Modelo ?? string.Empty)).Trim()
                    : string.Empty,
                Defeito = x.OrdemServico != null ? x.OrdemServico.DefeitoRelatado : null,
                Tecnico = x.OrdemServico != null && x.OrdemServico.Tecnico != null ? x.OrdemServico.Tecnico.Nome : null,
                ValorTotal = x.OrdemServico != null ? x.OrdemServico.ValorTotal : null,
                StatusFinanceiro = null,
                StatusPeca = null,
                Atrasada = x.OrdemServico != null
                    && x.OrdemServico.DataPrevisao.HasValue
                    && x.OrdemServico.DataPrevisao.Value < DateTime.UtcNow
                    && !x.KanbanColuna!.EtapaFinal,
                Ordem = x.Ordem
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? NormalizarTipoFinalizacao(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var tipo = valor.Trim().ToUpperInvariant();

        return tipo switch
        {
            "ENTREGUE" => "ENTREGUE",
            "CANCELADA" => "CANCELADA",
            _ => null
        };
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string NormalizarCor(string? cor)
    {
        if (string.IsNullOrWhiteSpace(cor))
            return "#CBD5E1";

        var valor = cor.Trim();
        return valor.StartsWith("#") ? valor : "#CBD5E1";
    }
}