# ALACARTE VIP — PHÂN TÍCH NGHIỆP VỤ V2 (PHẢN BIỆN CHUYÊN SÂU)

## Ngày: 2026-06-18
## Vai trò: Senior Business Analyst / Chuyên gia Vận hành Nhà hàng VIP

---

## 1. ĐÁNH GIÁ TÀI LIỆU V1

### Điểm đúng

| Mục | Đánh giá |
|---|---|
| Phân biệt 3 module (POS Cà phê / À La Carte VIP / Tiệc) | **Chuẩn xác** — đây là điểm gây hiểu nhầm lớn nhất cho lập trình viên |
| 4 quy tắc tính món (PER_PERSON / PER_TABLE / PER_ROOM / MANUAL) | **Đúng nhưng chưa đủ** — xem mục 5 bên dưới |
| Quy trình Báo giá → Duyệt → Đơn mở → Phục vụ → Thanh toán | **Đúng luồng chính** — nhưng thiếu chi tiết vận hành thực tế |
| Bàn tổng (ảo) để thanh toán | **Đúng** — đây là khái niệm cốt lõi |
| Chia bàn động từ số khách | **Đúng** — nhưng chưa phân tích sâu các kiểu chia |

### Điểm còn thiếu hoặc chưa sâu

| Mục | Vấn đề |
|---|---|
| Room Calendar / Timeline | Chỉ đề cập sơ lược, chưa phân tích vận hành thực tế |
| Menu Calculation Engine | 4 quy tắc là chưa đủ cho Fine Dining |
| Guest Recalculation | Mới nêu ví dụ, chưa có quy tắc hệ thống |
| Service Style | Hoàn toàn chưa đề cập |
| Xử lý thời gian phục vụ | Chưa có (kéo dài giờ, check-in sớm, overbook) |
| In bếp theo phòng/bàn | Chưa có quy trình bếp núc |
| Phí phục vụ & VAT cho từng loại món | Chưa rõ ràng |
| Tích hợp kho / tồn nguyên liệu | Chưa đề cập |

---

## 2. ROOM TIMELINE / ROOM CALENDAR

### Hiện trạng

Tài liệu V1 mới dừng ở mức: "có phòng VIP, có đặt phòng, có báo giá". Nhưng thực tế nhà hàng VIP vận hành theo **slot thời gian**, không phải theo ngày như khách sạn.

### Mô hình slot thời gian

Mỗi phòng VIP có thể phục vụ **nhiều slot trong 1 ngày**:

```
VIP 1:
  09:00 - 11:00  |  Tiệc sáng doanh nghiệp  |  20 khách
  11:30 - 14:00  |  Tiệc trưa gia đình      |  30 khách
  15:00 - 17:00  |  Trống (dọn dẹp)
  17:30 - 21:00  |  Tiệc tối VIP            |  24 khách
```

### Vấn đề vận hành thực tế

#### 2.1 Trùng lịch / Chồng slot

**Tình huống:**
VIP 1 đã có booking 11:00-14:00. Nhân viên mới vào tạo báo giá cho VIP 1 lúc 12:00-15:00.

**Cơ chế cảnh báo:**
```
Hệ thống phát hiện:
- VIP 1 đã có đơn #ALC-089 (11:00-14:00, 30 khách)
- Báo giá mới BG-105 (12:00-15:00, 20 khách)
→ CHỒNG LỊCH 2 tiếng (12:00-14:00)

Xử lý:
- Mức 1 (warning): Cảnh báo vàng "Có chồng lịch — kiểm tra lại"
- Mức 2 (block): "Không thể tạo — phòng đã có booking"
- Mức 3 (override): Cho phép manager ghi đè với lý do
```

**Quy tắc chồng lịch:**
| Khoảng cách giữa 2 slot | Xử lý |
|---|---|
| ≥ 60 phút | OK (đủ dọn dẹp) |
| 30-59 phút | Warning (dọn dẹp gấp) |
| < 30 phút | Block (không kịp dọn) |
| Chồng giờ | Block (trừ khi manager override) |

**Buffer time mặc định:** 60 phút giữa các slot để dọn dẹp và setup.

#### 2.2 Check-in sớm

**Tình huống:**
Booking 11:00, khách đến 10:30 (sớm 30 phút).

**Xử lý:**
1. Nếu phòng trống (slot trước đã kết thúc + đã dọn): **Cho vào ngay**
2. Nếu phòng đang dọn: **Mời khách chờ ở khu vực lounge**
3. Nếu phòng còn khách cũ: **Không thể — đề xuất lounge hoặc quầy bar**
4. Ghi nhận: `EarlyCheckIn = 30 phút`

