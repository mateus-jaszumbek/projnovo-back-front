namespace ServicosApp.Application.DTOs.Fornecedores;

public class UpdateFornecedorDto : CreateFornecedorDto
{
    public bool Ativo { get; set; } = true;
}
