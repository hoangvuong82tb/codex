# ALACARTE_VIP_GAP_ANALYSIS — V1

## So sánh Blueprint Nghiệp vụ V3 vs Module À La Carte đang triển khai

Ngày: 23/06/2026

---

# PHẦN 1: TÓM TẮT BLUEPRINT

## Mục tiêu module

Quản lý dịch vụ **đặt phòng ăn uống cao cấp** — khách thuê phòng riêng (VIP, karaoke, hội họp nhỏ), đặt suất hoặc gọi món tự do, hệ thống tự động chia bàn theo số khách, phục vụ theo bàn, thanh toán tổng tại Bàn Tổng.

## Đối tượng sử dụng

- Nhân viên nhà hàng (tạo báo giá, mở đơn walk-in)
- Giám sát / Quản lý (duyệt báo giá, giảm giá, hủy đơn)
- Bếp trưởng (tạo suất, quản lý menu)
- Thu ngân (thanh toán)

## Khác biệt với POS Bữa sáng

| Tiêu chí | POS | À La Carte |
|---|---|---|
| Đơn vị đặt | Bàn | **Phòng** |
| Bàn | Cố định | **Sinh từ số khách (gán vào vị trí)** |
| Menu | Gọi tự do từng món | **Suất (tập hợp món) + gọi tự do** |
| Dịch vụ | Không | **Cấp phòng (MC, âm thanh, LED...)** |
| Thanh toán | Từng bàn | **Bàn Tổng (gộp tất cả bàn + dịch vụ)** |
| Flow | Vào → Gọi → Trả tiền | **Báo giá → Duyệt → Mở → Phục vụ → TT** |

## Khác biệt với Nhà hàng Tiệc

| Tiêu chí | Tiệc | À La Carte |
|---|---|---|
| Menu | Set menu cố định, không đổi | **Suất có thể tùy biến, thêm bớt món** |
| Bàn | Tất cả bàn giống hệt nhau | **Các bàn KHÔNG giống (10 khách ≠ 5 khách)** |
| Giá | Theo bàn hoặc theo hợp đồng | **Theo đầu người (PerGuest) hoặc theo phòng (PerRoom)** |
| Quy mô | Lớn (50-500 khách) | **Vừa và nhỏ (10-80 khách/phòng)** |

---

# PHẦN 2: PHÂN TÍCH CHỨC NĂNG CỐT LÕI

## Đánh giá mức độ quan trọng

### 1. Quản lý phòng — CRITICAL

**Lý do**: Không có phòng thì không có gì để đặt. Đây là entity nền tảng.

**Hiện trạng**: Đã có đầy đủ (DiningRoom, DiningArea/RoomType, CRUD, trạng thái động).

### 2. Quản lý lịch phòng — HIGH

**Lý do**: Tránh trùng lịch là bắt buộc với nhà hàng cao cấp. Nhưng có thể làm sau P0 vì flow chính (báo giá → đơn mở) vẫn chạy được nếu không có calendar view.

**Hiện trạng**: Có Reservation entity + validate trùng 4h. Thiếu calendar view.

### 3. Báo giá — CRITICAL

**Lý do**: Flow cốt lõi của À La Carte. Không có báo giá → không khác gì POS.

**Hiện trạng**: Đã có đầy đủ (Quote CRUD, duyệt, Quote→Order conversion). Thiếu ServingSet integration.

### 4. Suất — HIGH

**Lý do**: Là điểm khác biệt chính với POS. Nhưng module vẫn chạy được nếu chỉ có gọi món tự do (PerGuest/PerTable/PerRoom đã có).

**Hiện trạng**: Mockup ở UI ALC. BanquetMenuPackage có thật nhưng chưa tích hợp với ALC Quote.

### 5. Dịch vụ — HIGH

**Lý do**: Dịch vụ cấp phòng là đặc trưng của À La Carte. Đã có entity + service + seeding 11 dịch vụ.

**Hiện trạng**: Đã có đầy đủ (RestaurantExtraService, gán vào order, phân biệt với món).

### 6. Chia bàn — CRITICAL

**Lý do**: Không có chia bàn → không thể phục vụ nhiều bàn trong 1 phòng.

