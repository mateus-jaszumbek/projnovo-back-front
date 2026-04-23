namespace ServicosApp.Application.DTOs;

public class CampoLayoutDto
{
    public Guid Id { get; set; }
    public string Aba { get; set; } = "Principal";
    public int Linha { get; set; }
    public int Posicao { get; set; }
    public int Ordem { get; set; }
}
