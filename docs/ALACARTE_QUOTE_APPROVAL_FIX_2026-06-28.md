# Alacarte Quote Approval Fix - 2026-06-28

## Pham vi

Sua loi module `Nha hang A La Carte` tai man `DS Bao Gia`, cu the la luong `Duyet bao gia`.

## Trieu chung ghi nhan

- Khong duyet duoc bao gia khi quote co dong dich vu nhu `Hoa tuoi`.
- Sau mot lan duyet loi, quote co the bi ket o trang thai da tao `AlacarteOrder` nhung chua gan lai `ApprovedOrderId`.
- Bam duyet lai tiep tuc loi duplicate key:
  - `IX_AlacarteOrders_RestaurantQuoteId`
  - truoc do co the gap `IX_TableReservations_ReservationNo`
- Sau khi duyet thanh cong, UI chua tu chuyen sang ngu canh `Da duyet` va `Don mo`.

## File da sua

- `ASMILE.BO.Infrastructure/Services/RestaurantServices.cs`
- `ASMILE.BO.App/ViewModels/RestaurantViewModels.cs`

## Noi dung da chinh

### 1. Hardening backend approve quote

Tai `RestaurantServices.cs`:

- Sua nhan dien dong `Dich vu` de nhan ca:
  - `Dich vu`
  - `Dịch vụ`
  - `Service`
- Them rollback transaction va `ChangeTracker.Clear()` khi `ApproveQuoteAsync` loi.
- Doi cach sinh so chung tu `ReservationNo`, `OrderNo`, `QuoteNo` tu `count + 1` sang lay `max suffix + 1`.
- Bo sung luong recovery:
  - Neu quote da co `AlacarteOrder` theo `RestaurantQuoteId` thi dung lai order cu.
  - Neu quote/order da co `Reservation` thi noi lai thay vi tao moi.
  - Neu quote da co `Deposit` hoac `CashTransaction` thi dung lai ban ghi cu va gan lai vao quote.
- Chinh trang thai order sau khi duyet theo gio su kien:
  - Neu da toi gio phuc vu: `Serving`
  - Neu chua toi gio phuc vu: `Confirmed`
- Chinh trang thai phong/ban duoc tao tu quote:
  - Neu chua toi gio: ban vat ly `Reserved`, ban trong order `Waiting`, phong `Reserved`
  - Neu da toi gio: giu logic `Serving` / `Occupied`

### 2. Chinh hanh vi UI sau khi duyet

Tai `RestaurantViewModels.cs`:

- Sau khi duyet thanh cong:
  - refresh lai `OpenOrders`
  - refresh lai workflow state cua quote
  - chuyen sub-tab `DS Bao Gia` sang `Da duyet`
  - chon `Order` vua tao/vua recovery
  - chuyen main tab sang `Don mo`
- Them guard o `ResolveQuoteWorkflowState`:
  - Neu order dang mang `Serving` nhung thoi gian su kien van chua toi thi van hien thi `Da duyet`
  - Chi hien thi `Da phuc vu` khi order `Serving` va thoi diem phuc vu da bat dau

### 3. Sua thong tin thoi gian tai man Don mo

Tai `RestaurantDtos.cs`, `RestaurantServices.cs`, `AlacarteRestaurantView.xaml`:

- Bo sung du lieu `ArrivalDateTime`, `StartTime`, `EndTime` cho `AlacarteOrderDto`
- Khi map `AlacarteOrder -> AlacarteOrderDto`:
  - uu tien lay gio tu quote (`StartTime`, `EndTime`)
  - fallback sang `ReservationDateTime` neu can
- Man `Don mo` khong con hien `Giờ đến` theo `OrderDate` (gio tao don)
- Doi sang hien `Giờ phục vụ: 17:00 - 21:00` theo khung gio thuc te cua bao gia/dat cho

### 4. Thiet ke lai panel phai tab Khu vuc

Tai `RestaurantViewModels.cs` va `AlacarteRestaurantView.xaml`:

- Bo giao dien tab con `Lịch phục vụ / Chi tiết bàn` trong panel phai cua tab `Khu vực`
- Thay bang layout moi:
  - khoi tong quan phong dang chon
  - lich thang nam o phia tren
  - danh dau cac ngay co booking
  - bam vao ngay tren lich se loc danh sach booking ngay do o phia duoi
- Bo sung state hien thi cho lich:
  - ngay hom nay
  - ngay dang chon
  - so ngay da dat trong thang
  - so booking cua ngay dang xem

### 5. Sua loi lich phong khong hien booking ngay tuong lai

Tai `RestaurantViewModels.cs`:

- Truoc do collection `Reservations` trong app chi nap `GetReservationsAsync(DateTime.Today)`
- He qua:
  - lich thang cua phong khong danh dau booking o ngay tuong lai
  - bam vao ngay tuong lai khong thay danh sach booking
- Da doi sang nap `GetReservationsAsync()` day du o cac luong:
  - load du lieu ban dau
  - sau khi duyet bao gia
  - sau khi thanh toan
  - sau khi huy don
