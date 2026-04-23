using System.ComponentModel.DataAnnotations;

namespace ServicosApp.Application.DTOs;

public class EmitirDfeVendaDto
{
    public DateTime? DataEmissao { get; set; }

    [MaxLength(1000)]
    public string? ObservacoesNota { get; set; }

    public bool GerarContaReceber { get; set; } = false;

    public bool ValidarTributacaoCompleta { get; set; } = true;
}