**Chính sách check-in sớm:**
- Dưới 30 phút: Miễn phí, tự động nếu phòng sẵn sàng
- 30-60 phút: Có thể tính phí (cấu hình)
- Trên 60 phút: Tính như slot riêng

#### 2.3 Check-out muộn / Kéo dài giờ

**Tình huống:**
Booking 11:00-14:00, khách vẫn ngồi tới 16:00.

**Xử lý theo giai đoạn:**

**Giai đoạn 1 — Cảnh báo (30 phút trước giờ kết thúc):**
```
[14:00] Hệ thống: "Còn 30 phút đến giờ kết thúc"
Nhân viên: Hỏi khách có muốn gia hạn không
```

**Giai đoạn 2 — Hết giờ (đúng 14:00):**
```
[14:00] Hệ thống: "Đã hết giờ phục vụ"
Nhân viên: Thông báo khách
Nếu khách muốn ở lại → Gia hạn
```

**Giai đoạn 3 — Quá giờ (14:00-14:30):**
```
Tự động tính phí gia hạn:
- Phí phòng/giờ (vd: 500.000đ/giờ)
- Dịch vụ thêm (nước, tráng miệng)
```

**Giai đoạn 4 — Ảnh hưởng slot sau (14:30+):**
```
Nếu có slot sau (15:00):
- Cảnh báo đỏ: "Slot sau bắt đầu lúc 15:00 — cần dọn dẹp gấp"
- Đề xuất manager: chuyển slot sau sang phòng khác
- Hoặc: gia hạn + dời slot sau
```

#### 2.4 Overbook (Đặt vượt sức chứa)

**Tình huống:**
VIP 1 sức chứa tối đa 40 khách. Báo giá ghi 45 khách.

**Xử lý:**
1. Cảnh báo: "Vượt sức chứa phòng (45/40)"
2. Đề xuất:
   - Giảm khách
   - Đổi sang phòng lớn hơn
   - Ghép phòng (nếu có)
3. Manager override nếu khách chấp nhận ngồi chật
4. Ghi log: `OverbookApproved` + lý do

#### 2.5 Giữ phòng (Hold)

**Tình huống:**
Khách gọi điện muốn giữ phòng nhưng chưa chốt.

**Xử lý:**
```
Trạng thái phòng: Available → Hold (giữ tạm)
Thời gian giữ: cấu hình (vd: 30 phút, 1 giờ, 2 giờ)
Hết thời gian giữ:
  - Phòng tự động về Available
  - Gửi thông báo cho nhân viên đã giữ
  - Log: "Hold expired"
```

**Chính sách giữ phòng:**
- Khách VIP: giữ 2 giờ không cần cọc
- Khách thường: giữ 30 phút, sau đó cần cọc
- Cuối tuần / Lễ Tết: không giữ nếu không cọc

#### 2.6 Calendar View — Thiết kế UX

**Màn hình nên có:**

1. **Chế độ xem ngày** (Day View):
   - Cột trái: danh sách phòng (VIP 1, VIP 2...)
   - Timeline ngang: 08:00 → 23:00, mỗi slot 30 phút
   - Block màu: mỗi booking là 1 block, kéo dài theo thời gian
   - Màu block: Xanh lá (đã xác nhận), Cam (chờ duyệt), Xám (đã hủy), Đỏ (đang phục vụ)

2. **Chế độ xem tuần** (Week View):
   - 7 cột (T2 → CN)
   - Mỗi cột hiển thị các slot theo phòng
   - Dạng lịch compact

3. **Chế độ xem phòng** (Room View):
   - Focus vào 1 phòng trong 1 tuần
   - Hiển thị chi tiết từng booking

4. **Bộ lọc**:
   - Theo phòng
   - Theo trạng thái
   - Theo nhân viên phụ trách
   - Theo khách hàng

#### 2.7 Luồng trạng thái phòng

```
                 ┌──────────┐
    ┌───────────→│ Available│←───────────┐
    │            └────┬─────┘            │
    │                 │                  │
    │            ┌────▼─────┐      ┌─────┴────┐
    │            │   Hold   │      │ Cleaning │
    │            └────┬─────┘      └─────┬────┘
    │                 │                  │
    │            ┌────▼─────┐            │
    │            │ Reserved │            │
    │            └────┬─────┘            │
    │                 │                  │
    │            ┌────▼─────┐            │
    │            │ Serving  │────────────┘
    │            └────┬─────┘
    │                 │
    │            ┌────▼─────┐
    └────────────┤Completed │
                 └──────────┘
```

