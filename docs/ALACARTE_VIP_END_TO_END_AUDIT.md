# ALACARTE_VIP_END_TO_END_AUDIT.md

## Audit End-to-End Luồng À La Carte VIP — Sau Tích Hợp ServingSet

Ngày: 24/06/2026

---

# KẾT LUẬN CHÍNH

**Tất cả 13 bước trong luồng đều là REAL. Không bước nào là MOCK hoàn toàn.**

Đây là kết quả audit **tích cực hơn dự kiến**. Module đã có nền móng vững chắc. Các vấn đề còn lại tập trung ở mức độ hoàn thiện (partial), không phải thiếu hụt nghiệp vụ (missing).

---

# BẢNG TỔNG KẾT

| # | Bước | Trạng thái | Phương thức chính | File:Line |
|---|---|---|---|---|
| 1 | Tạo báo giá | **REAL** | `SaveQuoteAsync` | Service:1278, VM:5828 |
| 2 | Chọn Suất/Menu có sẵn | **REAL** (client-side expand) | `AddMenuPackageToQuote`, `GetMenuPackagesAsync` | VM:1152, Service:1537 |
| 3 | Bung món từ suất ra QuoteLine | **REAL** (client-side) | `AddMenuPackageToQuote` | VM:1166-1199 |
| 4 | PricingRule: PerGuest/PerTable/PerRoom/Manual | **REAL** | `MenuCalculationEngine.Calculate`, `NormalizePricingRule` | Engine:97, Service:1412,1925 |
| 5 | Chia bàn theo số khách | **REAL** | `TableAllocationEngine.Allocate` | Engine:9, Service:461 |
| 6 | Duyệt báo giá | **REAL** | `ApproveQuoteAsync` (transactional) | Service:1444-1523 |
| 7 | Sinh đơn mở | **REAL** | `CreateOrderForQuoteAsync` | Service:1833-1949 |
| 8 | Sinh Bàn Tổng + bàn thật | **REAL** | Quote tables + walk-in tables | Service:402,1371,1980 |
| 9 | Phân món xuống từng bàn | **REAL** | `CreateOrderForQuoteAsync` PerPerson/PerTable loop | Service:1925-1944 |
| 10 | Thêm món phát sinh theo bàn | **REAL** | `AddOrUpdateLineAsync`, `ApplySentLineAdjustmentAsync` | Service:735,1644 |
| 11 | Thêm dịch vụ cấp phòng | **REAL** | `AddExtraServiceToOrderAsync` | Service:796 |
| 12 | Gửi bếp/bar | **REAL** | `SendToPreparationAsync`, `CreateConsumptionIssuesAsync` | Service:917,2211 |
| 13 | Thanh toán ở Bàn Tổng | **REAL** (đơn giản) | `PayOrderAsync`, `Recalculate` | Service:852, VM:3106 |

---

# CHI TIẾT TỪNG BƯỚC

---

## Bước 1: Tạo báo giá

**Trạng thái: REAL**

### Flow

1. ViewModel `SaveQuoteAsync` thu thập: CustomerName, Phone, DiningRoomId, EventDate, StartTime, EndTime, GuestCount, QuoteLines
2. Service `SaveQuoteAsync` (line 1278):
   - Validate: tên khách, số khách > 0, ngày, ít nhất 1 dòng, phòng
   - Gọi `_tableAllocationEngine.Allocate(guestCount, tableStandard)` → sinh danh sách bàn (Bàn Tổng + bàn thật)
   - Gọi `_menuCalculationEngine.Calculate(pricingRule, guestCount, realTableCount, quantity)` cho từng dòng → tính `CalculatedQuantity`
   - Persist `RestaurantQuote` + `RestaurantQuoteTable` + `RestaurantQuoteLine`

### Điểm cần lưu ý

- `QuoteTableCount` computed property có thể sai: đang dùng `ReservationGuestCount` thay vì tính từ `GuestCount / GuestsPerTable`
- TableStandard mặc định = 10 khách/bàn, có thể cấu hình

