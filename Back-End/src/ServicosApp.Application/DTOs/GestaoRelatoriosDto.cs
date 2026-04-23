namespace ServicosApp.Application.DTOs;

public class DreGerencialDto
{
    public decimal ReceitaBruta { get; set; }
    public decimal CustoPecas { get; set; }
    public decimal DespesasPagas { get; set; }
    public decimal DespesasPendentes { get; set; }
    public decimal LucroBruto { get; set; }
    public decimal LucroLiquido { get; set; }
    public decimal MargemBrutaPercentual { get; set; }
    public decimal MargemLiquidaPercentual { get; set; }
}

public class ComissaoDto
{
    public string Tipo { get; set; } = string.Empty;
    public Guid? PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public decimal BaseCalculo { get; set; }
    public decimal Percentual { get; set; }
    public decimal ValorComissao { get; set; }
}

public class AuditoriaFinanceiraDto
{
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string OrigemTipo { get; set; } = string.Empty;
    public Guid? OrigemId { get; set; }
    public string? FormaPagamento { get; set; }
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
}
