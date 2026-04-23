using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class UsuarioEmpresaService : IUsuarioEmpresaService
{
    private readonly AppDbContext _context;

    public UsuarioEmpresaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UsuarioEmpresaDto> CriarAsync(
        CreateUsuarioEmpresaDto dto,
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!solicitanteEhSuperAdmin && dto.EmpresaId != empresaSolicitanteId)
            throw new AppUnauthorizedException("Você só pode vincular usuários à sua própria empresa.");

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.UsuarioId && x.Ativo, cancellationToken);

        if (usuario is null)
            throw new AppNotFoundException("Usuário não encontrado.");

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.EmpresaId && x.Ativo, cancellationToken);

        if (empresa is null)
            throw new AppNotFoundException("Empresa não encontrada.");

        var vinculoExiste = await _context.UsuarioEmpresas
            .AnyAsync(x => x.UsuarioId == dto.UsuarioId && x.EmpresaId == dto.EmpresaId, cancellationToken);

        if (vinculoExiste)
            throw new AppConflictException("Esse usuário já está vinculado a essa empresa.");

        var perfil = NormalizarPerfil(dto.Perfil);
        var nivelAcesso = NormalizarNivelAcesso(perfil, dto.NivelAcesso);

        var usuarioEmpresa = new UsuarioEmpresa
        {
            Id = Guid.NewGuid(),
            UsuarioId = dto.UsuarioId,
            EmpresaId = dto.EmpresaId,
            Perfil = perfil,
            NivelAcesso = nivelAcesso,
            Ativo = dto.Ativo
        };

        _context.UsuarioEmpresas.Add(usuarioEmpresa);
        await _context.SaveChangesAsync(cancellationToken);

        return new UsuarioEmpresaDto
        {
            Id = usuarioEmpresa.Id,
            UsuarioId = usuario.Id,
            UsuarioNome = usuario.Nome,
            EmpresaId = empresa.Id,
            EmpresaNomeFantasia = empresa.NomeFantasia,
            Perfil = usuarioEmpresa.Perfil,
            NivelAcesso = usuarioEmpresa.NivelAcesso,
            Ativo = usuarioEmpresa.Ativo
        };
    }

    public async Task<List<UsuarioEmpresaDto>> ListarAsync(
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UsuarioEmpresas
            .AsNoTracking()
            .Include(x => x.Usuario)
            .Include(x => x.Empresa)
            .AsQueryable();

        if (!solicitanteEhSuperAdmin)
            query = query.Where(x => x.EmpresaId == empresaSolicitanteId);

        return await query
            .OrderBy(x => x.Empresa!.NomeFantasia)
            .ThenBy(x => x.Usuario!.Nome)
            .Select(x => new UsuarioEmpresaDto
            {
                Id = x.Id,
                UsuarioId = x.UsuarioId,
                UsuarioNome = x.Usuario != null ? x.Usuario.Nome : null,
                EmpresaId = x.EmpresaId,
                EmpresaNomeFantasia = x.Empresa != null ? x.Empresa.NomeFantasia : null,
                Perfil = x.Perfil,
                NivelAcesso = x.NivelAcesso,
                Ativo = x.Ativo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioEmpresaDto?> AtualizarAsync(
        Guid id,
        UpdateUsuarioEmpresaDto dto,
        Guid usuarioSolicitanteId,
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UsuarioEmpresas
            .Include(x => x.Usuario)
            .Include(x => x.Empresa)
            .Where(x => x.Id == id);

        if (!solicitanteEhSuperAdmin)
            query = query.Where(x => x.EmpresaId == empresaSolicitanteId);

        var vinculo = await query.FirstOrDefaultAsync(cancellationToken);

        if (vinculo is null)
            return null;

        if (vinculo.UsuarioId == usuarioSolicitanteId && !solicitanteEhSuperAdmin)
            throw new AppValidationException("Você não pode alterar o próprio vínculo com a empresa.");

        if (vinculo.Usuario?.IsSuperAdmin == true && !solicitanteEhSuperAdmin)
            throw new AppUnauthorizedException("Apenas super administradores podem alterar vínculo de super administrador.");

        var perfil = NormalizarPerfil(dto.Perfil);
        vinculo.Perfil = perfil;
        vinculo.NivelAcesso = NormalizarNivelAcesso(perfil, dto.NivelAcesso);
        vinculo.Ativo = dto.Ativo;

        await _context.SaveChangesAsync(cancellationToken);

        return new UsuarioEmpresaDto
        {
            Id = vinculo.Id,
            UsuarioId = vinculo.UsuarioId,
            UsuarioNome = vinculo.Usuario?.Nome,
            EmpresaId = vinculo.EmpresaId,
            EmpresaNomeFantasia = vinculo.Empresa?.NomeFantasia,
            Perfil = vinculo.Perfil,
            NivelAcesso = vinculo.NivelAcesso,
            Ativo = vinculo.Ativo
        };
    }

    private static string NormalizarPerfil(string? perfil)
    {
        var normalized = string.IsNullOrWhiteSpace(perfil)
            ? "atendente"
            : perfil.Trim().ToLowerInvariant();

        if (normalized == "administrador")
            normalized = "admin";

        var perfisValidos = new[] { "owner", "admin", "gerente", "atendente", "tecnico", "financeiro", "estoque" };

        if (!perfisValidos.Contains(normalized))
            throw new AppValidationException("Perfil inválido.");

        return normalized;
    }

    private static int NormalizarNivelAcesso(string perfil, int nivelAcesso)
    {
        if (perfil is "owner" or "admin")
            return 5;

        if (nivelAcesso is < 1 or > 5)
            throw new AppValidationException("Nível de acesso deve estar entre 1 e 5.");

        return nivelAcesso;
    }
    public async Task<bool> InativarAsync(
        Guid id,
        Guid usuarioSolicitanteId,
        Guid empresaSolicitanteId,
        bool solicitanteEhSuperAdmin,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UsuarioEmpresas
            .Include(x => x.Usuario)
            .Where(x => x.Id == id);

        if (!solicitanteEhSuperAdmin)
            query = query.Where(x => x.EmpresaId == empresaSolicitanteId);

        var vinculo = await query.FirstOrDefaultAsync(cancellationToken);

        if (vinculo is null)
            return false;

        if (vinculo.UsuarioId == usuarioSolicitanteId)
            throw new AppValidationException("Você não pode remover o próprio vínculo com a empresa.");

        if (vinculo.Usuario?.IsSuperAdmin == true && !solicitanteEhSuperAdmin)
            throw new AppUnauthorizedException("Apenas super administradores podem remover vínculo de super administrador.");

        vinculo.Ativo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
