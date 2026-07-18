# Banquet Workflow Alignment 2026-07-02

## Muc tieu

Can chinh lai module Nha hang Tiec theo huong gan voi quy trinh van hanh thuc te:

- Tiep nhan
- Bao gia
- Giu lich
- Dat coc
- Hop dong
- Ke hoach tiec
- Checklist
- Thuc hien
- Nghiem thu
- Thanh toan
- Thanh ly
- Cham soc sau tiec

## Da chinh sua

### 1. Dieu huong sidebar theo phan he nghiep vu

Da doi sidebar trai tu kieu tab ngang mo rong sang cac muc nghiep vu ro rang hon:

- Tong quan
- Phong tiec
- Lich phong
- Su kien
- Hop dong
- Dat coc
- Ke hoach
- Checklist
- Thuc hien
- Dich vu
- Bao cao
- Cai dat

Moi muc ben trai gio dieu khien truc tiep vung xu ly ben phai thay vi bat nguoi dung phai vua chon sidebar vua chon them tab ngang.

### 2. An thanh tab ngang chi tiet

Noi dung chi tiet ben phai van dung lai cac man hien co, nhung da chuyen sang co che dieu huong bang sidebar. Viec nay giup giao dien gon hon va dung voi ky vong "sidebar la phan he quan ly".

### 3. Bo sung day tien do workflow

Da them mot dai workflow ben tren khu vuc chi tiet su kien de hien nhanh:

- buoc nao da xong
- buoc nao dang xu ly
- buoc nao con thieu

Trang thai workflow hien tai duoc suy ra tu du lieu that dang co:

- su kien
- hop dong
- dat coc
- menu
- dich vu
- checklist
- timeline
- so tien con lai

### 4. Lam sach ViewModel Banquet

Da mo rong `BanquetRestaurantViewModel` de:

- map sidebar sang dung workspace chi tiet
- cap nhat tieu de workspace theo tung phan he
- tao workflow summary dong bo theo su kien duoc chon
- loai bo mot loat chuoi thong bao bi vo ma trong partial ViewModel

## File da thay doi

- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\RestaurantViewModels.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\ViewModels\BanquetRestaurantViewModel.Partial.cs`
- `D:\ASMILE-BO\ASMILE.BO.App\Views\BanquetRestaurantView.xaml`

## Kiem tra

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln`
- Build thanh cong, 0 error, 0 warning

## Chua lam trong dot nay

Day la dot can chinh lai khung nghiep vu va dieu huong. Cac phan sau van can lam sau de dat muc "day du chuc nang" dung theo quy trinh:

- luong hold co ngay het han va tu huy
- nghiem thu co truong du lieu thuc te va bien ban
- thanh ly hop dong co trang thai va bien ban rieng
- cham soc sau tiec co du lieu luu va theo doi
- checklist tach theo bo phan Kinh doanh / Bep / Ky thuat / Trang tri / Phuc vu bang schema that
- timeline moc 90/60/30/15/7/3/1/ngay to chuc bang du lieu that thay vi chi timeline trong ngay

## Ghi chu

Huong xu ly uu tien o dot nay la:

1. khong pha vo du lieu va service dang dung
2. dua giao dien Banquet ve dung luong quan ly thuc te
3. tao nen tang de mo rong tiep cac buoc nghiep vu con thieu
