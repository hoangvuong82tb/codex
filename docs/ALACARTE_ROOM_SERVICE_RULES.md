# Tai lieu nghiep vu phong an A La Carte

## 1. Muc tieu

Tai lieu nay dung de chot nghiep vu truoc khi trien khai cho 2 hinh thuc:

- Dat phong truoc
- Khach vang lai dung ngay

Muc tieu la dung chung toi da phan loi:

- Don phong an
- Phong va ban
- Goi mon
- Dinh luong
- In bep/bar
- Kho va recipe

Chi tach rieng phan diem vao cua quy trinh.

## 2. Nguyen tac tong

### 2.1. Hai hinh thuc vao don

- `Reservation`: dat phong truoc, co the co bao gia, dat coc, menu du kien
- `WalkIn`: khach den dung ngay, chon phong trong, mo phong, cau hinh ban, goi mon

Sau khi don da duoc tao, ca 2 hinh thuc deu di vao cung mot man van hanh.

### 2.2. Don vi nghiep vu can tach ro

- `Ban Tong`: quan ly thong tin doan, so khach, ghi chu chung, dich vu chung
- `Ban con`: ban vat ly dang phuc vu hoac dang cho
- `Nhom ban`: nhom cac ban co cung cach len mon va dinh luong
- `Line mon`: dong mon an/dich vu thuc te dung de phuc vu, in bep, tru kho

### 2.3. Don vi thao tac chinh

Khuyen nghi:

- Thong tin doan thao tac o `Ban Tong`
- Goi mon chinh thao tac theo `Nhom ban`
- Mon rieng, mon phat sinh, khac dinh luong thao tac theo `Ban le`

Khong nen coi ca phong la mot noi goi mon duy nhat.

## 3. Luong nghiep vu chuan

### 3.1. Dat phong truoc

1. Tao bao gia hoac thong tin dat phong
2. Xac nhan phong va so khach
3. Co the co menu du kien
4. Den ngay phuc vu, chuyen thanh don van hanh
5. Xac nhan cau hinh ban thuc te
6. Goi mon theo nhom ban

### 3.2. Khach vang lai dung ngay

1. Nhan vien nhay dup phong trong phu hop suc chua
2. He thong mo don
3. He thong tao `Ban Tong` va cac ban vat ly cua phong
4. Nhan vien nhap:
   - ten khach
   - so dien thoai
   - so khach
   - so ban can phuc vu
   - ghi chu chung
5. He thong chia ban va danh dau:
   - ban dang phuc vu
   - ban cho
6. Nhan vien len mon theo nhom ban

### 3.3. Luong sau khi don da mo

1. Chon `Ban Tong` de xem thong tin doan
2. Tao `Nhom ban cung mon`
3. Goi mon cho tung nhom
4. Goi mon rieng cho ban le neu co
5. Them dich vu phong neu co
6. Gui bep/bar theo dot
7. Theo doi phat sinh
8. Tam tinh / thanh toan

Sau moi dot gui bep/bar:

- he thong phai luu phieu gui de doi chieu van hanh
- neu sua mon sau khi da gui, phai sinh phieu dieu chinh tang / giam rieng
- nhan vien can xem duoc lich su cac phieu nay ngay tren panel chi tiet don

## 4. Quy tac chia ban

## 4.1. Khai niem

Moi phong can co 3 nguong:

- `Suc chua chuan`: vi du 10 khach/ban
- `Suc chua toi da mem`: vi du 12 khach/ban
- `Suc chua toi da cung`: vi du 13 khach/ban

`Suc chua chuan` la chuan tham chieu, khong phai luat cung.

## 4.2. Nguyen tac chia ban

Uu tien chia ban theo thu tu:

1. Chia deu, dep mat bang
2. Han che tao ban le qua nho
3. Chap nhan vuot nhe 1-2 khach/ban neu can
4. Khong vuot qua nguong mem neu khong co phe duyet
5. Vuot qua nguong cung phai canh bao hoac chan

## 4.3. Vi du chia ban

### 25 khach

Khuyen nghi:

- Ban 01: 10
- Ban 02: 10
- Ban 03: 5

### 33 khach

Khuyen nghi:

- Ban 01: 11
- Ban 02: 11
- Ban 03: 11

Khong khuyen nghi:

- 10 - 10 - 10 - 3

### 34 khach

