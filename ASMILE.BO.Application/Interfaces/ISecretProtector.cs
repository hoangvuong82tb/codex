namespace ASMILE.BO.Application.Interfaces;

public interface ISecretProtector
{
    string Protect(string? plainText);
    string Unprotect(string? protectedText);
    bool IsProtected(string? value);
}
