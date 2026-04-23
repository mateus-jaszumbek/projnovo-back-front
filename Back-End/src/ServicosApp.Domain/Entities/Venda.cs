namespace ServicosApp.Domain.Entities;

public class Venda : EmpresaOwnedEntity
{
    public long NumeroVenda { get; set; }
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public string Status { get; set; } = "ABERTA";
    public string FormaPagamento { get; set; } = "DINHEIRO";
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public Usuario? UsuarioCriacao { get; set; }
    public List<VendaItem> Itens { get; set; } = new();
}