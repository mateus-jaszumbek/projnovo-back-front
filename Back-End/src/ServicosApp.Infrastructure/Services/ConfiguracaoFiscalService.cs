using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Domain.Enums;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ConfiguracaoFiscalService : IConfiguracaoFiscalService
{
    private readonly AppDbContext _context;

    public ConfiguracaoFiscalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConfiguracaoFiscalDto> SalvarAsync(
        Guid empresaId,
        UpdateConfiguracaoFiscalDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarDto(dto);

        var entity = await _context.ConfiguracoesFiscais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        if (entity is null)
        {
            entity = new ConfiguracaoFiscal
            {
                EmpresaId = empresaId
            };

            _context.ConfiguracoesFiscais.Add(entity);
        }

        entity.Ambiente = ParseAmbiente(dto.Ambiente);
        entity.RegimeTributario = dto.RegimeTributario.Trim();

        entity.SerieNfce = dto.SerieNfce;
        entity.SerieNfe = dto.SerieNfe;
        entity.SerieNfse = dto.SerieNfse;

        entity.ProximoNumeroNfce = dto.ProximoNumeroNfce;
        entity.ProximoNumeroNfe = dto.ProximoNumeroNfe;
        entity.ProximoNumeroNfse = dto.ProximoNumeroNfse;

        entity.ProvedorFiscal = string.IsNullOrWhiteSpace(dto.ProvedorFiscal)
            ? null
            : dto.ProvedorFiscal.Trim();

        entity.MunicipioCodigo = string.IsNullOrWhiteSpace(dto.MunicipioCodigo)
            ? null
            : dto.MunicipioCodigo.Trim();

        entity.CnaePrincipal = string.IsNullOrWhiteSpace(dto.CnaePrincipal)
            ? null
            : dto.CnaePrincipal.Trim();

        entity.ItemListaServico = string.IsNullOrWhiteSpace(dto.ItemListaServico)
            ? null
            : dto.ItemListaServico.Trim();

        entity.NaturezaOperacaoPadrao = string.IsNullOrWhiteSpace(dto.NaturezaOperacaoPadrao)
            ? null
            : dto.NaturezaOperacaoPadrao.Trim();

        entity.IssRetidoPadrao = dto.IssRetidoPadrao;
        entity.AliquotaIssPadrao = dto.AliquotaIssPadrao;
        entity.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ConfiguracaoFiscalDto?> ObterAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ConfiguracoesFiscais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    private static ConfiguracaoFiscalDto Map(ConfiguracaoFiscal entity)
    {
        return new ConfiguracaoFiscalDto
        {
            Id = entity.Id,
            EmpresaId = entity.EmpresaId,
            Ambiente = entity.Ambiente.ToString(),
            RegimeTributario = entity.RegimeTributario,

            SerieNfce = entity.SerieNfce,
            SerieNfe = entity.SerieNfe,
            SerieNfse = entity.SerieNfse,

            ProximoNumeroNfce = entity.ProximoNumeroNfce,
            ProximoNumeroNfe = entity.ProximoNumeroNfe,
            ProximoNumeroNfse = entity.ProximoNumeroNfse,

            ProvedorFiscal = entity.ProvedorFiscal,
            MunicipioCodigo = entity.MunicipioCodigo,
            CnaePrincipal = entity.CnaePrincipal,
            ItemListaServico = entity.ItemListaServico,

            NaturezaOperacaoPadrao = entity.NaturezaOperacaoPadrao,
            IssRetidoPadrao = entity.IssRetidoPadrao,
            AliquotaIssPadrao = entity.AliquotaIssPadrao,

            Ativo = entity.Ativo,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static AmbienteFiscal ParseAmbiente(string? ambiente)
    {
        if (string.IsNullOrWhiteSpace(ambiente))
            return AmbienteFiscal.Homologacao;

        if (Enum.TryParse<AmbienteFiscal>(ambiente.Trim(), true, out var result))
            return result;

        throw new AppValidationException("Ambiente fiscal inválido. Use Homologacao ou Producao.");
    }

    private static void ValidarDto(UpdateConfiguracaoFiscalDto dto)
    {
        var ambiente = ParseAmbiente(dto.Ambiente);

        if (string.IsNullOrWhiteSpace(dto.RegimeTributario))
            throw new AppValidationException("Regime tributário é obrigatório.");

        if (dto.SerieNfce <= 0 || dto.SerieNfe <= 0 || dto.SerieNfse <= 0)
            throw new AppValidationException("As séries fiscais devem ser maiores que zero.");

        if (dto.ProximoNumeroNfce <= 0 || dto.ProximoNumeroNfe <= 0 || dto.ProximoNumeroNfse <= 0)
            throw new AppValidationException("A próxima numeração fiscal deve ser maior que zero.");

        if (dto.AliquotaIssPadrao is < 0 or > 100)
            throw new AppValidationException("A alíquota de ISS deve estar entre 0 e 100.");

        var provedor = dto.ProvedorFiscal?.Trim();
        if (ambiente == AmbienteFiscal.Producao &&
            (string.IsNullOrWhiteSpace(provedor) || string.Equals(provedor, "Fake", StringComparison.OrdinalIgnoreCase)))
        {
            throw new AppValidationException("Produção fiscal exige provedor real configurado. Use Homologacao enquanto estiver em modo fake.");
        }
    }
}
