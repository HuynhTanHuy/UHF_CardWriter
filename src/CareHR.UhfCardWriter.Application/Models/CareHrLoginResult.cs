namespace CareHR.UhfCardWriter.Application.Models;

/// <summary>Result of CareHR username/password login.</summary>
public sealed class CareHrLoginResult
{
    public CareHrLoginResult(bool success, string? token, string message)
    {
        Success = success;
        Token = token;
        Message = message ?? string.Empty;
    }

    public bool Success { get; }

    /// <summary>JWT access token when <see cref="Success"/>; never log this value.</summary>
    public string? Token { get; }

    public string Message { get; }

    public static CareHrLoginResult Ok(string token, string message = "Đăng nhập thành công.") =>
        new(true, token, message);

    public static CareHrLoginResult Fail(string message) =>
        new(false, null, message);
}