**Hiện trạng**: Đã có TableAllocationEngine + QuoteTable + OrderTable. Hoạt động thật.

### 7. Đơn mở — CRITICAL

**Lý do**: Đầu ra của flow báo giá. Nơi diễn ra phục vụ và thanh toán.

**Hiện trạng**: Đã có đầy đủ (single-table + walk-in room + Bàn Tổng ảo).

### 8. Phục vụ — HIGH

**Lý do**: Cần cho vận hành thực tế. Có thể làm sau P0.

**Hiện trạng**: Có OrderFoodLine, gửi bếp (KitchenTicket), thêm/hủy món.

### 9. Thanh toán — CRITICAL

**Lý do**: Không thanh toán được → không có doanh thu.

**Hiện trạng**: Có PayOrderAsync. Thiếu split payment, invoice.

### 10. Đặt cọc — HIGH

**Lý do**: Quan trọng với nhà hàng cao cấp. Có thể làm sau P0.

**Hiện trạng**: Có RestaurantDeposit + AddDepositAsync + tích hợp CashFund.

### 11. Báo cáo — LOW

**Lý do**: Cần cho quản lý nhưng không block vận hành.

**Hiện trạng**: Có Dashboard KPIs (6 chỉ số thật từ DB). Thiếu báo cáo chi tiết.

---

# PHẦN 3: PHÂN TÍCH MOCKUP VS CHỨC NĂNG THẬT

## Dashboard

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **THẬT** |
| Bằng chứng | 6 KPIs từ `GetDashboardAsync()` — ActiveRooms, ServingTables, TodayReservations, TodayRevenue, TodayDeposits, ExpectedGuests — tất cả là query DB thật |
| Thiếu gì | Báo giá chờ duyệt, tỉ lệ lấp đầy phòng |
| Rủi ro | Thấp |

## Báo giá

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **HỖN HỢP** (80% thật, 20% mockup) |
| Phần THẬT | Quote CRUD, QuoteLine với PricingRule, TablePlan, Duyệt báo giá → sinh Đơn mở (`ApproveQuoteAsync`), Quote → Contract |
| Phần MOCKUP | Tab "Menu có sẵn" (Set tiệc cơ bản/VIP/Hải sản/Lẩu) — hardcoded trong XAML, không có command binding, không kết nối backend |
| Thiếu gì | Tích hợp ServingSet/BanquetMenuPackage vào Quote. Khả năng vừa chọn suất vừa thêm món lẻ |
| Rủi ro | Trung bình — flow chính chạy được, chỉ thiếu suất |

## Đơn mở

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **THẬT** |
| Bằng chứng | `OpenOrderAsync` (đơn lẻ), `OpenRoomWalkInOrderAsync` (walk-in phòng), `CreateOrderForQuoteAsync` (từ báo giá). Bàn Tổng là `RestaurantOrderTable` với IsVirtual=true |
| Thiếu gì | Split payment (thanh toán nhiều lần). Invoice generation |
| Rủi ro | Thấp |

## Thanh toán

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **THẬT — cơ bản** |
| Bằng chứng | `PayOrderAsync` — thanh toán toàn bộ, cập nhật trạng thái bàn, giải phóng reservation |
| Thiếu gì | Thanh toán nhiều lần (split). Nhiều phương thức trong 1 lần (multi-tender). Tự động tạo Invoice |
| Rủi ro | Trung bình — nhà hàng cao cấp thường cần thanh toán từng phần |

## Cài đặt

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **THẬT** |
| Bằng chứng | CRUD DiningRoom, DiningTable, RestaurantExtraService đầy đủ. Cấu hình VAT, ServiceCharge |
| Thiếu gì | Cấu hình thời hạn báo giá, % đặt cọc mặc định, hạn mức giảm giá. Cấu hình vị trí bàn (TableSlot) |
| Rủi ro | Thấp |

## Quản lý phòng

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **THẬT — 90%** |
| Bằng chứng | CRUD phòng, khu vực, bàn. Trạng thái động từ DB |
| Thiếu gì | TableSlot (vị trí bàn vật lý). Calendar view lịch phòng |
| Rủi ro | Trung bình — thiếu TableSlot có thể gây khó khi cần sơ đồ phòng thực tế |

