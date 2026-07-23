using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServicosApp.Application.DTOs;
using ServicosApp.Application.Exceptions;
using ServicosApp.Application.Interfaces;
using ServicosApp.Application.Legal;
using ServicosApp.Domain.Entities;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private static readonly TimeSpan PasswordResetTokenValidade = TimeSpan.FromHours(1);

    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthService(
        AppDbContext context,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegistrarEmpresaAsync(
        RegistrarEmpresaDto dto,
        CancellationToken cancellationToken = default)
    {
        var emailUsuario = dto.EmailUsuario.Trim().ToLowerInvariant();
        var cnpj = dto.Cnpj.Trim();
        var acceptedAtUtc = DateTime.UtcNow;

        ValidarForcaSenha(dto.Senha);

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

    public async Task EsqueciSenhaAsync(
        ForgotPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(x => x.Email == email && x.Ativo, cancellationToken);

        // Nao revela se o e-mail existe ou nao: se nao encontrar, simplesmente nao envia nada.
        if (usuario is null)
            return;

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        usuario.PasswordResetTokenHash = HashResetToken(token);
        usuario.PasswordResetTokenExpiraEmUtc = DateTime.UtcNow.Add(PasswordResetTokenValidade);

        await _context.SaveChangesAsync(cancellationToken);

        var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var linkRedefinicao = $"{frontendBaseUrl}/redefinir-senha?token={Uri.EscapeDataString(token)}";

        var corpoEmail = $"""
            <p>Olá, {System.Net.WebUtility.HtmlEncode(usuario.Nome)}.</p>
            <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
            <p><a href="{linkRedefinicao}">Clique aqui para criar uma nova senha</a>.</p>
            <p>Esse link expira em 1 hora. Se você não solicitou essa alteração, ignore este e-mail.</p>
            """;

        await _emailSender.SendAsync(
            usuario.Email,
            "Redefinição de senha - Servicos App",
            corpoEmail,
            cancellationToken);
    }

    public async Task RedefinirSenhaAsync(
        ResetPasswordDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarForcaSenha(dto.NovaSenha);

        var tokenHash = HashResetToken(dto.Token.Trim());

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(
                x => x.PasswordResetTokenHash == tokenHash &&
                     x.PasswordResetTokenExpiraEmUtc != null &&
                     x.PasswordResetTokenExpiraEmUtc > DateTime.UtcNow,
                cancellationToken);

        if (usuario is null)
            throw new AppValidationException("Token inválido ou expirado. Solicite a redefinição novamente.");

        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.NovaSenha);
        usuario.PasswordResetTokenHash = null;
        usuario.PasswordResetTokenExpiraEmUtc = null;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidarForcaSenha(string senha)
    {
        if (senha.Length < 7 || !senha.Any(char.IsUpper) || !senha.Any(char.IsLower) || !senha.Any(char.IsDigit))
            throw new AppValidationException("Senha deve ter mais de 6 caracteres, letra maiúscula, letra minúscula e número.");
    }

    private static string HashResetToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
