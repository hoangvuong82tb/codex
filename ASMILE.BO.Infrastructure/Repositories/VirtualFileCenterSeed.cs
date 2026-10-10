using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Infrastructure.Repositories;

internal static class VirtualFileCenterSeed
{
    internal sealed record SupplierSeed(Guid Id, string Code, string Name);

    internal sealed record FolderSeed(
        string DriveCode,
        string Name,
        string DisplayName,
        string VirtualPath,
        VirtualNodeType FolderType,
        int SortOrder,
        BusinessEntityType? EntityType = null);

    public static Guid StableGuid(string seed)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes);
    }

    /// <summary>
    /// System VFC folders are persisted catalog data, not a second source of
    /// runtime documents.  Keeping the catalog here lets startup materialize
    /// the same folders on every deployment while user-created rows remain
    /// untouched.
    /// </summary>
    internal static IReadOnlyList<FolderSeed> DefaultFolders { get; } =
    [
        new("C", "Database Schemas", "Database Schemas", @"This PC\C: SYSTEM\Database Schemas", VirtualNodeType.SystemTool, 10),
        new("C", "Tables", "Tables", @"This PC\C: SYSTEM\Tables", VirtualNodeType.SystemTool, 20),
        new("C", "Views", "Views", @"This PC\C: SYSTEM\Views", VirtualNodeType.SystemTool, 30),
        new("C", "Stored Procedures", "Stored Procedures", @"This PC\C: SYSTEM\Stored Procedures", VirtualNodeType.SystemTool, 40),
        new("C", "Backup / Restore", "Backup / Restore", @"This PC\C: SYSTEM\Backup / Restore", VirtualNodeType.SystemTool, 50),
        new("C", "Users & Roles", "Users & Roles", @"This PC\C: SYSTEM\Users & Roles", VirtualNodeType.SystemTool, 60),
        new("C", "System Settings", "System Settings", @"This PC\C: SYSTEM\System Settings", VirtualNodeType.SystemTool, 70),

        new("D", "Nhà cung cấp", "Nhà cung cấp", @"This PC\D: ACCOUNTING\Nhà cung cấp", VirtualNodeType.EntityFolder, 10, BusinessEntityType.Supplier),
        new("D", "Khách hàng", "Khách hàng", @"This PC\D: ACCOUNTING\Khách hàng", VirtualNodeType.EntityFolder, 20, BusinessEntityType.Customer),
        new("D", "Hóa đơn", "Hóa đơn", @"This PC\D: ACCOUNTING\Hóa đơn", VirtualNodeType.Folder, 30),
        new("D", "Phiếu nhập", "Phiếu nhập", @"This PC\D: ACCOUNTING\Phiếu nhập", VirtualNodeType.Folder, 40),
        new("D", "Phiếu xuất", "Phiếu xuất", @"This PC\D: ACCOUNTING\Phiếu xuất", VirtualNodeType.Folder, 50),
        new("D", "Công nợ", "Công nợ", @"This PC\D: ACCOUNTING\Công nợ", VirtualNodeType.Folder, 60),
        new("D", "Báo cáo kế toán", "Báo cáo kế toán", @"This PC\D: ACCOUNTING\Báo cáo kế toán", VirtualNodeType.Report, 70),

        new("E", "FO", "FO", @"This PC\E: SALES\FO", VirtualNodeType.Folder, 10),
        new("E", "POS", "POS", @"This PC\E: SALES\POS", VirtualNodeType.Folder, 20),
        new("E", "Đơn hàng", "Đơn hàng", @"This PC\E: SALES\Đơn hàng", VirtualNodeType.Folder, 30),
        new("E", "Ca bán hàng", "Ca bán hàng", @"This PC\E: SALES\Ca bán hàng", VirtualNodeType.Folder, 40),
        new("E", "Doanh thu", "Doanh thu", @"This PC\E: SALES\Doanh thu", VirtualNodeType.Report, 50),

        new("F", "Nhân sự", "Nhân sự", @"This PC\F: HR\Nhân sự", VirtualNodeType.Folder, 10),
        new("F", "Chấm công", "Chấm công", @"This PC\F: HR\Chấm công", VirtualNodeType.Folder, 20),
        new("F", "Lương", "Lương", @"This PC\F: HR\Lương", VirtualNodeType.Folder, 30),
        new("F", "Phân ca", "Phân ca", @"This PC\F: HR\Phân ca", VirtualNodeType.Folder, 40),

        new("G", "Audit Logs", "Audit Logs", @"This PC\G: LOGS\Audit Logs", VirtualNodeType.Folder, 10),
        new("G", "Error Logs", "Error Logs", @"This PC\G: LOGS\Error Logs", VirtualNodeType.Folder, 20),
        new("G", "Sync Logs", "Sync Logs", @"This PC\G: LOGS\Sync Logs", VirtualNodeType.Folder, 30),
        new("G", "Login Logs", "Login Logs", @"This PC\G: LOGS\Login Logs", VirtualNodeType.Folder, 40),
        new("G", "Backup Logs", "Backup Logs", @"This PC\G: LOGS\Backup Logs", VirtualNodeType.Folder, 50)
    ];

    internal static IReadOnlyList<VirtualFolder> CreateDefaultFolders(DateTime nowUtc)
    {
        var folders = new List<VirtualFolder>(DefaultFolders.Count);
        var idsByPath = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in DefaultFolders)
        {
            var id = StableGuid($"vfc-folder:{seed.VirtualPath}");
            var parentPath = seed.VirtualPath[..seed.VirtualPath.LastIndexOf('\\')];
            Guid? parentId = idsByPath.TryGetValue(parentPath, out var parent) ? parent : null;
            folders.Add(new VirtualFolder
            {
                Id = id,
                DriveCode = seed.DriveCode,
                ParentId = parentId,
                Name = seed.Name,
                DisplayName = seed.DisplayName,
                VirtualPath = seed.VirtualPath,
                FolderType = seed.FolderType,
                EntityType = seed.EntityType,
                SortOrder = seed.SortOrder,
                IsSystem = true,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            });
            idsByPath[seed.VirtualPath] = id;
        }

        return folders;
    }
}
