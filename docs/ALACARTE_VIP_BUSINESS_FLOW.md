# ALACARTE VIP — PHÂN TÍCH NGHIỆP VỤ & THIẾT KẾ GIẢI PHÁP

## Ngày: 2026-06-18
## Vai trò: Business Analyst / Solution Architect

---

## 1. HIỂU ĐÚNG NGHIỆP VỤ À LA CARTE VIP

### Định nghĩa

Nhà hàng À La Carte VIP là mô hình **phục vụ cao cấp theo phòng riêng**, khách thuê **nguyên phòng VIP**, nhà hàng phục vụ theo **suất/người** cho món đắt tiền và **theo bàn/phòng** cho món chia sẻ. Đây KHÔNG phải POS bàn lẻ, KHÔNG phải tiệc cưới menu cố định.

### Đặc trưng cốt lõi

| Đặc điểm | Mô tả |
|---|---|
| Đơn vị kinh doanh | **Phòng VIP** (không phải bàn lẻ) |
| Cách tính món | **4 quy tắc**: Theo người, Theo bàn, Theo phòng, Nhập tay |
| Chia bàn | **Tự động** từ số khách + chuẩn bàn (6/8/10/12) |
| Bàn lẻ | **Luôn có thể tồn tại** (bàn 6, 7, 11 người...) |
| Bàn tổng | **Bàn ảo** để tổng hợp thanh toán |
| Dịch vụ | **Theo phòng**, không theo bàn |
| Phát sinh | **Gắn theo bàn thật**, tổng về bàn tổng |

---

## 2. SO SÁNH 3 MODULE

| Tiêu chí | Bán đồ ăn sáng & Cà phê | À La Carte VIP | Nhà hàng Tiệc |
|---|---|---|---|
| Đơn vị phục vụ | Bàn lẻ | Phòng VIP | Sảnh/Mâm |
| Đặt trước | Không bắt buộc | Có (báo giá) | Bắt buộc |
| Chia bàn | Bàn cố định DB | Tự sinh theo số khách | Bàn cố định DB |
| Cách tính món | 1 giá/bàn | 4 quy tắc (người/bàn/phòng/tay) | Cố định/mâm |
| Dịch vụ | Không | Theo phòng | Theo sự kiện |
| Thanh toán | Từng bàn | Bàn tổng (phòng) | Hợp đồng tổng |
| Linh hoạt số khách | Không cần | Rất cao (đổi phút cuối) | Thấp (cố định) |
| Báo giá | Không | Có | Có (hợp đồng) |
| Phát sinh thêm | Gọi thêm món | Gắn theo bàn thật | Thêm mâm/bàn |

---

## 3. QUY TRÌNH BÁO GIÁ → DUYỆT → ĐƠN MỞ → PHỤC VỤ → THANH TOÁN

### Bước 1: Tạo báo giá

**Đầu vào:**
- Khách hàng, SĐT
- Ngày phục vụ, giờ bắt đầu, giờ kết thúc dự kiến
- Phòng VIP (chọn từ danh sách phòng khả dụng)
- Số khách dự kiến
- Chuẩn chia bàn (6/8/10/12/tùy chỉnh)
- Ghi chú đặc biệt (dị ứng, ăn chay...)

**Hệ thống tự động:**
- Tính số bàn = ceil(số khách / chuẩn bàn)
- Phân bổ khách vào các bàn
- Bàn cuối có thể ít hơn chuẩn (bàn lẻ)

### Bước 2: Chọn món

Mỗi món chọn phải gắn **quy tắc tính số lượng**:

| Quy tắc | Ký hiệu | Ví dụ | Cách tính |
|---|---|---|---|
| Theo người | `PER_PERSON` | Tôm hùm 1 con/người | SL = số khách × định mức |
| Theo bàn | `PER_TABLE` | Lẩu 1 nồi/bàn | SL = số bàn × định mức |
| Theo phòng | `PER_ROOM` | Màn LED 1 gói/phòng | SL = 1 × định mức |
| Nhập tay | `MANUAL` | Rượu vang 5 chai | SL = số nhập |

