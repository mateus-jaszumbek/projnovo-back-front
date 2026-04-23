using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _context;

    public ClienteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClienteDto> CriarAsync(Guid empresaId, CreateClienteDto dto, CancellationToken cancellationToken = default)
    {
        var empresaExiste = await _context.Empresas
            .AnyAsync(x => x.Id == empresaId && x.Ativo, cancellationToken);

        if (!empresaExiste)
            throw new InvalidOperationException("Empresa não encontrada.");

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        var documento = NormalizarDocumento(dto.CpfCnpj);
        await GarantirDocumentoUnicoAsync(empresaId, documento, null, cancellationToken);

        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Nome = dto.Nome.Trim(),
            TipoPessoa = string.IsNullOrWhiteSpace(dto.TipoPessoa)
                ? "FISICA"
                : dto.TipoPessoa.Trim().ToUpper(),
            CpfCnpj = documento,
            Telefone = dto.Telefone?.Trim(),
            Email = dto.Email?.Trim(),
            Cep = dto.Cep?.Trim(),
            Logradouro = dto.Logradouro?.Trim(),
            Numero = dto.Numero?.Trim(),
            Complemento = dto.Complemento?.Trim(),
            Bairro = dto.Bairro?.Trim(),
            Cidade = dto.Cidade?.Trim(),
            Uf = dto.Uf?.Trim().ToUpper(),
            Observacoes = dto.Observacoes?.Trim(),
            Ativo = true
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(cliente);
    }

    public async Task<List<ClienteDto>> ListarAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new ClienteDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                TipoPessoa = x.TipoPessoa,
                CpfCnpj = x.CpfCnpj,
                Telefone = x.Telefone,
                Email = x.Email,
                Cep = x.Cep,
                Logradouro = x.Logradouro,
                Numero = x.Numero,
                Complemento = x.Complemento,
                Bairro = x.Bairro,
                Cidade = x.Cidade,
                Uf = x.Uf,
                Observacoes = x.Observacoes,
                Ativo = x.Ativo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClienteDto?> ObterPorIdAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId, cancellationToken);

        return cliente is null ? null : Map(cliente);
    }

    public async Task<ClienteDto?> AtualizarAsync(Guid empresaId, Guid id, UpdateClienteDto dto, CancellationToken cancellationToken = default)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId, cancellationToken);

        if (cliente is null)
            return null;

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new InvalidOperationException("Nome é obrigatório.");

        var documento = NormalizarDocumento(dto.CpfCnpj);
        await GarantirDocumentoUnicoAsync(empresaId, documento, id, cancellationToken);

        cliente.Nome = dto.Nome.Trim();
        cliente.TipoPessoa = string.IsNullOrWhiteSpace(dto.TipoPessoa)
            ? "FISICA"
            : dto.TipoPessoa.Trim().ToUpper();
        cliente.CpfCnpj = documento;
        cliente.Telefone = dto.Telefone?.Trim();
        cliente.Email = dto.Email?.Trim();
        cliente.Cep = dto.Cep?.Trim();
        cliente.Logradouro = dto.Logradouro?.Trim();
        cliente.Numero = dto.Numero?.Trim();
        cliente.Complemento = dto.Complemento?.Trim();
        cliente.Bairro = dto.Bairro?.Trim();
        cliente.Cidade = dto.Cidade?.Trim();
        cliente.Uf = dto.Uf?.Trim().ToUpper();
        cliente.Observacoes = dto.Observacoes?.Trim();
        cliente.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return Map(cliente);
    }

    public async Task<bool> InativarAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId, cancellationToken);

        if (cliente is null)
            return false;

        cliente.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task GarantirDocumentoUnicoAsync(
        Guid empresaId,
        string? documento,
        Guid? ignorarClienteId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return;

        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.CpfCnpj != null &&
                (!ignorarClienteId.HasValue || x.Id != ignorarClienteId.Value))
            .Select(x => new { x.Id, x.Nome, x.CpfCnpj })
            .ToListAsync(cancellationToken);

        var duplicado = clientes.FirstOrDefault(x => NormalizarDocumento(x.CpfCnpj) == documento);
        if (duplicado is not null)
            throw new InvalidOperationException($"Ja existe um cliente com este CPF/CNPJ: {duplicado.Nome}.");
    }

    private static string? NormalizarDocumento(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static ClienteDto Map(Cliente cliente)
    {
        return new ClienteDto
        {
            Id = cliente.Id,
            EmpresaId = cliente.EmpresaId,
            Nome = cliente.Nome,
            TipoPessoa = cliente.TipoPessoa,
            CpfCnpj = cliente.CpfCnpj,
            Telefone = cliente.Telefone,
            Email = cliente.Email,
            Cep = cliente.Cep,
            Logradouro = cliente.Logradouro,
            Numero = cliente.Numero,
            Complemento = cliente.Complemento,
            Bairro = cliente.Bairro,
            Cidade = cliente.Cidade,
            Uf = cliente.Uf,
            Observacoes = cliente.Observacoes,
            Ativo = cliente.Ativo
        };
    }
}
