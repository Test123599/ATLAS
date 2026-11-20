namespace MediaLib.Api.Common;

/// <summary>
/// A deliberate, user-facing business/validation failure. The centralized handler
/// turns it into a consistent ApiError payload; unexpected exceptions are never exposed.
/// </summary>
public sealed class AppException : Exception
{
    public AppException(int statusCode, string code, string message, Dictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors;
    }

    public int StatusCode { get; }

    public string Code { get; }

    public Dictionary<string, string[]>? Errors { get; }

    public static AppException NotFound(string message, string code = "RESSOURCE_INTROUVABLE")
        => new(StatusCodes.Status404NotFound, code, message);

    public static AppException Conflict(string message, string code = "CONFLIT")
        => new(StatusCodes.Status409Conflict, code, message);

    public static AppException BadRequest(string message, string code = "REQUETE_INVALIDE", Dictionary<string, string[]>? errors = null)
        => new(StatusCodes.Status400BadRequest, code, message, errors);

    public static AppException Forbidden(string message, string code = "ACCES_REFUSE")
        => new(StatusCodes.Status403Forbidden, code, message);

    public static AppException Unauthorized(string message, string code = "NON_AUTHENTIFIE")
        => new(StatusCodes.Status401Unauthorized, code, message);
}