### Bước 3: Hệ thống tự chia bàn & món

**Ví dụ 1: 31 khách, bàn chuẩn 10**
```
Bàn 01: 10 khách → Tôm hùm 10 con, Lẩu 1 nồi, Salad 1 đĩa
Bàn 02: 10 khách → Tôm hùm 10 con, Lẩu 1 nồi, Salad 1 đĩa
Bàn 03: 11 khách → Tôm hùm 11 con, Lẩu 1 nồi, Salad 1 đĩa
Phòng VIP 1: Màn LED 1 gói
```

**Ví dụ 2: 46 khách, bàn chuẩn 10**
```
Bàn 01: 10 khách
Bàn 02: 10 khách
Bàn 03: 10 khách
Bàn 04: 10 khách
Bàn 05: 6 khách
```

### Bước 4: Dịch vụ bổ sung

Tất cả dịch vụ tính **PER_ROOM**:
- Màn LED, Âm thanh, Ánh sáng
- MC, Ca sĩ
- Trang trí, Hoa tươi
- Máy chiếu, Photobooth
- Phí phòng VIP, Phí phục vụ

### Bước 5: Lưu & Duyệt báo giá

**Trạng thái báo giá:**
```
Nháp → Đã gửi khách → Khách yêu cầu sửa → Chờ duyệt → Đã duyệt
                                                      ↘ Đã hủy
                                                      ↘ Hết hạn
```

**Khi duyệt:**
- Tạo **Đơn mở đặt trước** (status = Reserved)
- Sinh danh sách bàn phục vụ
- Sinh bàn tổng (ảo)
- Tự động chia món theo bàn

### Bước 6: Phục vụ thật

Khi khách đến:
- Đơn chuyển `Reserved → Serving`
- Có thể điều chỉnh số khách thực tế
- Có thể chia lại bàn
- Có thể thêm phát sinh theo bàn

### Bước 7: Thanh toán

Tại **Bàn Tổng**:
```
Tạm tính món (theo người × số khách thực tế)
+ Tạm tính món (theo bàn × số bàn)
+ Tạm tính món (theo phòng)
+ Dịch vụ phòng
+ Phát sinh từng bàn thật
+ VAT (8%)
+ Phí phục vụ
- Giảm giá
- Đặt cọc
= Còn phải thu
```

---

## 4. QUY TRÌNH KHÁCH ĂN NGAY

```
Chọn phòng VIP → Nhập số khách → Chọn chuẩn bàn
→ Hệ thống sinh bàn → Chọn món (4 quy tắc)
→ Phục vụ ngay → Thanh toán bàn tổng
```

Khác với POS cà phê:
- **Bắt buộc** chọn phòng (không bán bàn lẻ)
- **Bắt buộc** nhập số khách
- **Bắt buộc** chia bàn trong phòng
- **Bắt buộc** thanh toán tổng phòng

---

## 5. MÔ HÌNH DỮ LIỆU ĐỀ XUẤT

### Entity mới cần có