---

## 3. MENU CALCULATION ENGINE — HOÀN CHỈNH

### 3.1 Đánh giá 4 quy tắc hiện tại

| Quy tắc | Dùng cho | Đánh giá |
|---|---|---|
| PER_PERSON | Tôm hùm, Bào ngư, Súp... | **Đúng** |
| PER_TABLE | Lẩu, Salad, Trái cây... | **Đúng** |
| PER_ROOM | LED, MC, Trang trí... | **Đúng** |
| MANUAL | Rượu, Bia, Nước ngọt... | **Đúng nhưng thiếu context** |

### 3.2 Các quy tắc còn thiếu

#### PER_PERSON_ADULT_ONLY
**Dùng cho:** Rượu vang, Bia cao cấp (chỉ tính người lớn).
```
Tôm hùm = 30 suất (tất cả khách)
Rượu vang = 28 suất (chỉ người lớn, trẻ em không tính)
```

#### PER_PERSON_TIERED
**Dùng cho:** Món có giá khác nhau theo số lượng.
```
Bào ngư:
  1-20 khách: 350.000đ/con
  21-40 khách: 320.000đ/con
  41+ khách: 290.000đ/con
```
Đây là **chiết khấu số lượng** tự động — rất phổ biến trong Fine Dining.

#### PER_TABLE_MINIMUM
**Dùng cho:** Món yêu cầu số lượng tối thiểu mới phục vụ.
```
Súp cá tầm hầm: tối thiểu 8 khách/bàn mới nấu
→ Bàn 6 khách: không phục vụ món này
→ Cảnh báo khi chọn món cho bàn < MinGuests
```

#### PER_SERVICE_STYLE
**Dùng cho:** Món thay đổi cách tính theo Service Style (xem mục 5).
```
Style = Fine Dining:
  Tôm hùm → PER_PERSON (từng con riêng)
  Salad → PER_PERSON (đĩa riêng)

Style = Sharing:
  Tôm hùm → PER_TABLE (đĩa lớn chia)
  Salad → PER_TABLE
```

#### PER_TIME_SLOT
**Dùng cho:** Món/dịch vụ tính theo giờ.
```
MC: 3 giờ đầu giá X, mỗi giờ thêm giá Y
Âm thanh: trọn gói / giờ
Ca sĩ: theo set (30 phút/set)
```

#### PER_EVENT
**Dùng cho:** Món đặc biệt chỉ có trong sự kiện cụ thể.
```
Rượu Champagne khai vị: 1 chai/SỰ KIỆN (không phải phòng)
Bánh sinh nhật VIP: 1 cái/SỰ KIỆN
```

#### PER_GUEST_GROUP
**Dùng cho:** Phân biệt nhóm khách trong cùng 1 phòng.
```
28 người lớn: tính giá NL
4 trẻ em: tính giá TE (50%)
2 em bé (<2t): miễn phí

Tôm hùm: 28 + (4×0.5) + (2×0) = 30 suất
```

### 3.3 Mô hình Menu Calculation hoàn chỉnh

Mỗi `MenuItem` trong hệ thống nên có:

```
MenuItemPricingProfile {
  PrimaryRule: PER_PERSON | PER_TABLE | PER_ROOM | MANUAL
  FallbackRule: null | PER_TABLE (nếu PER_PERSON không áp dụng được)
  
  AdultPortionRate: 1.0      // người lớn = 1 suất
  ChildPortionRate: 0.5      // trẻ em = 0.5 suất (cấu hình theo tuổi)
  ChildFreeUnderAge: 6       // dưới 6t miễn phí (null = không miễn)
  
  MinPerTable: null          // null = không giới hạn, 8 = tối thiểu 8 khách/bàn
  MinPerRoom: null           // null = không giới hạn, 20 = tối thiểu 20 khách/phòng
  
  TieredPricing: [           // chiết khấu số lượng
    { MinQty: 1,  MaxQty: 20, UnitPrice: 350000 }
    { MinQty: 21, MaxQty: 40, UnitPrice: 320000 }
    { MinQty: 41, MaxQty: null, UnitPrice: 290000 }
  ]
  
  ServiceStyleOverrides: {   // ghi đè theo Service Style
    "SetMenu":    { Rule: PER_TABLE, DefaultQty: 1 }
    "Sharing":    { Rule: PER_TABLE, DefaultQty: 1 }
    "FineDining": { Rule: PER_PERSON, DefaultQty: 1 }
  }
  
  TimeSlotPricing: null | [  // tính theo giờ
    { Hours: 3, Price: 800000 }   // 3 giờ đầu
    { Hours: null, Price: 200000 } // mỗi giờ thêm
  ]
}
```

