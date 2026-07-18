using System;
using System.Security.Principal;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class PrintPreviewDialog : Window
{
    public string PreviewTitle { get; }
    public string PreviewContent { get; }
    public string PreviewMetaLeft { get; }
    public string PreviewMetaRight { get; }
    public string PreviewFooter { get; }

    public PrintPreviewDialog(string title, string content)
    {
        PreviewTitle = title;
        PreviewContent = content;
        PreviewMetaLeft = $"Kỳ in: {DateTime.Now:dd/MM/yyyy HH:mm}";
        PreviewMetaRight = $"Người lập: {GetCurrentUser()}";
        PreviewFooter = "Dữ liệu: In-memory sample mode • ASMILE BO";

        InitializeComponent();
        DataContext = this;
    }

    private static string GetCurrentUser()
    {
        try
        {
            return WindowsIdentity.GetCurrent()?.Name ?? Environment.UserName;
        }
        catch
        {
            return Environment.UserName;
        }
    }
}