---

## Bước 2: Chọn Suất/Menu có sẵn

**Trạng thái: REAL (client-side expand)**

### Flow

1. Tab "Menu có sẵn" bind `MenuPackages` (loại `BanquetMenuPackageDto`)
2. `MenuPackages` load từ `_service.GetMenuPackagesAsync()` → query `BanquetMenuPackage` + lines + menu items
3. Nút "+" gọi `AddMenuPackageToQuoteCommand` → expand package lines thành `QuoteLineItem`

### Điểm cần lưu ý

- **`BanquetMenuPackage` là entity DÙNG CHUNG** giữa Banquet và ALC — không cần tạo entity mới
- Giá món (`UnitPrice`) lấy từ DTO đã cache, không re-query. Nếu giá thay đổi sau khi load → giá trong quote có thể cũ
- Package expansion là **client-side** — không có server method `AttachMenuPackageToQuote`. Đây không phải bug, nhưng đáng lưu ý nếu cần audit trail

---

## Bước 3: Bung món từ suất ra QuoteLine

**Trạng thái: REAL (client-side)**

### Flow

`AddMenuPackageToQuote` (VM line 1152):
- Duyệt `package.Lines`
- Tạo `QuoteLineItem` với `MenuItemId`, `Name`, `UnitPrice`, `Quantity`, `Type="Món ăn"`, `PricingRule` từ line/package
- Nếu trùng `MenuItemId + PricingRule` → gộp số lượng

### Điểm cần lưu ý

- Merge logic đúng — không tạo duplicate line
- PricingRule được inherit từ package level (`PricingMode`) hoặc line level

---

## Bước 4: PricingRule từng món

**Trạng thái: REAL**

### Engine

`MenuCalculationEngine.Calculate` (Engine line 97):
- **PerPerson**: `quantity * guestCount`
- **PerTable**: `quantity * realTableCount`
- **PerRoom**: `quantity` (cố định)
- **Manual**: `manualQuantity`

`NormalizePricingRule` (Service line 2478):
- "PerPerson" → PerPerson
- "PerTable" → PerTable
- "PerRoom" → PerRoom
- Service line → mặc định PerRoom
- Food line → mặc định Manual

### Điểm cần lưu ý

- **KHÔNG CÓ SAI**. 4 quy tắc hoạt động đúng trong cả Quote save và Quote→Order conversion
- Khi convert Quote→Order (Service line 1925-1944):
  - PerPerson: quantity × GuestCount của từng bàn, lặp qua tất cả bàn thật
  - PerTable: quantity × 1, lặp qua tất cả bàn thật
  - PerRoom/Manual: 1 dòng cho Bàn Tổng (virtual)

---

## Bước 5: Chia bàn theo số khách

**Trạng thái: REAL**

### Engine

`TableAllocationEngine.Allocate` (Engine line 9):
1. Tạo Bàn Tổng (IsVirtual=true, GuestCount=total)
2. `BuildSuggestedAllocations(guestCount, standard)`:
   - `fullTables = guestCount / standard` bàn đầy
   - Nếu dư ≤ softCapacity → phân phối đều vào bàn hiện có (tối đa standard+2)
   - Nếu dư > softCapacity → tạo bàn lẻ mới

### Điểm cần lưu ý

- TableStandard mặc định = 10, softCapacity = 2
- Walk-in flow dùng `BuildServingGuestAllocations` — phân phối khách vào bàn vật lý có sẵn trong phòng
- **Không có UI cho phép người dùng chỉnh sửa phân bổ bàn trong quote** — chỉ có thể chỉnh trong walk-in order flow

---

## Bước 6: Duyệt báo giá

**Trạng thái: REAL**

### Flow

`ApproveQuoteAsync` (Service line 1444):
1. Load quote + tables + lines
2. Kiểm tra chưa duyệt
3. Tạo `TableReservation` (Confirmed)
4. Gọi `CreateOrderForQuoteAsync` → sinh đơn mở
5. Set `quote.ApprovedOrderId`, `quote.Status = "Da duyet"`
6. Nếu có DepositAmount > 0 → tạo `RestaurantDeposit` + `CashFundReceipt`
7. Transaction commit

