using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Repositories;
using ASMILE.BO.Infrastructure.SettingsEngine;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class VirtualExplorerRepositoryTests
{
    [Fact]
    public async Task Concurrent_startup_reads_are_serialized_and_keep_the_business_catalog_visible()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var repository = CreateRepository(options);

        var results = await Task.WhenAll(
            repository.GetChildrenAsync("This PC"),
            repository.GetChildrenAsync("Network"),
            repository.GetChildrenAsync(@"This PC\D: ACCOUNTING"));

        results[0].Where(x => x.NodeType == VirtualNodeType.Drive).Should().HaveCount(5);
        results[0].Where(x => x.NodeType == VirtualNodeType.Drive)
            .Should().OnlyContain(x => x.Status == "Ổ logic" && !x.IsAlert);
        var sqlServerNode = results[1].Should().ContainSingle(x => x.DisplayName == "SQL Server").Which;
        sqlServerNode.Description.Should().Contain("Microsoft.EntityFrameworkCore.InMemory");
        sqlServerNode.Description.Should().NotContain(".\\SQLEXPRESS");
        sqlServerNode.Description.Should().NotContain("ASMILE_BO");
        results[2].Select(x => x.DisplayName).Should().Contain([
            "Nhà cung cấp", "Khách hàng", "Hóa đơn", "Phiếu nhập", "Phiếu xuất"]);
    }

    [Fact]
    public void Configured_vfc_path_health_distinguishes_readable_missing_and_non_directory_paths()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asmile-vfc-path-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "not-a-directory.txt");
        File.WriteAllText(filePath, "test");
        try
        {
            var repository = CreateRepository(CreateOptions());
            var probe = typeof(VirtualExplorerRepository).GetMethod(
                "BuildDriveHealth",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            var readable = probe.Invoke(repository, [directory, true])!;
            readable.GetType().GetProperty("Status")!.GetValue(readable).Should().Be("Đọc được");
            ((string)readable.GetType().GetProperty("Description")!.GetValue(readable)!)
                .Should().Contain("Có thể đọc nội dung");

            var missing = probe.Invoke(repository, [Path.Combine(directory, "missing"), true])!;
            missing.GetType().GetProperty("Status")!.GetValue(missing).Should().Be("Không khả dụng");

            var notDirectory = probe.Invoke(repository, [filePath, true])!;
            notDirectory.GetType().GetProperty("Status")!.GetValue(notDirectory).Should().Be("Không hợp lệ");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Partially_persisted_drive_catalog_keeps_missing_default_business_folders_visible()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.VirtualDrives.Add(new VirtualDrive
            {
                Code = "D",
                Name = "ACCOUNTING",
                DisplayName = "D: ACCOUNTING",
                Icon = "drive-d.ico",
                SortOrder = 20,
                IsEnabled = true
            });
            setup.VirtualFolders.Add(new VirtualFolder
            {
                DriveCode = "D",
                Name = "Custom folder",
                DisplayName = "Custom folder",
                VirtualPath = @"This PC\D: ACCOUNTING\Custom folder",
                FolderType = VirtualNodeType.Folder,
                SortOrder = 1,
                IsSystem = true
            });
            await setup.SaveChangesAsync();
        }

        var nodes = await CreateRepository(options).GetChildrenAsync(@"This PC\D: ACCOUNTING");

        nodes.Select(x => x.DisplayName).Should().Contain([
            "Custom folder", "Nhà cung cấp", "Khách hàng", "Hóa đơn", "Phiếu nhập", "Phiếu xuất"]);
    }

    [Fact]
    public async Task Invalid_network_endpoint_diagnostics_do_not_echo_url_credentials_or_query_tokens()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.SettingDefinitions.Add(new SettingDefinition
            {
                Key = "Network.ApiBaseUrl",
                DisplayName = "API Base URL",
                Category = "Network",
                Group = "NETWORK",
                Scope = SettingScope.System,
                ValueType = SettingValueType.String,
                DefaultValue = "https://localhost"
            });
            setup.SettingValues.Add(new SettingValue
            {
                DefinitionKey = "Network.ApiBaseUrl",
                Scope = SettingScope.System,
                Value = "ftp://user:topsecret@localhost/api?token=topsecret"
            });
            await setup.SaveChangesAsync();
        }

        var networkNodes = await CreateRepository(options).GetChildrenAsync("Network");
        var apiNode = networkNodes.Should().ContainSingle(x => x.DisplayName == "API Server").Which;

        apiNode.Status.Should().Be("Không hợp lệ");
        apiNode.Description.Should().NotContain("topsecret");
        apiNode.Description.Should().NotContain("user:");
    }

    [Fact]
    public async Task Unconfigured_network_endpoints_are_not_probed_or_presented_as_localhost()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var nodes = await CreateRepository(options).GetChildrenAsync("Network");

        foreach (var displayName in new[] { "API Server", "POS Terminals", "FO Clients" })
        {
            var node = nodes.Should().ContainSingle(x => x.DisplayName == displayName).Which;
            node.Status.Should().Be("Chưa cấu hình");
            node.Description.Should().Contain("Chưa cấu hình endpoint");
            node.Description.Should().NotContain("localhost");
        }
    }

    [Fact]
    public async Task Accounting_document_folders_show_only_persisted_business_documents()
    {
        var options = CreateOptions();
        var persistedId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var exportVoucherId = Guid.NewGuid();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.BusinessDocuments.Add(new BusinessDocument
            {
                Id = persistedId,
                Code = "PN-REAL-001",
                Title = "Phiếu nhập đã lưu",
                EntityType = BusinessEntityType.Supplier,
                EntityId = Guid.NewGuid(),
                DocumentType = BusinessDocumentType.ImportVoucher,
                DocumentDate = new DateTime(2026, 9, 1),
                Amount = 125000m,
                Currency = "VND",
                VirtualPath = @"This PC\D: ACCOUNTING\Nhà cung cấp\NCC 01\Phiếu nhập",
                PhysicalFilePath = "dbdoc://real-import-001",
                FileName = "PN-REAL-001.pdf",
                FileExtension = ".pdf",
                Status = DocumentStatus.Active,
                CreatedBy = "test",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            setup.BusinessDocuments.AddRange(
                new BusinessDocument
                {
                    Id = invoiceId,
                    Code = "INV-REAL-001",
                    Title = "Hóa đơn đầu vào đã lưu",
                    EntityType = BusinessEntityType.Supplier,
                    EntityId = Guid.NewGuid(),
                    DocumentType = BusinessDocumentType.Invoice,
                    DocumentDate = new DateTime(2026, 9, 2),
                    Amount = 225000m,
                    Currency = "VND",
                    VirtualPath = @"This PC\D: ACCOUNTING\Hóa đơn",
                    PhysicalFilePath = "dbdoc://real-invoice-001",
                    FileName = "INV-REAL-001.pdf",
                    FileExtension = ".pdf",
                    Status = DocumentStatus.Active,
                    CreatedBy = "test",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new BusinessDocument
                {
                    Id = exportVoucherId,
                    Code = "PX-DOC-REAL-001",
                    Title = "Phiếu xuất đã lưu",
                    EntityType = BusinessEntityType.Supplier,
                    EntityId = Guid.NewGuid(),
                    DocumentType = BusinessDocumentType.ExportVoucher,
                    DocumentDate = new DateTime(2026, 9, 3),
                    Amount = 325000m,
                    Currency = "VND",
                    VirtualPath = @"This PC\D: ACCOUNTING\Phiếu xuất",
                    PhysicalFilePath = "dbdoc://real-export-001",
                    FileName = "PX-DOC-REAL-001.pdf",
                    FileExtension = ".pdf",
                    Status = DocumentStatus.Active,
                    CreatedBy = "test",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            setup.VirtualFolders.Add(new VirtualFolder
            {
                DriveCode = "D",
                Name = "Archived",
                DisplayName = "Danh mục cũ",
                VirtualPath = @"This PC\D: ACCOUNTING\Phiếu nhập\Danh mục cũ",
                FolderType = VirtualNodeType.Folder,
                IsSystem = false
            });
            await setup.SaveChangesAsync();
        }

        var repository = CreateRepository(options);
        var importNodes = await repository.GetChildrenAsync(@"This PC\D: ACCOUNTING\Phiếu nhập");
        var invoiceNodes = await repository.GetChildrenAsync(@"This PC\D: ACCOUNTING\Hóa đơn");
        var exportNodes = await repository.GetChildrenAsync(@"This PC\D: ACCOUNTING\Phiếu xuất");

        importNodes.Should().ContainSingle(x => x.DocumentId == persistedId)
            .Which.DisplayName.Should().Be("Phiếu nhập đã lưu");
        invoiceNodes.Should().ContainSingle(x => x.DocumentId == invoiceId)
            .Which.DisplayName.Should().Be("Hóa đơn đầu vào đã lưu");
        exportNodes.Should().ContainSingle(x => x.DocumentId == exportVoucherId)
            .Which.DisplayName.Should().Be("Phiếu xuất đã lưu");
    }

    [Fact]
    public async Task Export_folder_includes_persisted_stock_issues()
    {
        var options = CreateOptions();
        int issueId;
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            var issue = new StockIssue
            {
                IssueNo = "PX-REAL-001",
                IssueDate = new DateTime(2026, 9, 12),
                Description = "Phiếu xuất hàng đã lưu",
                WarehouseId = 1,
                TotalAmount = 375000m,
                IsApproved = true,
                PostingStatus = "Posted"
            };
            setup.StockIssues.Add(issue);
            await setup.SaveChangesAsync();
            issueId = issue.Id;
        }

        var repository = CreateRepository(options);
        var nodes = await repository.GetChildrenAsync(@"This PC\D: ACCOUNTING\Phiếu xuất");
        var searchResults = await repository.SearchAsync("PX-REAL-001");

        nodes.Should().ContainSingle(x => x.Id == $"stock-issue-{issueId}")
            .Which.Should().Match<VirtualExplorerNodeDto>(x =>
                x.Name == "PX-REAL-001"
                && x.DisplayName.Contains("Phiếu xuất hàng đã lưu")
                && x.Status == "Đã ghi sổ"
                && x.Amount == 375000m
                && x.Currency == "VND");
        searchResults.Should().ContainSingle(x => x.Id == $"stock-issue-{issueId}");
    }

    [Fact]
    public async Task Customer_folder_reads_the_live_customer_table()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Customers.Add(new Customer
            {
                CustomerCode = "CUS-REAL-001",
                CustomerName = "Khách hàng đã lưu",
                IsActive = true
            });
            setup.VirtualFolders.Add(new VirtualFolder
            {
                DriveCode = "D",
                Name = "Legacy",
                DisplayName = "Thư mục cũ",
                VirtualPath = @"This PC\D: ACCOUNTING\Khách hàng\Thư mục cũ",
                FolderType = VirtualNodeType.Folder,
                IsSystem = false
            });
            await setup.SaveChangesAsync();
        }

        var repository = CreateRepository(options);
        var nodes = await repository.GetChildrenAsync(@"This PC\D: ACCOUNTING\Khách hàng");

        nodes.Should().ContainSingle(x => x.DisplayName == "Khách hàng đã lưu");
        nodes.Single(x => x.DisplayName == "Khách hàng đã lưu").Name.Should().Be("CUS-REAL-001");

        var searchResults = await repository.SearchAsync("CUS-REAL-001");
        searchResults.Should().ContainSingle(x => x.DisplayName == "Khách hàng đã lưu");
    }

    [Fact]
    public async Task Customer_folder_lists_only_persisted_documents_using_the_customer_code_path()
    {
        var options = CreateOptions();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Customers.Add(new Customer
            {
                CustomerCode = "CUS-REAL-002",
                CustomerName = "Khách hàng có chứng từ",
                IsActive = true
            });
            await setup.SaveChangesAsync();
        }

        var repository = CreateRepository(options);
        var customerRoot = @"This PC\D: ACCOUNTING\Khách hàng";
        var customer = (await repository.GetChildrenAsync(customerRoot))
            .Should().ContainSingle(x => x.DisplayName == "Khách hàng có chứng từ")
            .Which;
        customer.VirtualPath.Should().Be($@"{customerRoot}\CUS-REAL-002");
        customer.HasChildren.Should().BeFalse();

        var documentId = Guid.NewGuid();
        await using (var setup = new AppDbContext(options))
        {
            setup.BusinessDocuments.Add(new BusinessDocument
            {
                Id = documentId,
                Code = "HD-CUS-002",
                Title = "Hóa đơn của khách hàng",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Customer,
                EntityId = customer.EntityId!.Value,
                DocumentType = BusinessDocumentType.Invoice,
                DocumentDate = new DateTime(2026, 9, 1),
                VirtualPath = @"This PC\D: ACCOUNTING\Hóa đơn",
                PhysicalFilePath = @"D:\Data\HD-CUS-002.pdf",
                FileName = "HD-CUS-002.pdf",
                FileExtension = ".pdf",
                Status = DocumentStatus.Active
            });
            await setup.SaveChangesAsync();
        }

        await repository.RefreshAsync();

        var refreshedCustomer = (await repository.GetChildrenAsync(customerRoot))
            .Should().ContainSingle(x => x.DisplayName == "Khách hàng có chứng từ")
            .Which;
        refreshedCustomer.HasChildren.Should().BeTrue();

        var customerDocuments = await repository.GetChildrenAsync(refreshedCustomer.VirtualPath);
        customerDocuments.Should().ContainSingle(x => x.DocumentId == documentId)
            .Which.VirtualPath.Should().Be($@"{refreshedCustomer.VirtualPath}\{documentId:N}.pdf");
    }

    [Fact]
    public async Task Sales_folders_show_persisted_pos_and_restaurant_orders()
    {
        var options = CreateOptions();
        var posOrderId = Guid.NewGuid();
        var restaurantOrderId = Guid.NewGuid();
        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.BreakfastCafeOrders.Add(new BreakfastCafeOrder
            {
                Id = posOrderId,
                OrderNumber = "POS-REAL-001",
                OrderDate = new DateTime(2026, 9, 10),
                Status = "Closed",
                TotalAmount = 180000m
            });
            setup.AlacarteOrders.Add(new AlacarteOrder
            {
                Id = restaurantOrderId,
                OrderNo = "REST-REAL-001",
                OrderDate = new DateTime(2026, 9, 11),
                Status = "Confirmed",
                TotalAmount = 420000m
            });
            await setup.SaveChangesAsync();
        }

        var repository = CreateRepository(options);
        var posNodes = await repository.GetChildrenAsync(@"This PC\E: SALES\POS");
        var allOrderNodes = await repository.GetChildrenAsync(@"This PC\E: SALES\Đơn hàng");
        var searchResults = await repository.SearchAsync("REST-REAL-001");

        posNodes.Should().ContainSingle(x => x.Id == $"pos-order-{posOrderId:N}")
            .Which.Should().Match<VirtualExplorerNodeDto>(x =>
                x.Name == "POS-REAL-001"
                && x.DisplayName.Contains("POS")
                && x.Module == BusinessModule.Sales
                && x.Status == "Closed"
                && x.Amount == 180000m
                && x.Currency == "VND");
        posNodes.Should().NotContain(x => x.Id == $"restaurant-order-{restaurantOrderId:N}");
        allOrderNodes.Should().Contain(x => x.Id == $"pos-order-{posOrderId:N}");
        allOrderNodes.Should().ContainSingle(x => x.Id == $"restaurant-order-{restaurantOrderId:N}")
            .Which.Amount.Should().Be(420000m);
        searchResults.Should().ContainSingle(x => x.Id == $"restaurant-order-{restaurantOrderId:N}");
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"vfc-repository-{Guid.NewGuid():N}")
            .Options;

    private static VirtualExplorerRepository CreateRepository(DbContextOptions<AppDbContext> options)
        => new(new ScopedAppDbContextFactory(options, companyContext: null!), new SettingsRegistry());
}