### 3.4 Công thức tính tổng cho 1 món

```
TotalQty = 0

IF PrimaryRule == PER_PERSON:
  AdultQty = GuestCount.Adult × AdultPortionRate
  ChildQty = Σ (mỗi trẻ em: IF age < ChildFreeUnderAge THEN 0 ELSE ChildPortionRate)
  TotalQty = AdultQty + ChildQty
  IF MinPerRoom != null AND TotalQty < MinPerRoom:
    TotalQty = MinPerRoom (đạt tối thiểu)

IF PrimaryRule == PER_TABLE:
  TotalQty = TableCount × DefaultQty
  IF MinPerTable != null AND (GuestCount / TableCount) < MinPerTable:
    CẢNH BÁO (bàn quá ít khách cho món này)

IF PrimaryRule == PER_ROOM:
  TotalQty = 1 × DefaultQty

IF PrimaryRule == MANUAL:
  TotalQty = ManualInput

Amount = UnitPrice × TotalQty
IF TieredPricing:
  UnitPrice = LookupTier(TotalQty)
```

---

## 4. GUEST RECALCULATION ENGINE

Đây là engine quan trọng nhất khi khách thay đổi số lượng.

### 4.1 Phân loại thay đổi

| Loại thay đổi | Thời điểm | Mức độ phức tạp |
|---|---|---|
| **Pre-service** | Trước giờ phục vụ | Thấp — chỉ cập nhật báo giá/đơn |
| **During-service** | Đang phục vụ | Cao — đã gửi bếp 1 phần |
| **Post-kitchen** | Đã gửi bếp toàn bộ | Rất cao — món đã nấu |

### 4.2 Engine xử lý

#### Pre-service (Trước giờ phục vụ)

**Tình huống:** Báo giá 30 khách, 2 ngày trước event khách báo 36 khách.

```
Bước 1: Xác định delta
  OldGuestCount = 30, NewGuestCount = 36 → Delta = +6 (+20%)

Bước 2: Chia lại bàn
  Cũ: 10+10+10 (3 bàn)
  Mới: 10+10+10+6 (4 bàn)
  → +1 bàn

Bước 3: Tính lại từng dòng món
  PER_PERSON:
    Tôm hùm: 30→36 (+6 con)
    Bào ngư: 30→36 (+6 con)
    → Cập nhật SL, đơn giá có thể đổi (tiered pricing)
  
  PER_TABLE:
    Lẩu: 3→4 nồi (+1)
    Salad: 3→4 đĩa (+1)
    → Cập nhật SL theo số bàn mới
  
  PER_ROOM:
    LED: 1 gói (không đổi)
    MC: 1 buổi (không đổi)
    → Không thay đổi
  
  MANUAL:
    Rượu: giữ nguyên (khách không yêu cầu đổi)
    → Không tự động đổi

Bước 4: Cập nhật tổng tiền
  Tổng mới = Tổng cũ + Delta
  Hiển thị bảng so sánh trước/sau

Bước 5: Gửi lại báo giá cho khách xác nhận
```

#### During-service (Đang phục vụ)

**Tình huống:** Đơn đang Serving (30 khách), khách báo thêm 6 người đến sau.

```
Bước 1: Xác định trạng thái món
  Đã gửi bếp: Tôm hùm (30 con), Lẩu (3 nồi)
  Chưa gửi bếp: Tráng miệng, Trái cây

Bước 2: Phân loại xử lý
  MÓN ĐÃ GỬI BẾP:
    - KHÔNG hủy được (đã nấu)
    - Thêm phần mới: +6 con tôm hùm
    - Gửi phiếu bổ sung cho bếp: "BÀN 04 MỚI — 6 Tôm hùm"
  
  MÓN CHƯA GỬI BẾP:
    - Cập nhật SL bình thường
    - Tráng miệng: 30→36

Bước 3: Chia bàn mới
  Thêm 1 bàn 6 người (Bàn 04)
  KHÔNG chia lại bàn cũ (đã phục vụ)

Bước 4: Bàn mới
  Bàn 04: 6 khách
  Tôm hùm: 6 con (bổ sung)
  Lẩu: 1 nồi (bổ sung)
  Tráng miệng: 6 phần

Bước 5: Cảnh báo
  "Bàn 01-03 ĐÃ phục vụ. Bàn 04 là bàn mới thêm."
  "Cần gửi phiếu bổ sung cho bếp."
```