## Quản lý suất

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **MOCKUP (ALC) / THẬT (Banquet)** |
| Bằng chứng | `BanquetMenuPackage` có entity + service đầy đủ nhưng KHÔNG được tích hợp vào ALC Quote flow |
| Thiếu gì | Tích hợp BanquetMenuPackage vào ALC Quote. Hoặc tạo ServingSet riêng cho ALC. UI quản lý suất |
| Rủi ro | Cao — đây là điểm khác biệt chính giữa ALC và POS, hiện đang là mockup |

## Dịch vụ

| Tiêu chí | Đánh giá |
|---|---|
| Trạng thái | **THẬT** |
| Bằng chứng | 11 dịch vụ seed sẵn. Gán vào order. Phân biệt với món qua LineType. Quote→Order conversion xử lý đúng |
| Thiếu gì | Gói dịch vụ (bundle). Dịch vụ theo giờ |
| Rủi ro | Thấp |

---

# PHẦN 4: GAP ANALYSIS

| # | Chức năng | Blueprint yêu cầu | Thực tế codebase | Khoảng cách | Mức độ |
|---|---|---|---|---|---|
| 1 | **ServingSet / Suất** | Entity Suất riêng, gồm nhiều món, gán vào Quote, có versioning | BanquetMenuPackage có thật nhưng không tích hợp ALC. Tab "Menu có sẵn" trong XAML là mockup | **Lớn** — cần tích hợp hoặc tạo mới | CRITICAL |
| 2 | **TableSlot** | Vị trí bàn vật lý trong phòng, gán OrderTable vào | Không có entity. Bàn được sinh động hoặc dùng bàn có sẵn | **Lớn** — ảnh hưởng sơ đồ phòng | HIGH |
| 3 | **Calendar / Lịch phòng** | Xem phòng trống/đặt theo giờ, tránh trùng | Có Reservation + validate trùng 4h nhưng không có calendar view | **Trung bình** — flow chính vẫn chạy | HIGH |
| 4 | **Split Payment** | Thanh toán nhiều lần, nhiều phương thức | `PayOrderAsync` thanh toán toàn bộ 1 lần | **Trung bình** — cần cho nhà hàng cao cấp | HIGH |
| 5 | **Invoice Generation** | Tự động tạo hóa đơn khi thanh toán | Không có Invoice entity hay service | **Trung bình** | MEDIUM |
| 6 | **Đặt cọc nhiều lần** | Khách đặt cọc nhiều đợt | `AddDepositAsync` có nhưng chưa rõ hỗ trợ nhiều lần | **Nhỏ** | MEDIUM |
| 7 | **Hủy đơn + phí hủy** | Tính phí hủy, hoàn cọc theo chính sách | CancelFeePercent có trên entity nhưng không có logic tính/trừ | **Trung bình** | MEDIUM |
| 8 | **No-show handling** | Tự động đánh dấu no-show, xử lý cọc | Không có | **Trung bình** | MEDIUM |
| 9 | **Tăng/giảm khách sau duyệt** | Mở lại Quote, tính lại, duyệt lại | Chưa có flow | **Trung bình** | MEDIUM |
| 10 | **Đổi phòng sau duyệt** | Chuyển Quote + Order sang phòng mới | Chưa có flow | **Trung bình** | MEDIUM |
| 11 | **Ghép/tách bàn** | Gộp 2 OrderTable hoặc tách 1 thành 2 | Chưa có service method | **Trung bình** | MEDIUM |
| 12 | **Phân quyền** | Giới hạn giảm giá, duyệt theo vai trò | Chưa có | **Trung bình** | MEDIUM |
| 13 | **Báo cáo doanh thu** | Doanh thu theo phòng, theo suất, theo ngày | Chỉ có 6 KPI Dashboard | **Trung bình** | LOW |
| 14 | **Gửi email báo giá** | Gửi báo giá qua email cho khách | Không có | **Nhỏ** | LOW |
| 15 | **Giá vốn** | Tích hợp Module Định lượng để tính giá vốn suất/món | Chưa tích hợp | **Trung bình** | MEDIUM |
| 16 | **Stock Consumption tự động** | Xuất kho tự động khi phục vụ | Có `CreateConsumptionIssuesAsync` nhưng tạo Draft, không tự động post | **Nhỏ** | MEDIUM |
| 17 | **ServiceCharge** | Tự động tính phí phục vụ | Field có nhưng `Recalculate()` không populate | **Nhỏ** — dễ sửa | LOW |
| 18 | **In hóa đơn VAT** | Hóa đơn đỏ | Chưa có | **Trung bình** | MEDIUM |
| 19 | **Bàn Tổng** | Nơi tổng hợp thanh toán | **ĐÃ CÓ** — `RestaurantOrderTable` với IsVirtual=true, DisplayName="Bàn Tổng" | **Không có gap** | — |
| 20 | **Quote → Order conversion** | Duyệt báo giá → tự động sinh đơn mở + chia món + sinh bàn | **ĐÃ CÓ** — `ApproveQuoteAsync` + `CreateOrderForQuoteAsync` đầy đủ | **Không có gap** | — |
| 21 | **PerGuest/PerTable/PerRoom** | Tính số lượng món theo quy tắc | **ĐÃ CÓ** — `NormalizePricingRule` + `IMenuCalculationEngine` | **Không có gap** | — |
| 22 | **Walk-in flow** | Mở đơn không qua báo giá | **ĐÃ CÓ** — `OpenRoomWalkInOrderAsync` + `ConfigureRoomWalkInOrderAsync` | **Không có gap** | — |
| 23 | **Dịch vụ phòng** | Dịch vụ cấp phòng, không chia theo bàn | **ĐÃ CÓ** — `RestaurantExtraService` + phân biệt LineType | **Không có gap** | — |
| 24 | **Gửi bếp** | In ticket bếp riêng từng bàn | **ĐÃ CÓ** — `RestaurantPreparationTicket` + `SendToPreparationAsync` | **Không có gap** | — |

