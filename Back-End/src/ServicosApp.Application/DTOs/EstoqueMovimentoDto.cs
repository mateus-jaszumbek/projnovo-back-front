namespace ServicosApp.Application.DTOs;

public class EstoqueMovimentoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PecaId { get; set; }

    public string TipoMovimento { get; set; } = string.Empty;
    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }

    public decimal Quantidade { get; set; }
    public decimal? CustoUnitario { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; }

    public DateTime DataMovimento { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}