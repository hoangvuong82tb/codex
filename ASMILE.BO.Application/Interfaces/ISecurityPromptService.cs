namespace ASMILE.BO.Application.Interfaces;

public interface ISecurityPromptService
{
    Task<bool> ConfirmDangerousActionAsync(string title, string message);
}
