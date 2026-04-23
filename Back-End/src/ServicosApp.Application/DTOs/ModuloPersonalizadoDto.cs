namespace ServicosApp.Application.DTOs;

public class ModuloPersonalizadoDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
    public List<CampoPersonalizadoDto> Campos { get; set; } = new();
}