```
VipRoom (đã có: DiningRoomDto)
  - Id, Code, Name, Capacity, Floor, Status
  - TableStandard (chuẩn bàn mặc định: 6/8/10/12)

QuoteTable (bàn trong báo giá - KHÔNG phải bàn cố định DB)
  - Id, QuoteId, TableNo, GuestCount
  - IsVirtual (true = bàn tổng)
  - Status

QuoteLine (dòng báo giá)
  - Id, QuoteId, MenuItemId, MenuItemName
  - PricingRule: PER_PERSON | PER_TABLE | PER_ROOM | MANUAL
  - UnitPrice, DefaultQty, ActualQty
  - Type: Food | Service

Quote (báo giá)
  - Id, QuoteNo, CustomerName, Phone
  - VipRoomId, GuestCount, TableStandard
  - EventDate, StartTime, EndTime
  - Status: Draft | Sent | Revision | PendingApproval | Approved | Cancelled | Expired
  - TotalAmount, DepositAmount, DiscountAmount
  - ApprovedOrderId (khi đã duyệt)

VipOrder (đơn mở - kế thừa từ báo giá)
  - Id, OrderNo, QuoteId (nullable)
  - VipRoomId, GuestCount
  - Status: Reserved | Serving | Completed | Paid | Cancelled
  - OrderType: Advance | WalkIn

OrderTable (bàn trong đơn)
  - Id, OrderId, TableNo, GuestCount
  - IsVirtual (bàn tổng)
  - IsSupplement (phát sinh thêm không có trong báo giá)

OrderLine (dòng đơn)
  - Id, OrderId, OrderTableId
  - MenuItemId, MenuItemName
  - PricingRule, Qty, UnitPrice, Amount
  - IsSupplement (phát sinh thêm)
  - LineType: Food | Service
```

### Entity hiện tại có thể tái sử dụng

| Entity hiện tại | Dùng cho |
|---|---|
| `DiningRoomDto` | Phòng VIP (thêm field TableStandard, Floor) |
| `MenuItemDto` | Món ăn (thêm field PricingRule mặc định) |
| `AlacarteOrderDto` | Đơn mở (cần thêm VipRoomId, GuestCount, OrderType) |
| `AlacarteOrderLineDto` | Dòng đơn (cần thêm PricingRule, OrderTableId) |
| `RestaurantContractDto` | Báo giá (cần đổi tên/mở rộng) |
| `AlacarteServiceItem` | Dịch vụ (giữ nguyên) |

### Entity/field còn thiếu

| Mục | Mô tả |
|---|---|
| `MenuItemDto.PricingRule` | Quy tắc tính món (PER_PERSON/PER_TABLE/PER_ROOM/MANUAL) |
| `MenuItemDto.DefaultQty` | Định mức mặc định (vd: 1 con/người, 1 nồi/bàn) |
| `Quote` entity | Báo giá với đầy đủ fields |
| `QuoteTable` entity | Bàn trong báo giá (sinh động, không cố định) |
| `QuoteLine.PricingRule` | Quy tắc tính cho từng dòng báo giá |
| `VipRoom.TableStandard` | Chuẩn bàn mặc định của phòng |
| `VipOrder.GuestCount` | Số khách thực tế |
| `VipOrder.OrderType` | Advance (đặt trước) / WalkIn (ăn ngay) |
| `OrderTable.IsVirtual` | Đánh dấu bàn tổng ảo |
| `OrderTable.IsSupplement` | Đánh dấu bàn phát sinh thêm |
| `OrderLine.PricingRule` | Quy tắc tính cho từng dòng đơn |
| `OrderLine.OrderTableId` | Dòng đơn thuộc bàn nào |

---

## 6. SERVICE METHOD CẦN CÓ

```
IQuoteService
  - CreateQuote(CreateQuoteRequest)
  - UpdateQuote(UpdateQuoteRequest)
  - AddQuoteLine(AddQuoteLineRequest)  // tự động tính Qty theo PricingRule
  - RemoveQuoteLine(quoteLineId)
  - AutoDivideTables(quoteId)          // chia bàn từ số khách + chuẩn
  - AutoCalculateLineQtys(quoteId)     // tính lại SL từng dòng theo bàn mới
  - SubmitQuote(quoteId)               // gửi khách
  - ApproveQuote(quoteId)              // duyệt → tạo VipOrder
  - CancelQuote(quoteId)
  - GetQuoteDetail(quoteId)
  - GetQuotes(filter)

IVipOrderService
  - CreateWalkInOrder(CreateWalkInRequest)  // khách ăn ngay
  - StartService(orderId)                   // bắt đầu phục vụ
  - UpdateGuestCount(orderId, newCount)     // đổi số khách thực tế
  - RedivideTables(orderId, newStandard)    // chia lại bàn
  - AddSupplementLine(orderId, tableId, request)  // thêm phát sinh
  - GetOrderDetail(orderId)
  - GetOrderTables(orderId)
  - CalculateTotals(orderId)               // tính tổng tiền (gồm phát sinh)
  - SettleOrder(orderId, paymentRequest)   // thanh toán
```

