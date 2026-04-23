namespace ServicosApp.Domain.Entities;

public class VendaItem : EmpresaOwnedEntity
{
    public Guid VendaId { get; set; }
    public Venda? Venda { get; set; }

    public Guid PecaId { get; set; }
    public Peca? Peca { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Quantidade { get; set; } = 1;
    public decimal CustoUnitario { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }
}