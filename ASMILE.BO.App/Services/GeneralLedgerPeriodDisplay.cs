using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using System.Globalization;

namespace ASMILE.BO.App.Services;

public sealed record GeneralLedgerPeriodDisplay(
    string PeriodCode,
    string FromDate,
    string ToDate,
    string Status,
    string LastLockedAt,
    string LockedBy)
{
    public static GeneralLedgerPeriodDisplay From(AccountingPeriod? period)
    {
        if (period is null)
        {
            return new(
                "Chưa cấu hình kỳ kế toán hiện tại",
                "Chưa có dữ liệu",
                "Chưa có dữ liệu",
                "Chưa mở kỳ",
                "Chưa có dữ liệu",
                "-");
        }

        return new(
            period.PeriodCode.Replace("-", "/", StringComparison.Ordinal),
            period.FromDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            period.ToDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            period.Status switch
            {
                AccountingPeriodStatus.Locked => "Đã khóa",
                AccountingPeriodStatus.Closed => "Đã đóng",
                _ => "Đang mở"
            },
            period.LockedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) ?? "Chưa khóa sổ",
            period.LockedBy ?? "-");
    }
}
