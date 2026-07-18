namespace ASMILE.BO.App.Services;

public interface IDialogService
{
    bool Confirm(string message, string title = "Xác nhận");
    void Info(string message, string title = "Thông báo");
    void Error(string message, string title = "Lỗi");
}

