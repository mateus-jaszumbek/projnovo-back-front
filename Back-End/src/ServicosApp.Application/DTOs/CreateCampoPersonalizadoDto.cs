using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateCampoPersonalizadoDto
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Tipo { get; set; } = string.Empty;

    public bool Obrigatorio { get; set; }
    [MaxLength(80)]
    public string? Aba { get; set; }
    public int Linha { get; set; } = 1;
    public int Posicao { get; set; } = 1;
    public int Ordem { get; set; } = 1;

    [MaxLength(150)]
    public string? Placeholder { get; set; }

    [MaxLength(300)]
    public string? ValorPadrao { get; set; }

    public List<string> Opcoes { get; set; } = new();
    public bool ExportarExcel { get; set; } = true;
    public bool ExportarExcelResumo { get; set; }
    public bool ExportarPdf { get; set; } = true;
}
