namespace CloneEbay.Application.Common;

public sealed class AppProblemException : Exception
{
    public AppProblemException(int statusCode, string errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string ErrorCode { get; }
}
