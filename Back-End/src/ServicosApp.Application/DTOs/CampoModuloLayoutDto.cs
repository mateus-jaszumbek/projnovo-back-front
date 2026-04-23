namespace ServicosApp.Application.DTOs;

public class CampoModuloLayoutDto
{
    public string CampoChave { get; set; } = string.Empty;
    public string Aba { get; set; } = "Principal";
    public int Linha { get; set; }
    public int Posicao { get; set; }
    public int Ordem { get; set; }
}
