using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class CustomerImportExportServiceTests
{
    [Fact]
    public async Task ExportCurrentView_UsesCustomerFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"asmile-customer-export-{Guid.NewGuid():N}.csv");
        try
        {
            var service = new CustomerImportExportService();
            await service.ExportCurrentViewAsync(
            [
                new CustomerDto
                {
                    CustomerCode = "KH-001",
                    CustomerName = "Khách hàng thật",
                    Phone = "0901234567",
                    Email = "that@example.com",
                    TaxCode = "0101234567",
                    IsActive = true
                }
            ],
            path);

            var content = await File.ReadAllTextAsync(path);
            content.Should().Contain("Mã KH,Tên khách hàng,Điện thoại,Trạng thái");
            content.Should().Contain("KH-001,Khách hàng thật,0901234567,Kích hoạt");
            content.Should().Contain("that@example.com,0101234567");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
