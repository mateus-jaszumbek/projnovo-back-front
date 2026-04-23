namespace ServicosApp.Domain.Entities;

public class OrdemServicoItem : EmpresaOwnedEntity
{
    public Guid OrdemServicoId { get; set; }
    public OrdemServico? OrdemServico { get; set; }

    public string TipoItem { get; set; } = "SERVICO";
    public int Ordem { get; set; }

    public Guid? ServicoCatalogoId { get; set; }
    public ServicoCatalogo? ServicoCatalogo { get; set; }

    public Guid? PecaId { get; set; }
    public Peca? Peca { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Quantidade { get; set; } = 1;
    public decimal CustoUnitario { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal ValorTotal { get; set; }

    public int GarantiaDias { get; set; }
}