---

## 7. VIEWMODEL PROPERTY/COMMAND CẦN CÓ

```
QuoteViewModel
  Properties:
  - QuoteNo, CustomerName, Phone, EventDate, StartTime, EndTime
  - SelectedRoom, GuestCount, TableStandard
  - QuoteLines (ObservableCollection)
  - QuoteTables (ObservableCollection - auto-generated)
  - QuoteServices (ObservableCollection)
  - TotalAmount, DepositAmount, DiscountAmount, VatAmount
  - Status, CanEdit, CanApprove

  Commands:
  - AddFoodLine(menuItem)       // tự động set PricingRule từ menu item
  - AddServiceLine(serviceItem)
  - RemoveLine(line)
  - AutoDivideTables()          // gọi service chia bàn
  - SaveQuote()
  - SubmitQuote()
  - ApproveQuote()
  - PrintQuote()

VipOrderViewModel
  Properties:
  - OrderNo, QuoteNo (if from quote), Status
  - VipRoom, GuestCount, TableStandard
  - OrderTables (with IsVirtual flag)
  - SelectedTable (for supplement entry)
  - TotalAmount, PaidAmount, RemainingAmount

  Commands:
  - StartService()
  - UpdateGuestCount(newCount)
  - RedivideTables(newStandard)
  - AddSupplement(tableId, menuItem)
  - SettleOrder(paymentInfo)
  - PrintKitchen(tableId)
```

---

## 8. UI/TAB CẦN SỬA

| Tab hiện tại | Cần sửa thành |
|---|---|
| Khu vực | **Phòng VIP** — hiển thị phòng (không phải bàn lẻ), chip lọc tầng, card phòng với sức chứa + trạng thái |
| Báo giá | **Tạo báo giá** — form khách hàng + chọn phòng + nhập số khách + chuẩn bàn → auto chia bàn → chọn món (4 quy tắc) → dịch vụ → tổng kết |
| DS Báo giá | **Danh sách báo giá** — 3 cột: list / detail / bổ sung (giữ nguyên layout) |
| Đơn mở | **Đơn VIP** — hiển thị bàn tổng + danh sách bàn thật, click bàn để xem/phát sinh, tab món ăn/dịch vụ/thông tin |
| Dịch vụ | **Thực đơn & Dịch vụ** — giữ nguyên, thêm PricingRule cho mỗi món |

**Màn hình mới cần có:**
- **Sơ đồ bàn trong phòng**: Hiển thị trực quan các bàn đã chia, số khách mỗi bàn, trạng thái
- **Bàn tổng**: Luôn hiển thị ở panel phải khi chọn đơn, có tổng hợp tất cả

---

## 9. XỬ LÝ CÁC TÌNH HUỐNG BIÊN (EDGE CASES)

### 9.1 Khách đổi số lượng phút cuối

**Tình huống:** Báo giá 30 khách, ngày tổ chức 36 khách tới.

**Xử lý:**
1. Nhân viên mở đơn → chọn "Điều chỉnh số khách" → nhập 36
2. Hệ thống tự chia lại bàn:
   - Cũ: 10 + 10 + 10 (3 bàn)
   - Mới: 10 + 10 + 10 + 6 (4 bàn)
3. Hệ thống tự tính lại món PER_PERSON:
   - Cũ: Tôm hùm 30 con
   - Mới: Tôm hùm 36 con
