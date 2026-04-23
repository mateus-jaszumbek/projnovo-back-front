namespace ServicosApp.Domain.Entities;

public class EstoqueMovimento
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public Guid PecaId { get; set; }
    public Peca? Peca { get; set; }

    public string TipoMovimento { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }

    public string? OrigemTipo { get; set; }
    public Guid? OrigemId { get; set; }

    public string? Observacao { get; set; }

    public Guid? CreatedBy { get; set; }
    public Usuario? UsuarioCriacao { get; set; }
    public DateTime DataMovimento { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}