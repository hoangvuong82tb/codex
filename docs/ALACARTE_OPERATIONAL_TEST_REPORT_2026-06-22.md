# Bien Ban Test Va Soat Loi Module A La Carte

Ngay lap: 2026-06-22

## 1. Muc tieu

Tai lieu nay ghi lai ket qua:

- soat loi ma hoa font / chu
- soat nhanh loi logic nghiep vu chinh
- build va test tu dong
- ket luan hien trang truoc khi nghiem thu tiep bang tay

## 2. Pham vi da soat

Da soat tren cac nhom sau:

- giao dien module A La Carte
- ViewModel nghiep vu nha hang
- test tu dong unit va E2E
- tai lieu van hanh va tai lieu trang thai
- log khoi dong / log test moi nhat

## 3. Ket qua build va test that

### 3.1. Build

Lenh da chay:

```powershell
dotnet build D:\ASMILE-BO\ASMILE.BO.App\ASMILE.BO.App.csproj -c Debug /nodeReuse:false
```

Ket qua:

- Build thanh cong
- `0` warning
- `0` error
- encoding check cua XAML bao:
  - `Encoding check passed: no mojibake patterns found in XAML.`

### 3.2. Unit test

Lenh da chay:

```powershell
dotnet test D:\ASMILE-BO\ASMILE.BO.Tests.Unit\ASMILE.BO.Tests.Unit.csproj -c Debug --filter RestaurantModuleTests
```

Ket qua:

- `Passed 46/46`

### 3.3. E2E test

Lenh da chay:

```powershell
dotnet test D:\ASMILE-BO\ASMILE.BO.Tests.E2E\ASMILE.BO.Tests.E2E.csproj -c Debug
```

Ket qua:

- `Passed 4/4`

Phan E2E hien tai bao gom:

- app launch khong bat dialog loi
- shell A La Carte hien duoc be mat on dinh
- dieu huong co ban trong workspace khong lam app crash
- kich hoat du lieu mau khong sinh dialog loi

## 4. Ket qua soat loi ma hoa font

### 4.1. Trong file A La Carte dang van hanh

Ket qua:

- khong phat hien mojibake trong:
  - XAML module A La Carte
  - ViewModel nha hang
  - file trang thai hoan thien
  - file huong dan nhan vien moi

### 4.2. Trong he thong log va file cu

Co phat hien:

- mot so dong log canh bao van hien chu loi ma hoa
- mot so file backup cu va file phu ngoai A La Carte chinh van con chu loi

Danh gia:

- day khong phai loi giao dien A La Carte dang van hanh
- day la van de text nguon cu / log output
- khong chan van hanh module, nhung nen don dep tiep neu muon repo sach hon

## 5. Ket qua soat logic nghiep vu

### 5.1. Nhom logic da on

Qua doi chieu code, test va cac dot fix truoc, hien tai da on o cac nhom sau:

- mo phong tu khu vuc / phong trong
- tao Ban Tong va cac ban vat ly cua phong
- chia ban phuc vu / ban cho
- them mon theo nhom ban hoac ban le
- tong hop mon o Ban Tong
- sua mon da gui theo huong sinh phieu dieu chinh
- gui bep / bar
- tru kho theo recipe
- quan tri nhom phuc vu doc lap
- cau hinh va in thu may in theo ten may in Windows hoac IP

### 5.2. Canh bao du lieu / cau hinh

Trong log moi nhat co canh bao lap lai:

- thieu ma thue mac dinh `VAT0`
- thieu ma thue mac dinh `VAT5`
- thieu ma thue mac dinh `VAT8`
- thieu ma thue mac dinh `VAT10`

Danh gia:

- khong lam app khoi dong that bai
- khong chan luong A La Carte co ban
- co the anh huong den cac luong lien quan thue / hoa don / tam tinh neu don vi van hanh dung cac ma thue mac dinh nay

Khuyen nghi:

- kiem tra seed / master data ma thue trong moi truong van hanh that

## 6. Cac diem chua xem la loi chan van hanh, nhung con nen lam sau

### 6.1. E2E full scenario

Chua tu dong hoa full flow:

- nhay dup phong
- mo Ban Tong
- nhap thong tin khach
- them mon trong popup
- gui bep/bar
- reload panel phai va doi chieu danh sach mon

### 6.2. Quy tac khoa / mo khoa sau gui bep

Hien tai da co sua tang / giam va sinh phieu dieu chinh, nhung van con the lam chat hon:

- khoa thao tac theo tung trang thai che bien
- bat buoc ly do o mot so tinh huong nhay cam hon
- quy trinh duyet neu giam / huy mon sau khi bep da xu ly mot phan

### 6.3. Doi chieu in that tai hien truong

Can test tiep tren moi truong that:

- mau phieu bep
- mau phieu bar
- in lai khi sua mon da gui
- toc do va mapping tung may in vat ly

## 7. Tai lieu da tao / cap nhat

Da co cac file sau de doi ngu su dung va nghiem thu:

- [ALACARTE_IMPLEMENTATION_STATUS_2026-06-19.md](D:\ASMILE-BO\docs\ALACARTE_IMPLEMENTATION_STATUS_2026-06-19.md)
- [ALACARTE_USER_GUIDE_FOR_STAFF.md](D:\ASMILE-BO\docs\ALACARTE_USER_GUIDE_FOR_STAFF.md)
- [ALACARTE_ROOM_SERVICE_RULES.md](D:\ASMILE-BO\docs\ALACARTE_ROOM_SERVICE_RULES.md)

## 8. Ket luan nghiem thu ky thuat hien tai

Ket luan hien tai:

- Module A La Carte da dat muc su dung duoc cho van hanh noi bo va nghiem thu tiep bang tay.
- Khong thay loi ma hoa font trong cac file A La Carte dang van hanh va tai lieu moi vua chot.
- Khong phat hien loi logic chan van hanh qua build, unit test, E2E va soat log moi nhat.
- Van con canh bao master data ve ma thue mac dinh va can tiep tuc test hien truong cho phan in that va full flow thao tac.

## 9. De xuat buoc nghiem thu tiep theo

Nen nghiem thu bang tay theo dung thu tu sau:

1. `Khoi tao DL mau`
2. Mo phong trong tab `Khu vuc`
3. Mo `Ban Tong`, nhap thong tin khach, so khach, so ban mo
4. Them mon theo `Nhom ban`
5. Them mon rieng cho `Ban le`
6. Gui bep/bar
7. Sua tang / giam 1 mon da gui de kiem tra phieu dieu chinh
8. In thu bep / bar / hoa don
9. Thanh toan don

Neu ban muon, dot tiep theo minh co the viet them mot bo `UAT checklist` chi tiet theo tung thao tac click-1-click de quan ly, thu ngan va phuc vu ky xac nhan theo vai tro.
