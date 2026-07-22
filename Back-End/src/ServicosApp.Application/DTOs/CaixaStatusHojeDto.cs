namespace ServicosApp.Application.DTOs;

public class CaixaStatusHojeDto
{
    public bool Aberto { get; set; }
    public Guid? CaixaId { get; set; }
    public DateOnly? DataCaixa { get; set; }
    public bool EhHoje { get; set; }
    public decimal? ValorFechamentoSistema { get; set; }
}
