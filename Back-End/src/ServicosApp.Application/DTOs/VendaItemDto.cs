namespace ServicosApp.Application.DTOs;

public class VendaItemDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }

    public Guid VendaId { get; set; }
    public Guid PecaId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}