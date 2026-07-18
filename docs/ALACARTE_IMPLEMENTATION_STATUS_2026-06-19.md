# A La Carte Module Completion Status

Ngay cap nhat: 2026-06-22

## 1. Muc tieu dot hoan thien

Hoan thien module nha hang A La Carte theo huong:

- giao dien Win 11, sang, gon, thao tac nhanh
- mo phong dung ngay ro rang cho luong khach vang lai
- Ban Tong quan ly thong tin doan va tong quan don
- ban vat ly hien day du, dong deu, phan mau theo trang thai
- goi mon theo nhom ban va ban le phu hop van hanh phong VIP
- noi tiep den bep/bar, recipe va kho thay vi chi dung o UI

## 2. Da hoan thien

### 2.1. Luong mo phong dung ngay

- Nhan vien co the nhay dup phong trong tu tab `Khu vuc` de mo don.
- He thong tao don phong gom:
  - `Ban Tong`
  - danh sach ban vat ly cua phong
- Khi chua cau hinh xong thong tin khach va so ban phuc vu, don duoc giu o trang thai `Draft`.

### 2.2. Logic chia ban thuc te hon

Logic chia ban da duoc nang cap de tranh sinh cac ban le qua lac que.

Vi du:

- 21 khach -> 11 + 10
- 31 khach -> 11 + 10 + 10
- 34 khach -> 12 + 11 + 11
- 35 khach -> 12 + 12 + 11

Y nghia:

- uu tien chia deu, dep mat bang
- chap nhan ke them ghe mem 1-2 khach/ban
- tranh tao ban 2-3-4 khach neu khong can

### 2.3. Card Ban cua phong

- Ban Tong lon hon ban thuong.
- Cac card ban thuong da duoc khoa cung mot kich thuoc.
- Khoang cach card da duoc can vao ben trong khung chinh.
- Co bo loc so cot va mac dinh la 4 cot.
- Mau trang thai da tach ro:
  - dang phuc vu
  - trong
  - ban cho

### 2.4. Popup / modal in-app

Hai modal chinh trong A La Carte da duoc dua ve huong in-app:

- popup `Mo phong phuc vu`
- popup `Them mon va dich vu`

Trang thai hien tai:

- keo tha duoc trong shell
- co nut thu nho / phong to / dong
- co the thay doi kich thuoc
- noi dung dai da co panel cuon noi bo
- co overlay lam mo nen thao tac phia sau

### 2.5. Popup Them mon / nhom ban

Popup da duoc to chuc lai theo luong thao tac thuc te:

- cot trai: danh sach mon / dich vu
- cot giua: len mon theo nhom ban, cau hinh pham vi ap dung
- cot phai: preview theo tung ban va buoc gui bep/bar

Da co:

- o tim kiem
- tab `Mon an` / `Dich vu`
- chip nhom mon
- pager cho danh sach mon
- preview theo tung ban truoc khi luu
- luu va gui bep/bar tren popup

### 2.6. Sua dut diem loi mo phong trung khoa

Da tach `RestaurantOrderTable.Id` khoi `DiningTable.Id`.

Ban vat ly trong phong duoc map qua `DiningTableId` rieng.

Nhung thao tac sau da dung khoa:

- mo phong
- cau hinh lai ban phuc vu
- them mon vao ban dang chon
- mo lai cung mot phong sau khi don truoc da thanh toan

Ket qua:

- khong con loi `PK_RestaurantOrderTables`
- khong con loi `RestaurantOrderTables.TableNo` khi mo lai phong

### 2.7. Nhom phuc vu doc lap trong database

Da bo sung nen tang du lieu moi:

- `RestaurantServingGroup`
- `RestaurantServingGroupTable`

He thong tu dong dong bo cac nhom co san:

- `ALL-SERVING`
- `STANDARD`
- `ODD`
- `WAITING`

Phase nay da bo sung them:

- `GetServingGroupsAsync`
- `SaveServingGroupAsync`
- `DeleteServingGroupAsync`
- UI editor nhom phuc vu ngay trong popup cau hinh ban

