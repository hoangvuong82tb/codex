# MENU_RECIPE_CATEGORY_HIERARCHY_REPORT

## Muc tieu

- Bo sung kha nang tao `nhom con` trong module `Mon an & Dinh luong` tai man `Nhom mon`.
- Cho phep chon `nhom cha` khi tao moi va khi chinh sua nhom hien co.
- Luu du lieu that xuong DB, khong chi doi UI.
- Bo sung tab du lieu `Mon an` va `Dich vu` trong man hinh quan ly menu.

## Files da chinh

- `ASMILE.BO.Application/DTOs/BreakfastCafeSalesDtos.cs`
- `ASMILE.BO.Core/Entities/BreakfastCafeSalesEntities.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Services/MenuCatalogService.cs`
- `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260629114500_AddParentCategoryToMenuCategory.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices.cs`
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.cs`
- `ASMILE.BO.App/ViewModels/MenuRecipeViewModel.Services.cs`
- `ASMILE.BO.App/ViewModels/BreakfastCafeSalesViewModel.cs`
- `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`
- `ASMILE.BO.App/Views/MenuRecipe/Screens/MenuCategoriesScreen.xaml`
- `ASMILE.BO.App/Views/MenuRecipe/Panels/MenuListPanel.xaml`
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/MenuGeneralTab.xaml`
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/ServiceGeneralTab.xaml`
- `ASMILE.BO.App/Views/MenuRecipe/Tabs/ServiceGeneralTab.xaml.cs`
- `ASMILE.BO.App/Views/MenuRecipe/Screens/MenuItemsScreen.xaml`

## Noi dung thay doi

- Them `ParentCategoryId` va `ParentCategoryName` cho DTO nhom mon.
- Them `ParentCategoryId` vao entity `MenuCategory`.
- Cau hinh quan he tu tham chieu `MenuCategory -> MenuCategory` trong `AppDbContext`.
- Bo sung migration them cot `ParentCategoryId`, index va foreign key cho bang `MenuCategories`.
- Service `MenuCatalogService`:
  - tra ve thong tin nhom cha,
  - luu `ParentCategoryId`,
  - chan chon chinh no lam nhom cha,
  - chan vong lap cha-con,
  - sap xep danh sach nhom theo phan cap cha -> con.
- `MenuRecipeViewModel`:
  - them state chon `nhom cha` cho form tao moi va form chinh sua,
  - them danh sach option nhom cha,
  - giu lai nhom dang chon sau khi refresh lookups,
  - hien thi metadata nhom goc/nhom con,
  - them tab loc `Mon an / Dich vu`,
  - them tab `Nhom mon / Nhom dich vu` tai man `Nhom mon`,
  - loc danh sach nhom theo nhom mon hoac nhom dich vu dua tren ten/ma/quan he cha-con,
  - tach danh sach theo co `IsHotelService`,
  - doi nut tao moi theo tab dang chon,
  - gioi han combo nhom theo loai du lieu dang quan ly,
  - bo sung fallback de du lieu cu van hien thi dung sau khi lookup nhom tai xong,
  - bo sung luong editor rieng cho `Dich vu` dua tren `RestaurantExtraService`,
  - bo sung loc theo `Loai dich vu`,
  - bo sung command rieng de luu / sao chep / ngung ban dich vu.
- `MenuCategoriesScreen.xaml`:
  - bo sung 2 tab chuyen `Nhom mon / Nhom dich vu`,
  - them `ComboBox` chon `Nhom cha` trong form `Them nhom`,
  - them `ComboBox` chon `Nhom cha` trong phan `Thong tin nhom`,
  - danh sach nhom hien thi ro hon nhom goc / nhom con,
  - an KPI va bang mon khi dang xem `nhom dich vu` de tranh hien thi sai nghiep vu.
- `MenuListPanel.xaml`:
  - bo sung tab chuyen nhanh `Mon an` va `Dich vu`,
  - doi tieu de danh sach va nut tao moi theo tab,
  - doi bo loc ben trai sang `Loai dich vu` khi o tab dich vu.
- `MenuGeneralTab.xaml`:
  - doi nhan `Thong tin mon` thanh dong theo loai du lieu,
  - combo nhom chi hien danh sach nhom phu hop.
- `ServiceGeneralTab.xaml`:
  - tao man hinh them/sua dich vu rieng,
  - bo cac thanh phan sai nghiep vu nhu dinh luong, khu che bien, gia von mon, anh mon,
  - tap trung vao ma dv, ten dv, loai dv, don gia chuan, VAT, trang thai va ghi chu pham vi,
  - bo sung khu vuc `Anh dich vu` voi chuc nang chon/xoa anh giong man mon an.
- Bo sung co `IsVatIncluded` cho mon an va dich vu:
  - checkbox `Gia da gom VAT` duoc dat ngay truoc truong `VAT (%)`,
  - mac dinh o trang thai da tich,
  - bo tich nghia la gia nhap vao chua bao gom VAT,
  - luu xuong ca `MenuItem` va `RestaurantExtraService`,
  - dong bo qua cac man hinh su dung chung request save/menu settings.
- Bo sung anh dich vu cho `RestaurantExtraService`:
  - them `ImagePath/ImageUrl` o entity va DTO/service save,
  - tai man dich vu su dung chung luong chon/xoa anh voi mon an,
  - them lop tu-va-schema de DB tu bo sung cot anh neu chua co,
  - giu `Anh dich vu` o cot phai va dua khung `Goi y nhap lieu` xuong ngay ben duoi truong `Ghi chu` de bo cuc dung voi luong nhap lieu mong muon.
- `MenuItemsScreen.xaml`:
  - tach workspace `Mon an` va `Dich vu`,
  - an sidebar gia von/canh bao khi dang sua dich vu,
  - thay bang card quy tac nghiep vu dich vu.

## Kiem tra ky thuat

- `dotnet build D:\ASMILE-BO\ASMILE.BO.sln` thanh cong.

## Ghi chu

- Migration se duoc app tu chay khi khoi dong qua `DatabaseMigrationService.InitializeAsync()`.
- Da them migration rieng cho cot `IsVatIncluded` o `MenuItems`, `RestaurantExtraServices`, `AlacarteOrderLines` va `RestaurantOrderExtraServices` de tranh loi schema khi app mo lai.
- Bo sung them lop tu-va-schema trong `DatabaseMigrationService`: neu DB thuc te chua co cot `IsVatIncluded` thi app tu them cot khi khoi dong, tranh loi `Invalid column name 'IsVatIncluded'` lam trang danh sach mon / dich vu.
- Trong qua trinh kiem tra thuc te da phat hien DB hien hanh chua co cot `ParentCategoryId`, lam list `Nhom mon` trang du du lieu cu van con.
- Da bo sung guard fallback o `MenuCatalogService.GetCategoriesAsync()` de neu schema chua cap nhat kip thi danh sach nhom cu van hien thi, khong trang man hinh.
- Da kiem tra truc tiep DB `ASMILE_BO`: du lieu cu van con, gom nhom mon, mon an va dich vu bo sung; loi chinh nam o lop phan loai/loc hien thi, khong phai mat du lieu.
- Tab `Dich vu` khong con dung form cua mon an. Editor moi da chuyen sang du lieu `RestaurantExtraService` co san trong module A La Carte.
- Ban nay chu y bo trong viec mo rong schema DB cho dich vu. Form moi da du nghiep vu hien tai, nhung neu can theo doi sau hon nua co the bo sung them thong tin nha cung cap / don vi tinh / hinh thuc cung ung o dot sau.
