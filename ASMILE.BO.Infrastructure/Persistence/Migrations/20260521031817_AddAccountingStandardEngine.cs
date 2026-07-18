using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingStandardEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingStandardTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingStandardTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountLevel = table.Column<int>(type: "int", nullable: false),
                    AccountKind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NormalBalance = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllowPosting = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemAccount = table.Column<bool>(type: "bit", nullable: false),
                    IsFinancialAccount = table.Column<bool>(type: "bit", nullable: false),
                    IsManagementAccount = table.Column<bool>(type: "bit", nullable: false),
                    RequireCustomer = table.Column<bool>(type: "bit", nullable: false),
                    RequireSupplier = table.Column<bool>(type: "bit", nullable: false),
                    RequireWarehouse = table.Column<bool>(type: "bit", nullable: false),
                    RequireItem = table.Column<bool>(type: "bit", nullable: false),
                    RequireDepartment = table.Column<bool>(type: "bit", nullable: false),
                    RequireEmployee = table.Column<bool>(type: "bit", nullable: false),
                    RequireCurrency = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyAccountingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AccountingStandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    FiscalYearStartDay = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAccountingProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialStatementMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LineCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LineName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AccountFilter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccountRoleFilter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Formula = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialStatementMappings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AccountRoleMappings",
                columns: new[] { "Id", "AccountCode", "AccountingStandardCode", "IsActive", "IsDefault", "RoleCode" },
                values: new object[,]
                {
                    { new Guid("084ef9b2-3d28-4053-2334-52af8a409ad3"), "1331", "TT99", true, true, "VATInput" },
                    { new Guid("1261722a-f44e-f1eb-6c9a-fa4ae07f73c7"), "1111", "TT99", true, true, "Cash" },
                    { new Guid("16daf76a-7d75-a8dd-a176-9463adc93891"), "5111", "TT200", true, true, "Revenue" },
                    { new Guid("27e66cfb-8ba2-fbf5-b428-90b89d2e5747"), "1311", "TT200", true, true, "Receivable" },
                    { new Guid("28c9d98c-13a5-310d-7d9e-8d2388cd22a0"), "1121", "TT200", true, true, "Bank" },
                    { new Guid("2cdf8041-0a2b-e65a-5f3c-593773b875d0"), "1111", "TT200", true, true, "Cash" },
                    { new Guid("366959af-6b03-5589-8f76-ec419b0d24ea"), "642", "TT200", true, true, "Expense" },
                    { new Guid("5beefc39-6f8e-ed2e-5e17-d264b80288ac"), "3331", "TT200", true, true, "VATOutput" },
                    { new Guid("6282a244-7706-9065-57a3-58040ccad70d"), "5111", "TT99", true, true, "Revenue" },
                    { new Guid("6e3f936d-006d-0a10-e4bf-548f6b0bb914"), "1121", "TT99", true, true, "Bank" },
                    { new Guid("7d20c5e4-5f3d-15be-6873-11cdb10e09b2"), "214", "TT99", true, true, "Depreciation" },
                    { new Guid("92b1d03e-a22f-907d-9307-84270006ef9a"), "153", "TT200", true, true, "ToolAndSupply" },
                    { new Guid("9a72737d-3b3f-e6d0-172e-ff048181a97a"), "1331", "TT200", true, true, "VATInput" },
                    { new Guid("c0065adc-1d73-5356-d8f3-caf5a5f9d9ce"), "6321", "TT200", true, true, "COGS" },
                    { new Guid("c07f23bf-aa79-ae63-9fca-6ca14eeb1143"), "211", "TT99", true, true, "FixedAsset" },
                    { new Guid("c45d119c-1c87-9a46-9403-fa182f974f68"), "3311", "TT200", true, true, "Payable" },
                    { new Guid("c7364a40-5c3e-1ce1-fb65-ab1000638819"), "3331", "TT99", true, true, "VATOutput" },
                    { new Guid("d9361f9e-d7de-45e1-3e81-175bb0b335a7"), "211", "TT200", true, true, "FixedAsset" },
                    { new Guid("ddc5cd68-8e22-8dda-b942-4496b1aec26d"), "642", "TT99", true, true, "Expense" },
                    { new Guid("e71d40d6-6853-ba29-93c1-703f06b8d231"), "214", "TT200", true, true, "Depreciation" },
                    { new Guid("f4b2d931-85a8-8d06-4cf3-c52ab5d6f126"), "153", "TT99", true, true, "ToolAndSupply" },
                    { new Guid("f954661f-f4fb-dd00-5184-49748e7db9e0"), "1561", "TT200", true, true, "Inventory" }
                });

            migrationBuilder.InsertData(
                table: "AccountTemplates",
                columns: new[] { "Id", "AccountCategory", "AccountCode", "AccountKind", "AccountLevel", "AccountName", "AllowPosting", "IsActive", "IsFinancialAccount", "IsManagementAccount", "IsSystemAccount", "NormalBalance", "ParentCode", "RequireCurrency", "RequireCustomer", "RequireDepartment", "RequireEmployee", "RequireItem", "RequireSupplier", "RequireWarehouse", "SortOrder", "StandardCode" },
                values: new object[,]
                {
                    { new Guid("025508de-311e-2366-3a4b-25f1c05fc28e"), "Asset", "152", "D", 1, "Nguyên liệu, vật liệu", true, true, true, false, true, "Debit", null, false, false, false, false, true, false, true, 90, "TT99" },
                    { new Guid("04ea1f08-03ec-5803-8e41-51dca9a3b1f4"), "Asset", "156", "H", 1, "Hàng hóa", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 110, "TT200" },
                    { new Guid("088e7865-cfc9-6b08-6e30-3ebfed221bf5"), "Asset", "131", "H", 1, "Phải thu khách hàng", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 50, "TT200" },
                    { new Guid("0c852960-1799-22cb-3d10-70cc91932d65"), "Asset", "133", "H", 1, "Thuế GTGT được khấu trừ", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 70, "TT200" },
                    { new Guid("10140228-000b-a5dc-2802-fbf40e5d0c0d"), "Revenue", "511", "H", 1, "Doanh thu bán hàng và cung cấp dịch vụ", false, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 240, "TT200" },
                    { new Guid("17f30f88-b727-097f-4908-7a6296c3350c"), "Revenue", "711", "D", 1, "Thu nhập khác", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 300, "TT99" },
                    { new Guid("183ed488-26ff-532a-fd15-5501096a5cdd"), "Liability", "338", "D", 1, "Phải trả, phải nộp khác", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 210, "TT99" },
                    { new Guid("19afa75d-ee3f-ce4f-b271-c15a154e6444"), "Asset", "153", "D", 1, "Công cụ, dụng cụ", true, true, true, false, true, "Debit", null, false, false, false, false, true, false, true, 100, "TT200" },
                    { new Guid("1fc890e2-bb7c-d661-57b1-2b26ad18a933"), "Liability", "3331", "D", 2, "Thuế GTGT đầu ra", true, true, true, false, true, "Credit", "333", false, false, false, false, false, false, false, 190, "TT99" },
                    { new Guid("24601f3e-1e11-dca9-d483-d54643246bff"), "Asset", "242", "D", 1, "Chi phí trả trước", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 150, "TT200" },
                    { new Guid("284fd7c8-adb3-f313-51ec-db9f468f2a88"), "Liability", "331", "H", 1, "Phải trả nhà cung cấp", false, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 160, "TT200" },
                    { new Guid("29700cc1-b105-96bc-e8a0-4414b23b5d51"), "Asset", "111", "H", 1, "Tiền mặt", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 10, "TT200" },
                    { new Guid("2a067d52-22a5-b564-08b7-85235979a252"), "Expense", "642", "D", 1, "Chi phí quản lý doanh nghiệp", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 290, "TT99" },
                    { new Guid("2bf2e261-d9b9-3232-6df7-2d08c82e3c5d"), "Asset", "1561", "D", 2, "Giá mua hàng hóa", true, true, true, false, true, "Debit", "156", false, false, false, false, true, false, true, 120, "TT99" },
                    { new Guid("2e258141-702b-2748-2a09-89fb0d3f4926"), "Asset", "1331", "D", 2, "Thuế GTGT đầu vào", true, true, true, false, true, "Debit", "133", false, false, false, false, false, false, false, 80, "TT200" },
                    { new Guid("4b856cd9-274b-2340-cac8-97e0c5f9075b"), "Asset", "1311", "D", 2, "Phải thu khách hàng chi tiết", true, true, true, false, true, "Debit", "131", false, true, false, false, false, false, false, 60, "TT200" },
                    { new Guid("56b93dbc-f7fb-2234-5f71-5a1935e1be78"), "Asset", "1111", "D", 2, "Tiền Việt Nam", true, true, true, false, true, "Debit", "111", false, false, false, false, false, false, false, 20, "TT99" },
                    { new Guid("57ff43a6-2100-104c-db83-3a9d510dfca2"), "Asset", "1311", "D", 2, "Phải thu khách hàng chi tiết", true, true, true, false, true, "Debit", "131", false, true, false, false, false, false, false, 60, "TT99" },
                    { new Guid("598eb0f9-2daf-5723-0843-2656bd6bb8cd"), "Expense", "811", "D", 1, "Chi phí khác", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 310, "TT99" },
                    { new Guid("5ba977fb-f630-4c2a-1f95-6e50b1e81b6f"), "Liability", "334", "D", 1, "Phải trả người lao động", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 200, "TT200" },
                    { new Guid("5f658316-534b-1dce-b9fa-30ae0b693d39"), "Asset", "1561", "D", 2, "Giá mua hàng hóa", true, true, true, false, true, "Debit", "156", false, false, false, false, true, false, true, 120, "TT200" },
                    { new Guid("60edee36-2f10-e923-c4d4-220fa9fe1162"), "Asset", "133", "H", 1, "Thuế GTGT được khấu trừ", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 70, "TT99" },
                    { new Guid("6428a67a-c730-f210-9fb7-4e114fe73a56"), "Revenue", "711", "D", 1, "Thu nhập khác", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 300, "TT200" },
                    { new Guid("64b98cbc-dd18-1b65-4d64-b7ba3fe13206"), "Asset", "211", "D", 1, "Tài sản cố định hữu hình", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 130, "TT200" },
                    { new Guid("6745719e-2582-f8db-f594-4207102cfd68"), "Expense", "642", "D", 1, "Chi phí quản lý doanh nghiệp", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 290, "TT200" },
                    { new Guid("6a62361b-dfaa-003a-3797-eccd1967efa9"), "Asset", "211", "D", 1, "Tài sản cố định hữu hình", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 130, "TT99" },
                    { new Guid("6cf34201-ead0-6ea9-d155-836076276cc7"), "Asset", "242", "D", 1, "Chi phí trả trước", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 150, "TT99" },
                    { new Guid("7379f7a5-657d-030a-5caa-fa6f03a7248b"), "Other", "911", "D", 1, "Xác định kết quả kinh doanh", true, true, false, true, true, "Both", null, false, false, false, false, false, false, false, 320, "TT99" },
                    { new Guid("76b26f4a-6113-e4b2-fe6b-2d39ac30c0d7"), "Liability", "333", "H", 1, "Thuế và các khoản phải nộp Nhà nước", false, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 180, "TT99" },
                    { new Guid("77287d40-a16b-c7f2-f1c7-fedef57a5466"), "Revenue", "5111", "D", 2, "Doanh thu bán hàng hóa", true, true, true, false, true, "Credit", "511", false, false, false, false, false, false, false, 250, "TT200" },
                    { new Guid("7b4d454d-3050-993d-d17c-5043e915a16a"), "Expense", "811", "D", 1, "Chi phí khác", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 310, "TT200" },
                    { new Guid("81ca3594-4e8b-47e8-ac11-bd46a2dedbf4"), "Expense", "6321", "D", 2, "Giá vốn hàng hóa", true, true, true, false, true, "Debit", "632", false, false, false, false, false, false, false, 270, "TT99" },
                    { new Guid("84a75d6d-83df-a21e-e7b2-8f4b78e85267"), "Equity", "411", "D", 1, "Vốn góp của chủ sở hữu", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 220, "TT200" },
                    { new Guid("8688b1f2-28b3-6ad0-75ec-eb3abd093655"), "Liability", "3331", "D", 2, "Thuế GTGT đầu ra", true, true, true, false, true, "Credit", "333", false, false, false, false, false, false, false, 190, "TT200" },
                    { new Guid("8a6562f9-2370-e388-3960-c8fc039c226b"), "Asset", "214", "D", 1, "Hao mòn tài sản cố định", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 140, "TT200" },
                    { new Guid("8d0ca8f7-a446-5995-5ae8-1292372bd232"), "Revenue", "511", "H", 1, "Doanh thu bán hàng và cung cấp dịch vụ", false, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 240, "TT99" },
                    { new Guid("8d486cd7-a2be-3ed6-c2ec-9899f035fc8a"), "Asset", "156", "H", 1, "Hàng hóa", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 110, "TT99" },
                    { new Guid("928bb1a2-ad74-159e-5f32-46f41acc2c82"), "Liability", "3311", "D", 2, "Phải trả nhà cung cấp chi tiết", true, true, true, false, true, "Credit", "331", false, false, false, false, false, true, false, 170, "TT99" },
                    { new Guid("98355a78-7e61-9d0b-8d01-aec60bbd27c1"), "Equity", "411", "D", 1, "Vốn góp của chủ sở hữu", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 220, "TT99" },
                    { new Guid("988a0a84-a6be-7e48-898f-672de0e7e3fe"), "Other", "911", "D", 1, "Xác định kết quả kinh doanh", true, true, false, true, true, "Both", null, false, false, false, false, false, false, false, 320, "TT200" },
                    { new Guid("9ba9a426-8856-7471-49fb-0fa286c0f46e"), "Asset", "214", "D", 1, "Hao mòn tài sản cố định", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 140, "TT99" },
                    { new Guid("9c7d8add-aa2e-66ea-23f4-0e38322f0e22"), "Expense", "632", "H", 1, "Giá vốn hàng bán", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 260, "TT99" },
                    { new Guid("9fd87791-3bc2-908b-0f62-8ea4430360a9"), "Expense", "641", "D", 1, "Chi phí bán hàng", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 280, "TT200" },
                    { new Guid("aa953e33-968a-411a-4607-0dc2dbc08977"), "Liability", "3311", "D", 2, "Phải trả nhà cung cấp chi tiết", true, true, true, false, true, "Credit", "331", false, false, false, false, false, true, false, 170, "TT200" },
                    { new Guid("ad132247-9cdc-40f3-cc4d-0ff60395df41"), "Asset", "1331", "D", 2, "Thuế GTGT đầu vào", true, true, true, false, true, "Debit", "133", false, false, false, false, false, false, false, 80, "TT99" },
                    { new Guid("b23752de-4c21-28f5-dcd4-a1dea936ee5b"), "Asset", "153", "D", 1, "Công cụ, dụng cụ", true, true, true, false, true, "Debit", null, false, false, false, false, true, false, true, 100, "TT99" },
                    { new Guid("b4c397e6-0b94-e22c-9278-b72a7da3cd45"), "Asset", "1121", "D", 2, "Tiền Việt Nam gửi ngân hàng", true, true, true, false, true, "Debit", "112", false, false, false, false, false, false, false, 40, "TT200" },
                    { new Guid("bacde5d0-d1ed-9aa5-b351-805e5ae8ef59"), "Expense", "641", "D", 1, "Chi phí bán hàng", true, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 280, "TT99" },
                    { new Guid("bcfb23b9-32ed-f6b2-27a6-9dfae33faa4d"), "Asset", "1111", "D", 2, "Tiền Việt Nam", true, true, true, false, true, "Debit", "111", false, false, false, false, false, false, false, 20, "TT200" },
                    { new Guid("bd7fb5ae-ccdf-2936-9967-ebe55ac6e1e1"), "Expense", "632", "H", 1, "Giá vốn hàng bán", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 260, "TT200" },
                    { new Guid("c065d0fa-023d-b16a-4fb0-5271e5148f4a"), "Revenue", "5111", "D", 2, "Doanh thu bán hàng hóa", true, true, true, false, true, "Credit", "511", false, false, false, false, false, false, false, 250, "TT99" },
                    { new Guid("c200f46d-d7f5-1ecb-a0cb-c93a3098e592"), "Asset", "112", "H", 1, "Tiền gửi ngân hàng", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 30, "TT200" },
                    { new Guid("c5891608-7f1f-7f34-0009-362fb8b789a4"), "Liability", "331", "H", 1, "Phải trả nhà cung cấp", false, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 160, "TT99" },
                    { new Guid("c6305502-1af6-c027-e1ee-4279f9d38ae4"), "Asset", "152", "D", 1, "Nguyên liệu, vật liệu", true, true, true, false, true, "Debit", null, false, false, false, false, true, false, true, 90, "TT200" },
                    { new Guid("d0705825-f9fc-435f-5ae9-ed5f6bc34299"), "Asset", "112", "H", 1, "Tiền gửi ngân hàng", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 30, "TT99" },
                    { new Guid("d46ab24e-4675-33bd-a668-0ed75a7d769b"), "Equity", "421", "D", 1, "Lợi nhuận sau thuế chưa phân phối", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 230, "TT99" },
                    { new Guid("d559b674-eb66-7f73-688b-40c9c9f19576"), "Asset", "1121", "D", 2, "Tiền Việt Nam gửi ngân hàng", true, true, true, false, true, "Debit", "112", false, false, false, false, false, false, false, 40, "TT99" },
                    { new Guid("d8b6a044-5ac0-7b7b-9c1d-b7a57b57def5"), "Expense", "6321", "D", 2, "Giá vốn hàng hóa", true, true, true, false, true, "Debit", "632", false, false, false, false, false, false, false, 270, "TT200" },
                    { new Guid("dac90964-ec7f-11c1-a01e-ba10703bb21a"), "Liability", "334", "D", 1, "Phải trả người lao động", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 200, "TT99" },
                    { new Guid("dcca2214-0d4b-dc61-4823-acad129f8c9a"), "Equity", "421", "D", 1, "Lợi nhuận sau thuế chưa phân phối", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 230, "TT200" },
                    { new Guid("ea2d937e-3992-2636-729c-8694b562d023"), "Asset", "111", "H", 1, "Tiền mặt", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 10, "TT99" },
                    { new Guid("ebb3ec86-3f85-d42a-d7dd-0afadbb5a708"), "Liability", "333", "H", 1, "Thuế và các khoản phải nộp Nhà nước", false, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 180, "TT200" },
                    { new Guid("f0edc9bc-c303-4758-d9e5-ecd06ddbf5b8"), "Liability", "338", "D", 1, "Phải trả, phải nộp khác", true, true, true, false, true, "Credit", null, false, false, false, false, false, false, false, 210, "TT200" },
                    { new Guid("f6acf7bf-51a1-2a36-2eb3-d7e83b1fa71d"), "Asset", "131", "H", 1, "Phải thu khách hàng", false, true, true, false, true, "Debit", null, false, false, false, false, false, false, false, 50, "TT99" }
                });

            migrationBuilder.InsertData(
                table: "AccountingStandardTemplates",
                columns: new[] { "Id", "CreatedAt", "DefinitionJson", "IsActive", "IsSystem", "Name", "StandardCode", "TemplateCode", "TemplateType", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("1386fead-9944-45b8-7d8e-7cf4f84ebacf"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"CUSTOM\",\"type\":\"Account\",\"version\":1}", true, true, "Mẫu hệ thống tài khoản", "CUSTOM", "CUSTOM_ACCOUNTS", "Account", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("26c6b1bc-febb-8b6a-f14c-7eb8884c19fc"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT200\",\"type\":\"ReportMapping\",\"version\":1}", true, true, "Mẫu mapping báo cáo tài chính", "TT200", "TT200_REPORT_MAPPING", "ReportMapping", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2a77e81e-3caa-9dbf-d890-172559873b7d"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"CUSTOM\",\"type\":\"ReportMapping\",\"version\":1}", true, true, "Mẫu mapping báo cáo tài chính", "CUSTOM", "CUSTOM_REPORT_MAPPING", "ReportMapping", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2d505991-250c-5966-7f53-a8abc157cd31"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT99\",\"type\":\"Account\",\"version\":1}", true, true, "Mẫu hệ thống tài khoản", "TT99", "TT99_ACCOUNTS", "Account", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33f6661c-28e8-3228-9d2b-16940bbd7df0"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT200\",\"type\":\"PostingRule\",\"version\":1}", true, true, "Mẫu quy tắc hạch toán", "TT200", "TT200_POSTING_RULE", "PostingRule", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("661336a6-bfd3-3945-7e79-172502a03112"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT99\",\"type\":\"PostingRule\",\"version\":1}", true, true, "Mẫu quy tắc hạch toán", "TT99", "TT99_POSTING_RULE", "PostingRule", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("8225f128-462f-00f1-e610-101995fcae16"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT200\",\"type\":\"Account\",\"version\":1}", true, true, "Mẫu hệ thống tài khoản", "TT200", "TT200_ACCOUNTS", "Account", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("9c9a7c37-1237-1a8d-9998-f6107adf494a"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT99\",\"type\":\"RoleMapping\",\"version\":1}", true, true, "Mẫu mapping vai trò tài khoản", "TT99", "TT99_ROLE_MAPPING", "RoleMapping", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("d45c163e-bdcc-bdae-fc81-9d63cbed6fe8"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT99\",\"type\":\"ReportMapping\",\"version\":1}", true, true, "Mẫu mapping báo cáo tài chính", "TT99", "TT99_REPORT_MAPPING", "ReportMapping", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("dcb9b376-d9fc-eabb-a825-87e097c541d7"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"CUSTOM\",\"type\":\"PostingRule\",\"version\":1}", true, true, "Mẫu quy tắc hạch toán", "CUSTOM", "CUSTOM_POSTING_RULE", "PostingRule", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("e1b2b3d5-45b5-bb13-2930-668ac84fe4f0"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"TT200\",\"type\":\"RoleMapping\",\"version\":1}", true, true, "Mẫu mapping vai trò tài khoản", "TT200", "TT200_ROLE_MAPPING", "RoleMapping", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0d3245d-7789-1301-58e4-98a3ec36f4e8"), new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "{\"standardCode\":\"CUSTOM\",\"type\":\"RoleMapping\",\"version\":1}", true, true, "Mẫu mapping vai trò tài khoản", "CUSTOM", "CUSTOM_ROLE_MAPPING", "RoleMapping", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "CompanyAccountingProfiles",
                columns: new[] { "Id", "AccountingStandardCode", "CompanyId", "CompanyName", "CreatedAt", "EffectiveFrom", "FiscalYearStartDay", "FiscalYearStartMonth", "IsActive", "UpdatedAt" },
                values: new object[] { new Guid("9b2c65a2-1a31-4b6b-924c-000000000001"), "TT99", null, "Default Company", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, true, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "FinancialStatementMappings",
                columns: new[] { "Id", "AccountFilter", "AccountRoleFilter", "Formula", "IsActive", "LineCode", "LineName", "ReportCode", "SortOrder", "StandardCode" },
                values: new object[,]
                {
                    { new Guid("0182d2aa-0a17-791a-cb65-a65e5effab7a"), null, null, "GROSS_PROFIT - SELLING_EXPENSE - ADMIN_EXPENSE + OTHER_INCOME - OTHER_EXPENSE", true, "PROFIT_BEFORE_TAX", "Lợi nhuận trước thuế", "INCOME_STATEMENT", 80, "TT99" },
                    { new Guid("1009532d-729d-8d6e-4f9a-87f4bb281c74"), "111*,112*", "Cash,Bank", null, true, "CASH_AND_CASH_EQUIVALENTS", "Tiền và tương đương tiền", "BALANCE_SHEET", 10, "TT200" },
                    { new Guid("15c78b04-6e79-d552-26a4-e73c1cf809dc"), "152*,153*,156*", "Inventory,ToolAndSupply", null, true, "INVENTORY", "Hàng tồn kho", "BALANCE_SHEET", 30, "TT200" },
                    { new Guid("1838b8af-4691-772a-eca2-4397a2d490c1"), null, null, "REVENUE - COGS", true, "GROSS_PROFIT", "Lợi nhuận gộp", "INCOME_STATEMENT", 30, "TT200" },
                    { new Guid("21246897-c179-8a44-e21f-47b6ffdd43e5"), "711*", null, null, true, "OTHER_INCOME", "Thu nhập khác", "INCOME_STATEMENT", 60, "TT99" },
                    { new Guid("292432f7-5788-bec1-ce3f-cc814025844d"), "711*", null, null, true, "OTHER_INCOME", "Thu nhập khác", "INCOME_STATEMENT", 60, "TT200" },
                    { new Guid("37e6948c-6831-61c8-e055-7e3af262d316"), "811*", null, null, true, "OTHER_EXPENSE", "Chi phí khác", "INCOME_STATEMENT", 70, "TT200" },
                    { new Guid("47ed997b-5f57-b22d-5b54-8309c419f29a"), "152*,153*,156*", "Inventory,ToolAndSupply", null, true, "INVENTORY", "Hàng tồn kho", "BALANCE_SHEET", 30, "TT99" },
                    { new Guid("5076eb2e-81f8-607a-5b50-5b8c1b736d2e"), null, null, "REVENUE - COGS", true, "GROSS_PROFIT", "Lợi nhuận gộp", "INCOME_STATEMENT", 30, "TT99" },
                    { new Guid("52fadcac-2053-01a8-eaf9-9ba6d77714b6"), "511*", "Revenue", null, true, "REVENUE", "Doanh thu", "INCOME_STATEMENT", 10, "TT99" },
                    { new Guid("576f7773-3663-768b-d830-d8daf2730795"), "331*", "Payable", null, true, "PAYABLES", "Phải trả nhà cung cấp", "BALANCE_SHEET", 50, "TT99" },
                    { new Guid("5f7d1620-a49e-71c0-971f-877e63950596"), "331*", "Payable", null, true, "PAYABLES", "Phải trả nhà cung cấp", "BALANCE_SHEET", 50, "TT200" },
                    { new Guid("67a4801b-73cb-bd68-0f70-38712b9569d8"), "641*", null, null, true, "SELLING_EXPENSE", "Chi phí bán hàng", "INCOME_STATEMENT", 40, "TT200" },
                    { new Guid("681a72d7-516b-6fda-a9aa-faa59cc0bc4e"), "211*,214*", "FixedAsset,Depreciation", null, true, "FIXED_ASSETS", "Tài sản cố định", "BALANCE_SHEET", 40, "TT200" },
                    { new Guid("87bee6eb-e9c5-98e0-e3c3-9580228dd031"), "811*", null, null, true, "OTHER_EXPENSE", "Chi phí khác", "INCOME_STATEMENT", 70, "TT99" },
                    { new Guid("8b9d58dd-6cac-a0a1-6911-c53349006327"), "333*", "VATOutput", null, true, "TAX_PAYABLES", "Thuế và các khoản phải nộp", "BALANCE_SHEET", 60, "TT99" },
                    { new Guid("8e4e72ce-02d5-6fa7-0940-b8272443cc54"), "632*", "COGS", null, true, "COGS", "Giá vốn hàng bán", "INCOME_STATEMENT", 20, "TT200" },
                    { new Guid("95543140-a7a5-97a2-aa3a-0fb8617e029e"), "511*", "Revenue", null, true, "REVENUE", "Doanh thu", "INCOME_STATEMENT", 10, "TT200" },
                    { new Guid("964494b2-cedd-7c14-8d9e-5b6c74803097"), "411*,421*", null, null, true, "EQUITY", "Vốn chủ sở hữu", "BALANCE_SHEET", 70, "TT200" },
                    { new Guid("9bf102f0-400d-d6dd-61de-802b0559b105"), "642*", "Expense", null, true, "ADMIN_EXPENSE", "Chi phí quản lý doanh nghiệp", "INCOME_STATEMENT", 50, "TT99" },
                    { new Guid("a8c61726-fe70-dfbf-a3ca-ea80cf36f2d4"), "211*,214*", "FixedAsset,Depreciation", null, true, "FIXED_ASSETS", "Tài sản cố định", "BALANCE_SHEET", 40, "TT99" },
                    { new Guid("aed70647-f9d1-5a24-72f0-f9d955f00538"), null, null, "GROSS_PROFIT - SELLING_EXPENSE - ADMIN_EXPENSE + OTHER_INCOME - OTHER_EXPENSE", true, "PROFIT_BEFORE_TAX", "Lợi nhuận trước thuế", "INCOME_STATEMENT", 80, "TT200" },
                    { new Guid("b7188cbd-d96b-030b-ae96-22ead1485ce9"), "333*", "VATOutput", null, true, "TAX_PAYABLES", "Thuế và các khoản phải nộp", "BALANCE_SHEET", 60, "TT200" },
                    { new Guid("c7f9d121-abca-c78a-23fd-441cd5af9d6e"), "642*", "Expense", null, true, "ADMIN_EXPENSE", "Chi phí quản lý doanh nghiệp", "INCOME_STATEMENT", 50, "TT200" },
                    { new Guid("d16f401a-9c3e-60b9-5e93-8ec4b1f838ac"), "111*,112*", "Cash,Bank", null, true, "CASH_AND_CASH_EQUIVALENTS", "Tiền và tương đương tiền", "BALANCE_SHEET", 10, "TT99" },
                    { new Guid("d3228ff9-4a20-e563-3fef-cd5472f0829f"), "632*", "COGS", null, true, "COGS", "Giá vốn hàng bán", "INCOME_STATEMENT", 20, "TT99" },
                    { new Guid("d8f4f33e-6371-14fb-d0c2-1529ea42943a"), "131*", "Receivable", null, true, "RECEIVABLES", "Phải thu khách hàng", "BALANCE_SHEET", 20, "TT99" },
                    { new Guid("dc32a6b1-e3d6-025d-c7da-e53e70974dd3"), "131*", "Receivable", null, true, "RECEIVABLES", "Phải thu khách hàng", "BALANCE_SHEET", 20, "TT200" },
                    { new Guid("df4ca685-8a95-fa2e-bc7e-499eddc58e9b"), "411*,421*", null, null, true, "EQUITY", "Vốn chủ sở hữu", "BALANCE_SHEET", 70, "TT99" },
                    { new Guid("e9ee0684-7722-ab0b-637c-08f890529513"), "641*", null, null, true, "SELLING_EXPENSE", "Chi phí bán hàng", "INCOME_STATEMENT", 40, "TT99" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingStandardTemplates_StandardCode",
                table: "AccountingStandardTemplates",
                column: "StandardCode");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingStandardTemplates_TemplateCode",
                table: "AccountingStandardTemplates",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingStandardTemplates_TemplateType",
                table: "AccountingStandardTemplates",
                column: "TemplateType");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTemplates_StandardCode",
                table: "AccountTemplates",
                column: "StandardCode");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTemplates_StandardCode_AccountCode",
                table: "AccountTemplates",
                columns: new[] { "StandardCode", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccountingProfiles_AccountingStandardCode",
                table: "CompanyAccountingProfiles",
                column: "AccountingStandardCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAccountingProfiles_CompanyId",
                table: "CompanyAccountingProfiles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementMappings_LineCode",
                table: "FinancialStatementMappings",
                column: "LineCode");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementMappings_ReportCode",
                table: "FinancialStatementMappings",
                column: "ReportCode");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementMappings_StandardCode",
                table: "FinancialStatementMappings",
                column: "StandardCode");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementMappings_StandardCode_ReportCode_LineCode",
                table: "FinancialStatementMappings",
                columns: new[] { "StandardCode", "ReportCode", "LineCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingStandardTemplates");

            migrationBuilder.DropTable(
                name: "AccountTemplates");

            migrationBuilder.DropTable(
                name: "CompanyAccountingProfiles");

            migrationBuilder.DropTable(
                name: "FinancialStatementMappings");

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("084ef9b2-3d28-4053-2334-52af8a409ad3"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("1261722a-f44e-f1eb-6c9a-fa4ae07f73c7"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("16daf76a-7d75-a8dd-a176-9463adc93891"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("27e66cfb-8ba2-fbf5-b428-90b89d2e5747"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("28c9d98c-13a5-310d-7d9e-8d2388cd22a0"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("2cdf8041-0a2b-e65a-5f3c-593773b875d0"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("366959af-6b03-5589-8f76-ec419b0d24ea"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("5beefc39-6f8e-ed2e-5e17-d264b80288ac"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6282a244-7706-9065-57a3-58040ccad70d"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6e3f936d-006d-0a10-e4bf-548f6b0bb914"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("7d20c5e4-5f3d-15be-6873-11cdb10e09b2"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("92b1d03e-a22f-907d-9307-84270006ef9a"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("9a72737d-3b3f-e6d0-172e-ff048181a97a"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("c0065adc-1d73-5356-d8f3-caf5a5f9d9ce"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("c07f23bf-aa79-ae63-9fca-6ca14eeb1143"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("c45d119c-1c87-9a46-9403-fa182f974f68"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("c7364a40-5c3e-1ce1-fb65-ab1000638819"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("d9361f9e-d7de-45e1-3e81-175bb0b335a7"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("ddc5cd68-8e22-8dda-b942-4496b1aec26d"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("e71d40d6-6853-ba29-93c1-703f06b8d231"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("f4b2d931-85a8-8d06-4cf3-c52ab5d6f126"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("f954661f-f4fb-dd00-5184-49748e7db9e0"));
        }
    }
}
