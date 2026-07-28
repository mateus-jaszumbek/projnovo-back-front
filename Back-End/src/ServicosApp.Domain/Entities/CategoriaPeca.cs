namespace ServicosApp.Domain.Entities;

public class CategoriaPeca : EmpresaOwnedEntity
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    public List<Peca> Pecas { get; set; } = new();
}