- Them `RefreshRoomReservationCalendarState()` de ep refresh lai:
  - ngay da dat trong thang
  - danh sach booking theo ngay
  - caption va counter o panel lich

## Ket qua mong doi

Sau khi bam `Duyet`:

1. Bao gia khong con loi duplicate order neu quote da tao do dang tu lan truoc.
2. Neu chua toi gio phuc vu, bao gia hien o tab `Da duyet`, khong roi sang `Dang phuc vu`.
3. UI nhay sang `Don mo`.
4. Order lien quan duoc chon san de tiep tuc thao tac.
5. Khi den gio va nghiep vu thuc su mo phuc vu, moi chuyen sang nhom `Dang phuc vu`.

## Build/kiem tra

- `dotnet build ASMILE.BO.Infrastructure/ASMILE.BO.Infrastructure.csproj`: thanh cong
- `dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj`: thanh cong
- `dotnet build ASMILE.BO.sln`: thanh cong sau khi dong app dang chay

## Ghi chu test lai

- Mo app ban moi build.
- Vao `DS Bao Gia`.
- Thu duyet lai quote da tung bi ket.
- Xac nhan:
  - khong con popup loi duplicate key
  - quote roi khoi nhom cho duyet
  - app chuyen sang `Don mo`
  - order cua quote duoc mo san

## Cap nhat them

### 6. Dieu chinh highlight card phong o tab Khu vuc

Tai `AlacarteRestaurantView.xaml`:

- Bo kieu selected cu dang thay ca template card va hien chu `Da chon`
- Giu nguyen mau nen/mau trang thai theo nghiep vu:
  - `Trong`
  - `Da dat`
  - `Dang phuc vu`
  - `Co ban dang dung`
- Khi chon phong, card chi doi sang vien xanh `#3B82F6` voi do day `2`
- Khong hien them text `Da chon`, de van nhin ro trang thai dat phong thuc te

### 7. Sua trang thai phong theo dung ngay dang loc

Tai `RestaurantViewModels.cs`:

- Truoc do card phong o tab `Khu vuc` chi can co `OpenOrder` trong phong la se doi sang `Da dat`
- Loi phat sinh:
  - bao gia/booking ngay 29 da tao san order
  - nhung khi bo loc dang xem ngay 28, phong van bi khoa sai la `Da dat`
- Da sua logic:
  - chi tinh `OpenOrder` vao trang thai phong neu `ArrivalDateTime` cua order trung voi `SelectedDate`
  - neu order khong co `ArrivalDateTime` thi moi fallback sang `OrderDate`
- Ket qua mong doi:
  - phong co lich ngay 29 se khong chan phong khi dang xem ngay 28
  - ngay 28 van co the mo phong/nhan khach phuc vu ngay trong ngay neu phong thuc te dang trong

### 8. Hien thong tin khach va gio phuc vu tren card phong da dat

Tai `RestaurantDtos.cs`, `RestaurantViewModels.cs`, `AlacarteRestaurantView.xaml`:

- Bo sung du lieu hien thi tren `DiningRoomDto`:
  - `BookingSummaryText`
- Khi tinh `FilteredRooms`, app se lay booking uu tien theo ngay dang loc:
  - uu tien `OpenOrder` cua ngay do de co du `ten khach` + `StartTime/EndTime`
  - fallback sang `Reservation` cua ngay do neu chua co order
- Card phong trang thai `Da dat` hien 1 dong duy nhat theo format:
  - `Ten khach / 17h00 - 21h00 / 40 khach`

### 9. Dong bo trang thai phong sau khi chot thuc don

Tai `RestaurantServices.cs` va `RestaurantViewModels.cs`:

- Truoc do o tab `Don mo`, khi bam `Chot thuc don`:
  - mon duoc gui bep/bar
  - nhung tab `Khu vuc` co the chua doi phong sang `Dang phuc vu`
- Da sua:
  - trong `SendToPreparationAsync`, neu order da den gio phuc vu thi nang order sang `Serving`
  - dong bo luon trang thai `OrderTables`, `DiningTable`, `DiningRoom` sang `Serving/Occupied`
  - sau khi gui bep, ViewModel reload `OpenOrders` va goi lai `ApplyTableFilter()` de tab `Khu vuc` refresh ngay
- Ket qua mong doi:
  - bam `Chot thuc don` o dung gio phuc vu xong, quay sang tab `Khu vuc` se thay phong chuyen sang `Dang phuc vu`

### 10. Dong nhat tag card phong khi dang phuc vu

Tai `AlacarteRestaurantView.xaml`:

- Card phong o state `Occupied` truoc do hien text `Co ban dang dung`
- Da doi sang text `Dang phuc vu` de dung voi nghiep vu va dong nhat cach hien thi tren giao dien

### 11. Khong de booking ghi de len trang thai dang phuc vu

Tai `RestaurantViewModels.cs`:

- Da phat hien loi tinh trang thai card phong:
  - order da du dieu kien `Dang phuc vu`
  - nhung vi van con `Reservation` cung ngay nen code lai ghi de nguoc thanh `Da dat`