### Điểm cần lưu ý

- **Có transactional** — nếu bước nào fail, rollback toàn bộ
- Deposit tự động tạo receipt trong CashFund — đúng nghiệp vụ kế toán
- Reservation tự động tạo — đúng flow đặt phòng

---

## Bước 7: Sinh đơn mở

**Trạng thái: REAL**

### Flow

`CreateOrderForQuoteAsync` (Service line 1833):
- Tạo `AlacarteOrder` với `OrderType="Advance"`, `Status="Serving"`
- Copy `QuoteTables` → `RestaurantOrderTable`
- Xử lý từng `QuoteLine` theo PricingRule (xem Bước 4)
- Service line → `order.ExtraServices`
- Food line → `AlacarteOrderLine` với `LineType="Normal"`, `KitchenStatus="New"`

### Điểm cần lưu ý

- `OrderType="Advance"` phân biệt với walk-in order
- Tất cả OrderLine có `RestaurantQuoteLineId` để traceability

---

## Bước 8: Sinh Bàn Tổng + bàn thật

**Trạng thái: REAL**

### Hai đường dẫn

**Quote flow**: Bàn được tạo trong `SaveQuoteAsync` → copy sang `RestaurantOrderTable` khi duyệt
**Walk-in flow**: `OpenRoomWalkInOrderAsync` → `EnsureRoomPhysicalTablesAsync` → tạo `DiningTable` nếu thiếu → tạo Bàn Tổng ảo + bàn thật

### Đặc điểm Bàn Tổng

- `IsVirtual = true`
- `DisplayName = "Bàn Tổng"` (hardcoded)
- `Note = "Tổng hợp thanh toán"` (hardcoded)
- `GuestCount = total guests`

### Điểm cần lưu ý

- **Tên Bàn Tổng hardcoded** — không configurable
- `EnsureRoomPhysicalTablesAsync` tự động tạo bàn vật lý với code pattern `{ROOM_CODE}-B{NN}`

---

## Bước 9: Phân món xuống từng bàn

**Trạng thái: REAL (chỉ khi tạo order)**

### Flow

`CreateOrderForQuoteAsync` line 1925-1944:
- **PerPerson**: `qty = servingTable.GuestCount × quoteLine.Quantity`, lặp `foreach realTables`
- **PerTable**: `qty = quoteLine.Quantity`, lặp `foreach realTables`
- **PerRoom/Manual**: 1 dòng vào Bàn Tổng với `CalculatedQuantity`

### Điểm cần lưu ý

- **Chỉ phân phối khi tạo order** — không có re-distribution sau đó
- Món thêm sau khi order đã tạo (Bước 10) được gán thủ công vào bàn qua `OrderTableId`

---

## Bước 10: Thêm món phát sinh theo bàn

**Trạng thái: REAL**

### Flow

`AddOrUpdateLineAsync` (Service line 735):
- Nhận `request.OrderTableId` để gán vào bàn cụ thể
- Nếu trùng `MenuItemId + OrderTableId + chưa gửi bếp` → gộp
- Sau khi gửi bếp: `ApplySentLineAdjustmentAsync` → tạo adjustment ticket

### Điểm cần lưu ý

- **Có phân biệt pre-sent và post-sent**: điều chỉnh sau gửi bếp tạo ticket riêng
- Kitchen status tracking: New → Sent → (adjustment) → Supplemental

---

## Bước 11: Thêm dịch vụ cấp phòng

**Trạng thái: REAL**

### Flow

`AddExtraServiceToOrderAsync` (Service line 796):
- Thêm vào `order.ExtraServices` — **cấp order, không phải cấp bàn**
- DTO merge vào `Lines` với `LineType="Dịch vụ"`

### Điểm cần lưu ý

