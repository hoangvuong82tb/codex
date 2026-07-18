namespace ASMILE.BO.App.Services;

public static class OperationsDisplayTextTranslator
{
    public static string TranslateEventType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value switch
        {
            "SlowOperation" => "Tác vụ chậm",
            "Metric" => "Chỉ số",
            "RuntimeError" => "Lỗi runtime",
            "Exception" => "Ngoại lệ",
            "Snapshot" => "Bản chụp",
            "HealthCheck" => "Kiểm tra sức khỏe",
            "Workflow" => "Quy trình",
            "Task" => "Tác vụ",
            "UiLatency" => "Đơ UI",
            "SystemResource" => "Tài nguyên hệ thống",
            _ => value
        };
    }

    public static string TranslateStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value switch
        {
            "Completed" => "Hoàn thành",
            "Running" => "Đang chạy",
            "Pending" => "Đang chờ",
            "Failed" => "Thất bại",
            "Cancelled" => "Đã hủy",
            "Succeeded" => "Thành công",
            "Warning" => "Cảnh báo",
            "Information" => "Thông tin",
            "Error" => "Lỗi",
            "Critical" => "Nghiêm trọng",
            "New" => "Mới",
            "Active" => "Đang hoạt động",
            "Inactive" => "Không hoạt động",

            // Change type
            "Added" => "Đã thêm",
            "Removed" => "Đã gỡ",

            // Fiscal period
            "Open" => "Đang mở",
            "Locked" => "Đã khóa",
            "Closed" => "Đã đóng",

            // Cash/Bank statuses
            "Draft" => "Nháp",
            "Approved" => "Đã duyệt",
            "Posted" => "Đã ghi sổ",
            "Reversed" => "Đã đảo",
            "Confirmed" => "Đã xác nhận",
            "Paid" => "Đã thanh toán",
            "Unpaid" => "Chưa thanh toán",
            "PartiallyPaid" => "Thanh toán 1 phần",
            "Serving" => "Đang phục vụ",
            "Empty" => "Trống",
            "AwaitingPayment" => "Chờ thanh toán",

            // Cash/Bank transaction types
            "Receipt" => "Thu",
            "Payment" => "Chi",
            "Adjustment" => "Điều chỉnh",

            // Reconciliation
            "Matched" => "Đã khớp",
            "Unmatched" => "Chưa khớp",
            _ => value
        };
    }

    public static string TranslateSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value switch
        {
            "Warning" => "Cảnh báo",
            "Information" => "Thông tin",
            "Error" => "Lỗi",
            "Critical" => "Nghiêm trọng",
            "Info" => "Thông tin",
            _ => value
        };
    }

    public static string TranslateModule(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.ToLowerInvariant() switch
        {
            "scheduler" => "Bộ lập lịch",
            "audit" => "Kiểm tra hệ thống",
            "audit-center" => "Kiểm tra hệ thống",
            "navigation" => "Điều hướng",
            "inventory" => "Kho",
            "accounting" => "Kế toán",
            "general-ledger" => "Sổ cái",
            "breakfast-cafe" => "Bán đồ ăn sáng & cà phê",
            "breakfast-cafe-sales" => "Bán đồ ăn sáng & cà phê",
            "menu-recipe" => "Món ăn & định lượng",
            "settings" => "Cài đặt",
            "operations-center" => "Trung tâm vận hành",
            "tax-financial" => "Thuế & BCTC",
            "cash-bank" => "Quỹ & Ngân hàng",
            "ar" => "Công nợ phải thu",
            "ap" => "Công nợ phải trả",
            "asset-management" => "TSCĐ & CCDC",
            "training-data" => "Dữ liệu mẫu",
            "notification-center" => "Trung tâm thông báo",
            "security-admin" => "Quản trị bảo mật",
            "fiscal-periods" => "Kỳ kế toán",
            "tt99" => "TT99",
            "tt200" => "TT200",

            // Permission groups
            "Accounting" => "Kế toán",
            "Assets" => "Tài sản",
            "Audit" => "Kiểm tra",
            "CashBank" => "Quỹ & Ngân hàng",
            "Financial" => "Tài chính",
            "HR" => "Nhân sự",
            "Inventory" => "Kho",
            "MasterData" => "Dữ liệu chính",
            "Operations" => "Vận hành",
            "Reports" => "Báo cáo",
            "Sales" => "Bán hàng",
            "Security" => "Bảo mật",
            "Settings" => "Cài đặt",
            "Shell" => "Giao diện",
            "Tax" => "Thuế",

            _ => value
        };
    }

    public static string TranslateMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value switch
        {
            "Execution completed." => "Thực thi hoàn tất.",
            "Clear temp cache and compact workspace cache." => "Xóa bộ nhớ tạm và nén cache không gian làm việc.",
            "Needs optimization." => "Cần tối ưu.",
            "System exception." => "Ngoại lệ hệ thống.",
            "Recorded." => "Đã ghi nhận.",
            "TT200 validation completed." => "Kiểm tra TT200 hoàn tất.",
            "TT99 validation completed." => "Kiểm tra TT99 hoàn tất.",

            // Permission descriptions
            "Apply order discount" => "Áp dụng giảm giá đơn hàng",
            "Approve stock documents" => "Phê duyệt chứng từ kho",
            "Cancel unpaid orders" => "Hủy đơn hàng chưa thanh toán",
            "Close accounting period" => "Đóng kỳ kế toán",
            "Create and update menu items and recipe lines" => "Tạo và sửa món ăn và định lượng",
            "Create inventory adjustments" => "Tạo điều chỉnh tồn kho",
            "Create order and add menu lines" => "Tạo đơn hàng và thêm món",
            "Edit company, branch, warehouse, cash fund and bank account data permissions" => "Sửa dữ liệu công ty, chi nhánh, kho, quỹ và ngân hàng",
            "Edit role/user permissions" => "Sửa phân quyền vai trò/người dùng",
            "Execute reset and seed training data operations" => "Thực thi reset và tạo dữ liệu mẫu",
            "Export reports" => "Xuất báo cáo",
            "Lock VAT declaration" => "Khóa kê khai VAT",
            "Manage menu and operational settings" => "Quản lý cài đặt thực đơn và vận hành",
            "Manage menu recipe configuration" => "Quản lý cấu hình định lượng món",
            "Manage payable transactions" => "Quản lý giao dịch phải trả",
            "Manage receivable transactions" => "Quản lý giao dịch phải thu",
            "Manage roles and permissions" => "Quản lý vai trò và phân quyền",
            "Open AP workflow" => "Mở quy trình phải trả",
            "Open AR workflow" => "Mở quy trình phải thu",
            "Open audit center" => "Mở trung tâm kiểm tra",
            "Open bank workflow" => "Mở quy trình ngân hàng",
            "Open breakfast & cafe sales module" => "Mở module bán đồ ăn sáng & cà phê",
            "Open cash workflow" => "Mở quy trình quỹ tiền mặt",
            "Open customers/suppliers/items" => "Mở danh mục khách hàng/nhà cung cấp/hàng hóa",
            "Open financial statements" => "Mở báo cáo tài chính",
            "Open fixed/tool asset workflow" => "Mở quy trình TSCĐ/CCDC",
            "Open general ledger" => "Mở sổ cái",
            "Open HR module" => "Mở module nhân sự",
            "Open inventory workflow" => "Mở quy trình kho",
            "Open menu and recipe module" => "Mở module món ăn & định lượng",
            "Open operational dashboard" => "Mở bảng điều hành",
            "Open operations center" => "Mở trung tâm vận hành",
            "Open report center" => "Mở trung tâm báo cáo",
            "Open security admin" => "Mở quản trị bảo mật",
            "Open settings" => "Mở cài đặt",
            "Open VAT/tax workflow" => "Mở quy trình VAT/thuế",
            "Open virtual file center" => "Mở trung tâm tệp ảo",
            "Post asset documents" => "Ghi sổ chứng từ tài sản",
            "Post bank transactions" => "Ghi sổ giao dịch ngân hàng",
            "Post cash transactions" => "Ghi sổ giao dịch tiền mặt",
            "Post journal entries" => "Ghi sổ bút toán",
            "Post stock documents" => "Ghi sổ chứng từ kho",
            "Preview expected cost and margin" => "Xem trước giá vốn và lợi nhuận dự kiến",
            "Set menu item inactive" => "Ngừng bán món ăn",

            // Job names
            "Archive cleanup" => "Dọn dẹp lưu trữ",
            "Audit cleanup" => "Dọn dẹp kiểm tra",
            "Depreciation run" => "Tính khấu hao",
            "Integrity verification" => "Kiểm tra toàn vẹn",
            "Report snapshot cleanup" => "Dọn dẹp báo cáo",
            "Retention enforcement" => "Thực thi lưu giữ",
            "Search index refresh" => "Làm mới chỉ mục tìm kiếm",
            "Telemetry cleanup" => "Dọn dẹp đo lường",
            "Temp cleanup" => "Dọn dẹp tạm",
            "Tool allocation run" => "Phân bổ công cụ",
            "VAT sync" => "Đồng bộ VAT",
            "Workspace cleanup" => "Dọn dẹp không gian làm việc",

            // Common messages
            "Metric Execution completed." => "Chỉ số: Thực thi hoàn tất.",
            "Telemetry cleanup completed." => "Dọn dẹp đo lường hoàn tất.",
            "Temp cleanup completed." => "Dọn dẹp tạm hoàn tất.",
            "Workspace cleanup completed." => "Dọn dẹp không gian làm việc hoàn tất.",

            _ => value
        };
    }
}