#### Post-kitchen (Đã gửi bếp toàn bộ)

**Tình huống:** Tất cả món đã gửi bếp, khách muốn thêm.

```
Cách xử lý:
1. Tất cả thêm mới = PHÁT SINH
2. Gắn vào bàn cụ thể
3. Gửi phiếu bếp mới với nhãn "BỔ SUNG - BÀN XX"
4. KHÔNG điều chỉnh món đã gửi
5. Tổng tiền = Tổng cũ + Phát sinh
```

#### Giảm số khách (pre-service)

**Tình huống:** Báo giá 30 khách, còn 22 khách.

```
Bước 1: Xác định delta
  Delta = -8 (-27%)

Bước 2: Chia lại bàn
  Cũ: 10+10+10 (3 bàn)
  Mới: 10+8+4? hay 8+8+6?
  → Phụ thuộc strategy (LastTable vs Distribute)

Bước 3: Tính lại món
  PER_PERSON:
    Tôm hùm: 30→22 (-8 con)
    → GIẢM (nếu chưa chế biến)
  
  PER_TABLE:
    Lẩu: 3→? nồi (tùy số bàn mới)
    → GIẢM theo bàn

Bước 4: Chính sách hủy
  Nếu trong thời gian hủy miễn phí (>72h): OK
  Nếu sát ngày (<24h): Tính phí hủy cho 8 suất
  → Phí hủy = 8 × đơn giá × CancelFeeRate (30%)
```

#### Giảm số khách (during-service)

**Tình huống:** Đã phục vụ 30 khách, khách báo có 5 người không đến.

```
Xử lý:
1. Tôm hùm đã nấu 30 con → KHÔNG hoàn tiền (đã chế biến)
2. Lẩu đã nấu → KHÔNG hoàn tiền
3. Tráng miệng chưa làm → giảm còn 25 phần
4. Chính sách:
   - Nếu báo trước 2h: hoàn tiền món chưa chế biến
   - Nếu báo tại chỗ: tính đủ (no-show fee)
   - Cấu hình: NoShowChargePercent = 70%
```

### 4.3 Bảng quyết định tổng hợp

| Thời điểm | Hướng thay đổi | Món PER_PERSON | Món PER_TABLE | Món PER_ROOM | MANUAL |
|---|---|---|---|---|---|
| Pre-service | Tăng | Tự động tăng | Tự động tăng theo bàn mới | Không đổi | Không đổi |
| Pre-service | Giảm | Tự động giảm (nếu chưa chế biến) | Tự động giảm theo bàn | Không đổi | Không đổi |
| During-service | Tăng | Bổ sung (gửi phiếu mới) | Bổ sung theo bàn mới | Không đổi | Nhập tay |
| During-service | Giảm | Tính phí no-show | Không hoàn (đã nấu) | Không đổi | Không đổi |
| Post-kitchen | Bất kỳ | Phát sinh mới | Phát sinh mới | Phát sinh mới | Nhập tay |

---

## 5. SERVICE STYLE

### 5.1 Tại sao cần Service Style?

Tôi nhận thấy 1 nhà hàng VIP KHÔNG chỉ có 1 cách phục vụ. Cùng 1 thực đơn, cùng 1 phòng, nhưng cách **tổ chức bàn và tính món** thay đổi theo yêu cầu khách.

Việc gắn Service Style vào Báo giá/Đơn giúp:
- Tự động áp dụng quy tắc tính món phù hợp
- Tự động đề xuất cách chia bàn
- Tự động gợi ý dịch vụ kèm
- Tránh lỗi nhân viên chọn sai quy tắc

### 5.2 Các Service Style đề xuất

#### STYLE 1: FINE DINING (Fine Dining / Set Menu Cá Nhân)

**Mô tả:**
Mỗi khách có 1 set menu riêng, trình bày riêng từng đĩa. Phục vụ theo từng người, không chia sẻ.

**Đặc điểm:**
- Tất cả món: PER_PERSON
- Mỗi người 1 đĩa riêng
- Bàn 10 người = 10 đĩa mỗi món
- Dịch vụ: PER_ROOM
- Phục vụ đồng loạt theo từng course