Khuyen nghi:

- Ban 01: 12
- Ban 02: 11
- Ban 03: 11

### 35 khach

Khuyen nghi:

- Ban 01: 12
- Ban 02: 12
- Ban 03: 11

### 41 khach

Khuyen nghi:

- Ban 01: 11
- Ban 02: 10
- Ban 03: 10
- Ban 04: 10

hoac

- Ban 01: 11
- Ban 02: 10
- Ban 03: 10
- Ban 04: 10

Tuy vao cach set phong va dinh nghia nguong mem.

## 4.4. Quy tac thao tac

- He thong phai co `chia ban tu dong`
- Nhan vien duoc phep sua tay so khach tung ban
- He thong phai danh dau ban nao dang vuot chuan
- Moi thay doi so khach tung ban phai cap nhat lai goi y dinh luong mon

## 5. Quy tac nhom ban

## 5.1. Khi nao tao nhom ban

Tao `Nhom ban` khi:

- Cac ban an giong nhau
- Dinh luong giong nhau
- Ghi chu bep giong nhau

Vi du:

- Nhom A: Ban 01 + Ban 02, moi ban 10 khach, cung menu
- Nhom B: Ban 03, 5 khach, menu rieng

## 5.2. Khi nao phai tach nhom

Phai tach nhom khi:

- So khach khac dan den dinh luong khac
- Mon khac
- Cung mon nhung so luong khac
- Ghi chu bep khac
- Dot gui bep khac

## 5.3. Quy tac van hanh

- `Ban Tong` khong phai noi goi mon chinh
- `Nhom ban` la don vi goi mon chinh
- `Ban le` la noi xu ly phat sinh mon rieng

## 6. Phan loai mon theo nghiep vu

Moi mon can co `ServingRule`.

### 6.1. PerGuest

Dung cho:

- nuoc ngot
- bia theo chai/lon
- khan lanh
- tom 1 con/nguoi
- sup ca nhan

Cong thuc:

- So luong = so khach thuc te cua ban hoac nhom ban

### 6.2. PerFullTable

Dung cho:

- lau
- set rau
- mon theo ban tron chuan

Cong thuc:

- 1 don vi / 1 ban chuan

Mon nay can them quy tac xu ly ban le.

### 6.3. PerTableWithExtraUnit

Dung cho:

- dia tom 10 con
- dia cua theo set
- mon con chiec theo ban chuan nhung co bu le

Cong thuc:

- Mon chuan theo ban
- Khach vuot chuan se sinh `phu troi`

Vi du:

- Dia tom chuan: 10 con / 10 khach
- Ban 11 khach -> 1 dia tom + 1 con
- Ban 12 khach -> 1 dia tom + 2 con

### 6.4. PerRoom

Dung cho:

- dich vu phong
- trang tri
- may chieu
- phu thu phong

Gan vao `Ban Tong`.

### 6.5. Manual

Dung cho:

- mon goi them
- ruou/vat dung phat sinh
- truong hop nhan vien nhap tay

## 7. Quy tac mon ban le va vuot chuan

## 7.1. Van de thuc te

Thuc don co the chi co mon chuan:

- `Dia tom 10 con`

Nhung ban thuc te co the ngoi:

- 11 khach
- 12 khach

Nghiep vu can ho tro:

- mon chuan
- phan bu them

## 7.2. Khuyen nghi xu ly

Khong nen sua ten mon chuan.

Nen tach:

- `Mon chuan`
- `Phu troi dinh luong`

Vi du:

- Mon chuan: `Tom hap 10 con`
- Phu troi: `Them 1 con tom`

## 7.3. Cach hien thi cho bep

Co 2 cach:

### Cach 1: In tong so luong

- `Tom hap: 11 con`

### Cach 2: In mon chuan + bu them

- `Tom hap: 1 dia chuan 10 con`
- `Bu them: 1 con`

Khuyen nghi:

- luu du lieu theo ca 2 thanh phan
- cho phep cau hinh cach in cho bep

## 7.4. Cach tinh cho kho

Kho nen tru theo `dinh luong thuc te`, khong chi theo ten mon chuan.

Vi du:

- Ban 11 khach
- Tom hap 10 con + bu 1 con

Kho phai tru 11 con tom, khong phai 10.

## 8. Bang quy tac mon mau

