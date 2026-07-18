using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockPrinterSettingsService : IPrinterSettingsService
{
    public Task<IReadOnlyList<PrinterDeviceDto>> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<PrinterDeviceDto> result = new[]
        {
            new PrinterDeviceDto { Name = "HP-LJ-Accounting-01", SharePath = @"\\PRINT-SRV\HP-LJ-Accounting-01", Status = "Online" },
            new PrinterDeviceDto { Name = "EPSON-KHO-W2", SharePath = @"\\PRINT-SRV\EPSON-KHO-W2", Status = "Online" },
            new PrinterDeviceDto { Name = "XPRINTER-POS-01", SharePath = @"\\POS-01\XPRINTER-POS-01", Status = "Online" },
            new PrinterDeviceDto { Name = "Canon-HR-02", SharePath = @"\\PRINT-SRV\Canon-HR-02", Status = "Offline" },
            new PrinterDeviceDto { Name = "Brother-BackOffice", SharePath = @"\\PRINT-SRV\Brother-BackOffice", Status = "Online" }
        };

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PrinterPermissionDto>> GetPrinterPermissionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PrinterPermissionDto> permissions = new[]
        {
            new PrinterPermissionDto { PrinterName = "HP-LJ-Accounting-01", AllowedDocuments = "Phiếu nhập, Phiếu xuất, Hóa đơn, Công nợ phải thu, Công nợ phải trả", Profile = "Full" },
            new PrinterPermissionDto { PrinterName = "EPSON-KHO-W2", AllowedDocuments = "Phiếu nhập, Phiếu xuất, Báo cáo tồn kho", Profile = "Warehouse" },
            new PrinterPermissionDto { PrinterName = "XPRINTER-POS-01", AllowedDocuments = "Hóa đơn POS, Phiếu tạm tính, Bếp/FO ticket", Profile = "POS/FO" },
            new PrinterPermissionDto { PrinterName = "Canon-HR-02", AllowedDocuments = "Biểu mẫu nhân sự, Biên bản nội bộ", Profile = "HR" },
            new PrinterPermissionDto { PrinterName = "Brother-BackOffice", AllowedDocuments = "Công nợ, Báo cáo tài chính, Chứng từ tổng hợp", Profile = "BackOffice" }
        };

        return Task.FromResult(permissions);
    }
}
