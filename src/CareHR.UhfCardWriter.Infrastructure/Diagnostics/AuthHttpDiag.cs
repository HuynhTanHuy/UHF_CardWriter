using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CareHR.UhfCardWriter.Infrastructure.Diagnostics;

/// <summary>
/// Safe JWT/HTTP diagnostics for Writer login → CareHR API audit.
/// Never logs full JWT, Authorization header, or refresh tokens.
/// </summary>
public static class AuthHttpDiag
{
    public static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CareHR",
                "UhfCardWriter",
                "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{message}";
            File.AppendAllText(Path.Combine(dir, "write-diag.log"), line + Environment.NewLine);
        }
        catch
        {
            // Diagnostic only.
        }
    }

    public static string Fingerprint(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "(none)";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(hash.AsSpan(0, 4)); // 8 hex chars
    }

    public static string TokenPrefix(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "(none)";

        var t = token.Trim();
        return t.Length <= 12 ? t : t[..12];
    }

    public static void LogAuthSession(string source, string? token)
    {
        var has = !string.IsNullOrWhiteSpace(token);
        var length = has ? token!.Trim().Length : 0;
        Log(
            $"[AuthSession] Source={source} HasToken={has} TokenLength={length} " +
            $"TokenPrefix={TokenPrefix(token)} TokenFingerprint={Fingerprint(token)} " +
            DescribeClaims(token));
    }

    public static void LogHttpRequest(
        string operation,
        string method,
        string url,
        string? token,
        bool hasAuthorizationHeader,
        string? authorizationScheme)
    {
        var has = !string.IsNullOrWhiteSpace(token);
        Log(
            $"[HTTP] Operation={operation} Method={method} Url={url} " +
            $"HasToken={has} TokenLength={(has ? token!.Trim().Length : 0)} " +
            $"TokenFingerprint={Fingerprint(token)} " +
            $"HasAuthorizationHeader={hasAuthorizationHeader} Scheme={authorizationScheme ?? "(none)"}");
    }

    public static void LogHttpResult(string operation, int statusCode, string? reason, string? body)
    {
        var safeBody = SanitizeBody(body);
        Log(
            $"[HTTP] Result Operation={operation} Status={statusCode} " +
            $"Reason={reason ?? string.Empty} ResponseBody={safeBody}");
    }

    public static string DescribeClaims(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "Claims=(none)";

        try
        {
            var parts = token.Trim().Split('.');
            if (parts.Length < 2)
                return "Claims=(not-jwt)";

            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string Get(string name) =>
                TryGetString(root, name) ?? "(missing)";

            var role = TryGetString(root, "role")
                       ?? TryGetString(root, "roles")
                       ?? TryGetArrayJoined(root, "role")
                       ?? TryGetArrayJoined(root, "roles")
                       ?? "(missing)";

            var permission = TryGetString(root, "permission")
                             ?? TryGetString(root, "permissions")
                             ?? TryGetString(root, "scope")
                             ?? TryGetArrayJoined(root, "permission")
                             ?? TryGetArrayJoined(root, "permissions")
                             ?? "(missing)";

            var hospitalId = TryGetString(root, "hospitalId") ?? "(missing)";
            var hasHospitalId = hospitalId != "(missing)" && Guid.TryParse(hospitalId, out _);

            return
                $"Claims exp={Get("exp")} iss={Truncate(Get("iss"), 48)} aud={Truncate(Get("aud"), 48)} " +
                $"sub={Truncate(Get("sub"), 36)} hospitalId={(hasHospitalId ? "present" : hospitalId)} " +
                $"role={Truncate(role, 64)} permission/scope={Truncate(permission, 64)}";
        }
        catch
        {
            return "Claims=(decode-failed)";
        }
    }

    private static string SanitizeBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "(empty)";

        var s = body.Trim();
        // Never echo JWT-looking payloads.
        if (s.Contains("eyJ", StringComparison.Ordinal) && s.Contains('.'))
            return "(redacted-jwt-like)";

        return Truncate(s.Replace('\r', ' ').Replace('\n', ' '), 240);
    }

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max)
            return text;
        return text[..(max - 3)] + "...";
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var prop in root.EnumerateObject())
        {
            if (!string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            return prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            };
        }

        return null;
    }

    private static string? TryGetArrayJoined(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var prop in root.EnumerateObject())
        {
            if (!string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (prop.Value.ValueKind != JsonValueKind.Array)
                return null;

            var parts = new List<string>();
            foreach (var item in prop.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        parts.Add(s);
                }
            }

            return parts.Count == 0 ? null : string.Join(',', parts);
        }

        return null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }

        return Convert.FromBase64String(s);
    }
}
