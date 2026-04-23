namespace ServicosApp.Domain.Entities;

public class RegistroPersonalizado : EmpresaOwnedEntity
{
    public Guid ModuloPersonalizadoId { get; set; }
    public ModuloPersonalizado? ModuloPersonalizado { get; set; }

    public Guid? OrigemId { get; set; }
    public string ValoresJson { get; set; } = "{}";
    public bool Ativo { get; set; } = true;
}
