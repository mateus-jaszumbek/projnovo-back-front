namespace ServicosApp.Domain.Entities;

public class CaixaDiario
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public DateOnly DataCaixa { get; set; }

    public decimal ValorAbertura { get; set; }
    public decimal ValorFechamentoSistema { get; set; }
    public decimal? ValorFechamentoInformado { get; set; }
    public decimal? Diferenca { get; set; }

    public string Status { get; set; } = "ABERTO";

    public Guid? AbertoPor { get; set; }
    public Usuario? UsuarioAbertura { get; set; }

    public Guid? FechadoPor { get; set; }
    public Usuario? UsuarioFechamento { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
    public DateTime? DataFechamento { get; set; }

    public string? Observacoes { get; set; }

    public List<CaixaLancamento> Lancamentos { get; set; } = new();
}