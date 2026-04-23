using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ModuloPersonalizadoService : IModuloPersonalizadoService
{
    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "text",
        "email",
        "number",
        "currency",
        "percentage",
        "date",
        "select",
        "textarea",
        "checkbox"
    };

    private static readonly HashSet<string> ModulosSistemaPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "clientes",
        "aparelhos",
        "fornecedores",
        "tecnicos",
        "servicos",
        "pecas",
        "vendas",
        "ordem_servico",
        "ordem_servico_itens"
    };

    private readonly AppDbContext _context;

    public ModuloPersonalizadoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ModuloPersonalizadoDto>> ListarModulosAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        var modulos = await _context.ModulosPersonalizados
            .AsNoTracking()
            .Include(x => x.Campos)
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);

        return modulos.Select(MapModulo).ToList();
    }

    public async Task<ModuloPersonalizadoDto?> ObterModuloAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken = default)
    {
        var modulo = await _context.ModulosPersonalizados
            .AsNoTracking()
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == moduloId, cancellationToken);

        return modulo is null ? null : MapModulo(modulo);
    }

    public async Task<ModuloPersonalizadoDto?> ObterModuloPorChaveAsync(Guid empresaId, string chave, CancellationToken cancellationToken = default)
    {
        var normalizedKey = Slug(chave);
        var modulo = await _context.ModulosPersonalizados
            .AsNoTracking()
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Chave == normalizedKey, cancellationToken);

        return modulo is null ? null : MapModulo(modulo);
    }

    public async Task<ModuloPersonalizadoDto> CriarModuloAsync(Guid empresaId, CreateModuloPersonalizadoDto dto, CancellationToken cancellationToken = default)
    {
        var nome = Limpar(dto.Nome);
        var chave = await CriarChaveUnicaModuloAsync(empresaId, nome, cancellationToken);

        var modulo = new ModuloPersonalizado
        {
            EmpresaId = empresaId,
            Nome = nome,
            Chave = chave,
            Descricao = LimparOpcional(dto.Descricao),
            Ordem = dto.Ordem,
            Ativo = true
        };

        _context.ModulosPersonalizados.Add(modulo);
        await _context.SaveChangesAsync(cancellationToken);

        return await ObterModuloAsync(empresaId, modulo.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar modulo criado.");
    }

    public async Task<ModuloPersonalizadoDto> GarantirModuloSistemaAsync(
        Guid empresaId,
        EnsureModuloSistemaDto dto,
        CancellationToken cancellationToken = default)
    {
        var chave = Slug(dto.Chave);
        ValidarModuloSistema(chave);

        var modulo = await _context.ModulosPersonalizados
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(
                x => x.EmpresaId == empresaId && x.Chave == chave,
                cancellationToken);

        if (modulo is not null)
            return MapModulo(modulo);

        modulo = new ModuloPersonalizado
        {
            EmpresaId = empresaId,
            Nome = Limpar(dto.Nome),
            Chave = chave,
            Descricao = LimparOpcional(dto.Descricao),
            Ordem = 0,
            Ativo = true
        };

        _context.ModulosPersonalizados.Add(modulo);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            var existente = await _context.ModulosPersonalizados
                .AsNoTracking()
                .Include(x => x.Campos)
                .FirstOrDefaultAsync(
                    x => x.EmpresaId == empresaId && x.Chave == chave,
                    cancellationToken);

            if (existente is not null)
                return MapModulo(existente);

            throw;
        }

        return await ObterModuloAsync(empresaId, modulo.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar modulo criado.");
    }
    public async Task<ModuloPersonalizadoDto?> AtualizarModuloAsync(Guid empresaId, Guid moduloId, UpdateModuloPersonalizadoDto dto, CancellationToken cancellationToken = default)
    {
        var modulo = await _context.ModulosPersonalizados
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == moduloId, cancellationToken);

        if (modulo is null)
            return null;

        modulo.Nome = Limpar(dto.Nome);
        modulo.Descricao = LimparOpcional(dto.Descricao);
        modulo.Ordem = dto.Ordem;
        modulo.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);
        return await ObterModuloAsync(empresaId, modulo.Id, cancellationToken);
    }

    public async Task<CampoPersonalizadoDto> CriarCampoAsync(Guid empresaId, Guid moduloId, CreateCampoPersonalizadoDto dto, CancellationToken cancellationToken = default)
    {
        await GarantirModuloAsync(empresaId, moduloId, cancellationToken);
        ValidarTipo(dto.Tipo);
        ValidarLayout(dto.Linha, dto.Posicao);

        var nome = Limpar(dto.Nome);
        var chave = await CriarChaveUnicaCampoAsync(empresaId, moduloId, nome, cancellationToken);

        var campo = new CampoPersonalizado
        {
            EmpresaId = empresaId,
            ModuloPersonalizadoId = moduloId,
            Nome = nome,
            Chave = chave,
            Tipo = dto.Tipo.Trim().ToLowerInvariant(),
            Obrigatorio = dto.Obrigatorio,
            Aba = NormalizarAba(dto.Aba),
            Linha = dto.Linha,
            Posicao = dto.Posicao,
            Ordem = dto.Ordem,
            Placeholder = LimparOpcional(dto.Placeholder),
            ValorPadrao = LimparOpcional(dto.ValorPadrao),
            OpcoesJson = SerializarOpcoes(dto.Opcoes),
            ExportarExcel = dto.ExportarExcel,
            ExportarExcelResumo = dto.ExportarExcelResumo,
            ExportarPdf = dto.ExportarPdf,
            Ativo = true
        };

        _context.CamposPersonalizados.Add(campo);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            campo.Chave = await CriarChaveUnicaCampoAsync(empresaId, moduloId, nome, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await SincronizarLayoutCampoAsync(campo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapCampo(campo);
    }

    public async Task<CampoPersonalizadoDto?> AtualizarCampoAsync(Guid empresaId, Guid moduloId, Guid campoId, UpdateCampoPersonalizadoDto dto, CancellationToken cancellationToken = default)
    {
        var campo = await _context.CamposPersonalizados
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.ModuloPersonalizadoId == moduloId &&
                x.Id == campoId,
                cancellationToken);

        if (campo is null)
            return null;

        ValidarLayout(dto.Linha, dto.Posicao);

        campo.Nome = Limpar(dto.Nome);
        campo.Obrigatorio = dto.Obrigatorio;
        campo.Aba = NormalizarAba(dto.Aba);
        campo.Linha = dto.Linha;
        campo.Posicao = dto.Posicao;
        campo.Ordem = dto.Ordem;
        campo.Placeholder = LimparOpcional(dto.Placeholder);
        campo.ValorPadrao = LimparOpcional(dto.ValorPadrao);
        campo.OpcoesJson = SerializarOpcoes(dto.Opcoes);
        campo.ExportarExcel = dto.ExportarExcel;
        campo.ExportarExcelResumo = dto.ExportarExcelResumo;
        campo.ExportarPdf = dto.ExportarPdf;
        campo.Ativo = dto.Ativo;

        await SincronizarLayoutCampoAsync(campo, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return MapCampo(campo);
    }

    public async Task<bool> ExcluirCampoAsync(Guid empresaId, Guid moduloId, Guid campoId, CancellationToken cancellationToken = default)
    {
        var campo = await _context.CamposPersonalizados
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.ModuloPersonalizadoId == moduloId &&
                x.Id == campoId,
                cancellationToken);

        if (campo is null)
            return false;

        campo.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReordenarCamposAsync(Guid empresaId, Guid moduloId, List<CampoLayoutDto> campos, CancellationToken cancellationToken = default)
    {
        if (campos.Count == 0)
            return true;

        var posicoes = new HashSet<string>();
        foreach (var campo in campos)
        {
            ValidarLayout(campo.Linha, campo.Posicao);
            var aba = NormalizarAba(campo.Aba);
            if (!posicoes.Add($"{aba}:{campo.Linha}:{campo.Posicao}"))
                throw new InvalidOperationException("Dois campos nao podem ocupar a mesma posicao na mesma linha e aba.");
        }

        var ids = campos.Select(x => x.Id).ToHashSet();
        var entities = await _context.CamposPersonalizados
            .Where(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId && ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (entities.Count != ids.Count)
            return false;

        foreach (var entity in entities)
        {
            var layout = campos.First(x => x.Id == entity.Id);
            entity.Aba = NormalizarAba(layout.Aba);
            entity.Linha = layout.Linha;
            entity.Posicao = layout.Posicao;
            entity.Ordem = layout.Ordem;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<CampoModuloLayoutDto>> ListarLayoutAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken = default)
    {
        return await _context.CamposModuloLayout
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId)
            .OrderBy(x => x.Ordem)
            .Select(x => new CampoModuloLayoutDto
            {
                CampoChave = x.CampoChave,
                Aba = x.Aba,
                Linha = x.Linha,
                Posicao = x.Posicao,
                Ordem = x.Ordem
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SalvarLayoutAsync(Guid empresaId, Guid moduloId, List<CampoModuloLayoutDto> campos, CancellationToken cancellationToken = default)
    {
        if (campos.Count == 0)
            return true;

        await GarantirModuloAsync(empresaId, moduloId, cancellationToken);

        var posicoes = new HashSet<string>();
        var chaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var campo in campos)
        {
            var chave = campo.CampoChave.Trim();
            if (string.IsNullOrWhiteSpace(chave))
                throw new InvalidOperationException("Campo invalido.");

            if (!chaves.Add(chave))
                throw new InvalidOperationException("Campo duplicado no layout.");

            ValidarLayout(campo.Linha, campo.Posicao);
            var aba = NormalizarAba(campo.Aba);
            if (!posicoes.Add($"{aba}:{campo.Linha}:{campo.Posicao}"))
                throw new InvalidOperationException("Dois campos nao podem ocupar a mesma posicao na mesma linha e aba.");
        }

        var existing = await _context.CamposModuloLayout
            .Where(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId)
            .ToListAsync(cancellationToken);

        var removidos = existing.Where(x => !chaves.Contains(x.CampoChave)).ToList();
        if (removidos.Count > 0)
            _context.CamposModuloLayout.RemoveRange(removidos);

        foreach (var dto in campos)
        {
            var key = dto.CampoChave.Trim();
            var aba = NormalizarAba(dto.Aba);
            var entity = existing.FirstOrDefault(x => x.CampoChave == key)
                ?? existing.FirstOrDefault(x => string.Equals(x.CampoChave, key, StringComparison.OrdinalIgnoreCase));
            if (entity is null)
            {
                entity = new CampoModuloLayout
                {
                    EmpresaId = empresaId,
                    ModuloPersonalizadoId = moduloId,
                    CampoChave = key
                };
                _context.CamposModuloLayout.Add(entity);
            }

            entity.CampoChave = key;
            entity.Aba = aba;
            entity.Linha = dto.Linha;
            entity.Posicao = dto.Posicao;
            entity.Ordem = dto.Ordem;
        }

        var layoutsPorCampo = campos
            .Where(x => !IsTabMarker(x.CampoChave))
            .ToDictionary(x => x.CampoChave.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
        var camposChaves = layoutsPorCampo.Keys.ToList();
        var camposPersonalizados = await _context.CamposPersonalizados
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.ModuloPersonalizadoId == moduloId &&
                camposChaves.Contains(x.Chave))
            .ToListAsync(cancellationToken);

        foreach (var campo in camposPersonalizados)
        {
            if (!layoutsPorCampo.TryGetValue(campo.Chave, out var layout))
                continue;

            campo.Aba = NormalizarAba(layout.Aba);
            campo.Linha = layout.Linha;
            campo.Posicao = layout.Posicao;
            campo.Ordem = layout.Ordem;
            campo.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<RegistroPersonalizadoDto>> ListarRegistrosAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken = default)
    {
        var registros = await _context.RegistrosPersonalizados
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId && x.Ativo)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return registros.Select(MapRegistro).ToList();
    }

    public async Task<RegistroPersonalizadoDto> CriarRegistroAsync(Guid empresaId, Guid moduloId, CreateRegistroPersonalizadoDto dto, CancellationToken cancellationToken = default)
    {
        var campos = await _context.CamposPersonalizados
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId && x.Ativo)
            .ToListAsync(cancellationToken);

        if (campos.Count == 0)
            throw new InvalidOperationException("Crie ao menos um campo antes de cadastrar registros.");

        var valores = ValidarValores(campos, dto.Valores);

        var registro = new RegistroPersonalizado
        {
            EmpresaId = empresaId,
            ModuloPersonalizadoId = moduloId,
            ValoresJson = JsonSerializer.Serialize(valores),
            Ativo = true
        };

        _context.RegistrosPersonalizados.Add(registro);
        await _context.SaveChangesAsync(cancellationToken);

        return MapRegistro(registro);
    }

    public async Task<RegistroPersonalizadoDto> SalvarRegistroOrigemAsync(Guid empresaId, Guid moduloId, Guid origemId, CreateRegistroPersonalizadoDto dto, CancellationToken cancellationToken = default)
    {
        var campos = await _context.CamposPersonalizados
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId && x.Ativo)
            .ToListAsync(cancellationToken);

        var valores = campos.Count == 0
            ? new Dictionary<string, object?>()
            : ValidarValores(campos, dto.Valores);

        var registro = await _context.RegistrosPersonalizados
            .FirstOrDefaultAsync(x =>
                x.EmpresaId == empresaId &&
                x.ModuloPersonalizadoId == moduloId &&
                x.OrigemId == origemId,
                cancellationToken);

        if (registro is null)
        {
            registro = new RegistroPersonalizado
            {
                EmpresaId = empresaId,
                ModuloPersonalizadoId = moduloId,
                OrigemId = origemId,
                Ativo = true
            };
            _context.RegistrosPersonalizados.Add(registro);
        }

        registro.ValoresJson = JsonSerializer.Serialize(valores);
        registro.Ativo = true;

        await _context.SaveChangesAsync(cancellationToken);
        return MapRegistro(registro);
    }

    private async Task GarantirModuloAsync(Guid empresaId, Guid moduloId, CancellationToken cancellationToken)
    {
        var exists = await _context.ModulosPersonalizados
            .AnyAsync(x => x.EmpresaId == empresaId && x.Id == moduloId, cancellationToken);

        if (!exists)
            throw new InvalidOperationException("Modulo nao encontrado.");
    }

    private static Dictionary<string, object?> ValidarValores(List<CampoPersonalizado> campos, Dictionary<string, object?> valores)
    {
        var result = new Dictionary<string, object?>();

        foreach (var campo in campos.OrderBy(x => x.Ordem))
        {
            valores.TryGetValue(campo.Chave, out var valor);

            if (CampoVazio(valor))
            {
                if (campo.Obrigatorio)
                    throw new InvalidOperationException($"{campo.Nome} e obrigatorio.");

                result[campo.Chave] = null;
                continue;
            }

            result[campo.Chave] = NormalizarValor(campo, valor);
        }

        return result;
    }

    private static object? NormalizarValor(CampoPersonalizado campo, object? valor)
    {
        var texto = valor switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            JsonElement element when element.ValueKind == JsonValueKind.Number => element.GetRawText(),
            JsonElement element when element.ValueKind == JsonValueKind.True => true,
            JsonElement element when element.ValueKind == JsonValueKind.False => false,
            _ => valor
        };

        if (campo.Tipo == "checkbox")
            return texto is bool boolValue ? boolValue : bool.TryParse(Convert.ToString(texto), out var parsed) && parsed;

        if (campo.Tipo is "number" or "currency" or "percentage")
        {
            if (!decimal.TryParse(Convert.ToString(texto), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                throw new InvalidOperationException($"{campo.Nome} deve ser um numero valido.");

            return number;
        }

        if (campo.Tipo == "date")
        {
            if (!DateOnly.TryParse(Convert.ToString(texto), out var date))
                throw new InvalidOperationException($"{campo.Nome} deve ser uma data valida.");

            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (campo.Tipo == "select")
        {
            var selected = Convert.ToString(texto) ?? string.Empty;
            var opcoes = DesserializarOpcoes(campo.OpcoesJson);
            if (opcoes.Count > 0 && !opcoes.Contains(selected))
                throw new InvalidOperationException($"{campo.Nome} possui uma opcao invalida.");

            return selected;
        }

        return Convert.ToString(texto)?.Trim();
    }

    private static bool CampoVazio(object? valor)
    {
        if (valor is null)
            return true;

        if (valor is JsonElement element)
            return element.ValueKind == JsonValueKind.Null ||
                element.ValueKind == JsonValueKind.Undefined ||
                (element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()));

        return valor is string text && string.IsNullOrWhiteSpace(text);
    }

    private async Task<string> CriarChaveUnicaModuloAsync(Guid empresaId, string nome, CancellationToken cancellationToken)
    {
        var baseKey = Slug(nome);
        var key = baseKey;
        var index = 2;

        while (await _context.ModulosPersonalizados.AnyAsync(x => x.EmpresaId == empresaId && x.Chave == key, cancellationToken))
        {
            key = $"{baseKey}_{index}";
            index++;
        }

        return key;
    }

    private async Task<string> CriarChaveUnicaCampoAsync(Guid empresaId, Guid moduloId, string nome, CancellationToken cancellationToken)
    {
        var baseKey = Slug(nome);
        var key = baseKey;
        var index = 2;

        while (await _context.CamposPersonalizados.AnyAsync(x => x.EmpresaId == empresaId && x.ModuloPersonalizadoId == moduloId && x.Chave == key, cancellationToken))
        {
            key = $"{baseKey}_{index}";
            index++;
        }

        return key;
    }

    private static string Limpar(string value)
    {
        var text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Nome e obrigatorio.");

        return text;
    }

    private static string? LimparOpcional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizarAba(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Principal" : value.Trim();
        return text.Length > 80 ? text[..80] : text;
    }

    private static void ValidarTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new InvalidOperationException("Escolha o tipo do campo.");

        if (!TiposPermitidos.Contains(tipo.Trim()))
            throw new InvalidOperationException("Tipo de campo invalido.");
    }

    private static void ValidarModuloSistema(string chave)
    {
        if (!ModulosSistemaPermitidos.Contains(chave))
            throw new InvalidOperationException("Modulo de sistema nao permitido.");
    }

    private static bool IsUniqueConstraint(DbUpdateException ex)
    {
        return ex.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode == 19;
    }

    private async Task SincronizarLayoutCampoAsync(CampoPersonalizado campo, CancellationToken cancellationToken)
    {
        var layout = await _context.CamposModuloLayout.FirstOrDefaultAsync(
            x =>
                x.EmpresaId == campo.EmpresaId &&
                x.ModuloPersonalizadoId == campo.ModuloPersonalizadoId &&
                x.CampoChave == campo.Chave,
            cancellationToken);

        if (layout is null)
        {
            layout = await _context.CamposModuloLayout.FirstOrDefaultAsync(
                x =>
                    x.EmpresaId == campo.EmpresaId &&
                    x.ModuloPersonalizadoId == campo.ModuloPersonalizadoId &&
                    x.CampoChave.ToLower() == campo.Chave.ToLower(),
                cancellationToken);
        }

        if (layout is null)
        {
            layout = new CampoModuloLayout
            {
                EmpresaId = campo.EmpresaId,
                ModuloPersonalizadoId = campo.ModuloPersonalizadoId,
                CampoChave = campo.Chave
            };
            _context.CamposModuloLayout.Add(layout);
        }

        layout.CampoChave = campo.Chave;
        layout.Aba = NormalizarAba(campo.Aba);
        layout.Linha = campo.Linha;
        layout.Posicao = campo.Posicao;
        layout.Ordem = campo.Ordem;
    }

    private static void ValidarLayout(int linha, int posicao)
    {
        if (linha < 1)
            throw new InvalidOperationException("Linha deve ser maior que zero.");

        if (posicao is < 1 or > 4)
            throw new InvalidOperationException("Posicao deve estar entre 1 e 4.");
    }

    private static bool IsTabMarker(string? campoChave)
    {
        return campoChave?.StartsWith("__tab__", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string SerializarOpcoes(List<string> opcoes)
    {
        var normalized = opcoes
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return JsonSerializer.Serialize(normalized);
    }

    private static List<string> DesserializarOpcoes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static ModuloPersonalizadoDto MapModulo(ModuloPersonalizado modulo)
    {
        return new ModuloPersonalizadoDto
        {
            Id = modulo.Id,
            EmpresaId = modulo.EmpresaId,
            Nome = modulo.Nome,
            Chave = modulo.Chave,
            Descricao = modulo.Descricao,
            Ordem = modulo.Ordem,
            Ativo = modulo.Ativo,
            Campos = modulo.Campos
                .Where(x => x.Ativo)
                .OrderBy(x => x.Aba)
                .ThenBy(x => x.Linha)
                .ThenBy(x => x.Posicao)
                .ThenBy(x => x.Ordem)
                .Select(MapCampo)
                .ToList()
        };
    }

    private static CampoPersonalizadoDto MapCampo(CampoPersonalizado campo)
    {
        return new CampoPersonalizadoDto
        {
            Id = campo.Id,
            ModuloPersonalizadoId = campo.ModuloPersonalizadoId,
            Nome = campo.Nome,
            Chave = campo.Chave,
            Tipo = campo.Tipo,
            Obrigatorio = campo.Obrigatorio,
            Aba = campo.Aba,
            Linha = campo.Linha,
            Posicao = campo.Posicao,
            Ordem = campo.Ordem,
            Placeholder = campo.Placeholder,
            ValorPadrao = campo.ValorPadrao,
            Opcoes = DesserializarOpcoes(campo.OpcoesJson),
            ExportarExcel = campo.ExportarExcel,
            ExportarExcelResumo = campo.ExportarExcelResumo,
            ExportarPdf = campo.ExportarPdf,
            Ativo = campo.Ativo
        };
    }

    private static RegistroPersonalizadoDto MapRegistro(RegistroPersonalizado registro)
    {
        return new RegistroPersonalizadoDto
        {
            Id = registro.Id,
            ModuloPersonalizadoId = registro.ModuloPersonalizadoId,
            OrigemId = registro.OrigemId,
            Valores = DesserializarValores(registro.ValoresJson),
            Ativo = registro.Ativo,
            CreatedAt = registro.CreatedAt,
            UpdatedAt = registro.UpdatedAt
        };
    }

    private static Dictionary<string, object?> DesserializarValores(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private static string Slug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousUnderscore = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousUnderscore = false;
                continue;
            }

            if (!previousUnderscore)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }

        var slug = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "campo" : slug;
    }
}
