namespace ServicosApp.Application.DTOs;

public class CampoPersonalizadoDto
{
    public Guid Id { get; set; }
    public Guid ModuloPersonalizadoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Obrigatorio { get; set; }
    public string Aba { get; set; } = "Principal";
    public int Linha { get; set; }
    public int Posicao { get; set; }
    public int Ordem { get; set; }
    public string? Placeholder { get; set; }
    public string? ValorPadrao { get; set; }
    public List<string> Opcoes { get; set; } = new();
    public bool ExportarExcel { get; set; }
    public bool ExportarExcelResumo { get; set; }
    public bool ExportarPdf { get; set; }
    public bool Ativo { get; set; }
}