4. Món PER_TABLE tự cập nhật:
   - Cũ: Lẩu 3 nồi
   - Mới: Lẩu 4 nồi
5. Món PER_ROOM không đổi
6. Dịch vụ PER_ROOM không đổi
7. Hiển thị chênh lệch: "+6 khách → +6 tôm hùm, +1 lẩu, +XX đ"
8. Nhân viên xác nhận → cập nhật đơn

**Lưu ý:** Nếu khách đến ÍT hơn (25 thay vì 30), hệ thống vẫn tính đủ món đã chuẩn bị, nhưng cho phép giảm nếu chưa chế biến.

### 9.2 Khách đổi phòng

**Tình huống:** Đã duyệt báo giá VIP 1, khách muốn đổi sang VIP 2.

**Xử lý:**
1. Kiểm tra VIP 2 có trống ngày giờ đó không
2. Nếu có:
   - Chuyển toàn bộ bàn + món + dịch vụ sang phòng mới
   - Cập nhật sức chứa phòng → có thể cần chia lại bàn nếu chuẩn khác
   - Giữ nguyên báo giá (chỉ đổi phòng)
3. Nếu VIP 2 nhỏ hơn:
   - Cảnh báo: "VIP 2 sức chứa X, không đủ cho Y khách"
   - Đề xuất: giảm khách hoặc chọn phòng khác
4. Nếu không có phòng trống:
   - Đề xuất ngày/giờ khác hoặc phòng khác

### 9.3 Ghép phòng

**Tình huống:** Khách muốn VIP 1 + VIP 2 ghép làm 1.

**Xử lý:**
1. Kiểm tra 2 phòng có liền kề/ghép được không (cấu hình phòng)
2. Nếu được:
   - Tạo đơn mới với `CombinedRoom = "VIP 1 + VIP 2"`
   - Sức chứa = VIP1.Capacity + VIP2.Capacity
   - Tự động chia bàn trên tổng sức chứa mới
   - Bàn được đánh dấu thuộc khu vực VIP 1 hay VIP 2 (để phục vụ)
3. Nếu không ghép được:
   - Tạo 2 đơn riêng, cùng khách hàng
   - Có flag `LinkedOrderId` để tổng hợp thanh toán

### 9.4 Trẻ em

**Tình huống:** 28 người lớn, 4 trẻ em.

**Xử lý:**
1. Hệ thống cho phép nhập: `AdultCount = 28, ChildCount = 4`
2. Món PER_PERSON có thể cấu hình:
   - `ChildPortionRate = 0.5` (trẻ em = 0.5 suất)
   - `ChildFreeUnderAge = 6` (dưới 6 tuổi miễn phí)
3. Tính toán:
   - Tôm hùm: 28 + (4 × 0.5) = 30 suất (nếu ChildPortionRate = 0.5)
   - Tôm hùm: 28 + 0 = 28 suất (nếu 4 trẻ đều dưới 6 tuổi)
4. Trẻ em vẫn tính vào số khách để chia bàn (vẫn cần chỗ ngồi)
5. Hiển thị rõ: "28 người lớn + 4 trẻ em (miễn phí/50%)"

### 9.5 Khách VIP / Bàn đặc biệt

**Tình huống:** Chủ tịch, đối tác ngồi bàn riêng.

**Xử lý:**
1. Đánh dấu 1 bàn là "VIP Table" / "Head Table"
2. Bàn này có thể có:
   - Menu riêng (thêm món đặc biệt)
   - Rượu riêng (phát sinh gắn vào bàn này)
   - Ghi chú: "Bàn Chủ tịch - phục vụ rượu vang đỏ"
3. Khi in bếp/bar: đánh dấu ưu tiên "VIP"
4. Phát sinh bàn VIP vẫn tổng về bàn tổng

### 9.6 Phát sinh ngoài báo giá

