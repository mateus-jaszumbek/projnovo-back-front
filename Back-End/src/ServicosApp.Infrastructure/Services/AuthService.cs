using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Application.Legal;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthService(AppDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegistrarEmpresaAsync(
        RegistrarEmpresaDto dto,
        CancellationToken cancellationToken = default)
    {
        var emailUsuario = dto.EmailUsuario.Trim().ToLowerInvariant();
        var cnpj = dto.Cnpj.Trim();
        var acceptedAtUtc = DateTime.UtcNow;

        if (dto.Senha.Length < 7 || !dto.Senha.Any(char.IsUpper) || !dto.Senha.Any(char.IsLower) || !dto.Senha.Any(char.IsDigit))
            throw new AppValidationException("Senha deve ter mais de 6 caracteres, letra maiúscula, letra minúscula e número.");

        if (!dto.AceitouTermosUso)
            throw new AppValidationException("É obrigatório aceitar os Termos de Uso para criar a conta.");

        if (!dto.AceitouPoliticaPrivacidade)
            throw new AppValidationException("É obrigatório aceitar a Política de Privacidade e LGPD para criar a conta.");

        var emailJaExiste = await _context.Usuarios
            .AnyAsync(x => x.Email == emailUsuario, cancellationToken);

        if (emailJaExiste)
            throw new AppConflictException("Já existe um usuário com esse e-mail.");

        var cnpjJaExiste = await _context.Empresas
            .AnyAsync(x => x.Cnpj == cnpj, cancellationToken);

        if (cnpjJaExiste)
            throw new AppConflictException("Já existe uma empresa com esse CNPJ.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var empresa = new Empresa
        {
            Id = Guid.NewGuid(),
            RazaoSocial = dto.RazaoSocial.Trim(),
            NomeFantasia = dto.NomeFantasia.Trim(),
            Cnpj = cnpj,
            Email = dto.EmailEmpresa?.Trim(),
            Telefone = dto.TelefoneEmpresa?.Trim(),
            Ativo = true
        };

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = dto.NomeUsuario.Trim(),
            Email = emailUsuario,
            TermosUsoVersaoAceita = LegalDocumentVersions.TermsOfUse,
            TermosUsoAceitoEmUtc = acceptedAtUtc,
            PoliticaPrivacidadeVersaoAceita = LegalDocumentVersions.PrivacyPolicy,
            PoliticaPrivacidadeAceitaEmUtc = acceptedAtUtc,
            Ativo = true,
            IsSuperAdmin = false
        };

        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);

        var usuarioEmpresa = new UsuarioEmpresa
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            EmpresaId = empresa.Id,
            Perfil = "owner",
            Ativo = true
        };

        _context.Empresas.Add(empresa);
        _context.Usuarios.Add(usuario);
        _context.UsuarioEmpresas.Add(usuarioEmpresa);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var (token, expiresAtUtc) = _jwtTokenService.GerarToken(
            usuario,
            empresa.Id,
            empresa.NomeFantasia,
            "owner",
            5);

        return new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            IsSuperAdmin = usuario.IsSuperAdmin,
            EmpresaId = empresa.Id,
            EmpresaNomeFantasia = empresa.NomeFantasia,
            Perfil = "owner",
            NivelAcesso = 5
        };
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (usuario is null)
            throw new AppUnauthorizedException("Usuário ou senha inválidos.");

        if (!usuario.Ativo)
            throw new AppUnauthorizedException("Usuário inativo.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.SenhaHash,
            dto.Senha);

        if (verificationResult == PasswordVerificationResult.Failed)
            throw new AppUnauthorizedException("Usuário ou senha inválidos.");

        Guid? empresaId = null;
        string? empresaNomeFantasia = null;
        string? perfil = null;
        var nivelAcesso = usuario.IsSuperAdmin ? 5 : 1;

        if (!usuario.IsSuperAdmin)
        {
            var vinculo = await _context.UsuarioEmpresas
                .AsNoTracking()
                .Include(x => x.Empresa)
                .FirstOrDefaultAsync(
                    x => x.UsuarioId == usuario.Id &&
                         x.Ativo &&
                         x.Empresa != null &&
                         x.Empresa.Ativo,
                    cancellationToken);

            if (vinculo is null)
                throw new AppUnauthorizedException("Usuário sem vínculo ativo com empresa.");

            empresaId = vinculo.EmpresaId;
            empresaNomeFantasia = vinculo.Empresa?.NomeFantasia;
            perfil = vinculo.Perfil;
            nivelAcesso = vinculo.NivelAcesso;
        }

        var (token, expiresAtUtc) = _jwtTokenService.GerarToken(
            usuario,
            empresaId,
            empresaNomeFantasia,
            perfil,
            nivelAcesso);

        return new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            IsSuperAdmin = usuario.IsSuperAdmin,
            EmpresaId = empresaId,
            EmpresaNomeFantasia = empresaNomeFantasia,
            Perfil = perfil,
            NivelAcesso = nivelAcesso
        };
    }
}