| Nhom mon | Vi du | Rule | Chuan | Xu ly ban 11-12 |
|---|---|---|---|---|
| Nuoc ngot | Coca, Pepsi | PerGuest | 1 chai/nguoi | 11 khach = 11 chai |
| Bia | Heineken | PerGuest/Manual | 1 lon/nguoi hoac nhap tay | Co the goi y theo khach, nhan vien sua |
| Tom | Dia tom 10 con | PerTableWithExtraUnit | 10 con/10 khach | 11 khach = 10 + 1, 12 khach = 10 + 2 |
| Cua/Ghe | 1 set/10 khach | PerTableWithExtraUnit | 1 set/10 khach | Bu them theo quy tac nha hang |
| Lau | 1 noi lau/10 khach | PerFullTable | 1 noi/ban chuan | Ban 11-12 van 1 noi hoac 1 noi lon |
| Rau lau | 1 set rau/1 noi | PerFullTable | theo lau | Di theo so noi lau |
| Trai cay | 1 dia/10 khach | PerTableWithExtraUnit hoac PerFullTable | 1 dia/10 | Co the bu them 1 phan nho |
| Khan lanh | 1 cai/nguoi | PerGuest | 1/nguoi | 11 khach = 11 cai |
| Dich vu phong | Trang tri, may chieu | PerRoom | 1/phong | Khong phu thuoc tung ban |

## 9. De xuat mo hinh du lieu

## 9.1. Dung chung cho Reservation va WalkIn

- `Order`
- `OrderMode`: Reservation / WalkIn
- `DiningRoom`
- `OrderTable`
- `ServingGroup`
- `ServingGroupTable`
- `ServingGroupLine`

## 9.2. Truong can co o mon/menu

- `ServingRule`
- `StandardGuestCount`
- `StandardQuantity`
- `AllowSoftOverage`
- `ExtraUnitName`
- `ExtraUnitPerGuest`
- `OddTableHandlingMode`
- `KitchenDisplayMode`

## 9.3. Truong can co o line thuc te

- `ServingGuestCount`
- `BaseQuantity`
- `ExtraQuantity`
- `AppliedTableIds`
- `ServingGroupId`
- `KitchenNote`
- `KitchenDisplayText`
- `RecipeAdjustmentValue`

## 10. Tac dong toi UI

## 10.1. Cac khoi chinh nen co

### Bàn Tong

Nen co:

- thong tin doan
- so khach
- so ban mo
- dich vu phong
- danh sach nhom ban

Khong nen la noi them mon chinh.

### Card ban con

Nen hien:

- ten ban
- so khach ban do
- trang thai
- dang thuoc nhom nao
- co mon rieng hay khong

### Khu goi mon

Nen thao tac theo:

- `Len mon cho nhom ban`
- `Len mon rieng cho ban`
- `Them dich vu phong`

Khong nen chi co mot nut `Them mon` ma khong ro dang them cho ai.

## 10.2. Goi y giao dien thao tac

Khi nhan vien goi mon, can ro:

- dang thao tac cho nhom nao
- gom nhung ban nao
- tong bao nhieu khach
- he thong goi y bao nhieu suat / bao nhieu don vi

Vi du:

- `Nhom A - Ban 01 + Ban 02 - 20 khach`
- `Nhom B - Ban 03 - 5 khach`

## 11. Tac dong toi kho va recipe

## 11.1. Nguyen tac chung

- Mon ban ra la mon kinh doanh
- Kho tru theo dinh luong thuc te
- Phu troi dinh luong phai co cach map vao recipe

## 11.2. Hai cach khuyen nghi

### Cach A: Tao line phu troi rieng

Vi du:

- `Tom hap 10 con`
- `Phu troi tom +1 con`

Uu diem:

- ro rang cho kho
- ro rang cho bep

Nhuoc diem:

- nhieu line hon

### Cach B: Giu 1 line mon, luu them Base va Extra

Vi du:

- `Tom hap`
- `BaseQuantity = 10`
- `ExtraQuantity = 1`

Uu diem:

- sach du lieu nghiep vu

Nhuoc diem:

- can xu ly them o phan in bep va recipe

Khuyen nghi:

- Neu he thong kho/recipe da manh -> Cach B
- Neu can de hieu, de audit -> Cach A

## 12. Cac cau hoi can chot truoc khi code

### 12.1. Chia ban

