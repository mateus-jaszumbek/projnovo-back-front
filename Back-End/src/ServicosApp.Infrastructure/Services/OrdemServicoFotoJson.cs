using System.Text.Json;
using ServicosApp.Application.DTOs;

namespace ServicosApp.Infrastructure.Services;

public static class OrdemServicoFotoJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static List<OrdemServicoFotoDto> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<OrdemServicoFotoDto>();

        try
        {
            return JsonSerializer.Deserialize<List<OrdemServicoFotoDto>>(value, JsonOptions)
                ?? new List<OrdemServicoFotoDto>();
        }
        catch
        {
            return new List<OrdemServicoFotoDto>();
        }
    }

    public static string Serialize(IEnumerable<OrdemServicoFotoDto> fotos)
        => JsonSerializer.Serialize(fotos, JsonOptions);
}