- **Đúng nghiệp vụ**: dịch vụ phòng không bị chia xuống bàn
- 11 dịch vụ seed sẵn (LED, MC, Ca sĩ, Âm thanh...)

---

## Bước 12: Gửi bếp/bar

**Trạng thái: REAL**

### Flow

`SendToPreparationAsync` (Service line 917):
1. Lấy pending lines (`KitchenStatus != "Sent" && LineStatus != "Cancelled"`)
2. Tách Kitchen vs Bar dựa trên `PreparationArea`
3. Tạo `RestaurantPreparationTicket` cho từng station
4. Set `KitchenStatus = "Sent"`, `SentAt = now`
5. Gọi `CreateConsumptionIssuesAsync` → tạo `StockIssue` (xuất kho)

### Điểm cần lưu ý

- **Ticket KHÔNG tách theo bàn** — 1 ticket chứa tất cả bàn cho station đó
- Nếu cần in bếp riêng từng bàn → chưa hỗ trợ
- Xuất kho tạo `StockIssue` với `IsApproved=false` (Draft) — cần duyệt thủ công

---

## Bước 13: Thanh toán ở Bàn Tổng

**Trạng thái: REAL (đơn giản)**

### Flow

`PayOrderAsync` (Service line 852):
1. Validate order chưa Paid/Cancelled
2. `Recalculate(order)` → tổng tất cả line + extra service
3. Yêu cầu `paidAmount >= order.RemainingAmount`
4. Set `PaidAmount = TotalAmount`, `Status = "Paid"`
5. `ReleaseOrderTablesAsync` → giải phóng bàn

### Điểm cần lưu ý — **QUAN TRỌNG**

- **Không có split payment**: chỉ thanh toán toàn bộ 1 lần
- **Không verify Bàn Tổng**: thanh toán sum tất cả line không phân biệt bàn nào
- **`Recalculate` sum tất cả line + extra service** — đúng về mặt tổng, nhưng không có check "tổng Bàn Tổng = tổng các bàn con + dịch vụ"
- **`paidAmount < RemainingAmount` → throw** → không cho phép thanh toán từng phần

---

# PHÂN TÍCH RỦI RO

## Rủi ro về tiền (sai số tiền)

| # | Rủi ro | Mức độ | Mô tả |
|---|---|---|---|
| R1 | **Không có split payment** | CAO | Nhà hàng cao cấp thường thanh toán nhiều đợt (đặt cọc → đợt 1 → đợt 2). Hiện chỉ có 1 lần |
| R2 | **`PaidAmount < RemainingAmount` throw** | CAO | Không hỗ trợ thanh toán từng phần. Nếu khách muốn trả 1 nửa → lỗi |
| R3 | **ServiceCharge = 0** | THẤP | Field có nhưng `Recalculate` không populate |
| R4 | **Stock Issue Draft** | TRUNG BÌNH | Xuất kho tạo Draft, không tự động post → chênh lệch tồn kho |

## Rủi ro về số lượng món (sai SL)

| # | Rủi ro | Mức độ | Mô tả |
|---|---|---|---|
| R5 | **PerPerson × GuestCount sai nếu GuestCount thay đổi sau duyệt** | TRUNG BÌNH | Chưa có flow tăng/giảm khách sau duyệt |
| R6 | **Không re-distribute món sau khi tạo order** | THẤP | Món phát sinh thêm được gán thủ công, không bị sai |
| R7 | **Giá món từ cache, không re-query** | THẤP | Khi expand package, giá từ DTO đã load. Nếu giá thay đổi → quote dùng giá cũ |

## Rủi ro về nghiệp vụ

| # | Rủi ro | Mức độ | Mô tả |
|---|---|---|---|
| R8 | **Không có UI chỉnh sửa phân bổ bàn trong quote** | TRUNG BÌNH | Engine chia tự động, nhưng không cho phép người dùng chỉnh |
| R9 | **Bàn Tổng tên hardcoded** | THẤP | "Bàn Tổng" — có thể cần configurable |
| R10 | **Ticket bếp không tách theo bàn** | TRUNG BÌNH | Bếp nhận 1 ticket cho tất cả bàn, khó phân chia |