---

# PHẦN 5: PHÂN TÍCH RỦI RO KIẾN TRÚC

## 10 sai lầm phổ biến nhất — và hiện trạng trong codebase

### Sai lầm 1: Dùng DiningTable như POS

**Mô tả**: Mở đơn gắn với 1 bàn cố định, không có khái niệm phòng + nhiều bàn.

**Hiện trạng**: **ĐÃ TRÁNH ĐƯỢC**. Codebase có cả 2 flow: `OpenOrderAsync` (đơn lẻ, giống POS) và `OpenRoomWalkInOrderAsync` (phòng + nhiều bàn + Bàn Tổng). Quote→Order conversion cũng tạo nhiều bàn.

**Hậu quả nếu mắc**: Module không khác gì POS. Không phục vụ được tiệc phòng.

---

### Sai lầm 2: Không có Reservation

**Mô tả**: Mở đơn không qua đặt trước, không kiểm tra trùng lịch.

**Hiện trạng**: **ĐÃ TRÁNH ĐƯỢC**. Có `TableReservation` entity, validate trùng 4h, tự động tạo khi duyệt báo giá hoặc configure walk-in.

---

### Sai lầm 3: Không có Bàn Tổng

**Mô tả**: Thanh toán từng bàn riêng lẻ, không tổng hợp được dịch vụ phòng.

**Hiện trạng**: **ĐÃ TRÁNH ĐƯỢC**. `RestaurantOrderTable` với `IsVirtual=true` và `DisplayName="Bàn Tổng"`. Dịch vụ PerRoom được gán vào bàn tổng khi convert từ Quote.

---

### Sai lầm 4: Không có PerGuest / PerTable / PerRoom

**Mô tả**: Tất cả món tính theo 1 công thức (thường là Manual).

**Hiện trạng**: **ĐÃ TRÁNH ĐƯỢC**. `NormalizePricingRule` + `IMenuCalculationEngine` + `ITableAllocationEngine`. 3 quy tắc hoạt động thật.

---

### Sai lầm 5: Gắn dịch vụ vào bàn

**Mô tả**: Dịch vụ phòng (MC, âm thanh) bị chia đều xuống từng bàn.

