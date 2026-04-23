using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class OrdemServicoService : IOrdemServicoService
{
    private readonly AppDbContext _context;
    private readonly IKanbanService _kanbanService;

    public OrdemServicoService(AppDbContext context, IKanbanService kanbanService)
    {
        _context = context;
        _kanbanService = kanbanService;
    }

    public async Task<OrdemServicoDto> CriarAsync(
        Guid empresaId,
        CreateOrdemServicoDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarDto(dto);

        await ValidarRelacionamentosAsync(
            empresaId,
            dto.ClienteId,
            dto.AparelhoId,
            dto.TecnicoId,
            cancellationToken);

        var proximoNumero = await ObterProximoNumeroAsync(empresaId, cancellationToken);

        var entity = new OrdemServico
        {
            EmpresaId = empresaId,
            NumeroOs = proximoNumero,
            ClienteId = dto.ClienteId,
            AparelhoId = dto.AparelhoId,
            TecnicoId = dto.TecnicoId,
            Status = "ABERTA",
            DefeitoRelatado = dto.DefeitoRelatado.Trim(),
            Diagnostico = Normalizar(dto.Diagnostico),
            LaudoTecnico = Normalizar(dto.LaudoTecnico),
            ObservacoesInternas = Normalizar(dto.ObservacoesInternas),
            ObservacoesCliente = Normalizar(dto.ObservacoesCliente),
            ValorMaoObra = dto.ValorMaoObra,
            ValorPecas = 0,
            Desconto = dto.Desconto,
            ValorTotal = dto.ValorMaoObra - dto.Desconto,
            DataEntrada = DateTime.UtcNow,
            GarantiaDias = dto.GarantiaDias,
            DataPrevisao = dto.DataPrevisao
        };

        if (entity.ValorTotal < 0)
            entity.ValorTotal = 0;

        _context.OrdensServico.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return await ObterDtoAsync(empresaId, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar a OS criada.");
    }

    public async Task<List<OrdemServicoDto>> ListarAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.OrdensServico
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderByDescending(x => x.NumeroOs)
            .Select(x => new
            {
                Dto = new OrdemServicoDto
                {
                    Id = x.Id,
                    EmpresaId = x.EmpresaId,
                    NumeroOs = x.NumeroOs,
                    ClienteId = x.ClienteId,
                    ClienteNome = x.Cliente != null ? x.Cliente.Nome : string.Empty,
                    AparelhoId = x.AparelhoId,
                    AparelhoDescricao = x.Aparelho != null ? x.Aparelho.Marca + " " + x.Aparelho.Modelo : string.Empty,
                    TecnicoId = x.TecnicoId,
                    TecnicoNome = x.Tecnico != null ? x.Tecnico.Nome : null,
                    Status = x.Status,
                    DefeitoRelatado = x.DefeitoRelatado,
                    Diagnostico = x.Diagnostico,
                    LaudoTecnico = x.LaudoTecnico,
                    ObservacoesInternas = x.ObservacoesInternas,
                    ObservacoesCliente = x.ObservacoesCliente,
                    EmpresaLogoUrl = x.Empresa != null ? x.Empresa.LogoUrl : null,
                    ValorMaoObra = x.ValorMaoObra,
                    ValorPecas = x.ValorPecas,
                    Desconto = x.Desconto,
                    ValorTotal = x.ValorTotal,
                    DataEntrada = x.DataEntrada,
                    DataPrevisao = x.DataPrevisao,
                    DataAprovacao = x.DataAprovacao,
                    DataConclusao = x.DataConclusao,
                    DataEntrega = x.DataEntrega,
                    GarantiaDias = x.GarantiaDias,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                },
                x.FotosJson
            })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
            row.Dto.Fotos = OrdemServicoFotoJson.Parse(row.FotosJson);

        return rows.Select(row => row.Dto).ToList();
    }
    public async Task<OrdemServicoDto?> ObterPorIdAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await ObterDtoAsync(empresaId, id, cancellationToken);
    }

    public async Task<OrdemServicoDto?> AtualizarAsync(
        Guid empresaId,
        Guid id,
        UpdateOrdemServicoDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarDto(dto);

        var entity = await _context.OrdensServico
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        if (entity.Status == "CANCELADA" || entity.Status == "ENTREGUE")
            throw new InvalidOperationException("Não é possível alterar uma OS cancelada ou entregue.");

        await ValidarRelacionamentosAsync(
            empresaId,
            dto.ClienteId,
            dto.AparelhoId,
            dto.TecnicoId,
            cancellationToken);

        entity.ClienteId = dto.ClienteId;
        entity.AparelhoId = dto.AparelhoId;
        entity.TecnicoId = dto.TecnicoId;
        entity.DefeitoRelatado = dto.DefeitoRelatado.Trim();
        entity.Diagnostico = Normalizar(dto.Diagnostico);
        entity.LaudoTecnico = Normalizar(dto.LaudoTecnico);
        entity.ObservacoesInternas = Normalizar(dto.ObservacoesInternas);
        entity.ObservacoesCliente = Normalizar(dto.ObservacoesCliente);
        entity.ValorMaoObra = dto.ValorMaoObra;
        entity.Desconto = dto.Desconto;
        entity.GarantiaDias = dto.GarantiaDias;
        entity.DataPrevisao = dto.DataPrevisao;
        entity.UpdatedBy = dto.UpdatedBy;

        RecalcularTotais(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return await ObterDtoAsync(empresaId, entity.Id, cancellationToken);
    }

    public async Task<OrdemServicoDto?> AlterarStatusAsync(
        Guid empresaId,
        Guid id,
        AlterarStatusOrdemServicoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
            throw new InvalidOperationException("Status é obrigatório.");

        var novoStatus = dto.Status.Trim().ToUpperInvariant();

        var entity = await _context.OrdensServico
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        if (entity.Status == "CANCELADA" && novoStatus != "CANCELADA")
            throw new InvalidOperationException("Não é possível alterar uma OS cancelada.");

        entity.Status = novoStatus;

        switch (novoStatus)
        {
            case "APROVADA":
                entity.DataAprovacao ??= DateTime.UtcNow;
                break;

            case "PRONTA":
                entity.DataConclusao ??= DateTime.UtcNow;
                break;

            case "ENTREGUE":
                entity.DataConclusao ??= DateTime.UtcNow;
                entity.DataEntrega ??= DateTime.UtcNow;
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await ObterDtoAsync(empresaId, id, cancellationToken);
    }

    public async Task<bool> CancelarAsync(
        Guid empresaId,
        Guid usuarioId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.OrdensServico
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (entity is null)
            return false;

        if (entity.Status == "CANCELADA")
            return true;

        entity.Status = "CANCELADA";
        entity.DataConclusao ??= DateTime.UtcNow;
        entity.DataEntrega = null;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var colunaCanceladaId = await _context.KanbanColunas
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Ativa &&
                x.EtapaFinal &&
                x.TipoFinalizacao == "CANCELADA" &&
                x.KanbanFluxo != null &&
                x.KanbanFluxo.Tipo == "PUBLICO")
            .OrderBy(x => x.Ordem)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (colunaCanceladaId == Guid.Empty)
            return true;

        var proximaOrdem = await _context.KanbanCards
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.KanbanColunaId == colunaCanceladaId &&
                x.Ativo)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(cancellationToken) ?? 0;

        await _kanbanService.MoverCardPublicoAsync(
            empresaId,
            id,
            new MoveKanbanPublicoCardDto
            {
                ColunaId = colunaCanceladaId,
                Ordem = proximaOrdem + 1
            },
            usuarioId,
            cancellationToken);

        return true;
    }

    private async Task<long> ObterProximoNumeroAsync(Guid empresaId, CancellationToken cancellationToken)
    {
        var ultimoNumero = await _context.OrdensServico
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => (long?)x.NumeroOs)
            .MaxAsync(cancellationToken);

        return (ultimoNumero ?? 0) + 1;
    }

    private async Task ValidarRelacionamentosAsync(
        Guid empresaId,
        Guid clienteId,
        Guid aparelhoId,
        Guid? tecnicoId,
        CancellationToken cancellationToken)
    {
        var clienteExiste = await _context.Clientes
            .AsNoTracking()
            .AnyAsync(x => x.EmpresaId == empresaId && x.Id == clienteId, cancellationToken);

        if (!clienteExiste)
            throw new InvalidOperationException("Cliente não encontrado para esta empresa.");

        var aparelhoValido = await _context.Aparelhos
            .AsNoTracking()
            .AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.Id == aparelhoId &&
                x.ClienteId == clienteId &&
                x.Ativo,
                cancellationToken);

        if (!aparelhoValido)
            throw new InvalidOperationException("Aparelho não encontrado para este cliente nesta empresa.");

        if (tecnicoId.HasValue)
        {
            var tecnicoExiste = await _context.Tecnicos
                .AsNoTracking()
                .AnyAsync(x => x.EmpresaId == empresaId && x.Id == tecnicoId.Value, cancellationToken);

            if (!tecnicoExiste)
                throw new InvalidOperationException("Técnico não encontrado para esta empresa.");
        }
    }

    private static void ValidarDto(CreateOrdemServicoDto dto)
    {
        if (dto.ClienteId == Guid.Empty)
            throw new InvalidOperationException("Cliente é obrigatório.");

        if (dto.AparelhoId == Guid.Empty)
            throw new InvalidOperationException("Aparelho é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.DefeitoRelatado))
            throw new InvalidOperationException("Defeito relatado é obrigatório.");

        if (dto.ValorMaoObra < 0)
            throw new InvalidOperationException("Valor da mão de obra não pode ser negativo.");

        if (dto.Desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        if (dto.GarantiaDias < 0)
            throw new InvalidOperationException("Garantia não pode ser negativa.");
    }

    private static void ValidarDto(UpdateOrdemServicoDto dto)
    {
        if (dto.ClienteId == Guid.Empty)
            throw new InvalidOperationException("Cliente é obrigatório.");

        if (dto.AparelhoId == Guid.Empty)
            throw new InvalidOperationException("Aparelho é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.DefeitoRelatado))
            throw new InvalidOperationException("Defeito relatado é obrigatório.");

        if (dto.ValorMaoObra < 0)
            throw new InvalidOperationException("Valor da mão de obra não pode ser negativo.");

        if (dto.Desconto < 0)
            throw new InvalidOperationException("Desconto não pode ser negativo.");

        if (dto.GarantiaDias < 0)
            throw new InvalidOperationException("Garantia não pode ser negativa.");
    }

    private static void RecalcularTotais(OrdemServico entity)
    {
        entity.ValorPecas = entity.Itens
            .Where(x => x.TipoItem == "PECA")
            .Sum(x => x.ValorTotal);

        entity.ValorTotal = entity.ValorMaoObra + entity.ValorPecas - entity.Desconto;

        if (entity.ValorTotal < 0)
            entity.ValorTotal = 0;
    }

    private async Task<OrdemServicoDto?> ObterDtoAsync(
        Guid empresaId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await _context.OrdensServico
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == id)
            .Select(x => new
            {
                Dto = new OrdemServicoDto
                {
                    Id = x.Id,
                    EmpresaId = x.EmpresaId,
                    NumeroOs = x.NumeroOs,
                    ClienteId = x.ClienteId,
                    ClienteNome = x.Cliente != null ? x.Cliente.Nome : string.Empty,
                    AparelhoId = x.AparelhoId,
                    AparelhoDescricao = x.Aparelho != null ? x.Aparelho.Marca + " " + x.Aparelho.Modelo : string.Empty,
                    TecnicoId = x.TecnicoId,
                    TecnicoNome = x.Tecnico != null ? x.Tecnico.Nome : null,
                    Status = x.Status,
                    DefeitoRelatado = x.DefeitoRelatado,
                    Diagnostico = x.Diagnostico,
                    LaudoTecnico = x.LaudoTecnico,
                    ObservacoesInternas = x.ObservacoesInternas,
                    ObservacoesCliente = x.ObservacoesCliente,
                    EmpresaLogoUrl = x.Empresa != null ? x.Empresa.LogoUrl : null,
                    ValorMaoObra = x.ValorMaoObra,
                    ValorPecas = x.ValorPecas,
                    Desconto = x.Desconto,
                    ValorTotal = x.ValorTotal,
                    DataEntrada = x.DataEntrada,
                    DataPrevisao = x.DataPrevisao,
                    DataAprovacao = x.DataAprovacao,
                    DataConclusao = x.DataConclusao,
                    DataEntrega = x.DataEntrega,
                    GarantiaDias = x.GarantiaDias,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                },
                x.FotosJson
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        row.Dto.Fotos = OrdemServicoFotoJson.Parse(row.FotosJson);
        return row.Dto;
    }
    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}