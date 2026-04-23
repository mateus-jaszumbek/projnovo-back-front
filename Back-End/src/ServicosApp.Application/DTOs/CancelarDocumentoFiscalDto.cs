using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class CancelarDocumentoFiscalDto
{
    [Required(ErrorMessage = "Motivo é obrigatório.")]
    [MaxLength(1000)]
    public string Motivo { get; set; } = string.Empty;
}