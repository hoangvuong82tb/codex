# Banquet Workflow Refinement 2026-07-02

## Muc tieu
- Canh chinh lai workflow chi tiet cua module Nha hang Tiec theo luong nghiep vu thuc te.
- Loai bo tinh trang the workflow nhay sai noi dung do lech index tab.
- Bo sung cac buoc nghiep vu con thieu trong khu vuc chi tiet su kien.

## Da chinh sua
- Cap nhat mapping `SelectedDetailTabIndex` trong `BanquetRestaurantViewModel` va `RestaurantViewModels.cs`.
- Bo sung helper `NormalizeLegacyVietnameseText` va `LooksLikeLegacyMojibake` vao `BanquetRestaurantService` de sua duoc du lieu checklist/timeline cu bi loi ma hoa.
- Mo rong khu vuc chi tiet trong `BanquetRestaurantView.xaml` voi cac buoc:
  - Thong tin
  - Bao gia
  - Giu lich
  - Hop dong
  - Dat coc
  - Checklist
  - Thuc hien
  - Ke hoach
  - Nghiem thu
  - Thanh toan
  - Thanh ly
  - Cham soc
- Tiep nhan:
  - Khong con card phong tiec thua ben trong khu vuc chi tiet.
- Bao gia:
  - Gom thao tac gan thuc don, xem tong bao gia, va them nhanh dich vu phat sinh.
- Giu lich:
  - Co lich thang va danh sach su kien da giu lich theo ngay/phong dang chon.
- Checklist:
  - Bo sung form them muc moi.
  - Bo sung nut xoa tren tung dong.
  - Hien them ghi chu cua checklist.
- Thuc hien:
  - Bo sung form them moc thuc hien.
  - Bo sung nut xoa moc timeline.
  - Giu thao tac doi trang thai moc.
- Ke hoach:
  - Chuyen huong noi dung sang cac hang muc dich vu can chuan bi.
- Nghiem thu / Thanh toan / Thanh ly / Cham soc:
  - Bo sung cac tab tong hop theo su kien va du lieu thuc dang co.
- Giao dien tong the:
  - Chinh lai bo cuc tab Tong quan/Dieu hanh tiec theo huong sat man hinh mau:
    - Day KPI tren cung tang thanh 6 o thong tin.
    - Cot trai giu vai tro danh sach su kien.
    - Khoi giua tap trung vao chi tiet su kien va tab nghiep vu.
    - Cot phai dua lich phong tiec, lich da giu va danh sach dich vu da chon ra ngoai.
    - Day checklist cong viec xuong chan khoi chi tiet de de theo doi nhu dashboard.
  - Tinh gon the workflow de nhin giong thanh dieu huong quy trinh hon, khong con dang the dai va roi.
- Refactor ERP 2026-07-02:
  - Chuyen sidebar sang 8 phan he:
    - Tong quan
    - Lich phong
    - Khach hang
    - Su kien
    - Hop dong
    - Thu chi
    - Bao cao
    - Cai dat
  - Tach man hinh theo cau truc ERP:
    - Dashboard chi de xem nhanh
    - Danh sach su kien rieng co filter
    - Chi tiet su kien rieng co header, step progress va tab nghiep vu
  - Tao them state hien thi trong `BanquetRestaurantViewModel`:
    - bo loc su kien
    - danh sach su kien da loc
    - du lieu dashboard
    - tong hop khach hang / hop dong / thu chi
    - timeline milestone va checklist group hien thi
  - Thay toan bo `BanquetRestaurantView.xaml` theo giao dien ERP gon hon, bo kieu all-in-one.
  - Dong bo shell giao dien cua `BanquetRestaurantView.xaml` theo pattern cua `AlacarteRestaurantView.xaml`:
    - top bar cao 78 px
    - sidebar 78 px, nav button dang icon + label ngan
    - card dung cung bo shadow, vien va spacing
    - nut refresh dung chung style `ActionBtn`
    - workspace content dua ve cach canh le giong A La Carte

## Kiem tra
- `dotnet build D:\\ASMILE-BO\\ASMILE.BO.sln`
- Ket qua: Build succeeded, 0 errors, 0 warnings.

## Sua loi khoi dong 2026-07-02 17:18
- Phan tich log `D:\\ASMILE-BO\\logs\\asmile-bo-20260702.log` xac dinh loi UI startup:
  - `Cannot find resource named 'ActionBtn'`
  - loi phat sinh tai `BanquetRestaurantView.xaml`
- Bo sung lai 2 style resource bi thieu trong `BanquetRestaurantView.xaml`:
  - `NavBtn`
  - `ActionBtn`
- Build lai `ASMILE.BO.App.csproj` thanh cong.
- Mo lai app tu `bin\\Debug\\net8.0-windows\\ASMILE.BO.App.exe`, khong con phat sinh `Unhandled UI exception` trong log.

## Sua loi khoi dong 2026-07-02 20:50
- Bat tiep log startup xac dinh loi resource thu 2:
  - `Cannot find resource named 'ActionSuccess'`
  - loi tai nut `Quyet toan` trong tab Thanh toan cua `BanquetRestaurantView.xaml`
- Bo sung them style resource `ActionSuccess` trong `BanquetRestaurantView.xaml`.
- Build lai thanh cong va mo lai app.
- Log moi nhat chi con warning seed du lieu thue `VAT0/VAT5/VAT8/VAT10`, khong con `ERR`, `Unhandled UI exception` hay `XamlParseException`.
- `dotnet build D:\\ASMILE-BO\\ASMILE.BO.App\\ASMILE.BO.App.csproj -p:UseAppHost=false`
- Ket qua: Build succeeded, 0 errors. Con warning lock file do app dang mo khi build.
- `dotnet build D:\\ASMILE-BO\\ASMILE.BO.App\\ASMILE.BO.App.csproj`
- Ket qua: Build succeeded, 0 errors, 0 warnings.

## Ghi chu
- Cac muc in an hop dong / thanh ly va cac dieu khoan chi tiet chua duoc bo sung service/schema rieng trong dot nay.
- Phan "Bao gia" hien dang dung du lieu that tu menu package va service line hien co.
