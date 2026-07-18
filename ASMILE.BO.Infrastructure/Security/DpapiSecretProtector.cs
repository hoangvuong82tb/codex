using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace ASMILE.BO.Infrastructure.Security;

[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    public string Protect(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText ?? string.Empty;
        }

        if (IsProtected(plainText))
        {
            return plainText;
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
        return SecurityConstants.SecretPrefix + Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string? protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
        {
            return protectedText ?? string.Empty;
        }

        if (!IsProtected(protectedText))
        {
            return protectedText;
        }

        var raw = protectedText[SecurityConstants.SecretPrefix.Length..];
        var protectedBytes = Convert.FromBase64String(raw);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(bytes);
    }

    public bool IsProtected(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith(SecurityConstants.SecretPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
