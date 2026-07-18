using ASMILE.BO.Shared.Exceptions;
using System.IO;

namespace ASMILE.BO.App.Infrastructure;

public static class UserExceptionMapper
{
    public static string Map(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => "Thao tác đã được hủy.",
            AppValidationException validation => validation.Message,
            AppConcurrencyException => "Dữ liệu đã thay đổi bởi phiên khác. Vui lòng tải lại trước khi lưu.",
            UnauthorizedAccessException => "Bạn không có quyền thực hiện thao tác này.",
            IOException => "Không thể đọc hoặc ghi file. Kiểm tra đường dẫn và quyền truy cập.",
            InvalidOperationException invalid when !string.IsNullOrWhiteSpace(invalid.Message) => invalid.Message,
            ArgumentException argument when !string.IsNullOrWhiteSpace(argument.Message) => argument.Message,
            _ => "Thao tác không thành công. Vui lòng kiểm tra nhật ký hệ thống."
        };
    }
}