**Tình huống:** Trong lúc phục vụ, khách gọi thêm 5 tôm hùm, 1 chai vang.

**Xử lý:**

**Gắn vào bàn thật:**
1. Nhân viên chọn bàn (vd: Bàn 02) → "Thêm phát sinh"
2. Chọn món "Tôm hùm" → SL 5 → gắn vào Bàn 02
3. Chọn "Rượu vang" → SL 1 → gắn vào Bàn 02
4. Hệ thống:
   - Thêm dòng vào Bàn 02 với `IsSupplement = true`
   - Tổng tiền Bàn 02 tăng
   - Bàn Tổng tự động cập nhật

**Gắn vào phòng (dịch vụ):**
1. Nhân viên chọn "Thêm dịch vụ phòng"
2. Chọn "Thêm ca sĩ" → gắn vào phòng
3. Tính PER_ROOM, không chia theo bàn

**Quy tắc:**
- Món ăn/thức uống thêm → luôn gắn vào 1 bàn thật cụ thể
- Dịch vụ thêm → gắn vào phòng
- Tất cả tổng về Bàn Tổng

### 9.7 Thay đổi chia bàn giữa chừng

**Tình huống:** 31 khách, ban đầu chia 10+10+11, sau đổi thành 8+8+8+7.

**Xử lý:**
1. Nhân viên chọn "Chia lại bàn" → nhập chuẩn mới (8)
2. Hệ thống tính lại:
   - 31 khách / 8 = 3 bàn đủ + 1 bàn 7
   - Bàn mới: 8 + 8 + 8 + 7
3. Hệ thống tự động chuyển món từ bàn cũ sang bàn mới:
   - Món PER_PERSON: phân bổ theo số khách mới mỗi bàn
   - Món PER_TABLE: mỗi bàn mới có 1 phần
   - Món PER_ROOM: không đổi
4. Nếu có phát sinh đã gắn vào bàn cũ:
   - Hỏi: "Bàn 02 cũ có 5 tôm hùm phát sinh, chuyển sang bàn nào?"
   - Nhân viên chọn bàn mới tương ứng
5. Log lại toàn bộ thay đổi để audit

### 9.8 Hủy báo giá đã duyệt

**Tình huống:** Báo giá đã duyệt (đã tạo đơn mở), khách hủy.

**Xử lý:**
1. Không xóa đơn mở (giữ audit trail)
2. Đơn chuyển `Serving → Cancelled`
3. Báo giá chuyển `Approved → Cancelled`
4. Ghi lý do hủy (bắt buộc)
5. Nếu đã đặt cọc: xử lý hoàn cọc / phí hủy theo điều khoản
6. Phòng/bàn chuyển về trạng thái trống
7. Cảnh báo nếu hủy trong vòng X giờ trước giờ phục vụ

### 9.9 Chia bàn không đều (bàn lẻ)

**Quy tắc phân phối bàn lẻ:**

| Số khách | Chuẩn bàn | Kết quả | Ghi chú |
|---|---|---|---|
| 31 | 10 | 10+10+11 | Dồn lẻ vào bàn cuối |
| 31 | 10 (phân bổ đều) | 8+8+8+7 | Chia đều hơn nếu cấu hình |
| 46 | 10 | 10+10+10+10+6 | Bàn cuối 6 (< chuẩn) |
| 46 | 12 | 12+12+12+10 | Bàn cuối 10 (< chuẩn) |
| 8 | 10 | 8 | 1 bàn lẻ 8 người |
| 3 | 10 | 3 | 1 bàn lẻ 3 người (cảnh báo) |

**Cấu hình hệ thống:**
- `MinTableGuests = 4` (dưới 4 khách/bàn → cảnh báo)
- `AllowUnevenLastTable = true` (cho phép bàn cuối ít hơn)
- `UnevenStrategy`: "LastTable" | "Distribute" (bàn cuối lẻ / phân bổ đều)

### 9.10 Trùng lịch phòng

