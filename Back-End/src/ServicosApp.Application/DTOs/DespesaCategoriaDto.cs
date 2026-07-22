namespace ServicosApp.Application.DTOs;

public class DespesaCategoriaDto
{
    public string Categoria { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal TotalValor { get; set; }
    public decimal TotalPago { get; set; }
    public decimal TotalPendente { get; set; }
}
