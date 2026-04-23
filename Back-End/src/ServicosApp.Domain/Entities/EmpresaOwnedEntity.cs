namespace ServicosApp.Domain.Entities;

public abstract class EmpresaOwnedEntity : EntityBase
{
    public Guid EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}