**Ví dụ:**
```
Bàn 01 (10 khách):
  Amuse-bouche: 10 phần
  Appetizer: 10 phần
  Soup: 10 phần
  Main (Tôm hùm): 10 phần
  Dessert: 10 phần
```

**Phù hợp:** Tiệc doanh nghiệp, tiếp đối tác, khách VIP.

#### STYLE 2: SHARING (Family Style / Chia Sẻ)

**Mô tả:**
Món được bày đĩa lớn giữa bàn, mọi người tự chia. Phổ biến nhất trong nhà hàng Hoa/Việt.

**Đặc điểm:**
- Tất cả món: PER_TABLE (1 đĩa lớn/bàn)
- Định lượng: đĩa cho X người (cấu hình: 4, 6, 8, 10)
- Dịch vụ: PER_ROOM
- Bàn 10 người = 1-2 đĩa lớn tùy món

**Ví dụ:**
```
Bàn 01 (10 khách):
  Gỏi: 1 đĩa lớn (10 người)
  Lẩu: 1 nồi (10 người)
  Cơm: 1 nồi (10 người)
  Trái cây: 1 đĩa (10 người)
```

**Phù hợp:** Tiệc gia đình, họp lớp, liên hoan.

#### STYLE 3: HYBRID (Fine Dining + Sharing)

**Mô tả:**
Kết hợp — món chính theo người, món phụ theo bàn. Đây là kiểu **phổ biến nhất** trong À La Carte VIP.

**Đặc điểm:**
- Món đắt tiền (Tôm hùm, Bào ngư): PER_PERSON
- Món chia sẻ (Lẩu, Salad, Cơm): PER_TABLE
- Dịch vụ: PER_ROOM
- Rượu: MANUAL

**Ví dụ:**
```
Bàn 01 (10 khách):
  Tôm hùm: 10 con (PER_PERSON)
  Bào ngư: 10 con (PER_PERSON)
  Lẩu hải sản: 1 nồi (PER_TABLE)
  Salad: 1 đĩa (PER_TABLE)
  Cơm: 1 nồi (PER_TABLE)
```

**Phù hợp:** 90% trường hợp À La Carte VIP.

#### STYLE 4: SET MENU (Menu Cố Định Theo Bàn)

**Mô tả:**
Giống Tiệc — mỗi bàn 1 set menu cố định.

**Đặc điểm:**
- Tất cả món: PER_TABLE (set giá cố định/bàn)
- Không linh hoạt theo số khách lẻ
- Phù hợp tiệc đông người
- Giá = Giá set × số bàn

**Ví dụ:**
```
Set A (1.500.000đ/bàn 10 người):
  Khai vị: 1 đĩa
  Súp: 1 nồi
  Gà: 1 con
  Cá: 1 con
  Lẩu: 1 nồi
  Tráng miệng: 1 đĩa
```

**Phù hợp:** Khi khách muốn đơn giản, không chọn món lẻ.

#### STYLE 5: BUFFET VIP

**Mô tả:**
Món bày buffet trong phòng VIP, khách tự chọn.

**Đặc điểm:**
- Tất cả món: PER_ROOM (chuẩn bị 1 lần cho phòng)
- Số lượng chuẩn bị dựa trên số khách
- Nhân viên phục vụ bàn (rót rượu, dọn đĩa)
- Dịch vụ: PER_ROOM

**Ví dụ:**
```
Phòng VIP 1 (30 khách):
  Buffet hải sản: 1 setup
  Buffet nóng: 1 setup
  Buffet tráng miệng: 1 setup
  Rượu vang: phục vụ bàn (MANUAL)
```

### 5.3 Ma trận Style vs Quy tắc tính món

| Món | Fine Dining | Sharing | Hybrid | Set Menu | Buffet VIP |
|---|---|---|---|---|---|
| Tôm hùm | PER_PERSON | PER_TABLE | PER_PERSON | PER_TABLE | PER_ROOM |
| Lẩu | PER_TABLE | PER_TABLE | PER_TABLE | PER_TABLE | PER_ROOM |
| Salad | PER_PERSON | PER_TABLE | PER_TABLE | PER_TABLE | PER_ROOM |
| Cơm | PER_PERSON | PER_TABLE | PER_TABLE | PER_TABLE | PER_ROOM |
| Rượu | MANUAL | MANUAL | MANUAL | MANUAL | MANUAL |
| LED | PER_ROOM | PER_ROOM | PER_ROOM | PER_ROOM | PER_ROOM |