**Hiện trạng**: **ĐÃ TRÁNH ĐƯỢC**. `QuoteLine.LineType` phân biệt "Món ăn" và "Dịch vụ". Khi convert Quote→Order, dịch vụ được tạo thành `RestaurantOrderExtraService` (cấp order) thay vì `AlacarteOrderLine` (cấp bàn).

---

### Sai lầm 6: Không có Quote → Order conversion

**Mô tả**: Báo giá chỉ là "tờ giấy", không sinh được đơn mở.

**Hiện trạng**: **ĐÃ TRÁNH ĐƯỢC**. `ApproveQuoteAsync` tự động: tạo Reservation → tạo Order → sinh OrderTable → chia món theo PricingRule → tạo Bàn Tổng → gán dịch vụ.

---

### Sai lầm 7: Suất/Menu package là mockup

**Mô tả**: UI hiển thị suất nhưng không có backend.

**Hiện trạng**: **ĐANG MẮC**. Tab "Menu có sẵn" trong Quote XAML là mockup. `BanquetMenuPackage` có thật nhưng chưa tích hợp.

**Hậu quả**: Không thể tạo báo giá từ suất có sẵn. Phải thêm từng món thủ công.

---

### Sai lầm 8: Bàn cố định, không linh hoạt

**Mô tả**: Phòng có N bàn cố định, không thay đổi được theo số khách.

**Hiện trạng**: **HỖN HỢP**. Có bàn vật lý (DiningTable) + bàn ảo (QuoteTable/OrderTable với IsVirtual). Nhưng chưa có TableSlot để ánh xạ bàn ảo → vị trí thật.

**Hậu quả**: Nhà bếp không biết "Bàn 01" nằm ở đâu trong phòng.

---

### Sai lầm 9: Không có hủy đơn / no-show

**Mô tả**: Không xử lý được trường hợp khách hủy hoặc không đến.

**Hiện trạng**: **ĐANG MẮC MỘT PHẦN**. Có CancelFeePercent trên entity nhưng không có logic tính phí hủy. Không có no-show detection.

---

### Sai lầm 10: Thanh toán chỉ 1 lần

**Mô tả**: Không hỗ trợ thanh toán từng phần (đặt cọc → thanh toán đợt 1 → đợt 2).

**Hiện trạng**: **ĐANG MẮC**. `PayOrderAsync` thanh toán toàn bộ. Có `AddDepositAsync` cho đặt cọc nhưng không có partial payment.

---

# PHẦN 6: ĐÁNH GIÁ ROADMAP HIỆN TẠI

## Điểm số roadmap Blueprint

| Priority | Hạng mục | Đánh giá | Điểm |
|---|---|---|---|
| **P0** | Data model (Quote, QuoteSetLine, etc.) | ĐÚNG. Quote CRUD + conversion đã có. Thiếu ServingSet integration | 8/10 |
| **P0** | Quote CRUD | ĐÚNG. Đã có đầy đủ | 10/10 |
| **P0** | Tính giá báo giá (PerGuest/PerTable/PerRoom) | ĐÚNG. Đã có engine | 10/10 |
| **P0** | Duyệt báo giá → Sinh Đơn mở | ĐÚNG. Đã có ApproveQuoteAsync | 10/10 |
| **P0** | Chia bàn tự động | ĐÚNG. TableAllocationEngine đã có | 9/10 |
| **P0** | Sơ đồ phòng + Lọc khu vực | ĐÚNG. Đã có UI + service | 9/10 |
| **P1** | Quản lý Suất | ĐANG LÀM QUÁ MUỘN — đáng lẽ phải P0 vì là điểm khác biệt chính | 3/10 |
| **P1** | Bàn Tổng + Thanh toán tổng | ĐÚNG. Đã có nhưng cần split payment | 7/10 |
| **P1** | Đặt cọc | ĐÚNG. Đã có deposit | 8/10 |
| **P1** | Walk-in flow | ĐÚNG. Đã có OpenRoomWalkInOrderAsync | 10/10 |
| **P1** | Lịch phòng | ĐÚNG. Cần calendar view | 6/10 |
| **P2** | Chuyển món, ghép bàn, tách bàn | ĐÚNG. Cần làm sau | 5/10 |
| **P2** | In bếp riêng từng bàn | ĐÃ CÓ SỚM HƠN DỰ KIẾN. PreparationTicket đã hoạt động | 8/10 |
| **P2** | Phân quyền | ĐÚNG. Cần sau | — |
| **P3** | Báo cáo | ĐÚNG. Cần sau | — |

