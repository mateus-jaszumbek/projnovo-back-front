namespace ServicosApp.Application.Exceptions;

public class AppNotFoundException : Exception
{
    public AppNotFoundException(string message) : base(message)
    {
    }
}