**Tình huống:** 2 nhân viên cùng tạo báo giá cho VIP 1, cùng ngày giờ.

**Xử lý:**
1. Khi lưu báo giá: kiểm tra phòng đã có báo giá/đơn nào trùng giờ chưa
2. Nếu có: cảnh báo "Phòng VIP 1 đã có báo giá BG-xxx cùng khung giờ"
3. Cho phép ghi đè nếu có quyền quản lý
4. Hiển thị calendar view để thấy lịch phòng theo ngày

---

## 10. RỦI RO NẾU SỬA TRÊN SCHEMA HIỆN TẠI

| Rủi ro | Mức độ | Mô tả |
|---|---|---|
| `DiningTable` là bàn cố định DB | **Cao** | À La Carte cần bàn động sinh từ số khách, không thể dùng bàn cố định |
| `AlacarteOrderLine` thiếu `PricingRule` | **Cao** | Không thể phân biệt món theo người/bàn/phòng |
| Thiếu entity `Quote` | **Cao** | Không có nơi lưu báo giá với đầy đủ fields |
| Thiếu `OrderTable` | **Cao** | Không có bàn trong đơn (chỉ có bàn cố định DB) |
| Thiếu `VipRoom.TableStandard` | **Trung bình** | Không tự động chia bàn được |
| Service hiện tại chỉ hỗ trợ CRUD cơ bản | **Cao** | Thiếu AutoDivideTables, PricingRule calculation |

---

## 11. KẾ HOẠCH TRIỂN KHAI

### P0 — Nền tảng (bắt buộc để module hoạt động)

1. **Entity**: Thêm `Quote`, `OrderTable`, `PricingRule` vào schema
2. **Migration**: Tạo migration mới cho các entity trên
3. **Service**: `IQuoteService` với create/update/approve + auto-divide
4. **ViewModel**: `QuoteViewModel` với form khách hàng + chọn phòng + chia bàn
5. **UI**: Tab Báo giá — form tạo báo giá với auto-divide tables

### P1 — Vận hành (để dùng được thực tế)

1. **Duyệt báo giá → Đơn mở**: Approve flow + tạo VipOrder
2. **Đơn mở với bàn tổng + bàn thật**: Hiển thị sơ đồ bàn, phát sinh theo bàn
3. **Xử lý edge cases**: Đổi số khách, đổi phòng, chia lại bàn
4. **Thanh toán bàn tổng**: Tổng hợp tất cả về 1 điểm thanh toán

### P2 — Hoàn thiện (chuyên nghiệp)

1. **Calendar view**: Xem lịch phòng theo ngày, cảnh báo trùng
2. **Ghép phòng**: CombinedRoom logic
3. **Trẻ em / VIP table**: Cấu hình đặc biệt
4. **In bếp theo bàn**: Kitchen ticket per table
5. **Audit log**: Ghi lại mọi thay đổi báo giá/đơn

---

## 12. CÂU HỎI CẦN XÁC NHẬN VỚI KHÁCH HÀNG

1. **Chuẩn bàn mặc định**: Mỗi phòng VIP có chuẩn bàn riêng (VIP 1: 10, VIP 2: 8) hay toàn bộ nhà hàng 1 chuẩn?
2. **Bàn lẻ tối thiểu**: Dưới bao nhiêu khách thì không tạo bàn riêng mà dồn vào bàn khác? (vd: 3 khách → dồn, không tạo bàn 3)
3. **Trẻ em**: Có tính suất riêng không? Tỷ lệ bao nhiêu? Dưới mấy tuổi miễn phí?
4. **Phát sinh**: Có cần in lại phiếu bếp khi thêm phát sinh không?
5. **Hủy đơn**: Phí hủy = % tổng báo giá? Có hoàn cọc không?
6. **Ghép phòng**: Có vách ngăn di động giữa các phòng không? Phòng nào ghép được với nhau?
