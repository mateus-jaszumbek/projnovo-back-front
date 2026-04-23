namespace ServicosApp.Domain.Entities;

public class CampoModuloLayout : EmpresaOwnedEntity
{
    public Guid ModuloPersonalizadoId { get; set; }
    public ModuloPersonalizado? ModuloPersonalizado { get; set; }

    public string CampoChave { get; set; } = string.Empty;
    public string Aba { get; set; } = "Principal";
    public int Linha { get; set; } = 1;
    public int Posicao { get; set; } = 1;
    public int Ordem { get; set; } = 1;
}
