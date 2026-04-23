namespace ServicosApp.Application.Exceptions;

public class AppUnauthorizedException : Exception
{
    public AppUnauthorizedException(string message) : base(message)
    {
    }
}