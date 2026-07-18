using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class PlainTextSecretProtector : ISecretProtector
{
    public string Protect(string? plainText) => plainText ?? string.Empty;

    public string Unprotect(string? protectedText) => protectedText ?? string.Empty;

    public bool IsProtected(string? value) => false;
}