1. Chuan bao nhieu khach/ban?
2. Muc vuot mem toi da la bao nhieu?
3. Muc vuot cung toi da la bao nhieu?
4. Chia ban uu tien dep, deu hay it ban nhat?
5. Co cho sua tay so khach tung ban khong?

### 12.2. Mon va dinh luong

6. Mon nao theo nguoi?
7. Mon nao theo ban?
8. Mon nao co bu le?
9. Mon nao khong bu le ma phai doi size?
10. Co cho nhan vien sua so luong he thong goi y khong?

### 12.3. Bep/bar

11. Bep muon doc tong so luong hay chuan + bu?
12. In theo nhom ban hay theo tung ban?
13. Neu 2 ban giong nhau, in gop hay tach?

### 12.4. Kho/recipe

14. Phu troi co recipe rieng khong?
15. Kho tru theo mon chuan hay dinh luong thuc te?
16. Khi sua so luong tay, co can ghi ly do khong?

### 12.5. UI

17. Don vi thao tac chinh la nhom ban hay tung ban?
18. Ban Tong chi giu thong tin doan hay co them dich vu chung?
19. Co can gio mon tam truoc khi gui bep khong?
20. Co can phe duyet khi vuot nguong mem/cung khong?

## 13. Khuyen nghi chot nghiep vu

Neu can chot nhanh de trien khai dung huong, khuyen nghi:

1. Dung chung mot mo hinh don cho Reservation va WalkIn
2. Ban Tong chi quan ly doan va dich vu chung
3. Goi mon chinh theo Nhom ban
4. Ban le xu ly mon rieng va dinh luong rieng
5. 10 khach/ban la chuan tham chieu, khong phai luat cung
6. He thong phai ho tro ban 11-12 khach neu duoc phep
7. Mon vuot chuan phai co nghiep vu phu troi dinh luong
8. Kho tru theo dinh luong thuc te
9. Bep nhan thong tin theo cach de lam nhat: tong hoac chuan + bu
10. UI phai ro dang thao tac cho:
   - Ban Tong
   - Nhom ban
   - Ban le

## 14. Huong trien khai tiep theo

Sau khi doc tai lieu nay, nen lam tiep theo thu tu:

1. Chot bang quy tac mon
2. Chot bang chia ban thuc te
3. Chot cach map sang kho/recipe
4. Chot phieu in bep/bar
5. Moi bat dau thiet ke lai man hinh Don mo va Them mon

## 15. Trang thai ap dung hien tai trong code

Cap nhat theo ban trien khai 2026-06-22:

- luong `WalkIn` da mo phong va sinh `Ban Tong` + danh sach ban vat ly
- logic chia ban da cho phep vuot mem 1-2 khach/ban de tranh ban le qua nho
- da co `ServingGroup` / `ServingGroupTable` va editor nhom phuc vu trong popup cau hinh ban
- popup them mon da ho tro len mon theo nhom ban, ban le va preview truoc khi gui bep/bar
- luong gui bep/bar da noi sang:
  - `RestaurantPreparationTicket`
  - `StockIssue`
  - `StockIssueDetail`
- line mon da co metadata phan bu ingredient-level:
  - tang them so luong rieng cho tung nguyen lieu
  - thay the nguyen lieu bang substitute trong recipe
  - tach note hien thi cho bep khoi metadata ky thuat

Dieu nay co nghia la nghiep vu trong tai lieu nay khong con dung o muc y tuong UI nua, ma da co nen tang thuc thi that cho:

- chia ban
- nhom ban
- phu troi dinh luong
- map xuong recipe / kho

Phan van can di tiep:

- man hinh quan tri rieng cho nhom phuc vu
- test UI end-to-end sau hon cho luong desktop that

Bo sung cap nhat 2026-06-22:

- da co phieu dieu chinh tang / giam sau khi mon da gui bep/bar
- tang so luong sinh `Send` delta + issue kho delta
- giam so luong sinh `Cancel` delta
- popup sua mon va sua tong mon da gui da bat buoc nhap ly do dieu chinh
- `+/-` chi con la luong nhanh cho mon chua gui; mon da gui se bat popup sua de audit
- huong tiep theo nen tap trung vao:
  - giao dien quan tri nhom phuc vu rieng
  - luong in dieu chinh tren may that
  - UI automation desktop sau hon