## Nhận xét

1. **P0 nhìn chung ĐÚNG**. Những thứ quan trọng nhất đã được làm trước (Quote flow, conversion, chia bàn, PerGuest).

2. **Quản lý Suất (P1) đang bị làm quá muộn**. Đây là điểm khác biệt CHÍNH giữa ALC và POS. Nếu không có suất, module ALC không khác gì POS gọi món tự do trong phòng. Nên nâng lên P0.

3. **In bếp (P2) đã có sớm hơn dự kiến**. `RestaurantPreparationTicket` + `SendToPreparationAsync` đã hoạt động. Đây là điểm tốt — làm sớm giúp module có thể demo end-to-end.

4. **TableSlot (không có trong roadmap)** — thiếu. Cần bổ sung vào P1.

---

# PHẦN 7: 20 ĐIỀU TUYỆT ĐỐI KHÔNG NÊN LÀM

| # | Điều không nên làm | Lý do | Hiện trạng |
|---|---|---|---|
| 1 | **Không biến ALC thành POS** | ALC đặt theo phòng, POS đặt theo bàn. Nếu gộp → mất bản sắc cả 2 module | Đã tránh được (có flow riêng) |
| 2 | **Không biến ALC thành Tiệc** | Tiệc có set menu cứng, các bàn giống hệt. ALC linh hoạt hơn | Đã tránh được |
| 3 | **Không xóa flow walk-in** | Walk-in là nhu cầu thực tế. Không phải lúc nào cũng có báo giá | Đã giữ được |
| 4 | **Không bỏ Bàn Tổng** | Thiếu Bàn Tổng → không thanh toán tổng được → sai logic | Đã có |
| 5 | **Không bỏ PerGuest/PerTable/PerRoom** | Nếu chỉ có Manual → mất khả năng tự động tính số lượng | Đã có |
| 6 | **Không gộp dịch vụ phòng vào món bàn** | MC 5 triệu không phải chi phí bàn 01 | Đã tách được |
| 7 | **Không tạo định lượng riêng cho ALC** | Module Định lượng đã có. Tận dụng, không phát minh lại | OK |
| 8 | **Không tạo module khách hàng riêng** | Dùng chung Customer entity nếu có, hoặc giữ thông tin khách trong Quote | OK |
| 9 | **Không tạo module kế toán riêng** | Dùng chung CashFund, LedgerAccount có sẵn | OK |
| 10 | **Không tạo module kho riêng** | Dùng chung StockService, StockIssue có sẵn | OK |
| 11 | **Không hardcode suất trong XAML** | Suất phải quản lý được, không phải text tĩnh | **ĐANG VI PHẠM** — tab "Menu có sẵn" |
| 12 | **Không để Bàn Tổng là bàn thật** | Bàn Tổng là logic, không phải nơi khách ngồi | Đã đúng (IsVirtual=true) |
| 13 | **Không bắt buộc báo giá cho mọi đơn** | Walk-in là nhu cầu thực tế | Đã hỗ trợ |
| 14 | **Không để service tính giá kiêm luôn UI** | Tách biệt engine tính giá khỏi ViewModel | Đã tách (IAlacarteVipEngines) |
| 15 | **Không để DB context trong ViewModel** | Luôn qua service | Đã đúng |
| 16 | **Không bỏ qua validation trùng lịch** | Double-booking là lỗi nghiêm trọng | Đã có |
| 17 | **Không để thanh toán không qua Bàn Tổng** | Nếu thanh toán từng bàn → sai tổng, thiếu dịch vụ | Cần đảm bảo UI chỉ cho thanh toán tại Bàn Tổng |
| 18 | **Không tạo entity trùng lặp giữa ALC và Banquet** | `BanquetMenuPackage` có thể dùng chung, không cần tạo `ServingSet` riêng nếu không khác biệt | Cần quyết định |
| 19 | **Không để QuoteLine và OrderLine khác cấu trúc** | Khi convert Quote→Order, structure phải tương thích | Đã tương thích |
| 20 | **Không code tiếp UI khi data model chưa đúng** | Sửa data model sau khi có UI → vứt hết UI cũ | **CẢNH BÁO**: cần quyết định TableSlot + ServingSet integration trước khi làm thêm UI |