Cho phep:

- tao nhom tay
- chon danh sach ban dang phuc vu
- dat rule va ghi chu
- giu lai custom group khi cau hinh ban thay doi

### 2.8. Noi sang recipe / kho / phan bu dinh luong

Da noi tiep luong `Gui bep/bar` voi cac thanh phan san co:

- `MenuItemRecipe`
- `MenuItemRecipeLine`
- `StockIssue`
- `StockIssueDetail`

Hien tai khi gui bep/bar:

- van tao `RestaurantPreparationTicket`
- dong thoi tao `StockIssue` cho tieu hao A La Carte theo recipe
- tach theo tram `Kitchen` / `Bar`
- uu tien kho mac dinh cua recipe, neu thieu thi fallback `KHO-BEP` / `KHO-BAR`

### 2.9. Phan bu xuong tung nguyen lieu

Dot nay da hoan thien them phan con thieu cua phase recipe:

- line mon co the luu metadata phan bu o cap ingredient
- cho phep thay nguyen lieu thay the theo substitute cua recipe
- cho phep cong them so luong rieng cho tung nguyen lieu
- note hien thi cho UI / bep duoc tach khoi metadata noi bo

Tren popup sua dong mon:

- nhan vien co the sua tong SL
- sua ghi chu bep
- chon nguyen lieu thay the theo tung recipe line
- khai bao them luong phu troi cho tung nguyen lieu

Trong kho:

- `CreateConsumptionIssuesAsync` da doc metadata ingredient-level
- issue detail se tru dung nguyen lieu da thay the neu co
- issue detail se cong them so luong phu troi thuc te

Vi du:

- `Dia tom 10 con`, ban 11 khach -> co the tru 11 con tom
- neu thay bang muc thay the cua recipe -> kho tru theo item thay the va conversion rate da cau hinh

### 2.10. Panel chi tiet ben phai

Da bo sung:

- nut `Them mon / nhom ban`
- nut `Cau hinh ban`
- cot `STT`
- cot `Tac vu`

Logic tac vu hien tai:

- voi ban don: uu tien sua / xoa ngay tren dong
- voi Ban Tong: thao tac sua la chinh tong the de tranh lech giua cac ban

### 2.11. In bep / bar va cau hinh may in

Da noi luong cau hinh may in theo module A La Carte theo huong giong cac module ban hang san co:

- co cau hinh may in bep
- co cau hinh may in bar
- co in thu
- co luong gui bep/bar tu popup them mon

Luu y:

- phan nay da duoc noi nghiep vu trong module
- can kiem tra tiep tren moi may in that neu moi truong van hanh co driver / mapping rieng

### 2.12. Dieu chinh mon da gui bep / bar

Da hoan thien them luong sua dong mon sau khi mon da o trang thai `Sent`:

- tang so luong:
  - he thong cap nhat dong mon hien tai
  - sinh phieu `Send` dieu chinh ngay cho phan tang them
  - tru kho them theo phan delta thay vi tru lai toan bo
- giam so luong:
  - he thong cap nhat dong mon hien tai
  - sinh phieu `Cancel` dieu chinh ngay cho phan giam
- khong con cach lam cu la de dong pending bo sung gay roi du lieu

Y nghia van hanh:

- bep/bar nhan duoc phieu dieu chinh tach bach
- panel ban va Ban Tong giu duoc tong SL moi nhat
- kho khong bi tru lap khi nhan vien sua tang giam sau luc da gui

### 2.13. Lich su phieu bep / bar tren panel chi tiet

Da bo sung them mot khoi thong tin moi trong tab `Thong tin` cua panel ben phai:

- hien danh sach cac phieu da gui bep/bar theo thu tu moi nhat
- hien:
  - ma phieu
  - tram xu ly `Kitchen` / `Bar`
  - loai phieu `Send` / `Cancel`
  - thoi gian gui
  - danh sach dong mon trong phieu
