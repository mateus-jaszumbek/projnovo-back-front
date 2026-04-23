namespace ServicosApp.Domain.Entities;

public class CampoPersonalizado : EmpresaOwnedEntity
{
    public Guid ModuloPersonalizadoId { get; set; }
    public ModuloPersonalizado? ModuloPersonalizado { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string Tipo { get; set; } = "text";
    public bool Obrigatorio { get; set; }
    public string Aba { get; set; } = "Principal";
    public int Linha { get; set; } = 1;
    public int Posicao { get; set; } = 1;
    public int Ordem { get; set; } = 1;
    public string? Placeholder { get; set; }
    public string? ValorPadrao { get; set; }
    public string? OpcoesJson { get; set; }
    public bool ExportarExcel { get; set; } = true;
    public bool ExportarExcelResumo { get; set; }
    public bool ExportarPdf { get; set; } = true;
    public bool Ativo { get; set; } = true;
}
