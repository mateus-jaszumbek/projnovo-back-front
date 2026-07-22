namespace ServicosApp.Application.Interfaces;

public interface IPagamentoCredentialSecretProtector
{
    string? Protect(string? value);
    string? Unprotect(string? value);
}