- co nut `Xem phieu` de mo preview nhanh phuc vu doi chieu van hanh
- popup sua mon da bo sung truong ly do dieu chinh cho mon da gui
- nut `+/-` tren ban don:
  - mon chua gui -> tang giam nhanh
  - mon da gui -> chuyen sang popup sua de nhap ly do, tranh sua cam
- Ban Tong khi sua tong so luong mon da gui cung bat buoc co ly do dieu chinh
- order note va ticket note deu luu audit ly do thay doi

### 2.14. Man quan tri nhom phuc vu rieng

Da bo sung them man `Nhom phuc vu` ngay trong `Cai dat` de van hanh co the quan tri tach rieng, khong can mo popup cau hinh ban moi thao tac.

Da co:

- danh sach nhom he thong va nhom tuy chinh
- tao nhom moi
- sua ten nhom, quy tac mon, ghi chu
- chon tap ban thuoc nhom
- xoa nhom tuy chinh
- dung chung ViewModel va logic luu doc lap voi popup cau hinh ban de tranh lech nghiep vu

## 3. Test da chay that

Da xac nhan lai ngay 2026-06-22:

- `dotnet build ASMILE.BO.App\\ASMILE.BO.App.csproj -c Debug /nodeReuse:false`
  - ket qua: thanh cong, `0` loi
- `dotnet test ASMILE.BO.Tests.Unit\\ASMILE.BO.Tests.Unit.csproj -c Debug --filter RestaurantModuleTests`
  - ket qua: `Passed 46/46`
- `dotnet test ASMILE.BO.Tests.E2E\\ASMILE.BO.Tests.E2E.csproj -c Debug`
  - ket qua: `Passed 4/4`

## 4. Nhung gi van chua hoan tat 100%

### 4.1. UI automation end-to-end sau

Da co smoke test that va da mo rong them 1 case dieu huong co thao tac, nhung chua den muc scenario sau:

- nhay dup phong
- mo Ban Tong
- nhap thong tin khach
- them mon tren popup
- gui bep/bar
- kiem tra reload panel ben phai sau thao tac

### 4.2. Luong sua mon sau khi da gui bep

Da co nen tang sua dong mon, metadata ingredient-level va lich su phieu bep/bar, nhung quy trinh day du sau khi da gui bep van con the mo rong them:

- khoa / mo khoa chi tiet hon theo trang thai che bien thuc te
- doi chieu tren UI cho cac tinh huong bep da bat dau xu ly mot phan dong mon
- bo sung workflow duyet neu nha hang muon kiem soat phieu giam / huy sau khi da che bien

### 4.3. Doi chieu in that

Logic gui bep/bar da noi vao module, nhung van can test thuc te tren moi truong van hanh:

- may in bep that
- may in bar that
- mau phieu khi tang / giam / huy mon

## 5. Uu tien de lam tiep

### Uu tien 1

Mo rong E2E UI den muc thao tac that:

- mo phong
- luu thong tin khach
- them mon
- gui bep/bar
- doi panel ben phai cap nhat

### Uu tien 2

Hoan thien quy tac khoa / mo khoa va workflow duyet khi sua mon da gui bep:

- phieu dieu chinh
- phieu huy / giam
- audit ly do thay doi
- khoa thao tac theo trang thai che bien

### Uu tien 3

Chot va test in that tren moi truong van hanh:

- mapping may in
- mau phieu bep
- mau phieu bar
- in lai khi chinh sua mon

## 6. Ket luan hien tai

Module A La Carte da qua muc "demo UI" va da co nen tang nghiep vu that cho:

- mo phong dung ngay
- chia ban thuc te
- nhom phuc vu
- them mon theo nhom / ban le
- gui bep/bar
- tru kho theo recipe
- phan bu ingredient-level

Phan con lai chu yeu la:

- day sau UI automation cho full scenario thao tac that
- bo sung quy tac khoa / mo khoa mon chi tiet hon theo trang thai che bien
- doi chieu in that tren moi truong van hanh cu the
