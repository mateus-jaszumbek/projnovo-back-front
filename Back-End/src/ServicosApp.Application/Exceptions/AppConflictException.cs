namespace ServicosApp.Application.Exceptions;

public class AppConflictException : Exception
{
    public AppConflictException(string message) : base(message)
    {
    }
}