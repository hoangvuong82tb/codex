using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// Persists the workforce metadata edited by the directory. The command is
/// deliberately keyed by the canonical person code used by the UI.
/// </summary>
public sealed class EfWorkforceCommandService(AppDbContext db) : IWorkforceCommandService
{
    public async Task ChangeShiftAsync(WorkforceShiftChange request, CancellationToken cancellationToken = default)
    {
        var person = await GetPersonAsync(request.StaffCode, cancellationToken);
        var shift = request.Shift.Trim();
        if (shift is not ("Sáng" or "Chiều" or "Đêm" or "Chưa phân ca"))
            throw new InvalidOperationException("Ca làm việc không hợp lệ.");

        person.WorkforceShift = shift;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeAreaAsync(WorkforceAreaChange request, CancellationToken cancellationToken = default)
    {
        var person = await GetPersonAsync(request.StaffCode, cancellationToken);
        var building = request.Building.Trim();
        var floor = request.Floor.Trim();
        if (string.IsNullOrWhiteSpace(building) || string.IsNullOrWhiteSpace(floor))
            throw new InvalidOperationException("Tòa nhà và tầng là bắt buộc.");

        person.WorkforceBuilding = building;
        person.WorkforceFloor = floor;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ASMILE.BO.Core.Entities.Person> GetPersonAsync(string staffCode, CancellationToken cancellationToken)
    {
        var normalized = staffCode.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Mã nhân sự là bắt buộc.");

        return await db.People.SingleOrDefaultAsync(
                   person => person.PersonCode == normalized && person.IsActive,
                   cancellationToken)
               ?? throw new InvalidOperationException("Không tìm thấy nhân sự đang hoạt động.");
    }
}
