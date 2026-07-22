namespace ServicosApp.Application.DTOs;

public class ResumoMensalDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public decimal Receita { get; set; }
    public decimal Custo { get; set; }
    public decimal Despesas { get; set; }
    public decimal LucroLiquido { get; set; }
    public int QuantidadeVendas { get; set; }
}
