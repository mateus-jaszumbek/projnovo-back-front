using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UsuarioDto> CriarAsync(
        CreateUsuarioDto dto,
        Guid? empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new AppValidationException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new AppValidationException("E-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Senha))
            throw new AppValidationException("Senha é obrigatória.");

        if (dto.Senha.Length < 7 || !dto.Senha.Any(char.IsUpper) || !dto.Senha.Any(char.IsLower) || !dto.Senha.Any(char.IsDigit))
            throw new AppValidationException("Senha deve ter mais de 6 caracteres, letra maiúscula, letra minúscula e número.");

        if (dto.IsSuperAdmin && !solicitanteEhSuperAdmin)
            throw new AppUnauthorizedException("Apenas super administradores podem criar outro super administrador.");

        var email = dto.Email.Trim().ToLowerInvariant();

        var emailJaExiste = await _context.Usuarios
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (emailJaExiste)
            throw new AppConflictException("Já existe um usuário com esse e-mail.");

        if (!solicitanteEhSuperAdmin)
        {
            if (!empresaSolicitanteId.HasValue)
                throw new AppUnauthorizedException("Token sem empresa vinculada.");

            var empresaExiste = await _context.Empresas
                .AsNoTracking()
                .AnyAsync(x => x.Id == empresaSolicitanteId.Value && x.Ativo, cancellationToken);

            if (!empresaExiste)
                throw new AppNotFoundException("Empresa não encontrada.");
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome.Trim(),
            Email = email,
            Ativo = dto.Ativo,
            IsSuperAdmin = solicitanteEhSuperAdmin && dto.IsSuperAdmin
        };

        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);

        _context.Usuarios.Add(usuario);

        // Quando o usuário é criado por owner/usuário comum da empresa,
        // já cria o vínculo automaticamente para ele aparecer na tela da empresa.
        if (!usuario.IsSuperAdmin && empresaSolicitanteId.HasValue)
        {
            var usuarioEmpresa = new UsuarioEmpresa
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                EmpresaId = empresaSolicitanteId.Value,
                Perfil = "atendente", // ajuste aqui se quiser outro perfil padrão
                NivelAcesso = 2,
                Ativo = true
            };

            _context.UsuarioEmpresas.Add(usuarioEmpresa);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UsuarioDto
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Ativo = usuario.Ativo,
            IsSuperAdmin = usuario.IsSuperAdmin
        };
    }

    public async Task<List<UsuarioDto>> ListarAsync(
        Guid? empresaId,
        bool incluirTodasEmpresas,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Usuarios
            .AsNoTracking()
            .AsQueryable();

        if (!incluirTodasEmpresas)
        {
            if (!empresaId.HasValue)
                throw new AppUnauthorizedException("Token sem empresa vinculada.");

            query = query.Where(x => x.UsuarioEmpresas.Any(v =>
                v.EmpresaId == empresaId.Value &&
                v.Ativo));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Select(x => new UsuarioDto
            {
                Id = x.Id,
                Nome = x.Nome,
                Email = x.Email,
                Ativo = x.Ativo,
                IsSuperAdmin = x.IsSuperAdmin
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioDto?> ObterPorIdAsync(
        Guid id,
        Guid? empresaId,
        bool incluirTodasEmpresas,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Usuarios
            .AsNoTracking()
            .Where(x => x.Id == id);

        if (!incluirTodasEmpresas)
        {
            if (!empresaId.HasValue)
                throw new AppUnauthorizedException("Token sem empresa vinculada.");

            query = query.Where(x => x.UsuarioEmpresas.Any(v =>
                v.EmpresaId == empresaId.Value &&
                v.Ativo));
        }

        return await query
            .Select(x => new UsuarioDto
            {
                Id = x.Id,
                Nome = x.Nome,
                Email = x.Email,
                Ativo = x.Ativo,
                IsSuperAdmin = x.IsSuperAdmin
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<bool> InativarAsync(
        Guid id,
        Guid usuarioSolicitanteId,
        Guid? empresaId,
        bool incluirTodasEmpresas,
        CancellationToken cancellationToken = default)
    {
        if (id == usuarioSolicitanteId)
            throw new AppValidationException("Você não pode inativar o próprio usuário logado.");

        var query = _context.Usuarios
            .Include(x => x.UsuarioEmpresas)
            .Where(x => x.Id == id);

        if (!incluirTodasEmpresas)
        {
            if (!empresaId.HasValue)
                throw new AppUnauthorizedException("Token sem empresa vinculada.");

            query = query.Where(x => x.UsuarioEmpresas.Any(v => v.EmpresaId == empresaId.Value && v.Ativo));
        }

        var usuario = await query.FirstOrDefaultAsync(cancellationToken);

        if (usuario is null)
            return false;

        if (usuario.IsSuperAdmin && !incluirTodasEmpresas)
            throw new AppUnauthorizedException("Apenas super administradores podem inativar outro super administrador.");

        usuario.Ativo = false;

        foreach (var vinculo in usuario.UsuarioEmpresas)
        {
            if (incluirTodasEmpresas || vinculo.EmpresaId == empresaId)
                vinculo.Ativo = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

