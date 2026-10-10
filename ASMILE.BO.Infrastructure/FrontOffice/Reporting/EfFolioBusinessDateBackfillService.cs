using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Reporting;

/// <summary>
/// Controlled repair workflow for legacy Folio entries without BusinessDate.
/// This is deliberately separate from reporting queries so a report can never
/// mutate accounting history as a side effect of being opened or exported.
/// </summary>
public sealed class EfFolioBusinessDateBackfillService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor context,
    IAuthorizationService authorization,
    ICurrentUserService currentUser,
    IAuditLogService auditLog,
    IUnitOfWork unitOfWork) : IFolioBusinessDateBackfillService
{
    private const int MaxItems = 2_000;

    public async Task<Result<FolioBusinessDateBackfillResult>> ApplyAsync(FolioBusinessDateBackfillCommand command, CancellationToken ct)
    {
        if (!authorization.HasPermission("reports.backfill"))
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Authorization("REPORT_BACKFILL_PERMISSION_DENIED", "Bạn không có quyền backfill ngày hạch toán Folio."));

        var current = context.Current;
        if (current is null)
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Authorization("REPORT_CONTEXT_REQUIRED", "Chưa xác định cơ sở đang thao tác."));
        if (command.PropertyId == Guid.Empty || current.Property.PropertyId.Value != command.PropertyId)
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Authorization("REPORT_PROPERTY_SCOPE_MISMATCH", "Dữ liệu không thuộc cơ sở đang thao tác."));
        if (command.Items is null || command.Items.Count == 0)
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Validation("REPORT_BACKFILL_ITEMS_REQUIRED", "Phải chọn ít nhất một dòng Folio để backfill."));
        if (command.Items.Count > MaxItems)
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Validation("REPORT_BACKFILL_LIMIT_EXCEEDED", $"Mỗi lần chỉ được backfill tối đa {MaxItems:N0} dòng."));
        if (string.IsNullOrWhiteSpace(command.ApprovalReason) || command.ApprovalReason.Trim().Length < 5)
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Validation("REPORT_BACKFILL_REASON_REQUIRED", "Phải nhập lý do phê duyệt backfill."));
        if (string.IsNullOrWhiteSpace(command.ApprovalReference) || command.ApprovalReference.Trim().Length < 3)
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Validation("REPORT_BACKFILL_REFERENCE_REQUIRED", "Phải nhập số tham chiếu phê duyệt."));

        var duplicateIds = command.Items
            .GroupBy(x => x.EntryId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateIds.Length > 0 || command.Items.Any(x => x.EntryId == Guid.Empty || x.BusinessDate == default))
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Validation("REPORT_BACKFILL_ITEMS_INVALID", "Danh sách dòng Folio hoặc ngày hạch toán không hợp lệ."));

        var itemMap = command.Items.ToDictionary(x => x.EntryId, x => x.BusinessDate);
        var outcome = new FolioBusinessDateBackfillResult(0, 0);

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
            {
                var entries = await db.HotelFolioEntries
                    .Where(x => x.PropertyId == command.PropertyId && itemMap.Keys.Contains(x.HotelFolioEntryId))
                    .ToListAsync(transactionCt);

                if (entries.Count != itemMap.Count)
                    throw new BackfillValidationException(Error.Validation("REPORT_BACKFILL_ENTRY_NOT_FOUND", "Một hoặc nhiều dòng Folio không tồn tại hoặc không thuộc cơ sở đang thao tác."));

                var alreadyApplied = 0;
                var updated = 0;
                var changes = new List<string>(entries.Count);
                foreach (var entry in entries.OrderBy(x => x.HotelFolioEntryId))
                {
                    var requestedDate = itemMap[entry.HotelFolioEntryId];
                    if (entry.BusinessDate.HasValue)
                    {
                        if (entry.BusinessDate.Value != requestedDate)
                            throw new BackfillValidationException(Error.Concurrency("REPORT_BACKFILL_DATE_CONFLICT", "Một dòng Folio đã được backfill sang ngày khác; hãy tải lại danh sách trước khi tiếp tục."));
                        alreadyApplied++;
                        continue;
                    }

                    entry.BackfillBusinessDate(requestedDate);
                    updated++;
                    changes.Add($"{entry.HotelFolioEntryId:N}:{requestedDate:yyyy-MM-dd}");
                }

                if (updated > 0)
                {
                    await db.SaveChangesAsync(transactionCt);
                    var username = currentUser.CurrentUser?.UserName;
                    var detail = $"property={command.PropertyId:N};updated={updated};alreadyApplied={alreadyApplied};approvalReference={command.ApprovalReference.Trim()};reason={command.ApprovalReason.Trim()};changes={string.Join(',', changes)}";
                    await auditLog.LogAsync("FOLIO_BUSINESS_DATE_BACKFILL", detail, string.IsNullOrWhiteSpace(username) ? "system" : username, transactionCt);
                }

                outcome = new FolioBusinessDateBackfillResult(updated, alreadyApplied);
            }, ct);
        }
        catch (BackfillValidationException ex)
        {
            return Result<FolioBusinessDateBackfillResult>.Failure(ex.Error);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Concurrency("REPORT_BACKFILL_CONCURRENCY", "Dữ liệu đã thay đổi trong lúc phê duyệt; hãy tải lại danh sách và thử lại."));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result<FolioBusinessDateBackfillResult>.Failure(Error.Validation("REPORT_BACKFILL_INVALID", ex.Message));
        }

        return Result<FolioBusinessDateBackfillResult>.Success(outcome);
    }

    private sealed class BackfillValidationException(Error error) : Exception(error.Message)
    {
        public Error Error { get; } = error;
    }
}