---

# NHỮNG PHẦN ĐÃ ĐÚNG — KHÔNG CẦN SỬA

1. **Quote → Order conversion**: Transactional, đầy đủ, đúng nghiệp vụ
2. **PricingRule engine**: PerGuest/PerTable/PerRoom/Manual hoạt động chính xác
3. **TableAllocationEngine**: Chia bàn thông minh (bàn đầy + bàn lẻ)
4. **Bàn Tổng virtual**: Đúng — không phải bàn thật, dùng để tổng hợp
5. **Dịch vụ phòng tách biệt món**: `ExtraServices` ≠ `OrderLines`
6. **Gửi bếp + xuất kho**: Có đầy đủ, tracking trạng thái
7. **Deposit + CashFund tích hợp**: Đúng nghiệp vụ kế toán
8. **Reservation tự động**: Khi duyệt quote hoặc configure walk-in
9. **Walk-in flow**: Hỗ trợ đầy đủ (OpenRoomWalkIn → Configure → ServingPlan)
10. **`BanquetMenuPackage` dùng chung**: Không cần tạo entity mới cho ALC

---

# KHUYẾN NGHỊ HÀNH ĐỘNG

## P0 — Sửa ngay (ảnh hưởng tiền / nghiệp vụ cốt lõi)

| # | Hành động | Lý do |
|---|---|---|
| P0.1 | **Hỗ trợ partial payment** | `PayOrderAsync` cần cho phép `paidAmount < RemainingAmount`, cập nhật `PaidAmount +=`, `RemainingAmount -=` |
| P0.2 | **Cho phép thanh toán nhiều lần** | Tạo nhiều `Payment` record thay vì 1 lần duy nhất |

## P1 — Làm sau (hoàn thiện trải nghiệm)

| # | Hành động | Lý do |
|---|---|---|
| P1.1 | **UI chỉnh sửa phân bổ bàn trong quote** | Người dùng cần override engine |
| P1.2 | **Tách ticket bếp theo bàn** | In riêng từng bàn cho bếp |
| P1.3 | **Tự động post Stock Issue** | Khi gửi bếp → tự động duyệt xuất kho |
| P1.4 | **Populate ServiceCharge** | Field có sẵn, chỉ cần tính trong `Recalculate` |

## P2 — Cân nhắc sau

| # | Hành động | Lý do |
|---|---|---|
| P2.1 | **Configurable Bàn Tổng name** | Hardcoded → config |
| P2.2 | **Flow tăng/giảm khách sau duyệt** | Cần re-calculate + re-distribute |
| P2.3 | **Re-query giá món khi expand package** | Tránh stale price |

## KHÔNG CẦN ĐỤNG — Settings

Các tab Settings sau **không ảnh hưởng đến flow chính**, có thể để sau:

- Tab 2: Máy in / Bếp bar (cấu hình in — P2)
- Tab 6-8: Thanh toán / Đặt cọc / Hóa đơn (settings — P2, lưu vào BranchSettings)
- Tab 9-10: Phân quyền / Đồng bộ (XÓA BỎ hoặc để P3)
- Tab 11: Thông tin nhà hàng (P2, migrate sang Company/Branch)

---

# KẾT LUẬN

**Module À La Carte VIP đã có nền móng vững chắc hơn dự kiến.**

13/13 bước trong luồng end-to-end đều là **REAL**, không có mockup. Các vấn đề còn lại tập trung ở:

1. **Thanh toán** (P0): Chưa hỗ trợ partial/split payment — đây là gap nghiệp vụ quan trọng nhất
2. **UX hoàn thiện** (P1): Chỉnh sửa phân bổ bàn, tách ticket bếp, tự động post xuất kho
3. **Edge cases** (P2): Tăng/giảm khách, config Bàn Tổng, re-query giá

**Không có sai nghiệp vụ nào gây mất tiền hoặc sai số lượng món trong flow chính.**