---

# PHẦN 8: KẾT LUẬN

## Gửi đội phát triển

Anh em,

Tôi vừa audit toàn bộ module À La Carte. Dưới đây là đánh giá thẳng thắn.

### ĐIỂM TỐT — nhiều hơn tôi mong đợi

1. **Quote → Order conversion hoạt động thật**. Đây là phần khó nhất và các bạn đã làm xong. `ApproveQuoteAsync` tự động tạo Reservation + Order + chia bàn + chia món + Bàn Tổng. Rất tốt.

2. **PerGuest / PerTable / PerRoom đã có engine thật**. `IMenuCalculationEngine` và `ITableAllocationEngine` hoạt động. Không phải mockup.

3. **Bàn Tổng đã được implement đúng** — là `RestaurantOrderTable` ảo (IsVirtual=true), không phải bàn thật. Dịch vụ phòng được gán đúng vào Bàn Tổng khi convert.

4. **Walk-in flow đã có** — `OpenRoomWalkInOrderAsync` + `ConfigureRoomWalkInOrderAsync`. Khách đến bất ngờ vẫn phục vụ được.

5. **Dashboard KPIs là thật** — 6 chỉ số từ DB, không phải số cứng.

6. **Dịch vụ phòng đã tách biệt với món** — `RestaurantOrderExtraService` (cấp order) khác với `AlacarteOrderLine` (cấp bàn).

7. **Gửi bếp đã có** — `RestaurantPreparationTicket` hoạt động.

### ĐIỂM CẦN SỬA GẤP (P0)

**Chỉ có 1 thứ CRITICAL cần làm ngay trước khi code tiếp:**

> **Tích hợp ServingSet / Suất vào Quote flow.**

Hiện tại tab "Menu có sẵn" là mockup. `BanquetMenuPackage` có entity thật nhưng chưa nối vào ALC. Nếu không có suất, ALC không khác gì POS gọi món trong phòng.

**2 lựa chọn:**
- A) Dùng chung `BanquetMenuPackage` cho cả ALC và Banquet (nếu cấu trúc giống)
- B) Tạo `ServingSet` riêng cho ALC (nếu cần khác biệt)

Quyết định dứt khoát 1 trong 2 trước khi làm gì khác.

### ĐIỂM CẦN LÀM SAU P0 (P1)

1. **TableSlot** — cần entity ánh xạ bàn ảo → vị trí thật. Không có cái này, nhà bếp không biết "Bàn 01" ở đâu.
2. **Calendar view lịch phòng** — đã có Reservation entity, chỉ thiếu UI.
3. **Split payment** — thanh toán nhiều lần, nhiều phương thức.
4. **ServiceCharge** — field có rồi, chỉ cần populate trong `Recalculate()`.

### NHỮNG GÌ ĐANG LÀM ĐÚNG — TIẾP TỤC

- Quote flow (tạo → gửi → duyệt → sinh đơn)
- PerGuest/PerTable/PerRoom pricing
- Chia bàn tự động
- Walk-in flow
- Dịch vụ phòng (tách biệt món)
- Gửi bếp
- Dashboard KPIs

### MỘT LỜI KHUYÊN

Trước khi viết thêm bất kỳ dòng code nào:

1. **Quyết định xong ServingSet integration**
2. **Quyết định xong mô hình TableSlot**
3. **KHÔNG thêm UI mới cho đến khi 2 quyết định trên được implement vào data model**

Nếu tiếp tục thêm UI mà không có nền móng đúng, tất cả sẽ phải làm lại. Các bạn đã làm rất tốt phần khó nhất (Quote→Order conversion). Đừng để mấy cái mockup làm hỏng cả module.

— Chief Architect