### 5.4 Style ảnh hưởng đến chia bàn

| Style | Cách chia bàn | Bàn lẻ | Ghi chú |
|---|---|---|---|
| Fine Dining | Bàn nhỏ (2-6 người) | Phổ biến | Mỗi người 1 set |
| Sharing | Bàn tròn lớn (8-12 người) | Có thể | Đĩa lớn giữa bàn |
| Hybrid | Bàn 8-10 người | Có thể | Kết hợp |
| Set Menu | Bàn đều (10 người) | Hạn chế | Tối ưu cho set giá cố định |
| Buffet VIP | Bàn nhỏ (2-4) + bàn buffet | Không quan trọng | Setup buffet chính |

---

## 6. CÁC NGHIỆP VỤ CÒN THIẾU

### 6.1 Tích hợp bếp (Kitchen Integration)

Hiện tại tài liệu chưa đề cập:

1. **In phiếu bếp theo bàn**
   - Mỗi bàn trong phòng VIP có thể in phiếu bếp riêng
   - Bàn tổng KHÔNG in bếp (chỉ để thanh toán)
   - Phiếu bếp ghi rõ: "Bàn 02 — VIP 1 — 10 khách"

2. **Phân khu bếp**
   - Món nóng → Bếp nóng
   - Món lạnh → Bếp lạnh
   - Đồ uống → Bar
   - In tự động theo khu vực chế biến

3. **Phiếu bổ sung**
   - Khi thêm phát sinh: in phiếu "BỔ SUNG" với màu khác
   - Tránh nhầm với phiếu chính

4. **Hủy món đã gửi bếp**
   - In phiếu "HỦY" gửi bếp
   - Yêu cầu xác nhận từ bếp

### 6.2 Tồn kho nguyên liệu

1. **Dự báo nguyên liệu từ báo giá**
   - Từ báo giá đã duyệt → tính được lượng nguyên liệu cần
   - Cảnh báo nếu thiếu nguyên liệu

2. **Trừ kho khi phục vụ**
   - Khi món được gửi bếp → trừ kho nguyên liệu
   - Cập nhật real-time

### 6.3 Quản lý nhân viên phục vụ

1. **Phân công nhân viên theo phòng**
   - Mỗi phòng VIP → 1-2 nhân viên phụ trách
   - Hiển thị trên calendar

2. **Theo dõi giờ làm**
   - Check-in/check-out của nhân viên theo ca
   - Liên kết với phòng đang phục vụ

### 6.4 Upsell & Gợi ý

1. **Gợi ý món khi tạo báo giá**
   - "Khách đặt VIP 1 thường chọn Tôm hùm + Bào ngư"
   - "Mùa này có Cá Tầm tươi — đề xuất thêm"
   - Dựa trên lịch sử báo giá

2. **Gợi ý dịch vụ**
   - "Tiệc 30+ khách thường thuê MC"
   - "Phòng VIP 1 nên có Màn LED"

### 6.5 Loyalty & Khách quen

1. **Lịch sử khách hàng**
   - Khách này đã đến mấy lần
   - Món ưa thích
   - Dị ứng
   - Ngân sách trung bình

2. **Ưu đãi khách VIP**
   - Khách đến lần 5 → giảm 10%
   - Khách chi >100tr/năm → ưu tiên đặt phòng
   - Tự động áp dụng khi tạo báo giá

### 6.6 Đa ngôn ngữ (cho khách nước ngoài)

1. **Báo giá song ngữ**
   - In báo giá bằng tiếng Việt + tiếng Anh
   - Menu tên món có cả 2 ngôn ngữ

2. **Giao diện**
   - Nhân viên dùng tiếng Việt
   - Báo giá xuất ra tiếng Anh nếu khách nước ngoài

---

## 7. PHÂN BIỆT RÕ 3 MODULE — MA TRẬN SO SÁNH

