using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Enums;
using System.Security.Cryptography;
using System.Text;

namespace ASMILE.BO.Infrastructure.Repositories;

internal static class VirtualFileCenterSeed
{
    internal sealed record SupplierSeed(Guid Id, string Code, string Name);
    internal sealed record CustomerSeed(Guid Id, string Code, string Name);

    public static Guid StableGuid(string seed)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes);
    }

    public static List<SupplierSeed> DefaultSuppliers()
    {
        return new List<SupplierSeed>
        {
            new(StableGuid("supplier:ABC"), "NCC-ABC", "Công ty ABC"),
            new(StableGuid("supplier:MINHPHAT"), "NCC-MP", "Công ty Minh Phát"),
            new(StableGuid("supplier:ANKHANG"), "NCC-AK", "Công ty An Khang")
        };
    }

    public static List<CustomerSeed> DefaultCustomers()
    {
        return new List<CustomerSeed>
        {
            new(StableGuid("customer:KHLE"), "KH-LE", "Khách lẻ"),
            new(StableGuid("customer:NHATRANG"), "KH-NTR", "Nhà hàng Nha Trang")
        };
    }

    public static List<BusinessDocumentDto> DefaultDocuments()
    {
        var supplierAbc = StableGuid("supplier:ABC");
        var supplierMinhPhat = StableGuid("supplier:MINHPHAT");

        return new List<BusinessDocumentDto>
        {
            new()
            {
                Id = StableGuid("doc:PN00024"),
                Code = "PN00024",
                Title = "Phiếu nhập Công ty ABC",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Supplier,
                EntityId = supplierAbc,
                DocumentType = BusinessDocumentType.ImportVoucher,
                DocumentDate = new DateTime(2026, 4, 25),
                Amount = 125000000m,
                Currency = "VND",
                VirtualPath = @"D:\ACCOUNTING\Nhà cung cấp\Công ty ABC\02. Phiếu nhập\2026\04\25",
                PhysicalFilePath = "storage://accounting/import/2026/04/25/PN00024.pdf",
                FileName = "PN00024.pdf",
                FileExtension = ".pdf",
                FileSize = 364234,
                Status = DocumentStatus.Active,
                CreatedBy = "admin",
                CreatedAt = new DateTime(2026, 4, 25, 9, 12, 0),
                UpdatedAt = new DateTime(2026, 4, 25, 9, 12, 0)
            },
            new()
            {
                Id = StableGuid("doc:HD00051"),
                Code = "HD00051",
                Title = "Hóa đơn đầu vào Công ty ABC",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Supplier,
                EntityId = supplierAbc,
                DocumentType = BusinessDocumentType.Invoice,
                DocumentDate = new DateTime(2026, 4, 25),
                Amount = 125000000m,
                Currency = "VND",
                VirtualPath = @"D:\ACCOUNTING\Nhà cung cấp\Công ty ABC\03. Hóa đơn đầu vào\2026\04\25",
                PhysicalFilePath = "storage://accounting/invoices/in/2026/04/25/HD00051.pdf",
                FileName = "HD00051.pdf",
                FileExtension = ".pdf",
                FileSize = 298412,
                Status = DocumentStatus.Active,
                CreatedBy = "admin",
                CreatedAt = new DateTime(2026, 4, 25, 9, 26, 0),
                UpdatedAt = new DateTime(2026, 4, 25, 9, 26, 0)
            },
            new()
            {
                Id = StableGuid("doc:UNC00012"),
                Code = "UNC00012",
                Title = "Thanh toán Công ty ABC",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Supplier,
                EntityId = supplierAbc,
                DocumentType = BusinessDocumentType.PaymentVoucher,
                DocumentDate = new DateTime(2026, 4, 26),
                Amount = 50000000m,
                Currency = "VND",
                VirtualPath = @"D:\ACCOUNTING\Nhà cung cấp\Công ty ABC\04. Thanh toán - Tạm ứng\2026\04\26",
                PhysicalFilePath = "storage://accounting/payment/2026/04/26/UNC00012.pdf",
                FileName = "UNC00012.pdf",
                FileExtension = ".pdf",
                FileSize = 153920,
                Status = DocumentStatus.Active,
                CreatedBy = "admin",
                CreatedAt = new DateTime(2026, 4, 26, 10, 01, 0),
                UpdatedAt = new DateTime(2026, 4, 26, 10, 01, 0)
            },
            new()
            {
                Id = StableGuid("doc:TU00003"),
                Code = "TU00003",
                Title = "Tạm ứng Công ty Minh Phát",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Supplier,
                EntityId = supplierMinhPhat,
                DocumentType = BusinessDocumentType.AdvancePayment,
                DocumentDate = new DateTime(2026, 4, 26),
                Amount = 20000000m,
                Currency = "VND",
                VirtualPath = @"D:\ACCOUNTING\Nhà cung cấp\Công ty Minh Phát\04. Thanh toán - Tạm ứng\2026\04\26",
                PhysicalFilePath = "storage://accounting/payment/2026/04/26/TU00003.pdf",
                FileName = "TU00003.pdf",
                FileExtension = ".pdf",
                FileSize = 132200,
                Status = DocumentStatus.Active,
                CreatedBy = "admin",
                CreatedAt = new DateTime(2026, 4, 26, 11, 33, 0),
                UpdatedAt = new DateTime(2026, 4, 26, 11, 33, 0)
            }
        };
    }
}
