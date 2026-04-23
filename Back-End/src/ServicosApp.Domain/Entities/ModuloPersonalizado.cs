namespace ServicosApp.Domain.Entities;

public class ModuloPersonalizado : EmpresaOwnedEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;

    public List<CampoPersonalizado> Campos { get; set; } = new();
    public List<RegistroPersonalizado> Registros { get; set; } = new();
}