- Da sua:
  - chi cho `Reservation` doi phong sang `Da dat` khi phong chua o trang thai `Dang phuc vu`
- Case anh/chị neu:
  - ngay `2026-06-28`
  - gio he thong hien tai `17:45`
  - phong `VIP 5` co gio phuc vu `17:00`
  => card phong phai hien `Dang phuc vu`, khong duoc quay lai `Da dat`

### 12. Hien dong thong tin duoi tag Dang phuc vu

Tai `RestaurantDtos.cs`, `RestaurantViewModels.cs`, `AlacarteRestaurantView.xaml`:

- Bo sung `ServingSummaryText` cho card phong
- Khi phong o trang thai `Dang phuc vu`, dong duoi tag se hien theo format:
  - `Ten khach / so ban / so nguoi`
- App chi tinh so ban thuc te dang `Serving` tu `OrderTables`
- Neu order chua co danh sach ban chi tiet nhung da gan `DiningTableId`/`TableName` thi chi tinh `1 ban`
- Khong con fallback sai sang tong so ban cau hinh cua phong

### 13. Mac dinh hien tat ca booking tu hom nay tro di trong panel lich phong

Tai `RestaurantViewModels.cs` va `AlacarteRestaurantView.xaml`:

- Truoc do panel danh sach booking ben duoi lich:
  - neu chua bam ngay tren lich thi se rong
- Da sua:
  - khi vua chon phong, danh sach mac dinh hien tat ca booking cua phong co `ReservationDateTime >= ngay hien tai`
  - chi khi bam mot ngay cu the tren lich moi loc rieng theo ngay do
- Cap nhat them:
  - caption
  - dong huong dan
  - thong diep empty state
  de phan biet ro 2 che do `mac dinh xem tu hom nay tro di` va `dang loc theo ngay`
- Bo sung refresh state ngay trong `OnSelectedRoomChanged()` de khi bam vao phong co booking tuong lai nhu `VIP41`, danh sach hien ngay lap tuc ca khi `SelectedRoomCalendarDate` van dang `null`

### 14. Dua ngay gio dat len vung nhin thay ngay trong card booking

Tai `AlacarteRestaurantView.xaml`:

- Card booking trong panel lich phong duoc thiet ke lai de `ngay dat + gio dat` hien noi bat ngay tren dong dau
- Format badge moi:
  - `29/06 - 17:00`
- Muc tieu:
  - vua nhin vao card la thay ngay booking nao va gio nao
  - khong can doc xuong dong thong tin phu moi biet lich dat

### 15. Card booking co icon ngay do va bam de mo ngay chi tiet bao gia

Tai `RestaurantDtos.cs`, `RestaurantServices.cs`, `RestaurantViewModels.cs`, `AlacarteRestaurantView.xaml`:

- Bo sung du lieu lien ket tren `TableReservationDto`:
  - `QuoteId`
  - `QuoteNo`
  - `ApprovedOrderId`
- Service reservations tu map booking sang quote/order lien quan de UI co the dieu huong dung ban ghi
- Card booking duoc thiet ke lai:
  - ben trai la khoi ngay mau do
  - dong duoi trong khoi do la gio den
  - bam vao ca dong se mo ngay `DS bao gia` va chon dung quote lien ket

### 16. Sua bo loc card phong o tab Khu vuc de hoat dong dung

Tai `RestaurantViewModels.cs`:

- Truoc do:
  - o tim kiem va combobox trang thai chi tac dong toi `FilteredTables`
  - `FilteredRooms` gan nhu chi loc theo tang, nen nhin nhu bo loc khong hoat dong
- Da sua:
  - tim kiem card phong theo `Ten phong`, `Ma phong`, `Loai/tang`
  - loc trang thai card phong theo state thuc te:
    - `Trống` -> `Available`
    - `Đã đặt` -> `Reserved`
    - `Đang phục vụ` -> `Occupied`/`Serving`
    - `Khóa bàn` -> `Locked`

### 17. Noi lai nut In bao gia o man chi tiet bao gia

Tai `AlacarteRestaurantView.xaml`:

- Nut `In bao gia` o cuoi man chi tiet bao gia truoc do chua bind `Command`
- Da noi lai voi `PrintQuoteCommand`
- Ket qua:
  - bam nut se mo luong preview/in bao gia giong cac nut `Xem/In` dang hoat dong o khu vuc khac

### 18. Doi DS bao gia sang pager Win 11 dung chung

Tai `RestaurantViewModels.cs` va `AlacarteRestaurantView.xaml`:

- Danh sach `DS bao gia` duoc doi sang dung `TablePagerViewModel<SavedQuoteDto>` + `TablePagerControl`
- Ap dung cho toan bo 4 tab:
  - `Da duyet`
  - `Chua duyet`
  - `Dang phuc vu`
  - `Da phuc vu`
- Footer moi hien:
  - dong thong tin `Hien thi x - y cua n bao gia`
  - pager Win 11 chung
  - chon so dong/trang theo component co san