| Tiêu chí | POS Cà phê | À La Carte VIP | Tiệc (Banquet) |
|---|---|---|---|
| **Đơn vị đặt** | Bàn | Phòng VIP | Sảnh |
| **Cách gọi món** | Chọn món → phục vụ ngay | Báo giá trước hoặc ăn ngay | Menu cố định |
| **Tính món** | 1 giá/món | 4-7 quy tắc | Set giá/bàn |
| **Chia bàn** | Bàn cố định DB | Tự động sinh từ số khách | Bàn cố định |
| **Bàn lẻ** | Không có khái niệm | Luôn có thể | Không |
| **Bàn tổng** | Không | Có (bắt buộc) | Có thể |
| **Dịch vụ** | Không | PER_ROOM | PER_EVENT |
| **Báo giá** | Không | Có (bắt buộc với đặt trước) | Hợp đồng |
| **Đặt cọc** | Không | Có | Bắt buộc |
| **Thời gian** | Không giới hạn | Slot thời gian | Cố định |
| **Calendar** | Không cần | Cần (tránh trùng) | Cần |
| **Service Style** | Không | 5 styles | 1 style (Set Menu) |
| **Thay đổi số khách** | Không áp dụng | Engine riêng | Hạn chế |
| **Thanh toán** | Từng bàn | Bàn tổng (phòng) | Hợp đồng |

---

## 8. CÁC TÌNH HUỐNG BIÊN BỔ SUNG

### 8.1 Khách hủy cùng ngày

**Xử lý:**
- Tính phí hủy = % tổng báo giá
- Cấu hình theo thời gian báo:
  - Hủy trước 72h: 0%
  - Hủy trước 48h: 30%
  - Hủy trước 24h: 50%
  - Hủy trong 24h: 70%
  - No-show: 100%

### 8.2 Khách đổi món sau khi đã duyệt báo giá

**Xử lý:**
- Nếu chưa đến ngày phục vụ: Cho phép sửa, gửi lại báo giá
- Nếu trong ngày phục vụ, chưa gửi bếp: Cho phép sửa
- Nếu đã gửi bếp: Không sửa, thêm như phát sinh

### 8.3 Mất điện / Sự cố bếp

**Xử lý:**
- Ghi nhận sự cố vào đơn
- Đề xuất phương án thay thế (món nguội nếu bếp không hoạt động)
- Giảm giá đền bù nếu có

### 8.4 Khách yêu cầu tách bill

**Xử lý:**
- Mặc định: 1 bill cho cả phòng (bàn tổng)
- Nếu tách: tạo bill con cho từng bàn
- Bill con = món PER_PERSON của bàn đó + món PER_TABLE của bàn đó + 1/N dịch vụ phòng
- Bill con không có VAT/phí phục vụ riêng (tính trên bill tổng)

### 8.5 Khách mang bánh kem / rượu từ ngoài

**Xử lý:**
- Ghi nhận: `ExternalItem`
- Phí mở rượu / phí phục vụ bánh: cấu hình
- Không chịu trách nhiệm chất lượng

---

## 9. KHUYẾN NGHỊ TRƯỚC KHI LẬP TRÌNH

### 9.1 Về kiến trúc

1. **Tách biệt rạch ròi 3 module** — không dùng chung entity `DiningTable` giữa POS Cà phê và À La Carte VIP. Mỗi module có bảng riêng.

2. **Menu Calculation Engine là module riêng** — không nhúng vào service CRUD. Engine này được gọi từ QuoteService, OrderService, và cả khi Recalculate.

3. **Guest Recalculation là workflow riêng** — có UI confirm riêng, hiển thị delta trước/sau, yêu cầu xác nhận trước khi áp dụng.

### 9.2 Về UI/UX

1. **Calendar View là màn hình chính** của module (không phải màn hình phụ). Nhân viên nhìn calendar để biết phòng nào trống, phòng nào có khách.

2. **Sơ đồ bàn trong phòng** là màn hình quan trọng thứ 2. Hiển thị visual các bàn đã chia, số khách mỗi bàn.

3. **Bàn tổng luôn hiển thị ở panel phải** — không bao giờ ẩn. Đây là "trung tâm điều khiển" của đơn.

### 9.3 Về dữ liệu

1. **Không dùng bàn cố định DB cho À La Carte** — bàn được sinh động từ Quote/Order.

2. **Menu Item cần có PricingProfile** — không chỉ có giá, mà còn có quy tắc tính, chiết khấu số lượng, child portion rate.

3. **Mọi thay đổi báo giá/đơn phải có audit log** — ai đổi, đổi gì, lúc nào, lý do.

### 9.4 Thứ tự ưu tiên

| P0 | P1 | P2 |
|---|---|---|
| Quote CRUD | Calendar View | Loyalty |
| 4 Pricing Rules | Guest Recalculation | Upsell Engine |
| Auto Table Division | Kitchen Integration | Đa ngôn ngữ |
| Approve → Order | Service Style | Audit Log |
| Room Timeline | Child Pricing | Tách bill |
| Bàn tổng thanh toán | Tiered Pricing | External Items |
