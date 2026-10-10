using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ItemImportExportServiceTests
{
    [Fact]
    public async Task ExportAndReadCsv_PreservesCommercialAndMinibarFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"asmile-item-export-{Guid.NewGuid():N}.csv");
        try
        {
            var service = new ItemImportExportService();
            await service.ExportCurrentViewAsync(
            [
                new ItemDto
                {
                    ItemCode = "WATER",
                    ItemName = "Nước đóng chai",
                    Unit = "Chai",
                    GroupName = "Minibar",
                    SupplierCode = "NCC-001",
                    SupplierName = "Nhà cung cấp A",
                    RetailPrice = 25_000m,
                    VatRate = 8m,
                    IsMinibarItem = true,
                    MinibarCategory = "Nước",
                    MinibarStandardQuantity = 2m,
                    MinibarMinStock = 1m,
                    MinibarMaxStock = 4m,
                    IsActive = true
                }
            ], path);

            var rows = await service.ReadImportRowsAsync(path);
            rows.Should().ContainSingle();
            var row = rows[0];
            row.RetailPriceRaw.Should().Be("25000");
            row.VatRateRaw.Should().Be("8");
            row.IsMinibarItemRaw.Should().Be("Có");
            row.MinibarCategory.Should().Be("Nước");
            row.MinibarStandardQuantityRaw.Should().Be("2");
            row.MinibarMinStockRaw.Should().Be("1");
            row.MinibarMaxStockRaw.Should().Be("4");
            row.IsActiveRaw.Should().Be("Có");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ReadLegacyCsv_DefaultsNewFieldsAndKeepsActiveColumn()
    {
        var path = Path.Combine(Path.GetTempPath(), $"asmile-item-legacy-{Guid.NewGuid():N}.csv");
        try
        {
            await File.WriteAllTextAsync(
                path,
                "Mã hàng,Tên hàng,Đơn vị,Nhóm hàng,Mã NCC,Tên NCC,Kích hoạt\nOLD-001,Hàng cũ,Cái,Khác,,,Có\n");

            var rows = await new ItemImportExportService().ReadImportRowsAsync(path);
            rows.Should().ContainSingle();
            var row = rows[0];
            row.IsActiveRaw.Should().Be("Có");
            row.RetailPriceRaw.Should().BeEmpty();
            row.IsMinibarItemRaw.Should().BeEmpty();
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
