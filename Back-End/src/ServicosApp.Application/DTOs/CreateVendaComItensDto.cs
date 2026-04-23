using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CreateVendaComItensDto : CreateVendaDto
{
    [MinLength(1, ErrorMessage = "Adicione pelo menos um item na venda.")]
    public List<CreateVendaItemDto> Itens { get; set; } = new();

    public List<VendaParcelaDto> Parcelas { get; set; } = new();

    public bool Finalizar { get; set; }
}
