namespace MediaLib.Web.Services;

/// <summary>Client-side representation of the API's consistent error payload.</summary>
public sealed class ApiRequestException : Exception
{
    public ApiRequestException(int status, string code, string message, Dictionary<string, string[]>? errors = null)
        : base(message)
    {
        Status = status;
        Code = code;
        Errors = errors;
    }

    public int Status { get; }

    public string Code { get; }

    public Dictionary<string, string[]>? Errors { get; }

    public bool EstNotFound => Status == StatusCodes.Status404NotFound;
    public bool EstInterdit => Status == StatusCodes.Status403Forbidden;
    public bool EstValidation => Status == StatusCodes.Status400BadRequest && Code == "VALIDATION";
    public bool EstConflit => Status == StatusCodes.Status409Conflict;
}
