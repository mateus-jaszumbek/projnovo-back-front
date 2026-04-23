using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class GestaoService : IGestaoService
{
    private readonly AppDbContext _context;

    public GestaoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarCompraPecaAsync(Guid empresaId, Guid? usuarioId, CompraPecaDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Quantidade <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");

        if (dto.CustoUnitario < 0)
            throw new InvalidOperationException("Custo unitário não pode ser negativo.");

        var peca = await _context.Pecas
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == dto.PecaId && x.Ativo, cancellationToken);

        if (peca is null)
            throw new InvalidOperationException("Peça não encontrada.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var total = dto.Quantidade * dto.CustoUnitario;
        peca.EstoqueAtual += dto.Quantidade;
        peca.CustoUnitario = dto.CustoUnitario;

        _context.EstoqueMovimentos.Add(new EstoqueMovimento
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            PecaId = peca.Id,
            TipoMovimento = "COMPRA",
            OrigemTipo = "COMPRA_PECA",
            OrigemId = peca.Id,
            Quantidade = dto.Quantidade,
            CustoUnitario = dto.CustoUnitario,
            Observacao = string.IsNullOrWhiteSpace(dto.Observacoes) ? $"Compra de {peca.Nome}" : dto.Observacoes.Trim(),
            CreatedBy = usuarioId,
            DataMovimento = DateTime.UtcNow
        });

        if (dto.GerarContaPagar && total > 0)
        {
            var fornecedor = await ObterFornecedorAsync(empresaId, dto.FornecedorId, cancellationToken);
            _context.ContasPagar.Add(new ContaPagar
            {
                Id = Guid.NewGuid(),
                EmpresaId = empresaId,
                Descricao = $"Compra de {peca.Nome}",
                FornecedorId = fornecedor?.Id,
                Fornecedor = string.IsNullOrWhiteSpace(dto.Fornecedor) ? fornecedor?.Nome : dto.Fornecedor.Trim(),
                Categoria = "COMPRA_PECAS",
                DataEmissao = DateOnly.FromDateTime(DateTime.UtcNow),
                DataVencimento = dto.DataVencimento,
                Valor = total,
                ValorPago = 0,
                Status = "PENDENTE",
                Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Fornecedor?> ObterFornecedorAsync(Guid empresaId, Guid? fornecedorId, CancellationToken cancellationToken)
    {
        if (!fornecedorId.HasValue)
            return null;

        var fornecedor = await _context.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == fornecedorId.Value && x.Ativo, cancellationToken);

        if (fornecedor is null)
            throw new InvalidOperationException("Fornecedor nao encontrado.");

        return fornecedor;
    }

    public async Task<DreGerencialDto> ObterDreAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
    {
        var inicioDate = inicio?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var fimDate = (fim?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue);

        var vendas = await _context.Vendas
            .AsNoTracking()
            .Include(x => x.Itens)
            .Where(x => x.EmpresaId == empresaId && x.Status == "FECHADA" && x.DataVenda >= inicioDate && x.DataVenda <= fimDate)
            .ToListAsync(cancellationToken);

        var receita = vendas.Sum(x => x.ValorTotal);
        var custo = vendas.Sum(x => x.Itens.Sum(item => item.CustoUnitario * item.Quantidade));

        var inicioOnly = inicio ?? DateOnly.MinValue;
        var fimOnly = fim ?? DateOnly.MaxValue;

        var despesasPagas = await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.DataVencimento >= inicioOnly && x.DataVencimento <= fimOnly)
            .SumAsync(x => x.ValorPago, cancellationToken);

        var despesasPendentes = await _context.ContasPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Status != "PAGO" && x.DataVencimento >= inicioOnly && x.DataVencimento <= fimOnly)
            .SumAsync(x => x.Valor - x.ValorPago, cancellationToken);

        var lucroBruto = receita - custo;
        var lucroLiquido = lucroBruto - despesasPagas;

        return new DreGerencialDto
        {
            ReceitaBruta = receita,
            CustoPecas = custo,
            DespesasPagas = despesasPagas,
            DespesasPendentes = despesasPendentes,
            LucroBruto = lucroBruto,
            LucroLiquido = lucroLiquido,
            MargemBrutaPercentual = receita > 0 ? Math.Round((lucroBruto / receita) * 100, 2) : 0,
            MargemLiquidaPercentual = receita > 0 ? Math.Round((lucroLiquido / receita) * 100, 2) : 0
        };
    }

    public async Task<List<ComissaoDto>> ListarComissoesAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, decimal percentualVendas, decimal percentualServicos, CancellationToken cancellationToken = default)
    {
        var inicioDate = inicio?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var fimDate = fim?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        var vendas = await _context.Vendas
            .AsNoTracking()
            .Include(x => x.UsuarioCriacao)
            .Where(x => x.EmpresaId == empresaId && x.Status == "FECHADA" && x.DataVenda >= inicioDate && x.DataVenda <= fimDate)
            .GroupBy(x => new { x.CreatedBy, Nome = x.UsuarioCriacao != null ? x.UsuarioCriacao.Nome : "Sem vendedor" })
            .Select(g => new ComissaoDto
            {
                Tipo = "VENDEDOR",
                PessoaId = g.Key.CreatedBy,
                PessoaNome = g.Key.Nome,
                BaseCalculo = g.Sum(x => x.ValorTotal),
                Percentual = percentualVendas,
                ValorComissao = g.Sum(x => x.ValorTotal) * percentualVendas / 100
            })
            .ToListAsync(cancellationToken);

        var servicos = await _context.OrdensServico
            .AsNoTracking()
            .Include(x => x.Tecnico)
            .Where(x => x.EmpresaId == empresaId && x.DataEntrada >= inicioDate && x.DataEntrada <= fimDate && x.Status != "CANCELADA")
            .GroupBy(x => new { x.TecnicoId, Nome = x.Tecnico != null ? x.Tecnico.Nome : "Sem técnico" })
            .Select(g => new ComissaoDto
            {
                Tipo = "TECNICO",
                PessoaId = g.Key.TecnicoId,
                PessoaNome = g.Key.Nome,
                BaseCalculo = g.Sum(x => x.ValorMaoObra),
                Percentual = percentualServicos,
                ValorComissao = g.Sum(x => x.ValorMaoObra) * percentualServicos / 100
            })
            .ToListAsync(cancellationToken);

        return vendas.Concat(servicos).OrderBy(x => x.Tipo).ThenBy(x => x.PessoaNome).ToList();
    }

    public async Task<List<AuditoriaFinanceiraDto>> ListarAuditoriaFinanceiraAsync(Guid empresaId, DateOnly? inicio, DateOnly? fim, CancellationToken cancellationToken = default)
    {
        var inicioDate = inicio?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var fimDate = fim?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        return await _context.CaixaLancamentos
            .AsNoTracking()
            .Include(x => x.UsuarioCriacao)
            .Where(x => x.EmpresaId == empresaId && x.CreatedAt >= inicioDate && x.CreatedAt <= fimDate)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AuditoriaFinanceiraDto
            {
                Data = x.CreatedAt,
                Tipo = x.Tipo,
                OrigemTipo = x.OrigemTipo ?? "MANUAL",
                OrigemId = x.OrigemId,
                FormaPagamento = x.FormaPagamento,
                Valor = x.Valor,
                Observacao = x.Observacao,
                UsuarioId = x.CreatedBy,
                UsuarioNome = x.UsuarioCriacao != null ? x.UsuarioCriacao.Nome : null
            })
            .ToListAsync(cancellationToken);
    }
}
