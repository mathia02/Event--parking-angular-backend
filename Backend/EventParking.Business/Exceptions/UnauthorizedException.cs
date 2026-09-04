namespace EventParking.Business.Exceptions;

public class UnauthorizedException : Exception
{
    public int StatusCode { get; }

    public UnauthorizedException(
        string message,
        int statusCode = 401)
        : base(message)
    {
        StatusCode = statusCode;
    }
}