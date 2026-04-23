using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs.Fornecedores;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class FornecedorService : IFornecedorService
{
    private readonly AppDbContext _context;

    public FornecedorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FornecedorDto> CriarAsync(Guid empresaId, CreateFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var nome = LimparNome(dto.Nome);
        var documento = NormalizarDocumento(dto.CpfCnpj);
        await GarantirDocumentoUnicoAsync(empresaId, documento, null, cancellationToken);

        var fornecedor = new Fornecedor
        {
            EmpresaId = empresaId,
            Nome = nome,
            TipoPessoa = NormalizarTipoPessoa(dto.TipoPessoa),
            CpfCnpj = documento,
            Contato = LimparOpcional(dto.Contato),
            Telefone = LimparOpcional(dto.Telefone),
            WhatsApp = LimparOpcional(dto.WhatsApp),
            Email = LimparOpcional(dto.Email),
            ProdutosFornecidos = LimparOpcional(dto.ProdutosFornecidos),
            MensagemPadrao = LimparOpcional(dto.MensagemPadrao),
            Cep = LimparOpcional(dto.Cep),
            Logradouro = LimparOpcional(dto.Logradouro),
            Numero = LimparOpcional(dto.Numero),
            Complemento = LimparOpcional(dto.Complemento),
            Bairro = LimparOpcional(dto.Bairro),
            Cidade = LimparOpcional(dto.Cidade),
            Uf = LimparOpcional(dto.Uf)?.ToUpperInvariant(),
            Observacoes = LimparOpcional(dto.Observacoes),
            Ativo = true
        };

        _context.Fornecedores.Add(fornecedor);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(fornecedor);
    }

    public async Task<List<FornecedorDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _context.Fornecedores
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Nome)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<FornecedorDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _context.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        return fornecedor is null ? null : Map(fornecedor);
    }

    public async Task<FornecedorDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _context.Fornecedores
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (fornecedor is null)
            return null;

        var documento = NormalizarDocumento(dto.CpfCnpj);
        await GarantirDocumentoUnicoAsync(empresaId, documento, id, cancellationToken);

        fornecedor.Nome = LimparNome(dto.Nome);
        fornecedor.TipoPessoa = NormalizarTipoPessoa(dto.TipoPessoa);
        fornecedor.CpfCnpj = documento;
        fornecedor.Contato = LimparOpcional(dto.Contato);
        fornecedor.Telefone = LimparOpcional(dto.Telefone);
        fornecedor.WhatsApp = LimparOpcional(dto.WhatsApp);
        fornecedor.Email = LimparOpcional(dto.Email);
        fornecedor.ProdutosFornecidos = LimparOpcional(dto.ProdutosFornecidos);
        fornecedor.MensagemPadrao = LimparOpcional(dto.MensagemPadrao);
        fornecedor.Cep = LimparOpcional(dto.Cep);
        fornecedor.Logradouro = LimparOpcional(dto.Logradouro);
        fornecedor.Numero = LimparOpcional(dto.Numero);
        fornecedor.Complemento = LimparOpcional(dto.Complemento);
        fornecedor.Bairro = LimparOpcional(dto.Bairro);
        fornecedor.Cidade = LimparOpcional(dto.Cidade);
        fornecedor.Uf = LimparOpcional(dto.Uf)?.ToUpperInvariant();
        fornecedor.Observacoes = LimparOpcional(dto.Observacoes);
        fornecedor.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(fornecedor);
    }

    public async Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var fornecedor = await _context.Fornecedores
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == id, cancellationToken);

        if (fornecedor is null)
            return false;

        fornecedor.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FornecedorMensagemHistoricoDto> RegistrarMensagemAsync(
        Guid empresaId,
        Guid fornecedorId,
        CreateFornecedorMensagemHistoricoDto dto,
        CancellationToken cancellationToken = default)
    {
        var fornecedor = await _context.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == fornecedorId && x.Ativo, cancellationToken);

        if (fornecedor is null)
            throw new InvalidOperationException("Fornecedor nao encontrado para esta empresa.");

        Peca? peca = null;
        if (dto.PecaId.HasValue)
        {
            peca = await _context.Pecas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Id == dto.PecaId.Value, cancellationToken);

            if (peca is null)
                throw new InvalidOperationException("Peca nao encontrada para esta empresa.");
        }

        var mensagem = LimparObrigatorio(dto.Mensagem, "Mensagem e obrigatoria.");
        var canal = NormalizarCanal(dto.Canal);

        var historico = new FornecedorMensagemHistorico
        {
            EmpresaId = empresaId,
            FornecedorId = fornecedor.Id,
            PecaId = peca?.Id,
            Canal = canal,
            Assunto = LimparOpcional(dto.Assunto) ?? $"Reposicao - {peca?.Nome ?? fornecedor.Nome}",
            Mensagem = mensagem,
            QuantidadeSolicitada = dto.QuantidadeSolicitada,
            EnviadoEm = DateTime.UtcNow
        };

        _context.FornecedoresMensagensHistorico.Add(historico);
        await _context.SaveChangesAsync(cancellationToken);

        historico.Fornecedor = fornecedor;
        historico.Peca = peca;
        return MapHistorico(historico);
    }

    public async Task<List<FornecedorMensagemHistoricoDto>> ListarMensagensAsync(
        Guid empresaId,
        Guid fornecedorId,
        CancellationToken cancellationToken = default)
    {
        var existe = await _context.Fornecedores
            .AsNoTracking()
            .AnyAsync(x => x.EmpresaId == empresaId && x.Id == fornecedorId, cancellationToken);

        if (!existe)
            throw new InvalidOperationException("Fornecedor nao encontrado para esta empresa.");

        return await _context.FornecedoresMensagensHistorico
            .AsNoTracking()
            .Include(x => x.Fornecedor)
            .Include(x => x.Peca)
            .Where(x => x.EmpresaId == empresaId && x.FornecedorId == fornecedorId)
            .OrderByDescending(x => x.EnviadoEm)
            .Take(100)
            .Select(x => MapHistorico(x))
            .ToListAsync(cancellationToken);
    }

    private async Task GarantirDocumentoUnicoAsync(Guid empresaId, string? documento, Guid? ignorarFornecedorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return;

        var fornecedores = await _context.Fornecedores
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.CpfCnpj != null && (!ignorarFornecedorId.HasValue || x.Id != ignorarFornecedorId.Value))
            .Select(x => new { x.Nome, x.CpfCnpj })
            .ToListAsync(cancellationToken);

        var duplicado = fornecedores.FirstOrDefault(x => NormalizarDocumento(x.CpfCnpj) == documento);
        if (duplicado is not null)
            throw new InvalidOperationException($"Ja existe um fornecedor com este CPF/CNPJ: {duplicado.Nome}.");
    }

    private static string LimparNome(string value)
    {
        return LimparObrigatorio(value, "Nome do fornecedor e obrigatorio.");
    }

    private static string? LimparOpcional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizarTipoPessoa(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "JURIDICA" : value.Trim().ToUpperInvariant();
        return text == "FISICA" ? "FISICA" : "JURIDICA";
    }

    private static string? NormalizarDocumento(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static string LimparObrigatorio(string? value, string message)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(message);

        return text;
    }

    private static string NormalizarCanal(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "WHATSAPP" : value.Trim().ToUpperInvariant();
        return text is "EMAIL" or "WHATSAPP" ? text : "OUTRO";
    }

    private static FornecedorDto Map(Fornecedor fornecedor)
    {
        return new FornecedorDto
        {
            Id = fornecedor.Id,
            EmpresaId = fornecedor.EmpresaId,
            Nome = fornecedor.Nome,
            TipoPessoa = fornecedor.TipoPessoa,
            CpfCnpj = fornecedor.CpfCnpj,
            Contato = fornecedor.Contato,
            Telefone = fornecedor.Telefone,
            WhatsApp = fornecedor.WhatsApp,
            Email = fornecedor.Email,
            ProdutosFornecidos = fornecedor.ProdutosFornecidos,
            MensagemPadrao = fornecedor.MensagemPadrao,
            Cep = fornecedor.Cep,
            Logradouro = fornecedor.Logradouro,
            Numero = fornecedor.Numero,
            Complemento = fornecedor.Complemento,
            Bairro = fornecedor.Bairro,
            Cidade = fornecedor.Cidade,
            Uf = fornecedor.Uf,
            Observacoes = fornecedor.Observacoes,
            Ativo = fornecedor.Ativo
        };
    }

    private static FornecedorMensagemHistoricoDto MapHistorico(FornecedorMensagemHistorico historico)
    {
        return new FornecedorMensagemHistoricoDto
        {
            Id = historico.Id,
            FornecedorId = historico.FornecedorId,
            FornecedorNome = historico.Fornecedor?.Nome ?? string.Empty,
            PecaId = historico.PecaId,
            PecaNome = historico.Peca?.Nome,
            Canal = historico.Canal,
            Assunto = historico.Assunto,
            Mensagem = historico.Mensagem,
            QuantidadeSolicitada = historico.QuantidadeSolicitada,
            EnviadoEm = historico.EnviadoEm,
            CreatedAt = historico.CreatedAt
        };
    }
}
