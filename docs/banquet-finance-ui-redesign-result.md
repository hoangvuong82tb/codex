# Ket qua thiet ke lai tab Tai chinh Banquet

## 1. Van de giao dien cu

- Tab la mot form doc dai, thieu tong quan va phan cap thong tin.
- Khong tach gia tri thuc don, dich vu, VAT, tien coc va so con phai thu.
- Lich su coc thieu cot, khong co empty state va dinh dang tien/ngay chua phu hop.
- Form ghi coc thieu label, validation tai cho, goi y so tien va khoa chong bam lap.

## 2. Thiet ke moi

- Hang dau gom bon KPI: tong gia tri, da dat coc, con phai thu va trang thai.
- Cot trai gom chi tiet cau thanh chi phi va lich su dat coc.
- Cot phai gom trang thai thanh toan, progress va form ghi nhan coc.
- Form co goi y 30%, 50%, thanh toan phan con lai, validation inline va thong bao thanh cong.
- Empty state duoc hien thi khi su kien chua co phieu coc.

## 3. File da sua

| File | Thanh phan | Thay doi |
| --- | --- | --- |
| `ASMILE.BO.App/Views/BanquetRestaurantView.xaml` | Tab Tai chinh | Bo cuc KPI, chi phi, trang thai, lich su va form coc |
| `ASMILE.BO.App/ViewModels/BanquetRestaurantViewModel.Partial.cs` | Finance presentation | Property tong hop, progress, trang thai, validation va command goi y |
| `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs` | `AddDepositCommand` | Khoa bam lap, reload va reset form |
| `ASMILE.BO.App/Converters/VietnamCurrencyConverter.cs` | Dinh dang tien | Hien thi va parse tien theo `vi-VN` |
| `ASMILE.BO.Application/DTOs/RestaurantDtos.cs` | Deposit display | Ten phuong thuc va trang thai tieng Viet |
| `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs` | Deposit guardrails | Chan so am, vuot con lai, su kien huy/hoan tat |
| `ASMILE.BO.Tests.Unit/RestaurantModuleTests.cs` | Finance tests | Khong coc, coc mot phan, coc du, bam lap, mo lai va doi event |

## 4. Binding va ViewModel

| Thanh phan UI | Binding | Command/Property | Trang thai |
| --- | --- | --- | --- |
| Tong gia tri | `SelectedEvent.TotalAmount` | DTO su kien | Hoat dong |
| Da dat coc | `SelectedEvent.DepositAmount` | DTO su kien | Hoat dong |
| Con phai thu | `SelectedEvent.RemainingAmount` | DTO su kien | Hoat dong |
| Progress | `SelectedEvent.PaymentProgress` | Gioi han 0-100 | Hoat dong |
| Lich su | `EventDeposits` | Load tu database theo event | Hoat dong |
| So tien coc | `DepositAmount` | `VietnamCurrencyConverter` | Hoat dong |
| Xac nhan | `AddDepositCommand` | Validation va khoa bam lap | Hoat dong |
| Dat lai | `ResetDepositFormCommand` | Reset form | Hoat dong |

## 5. Nghiep vu tai chinh

```text
GrandTotal = SelectedEvent.TotalAmount
TotalDeposit = tong EventDeposits co Status = Confirmed
RemainingAmount = max(0, GrandTotal - TotalDeposit)
PaymentProgress = clamp(TotalDeposit / GrandTotal * 100, 0, 100)
```

VAT dich vu duoc tach de hien thi tu chenh lech giua `Amount` va `Qty * UnitPrice`. He thong chua co truong giam gia, phu phi va han thanh toan rieng cho Banquet nen giao dien khong tao du lieu gia.

## 6. Build va test

- Build Debug va Release thanh cong.
- XAML encoding gate dat: khong phat hien mojibake.
- Test Banquet: 9/9 dat, trong do co test tai chinh moi cho khong coc, coc mot phan, coc du, vuot muc, bam lap, mo lai va doi event.
- Build sach 0 error, 0 warning sau khi bo khai bao `using` bi lap trong file dang sua.

## 7. Kiem thu UI

- Da build va mo ung dung Release thanh cong.
- Da xac nhan module Banquet khoi dong va tai workspace.
- Da quan sat loi chong nhieu workspace tren cung mot Grid. Nguyen nhan la cac container cap cao phu thuoc converter visibility; da sua tai cho bang `DataTrigger` theo `SelectedTabIndex`, khong dung file backup.
- Sau sua, man Tong quan chi hien thi mot workspace dung va khong con cac man Lich phong, Bao cao, Cai dat chong len nhau.
- Lich su coc va empty state duoc doi sang `DataTrigger` theo `EventDeposits.Count` de khong lap lai loi visibility.
- Hop loi chung khi khoi dong van xuat hien tu luong tai du lieu dong thoi; day la loi runtime ton tai ngoai pham vi bo cuc tab Tai chinh.
- Cac tinh huong submit form tai runtime chua tu dong thuc hien de tranh tao phieu coc that trong du lieu dang dung.

## 8. Hang muc chua lam

- Ma tham chieu giao dich chua them vi entity/DTO hien tai khong co truong luu.
- Ngay nhan tuy chinh chua them; backend ghi `DateTime.Now` khi tao phieu.
- Han thanh toan khong hien thi vi model hien tai khong co du lieu nay.
