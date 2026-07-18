# ALACARTE VIP — PHÂN TÍCH NGHIỆP VỤ V3 (HOÀN THIỆN)

## Ngày: 2026-06-18
## Vai trò: Senior BA / Fine Dining Operations Consultant / ERP Solution Architect

---

## 1. APPROVAL WORKFLOW HOÀN CHỈNH

### 1.1 Tại sao cần workflow phức tạp hơn?

Nhà hàng VIP không hoạt động như quán cà phê. Một báo giá 50-100 triệu đồng cho 30-40 khách trải qua nhiều vòng thương lượng, chỉnh sửa, phê duyệt nội bộ trước khi đến tay khách. Workflow phải phản ánh đúng thực tế vận hành, không phải chỉ là "Nháp → Gửi → Duyệt".

### 1.2 Workflow đầy đủ

```
                         ┌──────────────────────────────────────────────────────────────┐
                         │                    LUỒNG CHÍNH                                │
                         └──────────────────────────────────────────────────────────────┘

  [Khách gọi đến]
       │
       ▼
  ┌─────────┐     ┌──────────┐     ┌───────────────┐     ┌──────────┐     ┌───────────┐
  │  NHÁP   │────▶│ NỘI DUYỆT │────▶│ ĐÃ GỬI KHÁCH  │────▶│ THƯƠNG   │────▶│ CHỜ ĐẶT  │
  │         │     │ (Internal)│     │ (SentToClient)│     │ LƯỢNG    │     │ CỌC       │
  └─────────┘     └──────────┘     └───────────────┘     └──────────┘     └───────────┘
       ▲                ▲                  │                   │                  │
       │                │                  │                   │                  │
       │                │                  ▼                   ▼                  ▼
       │                │          ┌───────────┐       ┌───────────┐     ┌───────────┐
       │                │          │ TỪ CHỐI    │       │ KHÁCH SỬA │     │ ĐÃ ĐẶT CỌC│
       │                │          │ (Declined) │       │ (Revision) │     │(Deposited)│
       │                │          └───────────┘       └───────────┘     └───────────┘
       │                │                                                     │
       │                │                                                     ▼
       │                │                                              ┌───────────┐
       │                │◀─────────────────────────────────────────────│ ĐÃ XÁC    │
       │                │                                              │ NHẬN      │
       │                │                                              │(Confirmed)│
       │                │                                              └───────────┘
       │                │                                                     │
       │                │                                                     ▼
       │                │                                              ┌───────────┐
       │                │                                              │ ĐÃ TẠO    │
       │                │                                              │ ĐƠN MỞ    │
       │                │                                              │(Ordered)  │
       │                │                                              └───────────┘
       │                │                                                     │
       │                │                                                     ▼
       │                │                                              ┌───────────┐
       │                │                                              │ ĐANG PHỤC │
       │                │                                              │ VỤ        │
       │                │                                              │(Serving)  │
       │                │                                              └───────────┘
       │                │                                                     │
       │                │                                                     ▼
       │                │                                              ┌───────────┐
       │                │                                              │ HOÀN      │
       │                │                                              │ THÀNH     │
       │                │                                              │(Completed)│
       │                │                                              └───────────┘
       │                │                                                     │
       │                │                                                     ▼
       │                │                                              ┌───────────┐
       │                │                                              │ ĐÃ THANH  │
       │                │                                              │ TOÁN      │
       │                │                                              │ (Paid)    │
       │                │                                              └───────────┘
       │                │
       │                │         ┌──────────────────────────────────────────────┐
       │                │         │              LUỒNG HỦY                       │
       │                │         └──────────────────────────────────────────────┘
       │                │
       │                │    Bất kỳ trạng thái nào trước "Đang Phục Vụ" đều có thể:
       │                │         │
       │                └─────────┤
       │                          ▼
       │                   ┌───────────┐     ┌───────────────┐
       └───────────────────│ ĐÃ HỦY    │────▶│ HẾT HẠN       │
                           │(Cancelled)│     │ (Expired)     │
                           └───────────┘     └───────────────┘
```

### 1.3 Chi tiết từng trạng thái

#### TRẠNG THÁI 1: NHÁP (Draft)

**Ai tạo:** Nhân viên kinh doanh / Nhân viên nhà hàng
**Khi nào:** Khi khách gọi điện, nhân viên tạo nhanh báo giá sơ bộ
**Đặc điểm:**
- Có thể sửa TOÀN BỘ (khách hàng, món, số lượng, phòng, giờ...)
- Có thể xóa báo giá
- Chưa gửi cho ai
- Không tính vào báo cáo
- Không giữ phòng

**Cho phép sửa:** Tất cả
**Khóa sửa:** Không

#### TRẠNG THÁI 2: NỘI DUYỆT (InternalReview)

**Ai chuyển:** Nhân viên → gửi lên Quản lý/Tổ trưởng
**Khi nào:** Nhà hàng có quy trình phê duyệt nội bộ trước khi gửi khách
**Đặc điểm:**
- Báo giá bị KHÓA sửa (chỉ người duyệt mới sửa được)
- Quản lý kiểm tra: giá đúng không, phòng trống không, món có đủ nguyên liệu không
- Có thể yêu cầu chỉnh sửa → quay về NHÁP

**Cho phép sửa:** Manager / Supervisor
**Khóa sửa:** Nhân viên thường

**Lưu ý:** Trạng thái này là TÙY CHỌN (cấu hình). Nhà hàng nhỏ có thể bỏ qua, chuyển thẳng NHÁP → GỬI KHÁCH.

#### TRẠNG THÁI 3: ĐÃ GỬI KHÁCH (SentToClient)

**Ai chuyển:** Nhân viên (sau khi có internal approval hoặc trực tiếp)
**Khi nào:** Báo giá được gửi qua email/Zalo/In cho khách
**Đặc điểm:**
- KHÓA sửa món & giá (không thể tự ý thay đổi khi khách đang xem)
- Có thể cập nhật ghi chú, trạng thái
- Tự động ghi nhận: `SentAt = DateTime.Now`, `SentBy = CurrentUser`

**Cho phép sửa:** Ghi chú, trạng thái
**Khóa sửa:** Món, giá, phòng, giờ, số khách

#### TRẠNG THÁI 4: THƯƠNG LƯỢNG (Negotiating)

**Ai chuyển:** Tự động khi khách yêu cầu sửa
**Khi nào:** Khách phản hồi muốn thay đổi
**Đặc điểm:**
- MỞ KHÓA sửa món & giá
- Ghi nhận lịch sử: "Khách yêu cầu đổi Tôm hùm → Tôm sú, giảm giá 5%"
- Có thể quay lại NHÁP để sửa tự do, hoặc giữ ở THƯƠNG LƯỢNG

**Cho phép sửa:** Tất cả (có audit log)
**Khóa sửa:** Không (đang thương lượng)

**Quy tắc đặc biệt:**
- Mỗi lần sửa tăng `RevisionCount`
- Sau N lần thương lượng (cấu hình: 3-5 lần), cảnh báo "Đã thương lượng nhiều lần"
- Manager có thể yêu cầu chốt: "Không thương lượng thêm"

#### TRẠNG THÁI 5: CHỜ ĐẶT CỌC (AwaitingDeposit)

**Ai chuyển:** Nhân viên sau khi khách đồng ý miệng
**Khi nào:** Khách OK báo giá nhưng chưa chuyển cọc
**Đặc điểm:**
- KHÓA sửa món & giá (đã chốt)
- Tự động gửi thông tin chuyển khoản
- Đếm ngược: `DepositDeadline = DateTime.Now + DepositDueHours`

**Cho phép sửa:** Trạng thái cọc
**Khóa sửa:** Món, giá, phòng, số khách

**Cảnh báo tự động:**
- Sau 24h chưa cọc: nhắc nhở
- Sau 48h chưa cọc: gọi điện
- Sau hết hạn: tự động chuyển về THƯƠNG LƯỢNG hoặc HỦY (cấu hình)

#### TRẠNG THÁI 6: ĐÃ ĐẶT CỌC (Deposited)

**Ai chuyển:** Tự động khi nhập giao dịch đặt cọc
**Khi nào:** Khách đã chuyển tiền cọc
**Đặc điểm:**
- KHÓA TOÀN BỘ báo giá (không thể sửa nếu không có manager)
- Ghi nhận: `DepositAmount`, `DepositDate`, `DepositMethod`
- Phòng chuyển trạng thái: Available → Reserved

**Cho phép sửa:** Manager chỉ định
**Khóa sửa:** Nhân viên thường

#### TRẠNG THÁI 7: ĐÃ XÁC NHẬN (Confirmed)

**Ai chuyển:** Manager / Tự động sau khi đặt cọc đủ
**Khi nào:** Báo giá được xác nhận cuối cùng, sẵn sàng tạo đơn
**Đặc điểm:**
- KHÓA TOÀN BỘ (kể cả manager cũng cần lý do để sửa)
- Phòng được giữ chính thức
- Có thể tạo Đơn mở đặt trước

**Cho phép sửa:** Manager + lý do bắt buộc
**Khóa sửa:** Tất cả (có override cho manager)

#### TRẠNG THÁI 8: ĐÃ TẠO ĐƠN (Ordered)

**Ai chuyển:** Tự động khi tạo VipOrder từ báo giá
**Khi nào:** Đã tạo đơn mở cho ngày phục vụ
**Đặc điểm:**
- Liên kết: `Quote.ApprovedOrderId = Order.Id`
- Báo giá KHÔNG thể sửa nữa
- Đơn mở có thể sửa (phát sinh, đổi số khách...)

**Cho phép sửa:** Không (sửa trên Đơn, không sửa trên Báo giá)
**Khóa sửa:** Toàn bộ

#### TRẠNG THÁI 9: ĐÃ HỦY (Cancelled)

**Ai chuyển:** Manager (cần quyền)
**Khi nào:** Khách hủy hoặc nhà hàng không phục vụ được
**Đặc điểm:**
- Ghi nhận: `CancelledAt`, `CancelledBy`, `CancelReason` (bắt buộc)
- Xử lý cọc: hoàn / không hoàn theo điều khoản
- Phòng chuyển về Available

**Cho phép sửa:** Không
**Khóa sửa:** Toàn bộ (chỉ xem)

#### TRẠNG THÁI 10: HẾT HẠN (Expired)

**Ai chuyển:** Tự động
**Khi nào:** 
- Báo giá ở trạng thái CHỜ ĐẶT CỌC quá `DepositDueHours`
- Báo giá ở trạng thái ĐÃ GỬI KHÁCH quá `QuoteValidityDays` (vd: 7 ngày)
- Báo giá ở trạng thái NHÁP quá `DraftMaxAge` (vd: 30 ngày)

**Đặc điểm:**
- Không thể kích hoạt lại (phải tạo báo giá mới)
- Phòng chuyển về Available

### 1.4 Ma trận trạng thái & quyền sửa

| Trạng thái | Sửa món | Sửa giá | Sửa phòng | Sửa SL khách | Xóa BG | Ai được sửa |
|---|---|---|---|---|---|---|
| NHÁP | ✓ | ✓ | ✓ | ✓ | ✓ | Nhân viên |
| NỘI DUYỆT | ✗ | ✗ | ✗ | ✗ | ✗ | Manager |
| ĐÃ GỬI KHÁCH | ✗ | ✗ | ✗ | ✗ | ✗ | Manager |
| THƯƠNG LƯỢNG | ✓ | ✓ | ✓ | ✓ | ✗ | Nhân viên (audit) |
| CHỜ ĐẶT CỌC | ✗ | ✗ | ✗ | ✗ | ✗ | Manager |
| ĐÃ ĐẶT CỌC | ✗ | ✗ | ✗ | ✗ | ✗ | Manager + lý do |
| ĐÃ XÁC NHẬN | ✗ | ✗ | ✗ | ✗ | ✗ | Manager + lý do |
| ĐÃ TẠO ĐƠN | ✗ | ✗ | ✗ | ✗ | ✗ | Không ai |
| ĐÃ HỦY | ✗ | ✗ | ✗ | ✗ | ✗ | Không ai |
| HẾT HẠN | ✗ | ✗ | ✗ | ✗ | ✗ | Không ai |

### 1.5 Tự động chuyển trạng thái

| Trigger | Từ | Đến |
|---|---|---|
| Nhân viên gửi email/Zalo | NHÁP → | ĐÃ GỬI KHÁCH |
| Khách phản hồi muốn sửa | ĐÃ GỬI KHÁCH → | THƯƠNG LƯỢNG |
| Khách đồng ý miệng | THƯƠNG LƯỢNG → | CHỜ ĐẶT CỌC |
| Nhập giao dịch cọc | CHỜ ĐẶT CỌC → | ĐÃ ĐẶT CỌC |
| Đủ cọc + Manager duyệt | ĐÃ ĐẶT CỌC → | ĐÃ XÁC NHẬN |
| Tạo VipOrder | ĐÃ XÁC NHẬN → | ĐÃ TẠO ĐƠN |
| Quá hạn cọc | CHỜ ĐẶT CỌC → | HẾT HẠN |
| Quá hạn báo giá | ĐÃ GỬI KHÁCH → | HẾT HẠN |
| Manager hủy | Bất kỳ → | ĐÃ HỦY |

### 1.6 Câu hỏi cho đội phát triển

Khi implement workflow này, đội phát triển cần trả lời:

1. **Ai có quyền chuyển trạng thái?** Cần role-based permission matrix
2. **Những thay đổi nào cần audit log?** Mọi thay đổi sau THƯƠNG LƯỢNG nên có log
3. **Có cần dual approval không?** Nhà hàng lớn có thể cần 2 người duyệt
4. **Thông báo cho khách thế nào?** Tự động gửi email/Zalo khi chuyển trạng thái?
5. **Lịch sử thương lượng lưu ở đâu?** Mỗi lần sửa tạo 1 revision hay ghi đè?

---

## 2. MENU TEMPLATE FRAMEWORK

### 2.1 Vấn đề

Nhân viên mới không biết suggest món gì cho khách. Mỗi lần tạo báo giá phải chọn lại từng món — mất thời gian, dễ sai, không nhất quán.

### 2.2 Giải pháp: Menu Template

Menu Template là **bộ sưu tập món được định nghĩa sẵn**, dùng làm **điểm xuất phát** cho báo giá.

### 2.3 Cấu trúc Menu Template

```
MenuTemplate {
  Code: "SET-HAISAN-VIP"
  Name: "Set Hải Sản VIP"
  Description: "Dành cho tiệc hải sản cao cấp 20-40 khách"
  TargetGuestRange: { Min: 20, Max: 40 }
  ServiceStyle: "Hybrid"
  
  RecommendedServices: [     // Dịch vụ gợi ý đi kèm
    "Màn LED",
    "MC",
    "Âm thanh"
  ]
  
  Courses: [                 // Các Course trong Set
    {
      CourseType: "Khai vị"
      Items: [
        { MenuItem: "Gỏi hải sản", PricingRule: PER_TABLE, DefaultQty: 1 },
        { MenuItem: "Súp bào ngư", PricingRule: PER_PERSON, DefaultQty: 1 }
      ]
    },
    {
      CourseType: "Món chính"
      Items: [
        { MenuItem: "Tôm hùm nướng", PricingRule: PER_PERSON, DefaultQty: 1 },
        { MenuItem: "Cá mú hấp", PricingRule: PER_TABLE, DefaultQty: 1 },
        { MenuItem: "Lẩu hải sản", PricingRule: PER_TABLE, DefaultQty: 1 }
      ]
    },
    {
      CourseType: "Tráng miệng"
      Items: [
        { MenuItem: "Trái cây nhập", PricingRule: PER_TABLE, DefaultQty: 1 }
      ]
    }
  ]
  
  SuggestedDrinks: [         // Đồ uống gợi ý
    { MenuItem: "Rượu vang đỏ", PricingRule: MANUAL },
    { MenuItem: "Bia nhập", PricingRule: MANUAL }
  ]
  
  EstimatedPricePerPerson: 650000   // Giá ước tính/người (để báo nhanh)
  EstimatedPriceRange: { Min: 500000, Max: 800000 }
}
```

### 2.4 Luồng sử dụng Menu Template

```
Bước 1: Nhân viên tạo báo giá → nhập khách hàng, số khách, phòng

Bước 2: Chọn Menu Template "Set Hải Sản VIP"
  → Hệ thống tự động thêm tất cả món từ template vào báo giá
  → Tự động áp dụng PricingRule từ template
  → Tự động thêm dịch vụ gợi ý
  → Hiển thị giá ước tính: 650.000đ/người × 30 khách ≈ 19.5 triệu

Bước 3: Nhân viên có thể:
  a) Giữ nguyên → Lưu báo giá
  b) Tùy chỉnh:
     - Thêm món: "Thêm 1 món Cua Hoàng Đế"
     - Bỏ món: "Khách không thích Cá mú → bỏ"
     - Đổi món: "Đổi Lẩu hải sản → Lẩu Thái"
     - Đổi PricingRule: "Tôm hùm chuyển sang PER_TABLE"
  c) Kết hợp template: Chọn thêm "Set Sinh Nhật VIP" để bổ sung

Bước 4: Khi đã tùy chỉnh, báo giá có flag:
  BasedOnTemplate: "SET-HAISAN-VIP"
  IsCustomized: true/false
```

### 2.5 Mối quan hệ Template ↔ Báo giá

| Hành động | Template có đổi không? | Báo giá có đổi không? |
|---|---|---|
| Sửa báo giá (đổi món) | Không | Có |
| Sửa template gốc | Có | Không (báo giá đã tạo độc lập) |
| Cập nhật template → báo giá cũ | Không tự động | Phải手动 "Cập nhật từ Template" |
| Xóa template | Không ảnh hưởng | Báo giá vẫn giữ nguyên |

**Quy tắc:**
- Khi tạo báo giá từ Template → báo giá SAO CHÉP dữ liệu từ template, không reference
- Khi template gốc thay đổi → báo giá cũ KHÔNG tự động cập nhật
- Có nút "Đồng bộ từ Template" nếu muốn cập nhật

### 2.6 Khi nào tách khỏi Template?

Báo giá tự động tách khỏi template khi:
1. Thay đổi > 30% số món (thêm/bỏ/đổi)
2. Thay đổi PricingRule của món
3. Thay đổi Service Style

Lúc này `IsCustomized = true`, báo giá trở thành menu riêng, không còn liên kết với template.

### 2.7 Template Library đề xuất

| Code | Tên | Dành cho | Giá ước tính/người |
|---|---|---|---|
| SET-HAISAN-VIP | Set Hải Sản VIP | Tiệc hải sản cao cấp 20-40 khách | 650.000đ |
| SET-TIEPKHACH | Set Tiếp Khách | Tiệc đối tác, khách hàng quan trọng | 850.000đ |
| SET-DOANHNGHIEP | Set Doanh Nghiệp | Họp mặt công ty, year-end party | 450.000đ |
| SET-GIADINH | Set Gia Đình | Sum họp gia đình, giỗ, Tết | 350.000đ |
| SET-SINHNHAT-VIP | Set Sinh Nhật VIP | Tiệc sinh nhật cá nhân | 550.000đ |
| SET-CHAY-VIP | Set Chay VIP | Tiệc chay cao cấp | 400.000đ |
| SET-TATNIAN | Set Tất Niên | Tiệc tất niên công ty | 500.000đ |

---

## 3. ROOM PACKAGE FRAMEWORK

### 3.1 Menu Template vs Room Package — KHÁC NHAU HOÀN TOÀN

| Tiêu chí | Menu Template | Room Package |
|---|---|---|
| **Bao gồm** | Chỉ MÓN ĂN + ĐỒ UỐNG | Phòng + Dịch vụ + Trang trí + Menu (optional) |
| **Mục đích** | Gợi ý thực đơn | Gợi ý trải nghiệm tổng thể |
| **Tính giá** | Theo món | Theo gói (flat fee) |
| **Tùy chỉnh** | Có thể đổi món | Có thể thêm/bớt dịch vụ |

Menu Template = "Ăn gì"
Room Package = "Không gian + Dịch vụ + Ăn gì"

### 3.2 Cấu trúc Room Package

```
RoomPackage {
  Code: "PKG-SINHNHAT-VIP"
  Name: "Gói Sinh Nhật VIP"
  Description: "Trọn gói sinh nhật cao cấp — khách không cần lo gì"
  
  Included: {
    Room: "VIP 1"                    // Phòng mặc định (có thể đổi)
    RoomCapacity: { Min: 20, Max: 40 }
    TableStandard: 10
    
    Services: [                      // Dịch vụ bao gồm
      { Service: "Màn LED", Qty: 1, Unit: "buổi" },
      { Service: "MC", Qty: 1, Unit: "buổi" },
      { Service: "Trang trí sinh nhật", Qty: 1, Unit: "gói" },
      { Service: "Hoa tươi", Qty: 1, Unit: "bình lớn" },
      { Service: "Bánh sinh nhật", Qty: 1, Unit: "cái" },
      { Service: "Ca sĩ", Qty: 1, Unit: "buổi" }
    ]
    
    MenuTemplate: "SET-SINHNHAT-VIP"  // Menu gợi ý (optional, có thể null)
    or
    MenuItems: [                      // Hoặc menu riêng cho package
      ...
    ]
    
    AddOns: [                         // Dịch vụ thêm tùy chọn
      { Service: "Photobooth", Price: 400000 },
      { Service: "Cổng hoa", Price: 500000 },
      { Service: "Rượu Champagne", Price: 1200000 }
    ]
  }
  
  PackagePrice: 8500000               // Giá gói (chưa gồm menu)
  PackagePriceWithMenu: null          // null = menu tính riêng
  
  ValidityRules: {
    MinDaysBeforeEvent: 3             // Phải đặt trước ít nhất 3 ngày
    MaxGuestCount: 40
  }
}
```

### 3.3 Luồng sử dụng Room Package

```
Bước 1: Nhân viên tạo báo giá → nhập khách hàng, số khách, ngày giờ

Bước 2: Chọn Room Package "Gói Sinh Nhật VIP"
  → Hệ thống:
    - Kiểm tra VIP 1 có trống ngày giờ đó không
    - Nếu VIP 1 bận → gợi ý phòng khác cùng sức chứa
    - Tự động thêm dịch vụ vào báo giá
    - Nếu package có MenuTemplate → tự động thêm món
    - Tính giá gói: 8.500.000đ (phòng + dịch vụ) + menu (tính riêng)

Bước 3: Nhân viên có thể:
  - Đổi phòng (VIP 1 → VIP 2)
  - Thêm AddOns (Photobooth, Cổng hoa...)
  - Tùy chỉnh menu (nếu package có menu template)
  - Điều chỉnh dịch vụ (bỏ MC, thêm Ca sĩ)

Bước 4: Lưu báo giá với flag:
  BasedOnPackage: "PKG-SINHNHAT-VIP"
  PackagePrice: 8.500.000đ
  AddOnsTotal: 900.000đ (Photobooth + Cổng hoa)
```

### 3.4 Package Library đề xuất

| Code | Tên | Giá gói | Bao gồm |
|---|---|---|---|
| PKG-SINHNHAT-VIP | Gói Sinh Nhật VIP | 8.500.000đ | VIP1, LED, MC, Trang trí, Hoa, Bánh, Ca sĩ |
| PKG-TIEPKHACH-VIP | Gói Tiếp Khách VIP | 6.500.000đ | VIP2, LED, MC, Hoa, Rượu khai vị |
| PKG-HOINGHI | Gói Hội Nghị | 12.000.000đ | VIP1, Máy chiếu, LED, MC, Âm thanh, Teabreak |
| PKG-HOPLOP | Gói Họp Lớp | 5.500.000đ | Tầng 1 (ghép), MC, Âm thanh, Karaoke |
| PKG-TIEC-CONGTY | Gói Tiệc Công Ty | 15.000.000đ | VIP1+VIP2, LED, MC, Ca sĩ, Trang trí, Âm thanh |

### 3.5 Chỉnh sửa sau khi áp dụng Package

- **Dịch vụ:** Có thể thêm/bỏ tự do. Giá gói KHÔNG đổi (dịch vụ trong gói giá cố định).
- **Phòng:** Có thể đổi. Giá gói thay đổi nếu phòng khác giá.
- **Menu:** Có thể tùy chỉnh như bình thường.
- **AddOns:** Tính thêm vào tổng, không ảnh hưởng giá gói.

---

## 4. TABLE ALLOCATION ENGINE

### 4.1 Yêu cầu

Engine chia bàn phải:
1. Nhận: Số khách + Chuẩn bàn + Chiến lược + Layout phòng
2. Xuất: Danh sách bàn (TableNo, GuestCount)
3. Hỗ trợ chia lại khi số khách thay đổi
4. Hỗ trợ nhiều chiến lược

### 4.2 Strategy 1: MIN_TABLES (Ít bàn nhất)

**Mục tiêu:** Tối thiểu số bàn, tận dụng tối đa sức chứa mỗi bàn.

**Thuật toán:**
```
Input: guestCount = 31, standard = 10

fullTables = floor(31 / 10) = 3
remainder = 31 % 10 = 1

IF remainder == 0:
  → 3 bàn × 10

IF remainder > 0:
  → 3 bàn × 10 + 1 bàn × remainder
  → Nhưng remainder = 1 < MinTableGuests (vd: 4)
  → Cảnh báo: "Bàn lẻ 1 khách — nên dồn vào bàn khác"
  → Đề xuất: 2 bàn × 10 + 1 bàn × 11
```

**Ví dụ:**
| Số khách | Chuẩn | Kết quả | Số bàn |
|---|---|---|---|
| 31 | 10 | 10 + 10 + 11 | 3 |
| 42 | 10 | 10 + 10 + 10 + 12 | 4 |
| 46 | 10 | 10 + 10 + 10 + 10 + 6 | 5 |
| 8 | 10 | 8 | 1 (cảnh báo) |
| 100 | 10 | 10 × 10 | 10 |

### 4.3 Strategy 2: BALANCED (Đều người nhất)

**Mục tiêu:** Phân bổ khách đều nhất có thể giữa các bàn.

**Thuật toán:**
```
Input: guestCount = 31, standard = 10

tableCount = ceil(31 / 10) = 4
baseGuests = floor(31 / 4) = 7
remainder = 31 % 4 = 3

→ 3 bàn × (7+1) = 3 bàn × 8
→ 1 bàn × 7
→ 8 + 8 + 8 + 7
```

**Ví dụ:**
| Số khách | Chuẩn | Kết quả | Số bàn |
|---|---|---|---|
| 31 | 10 | 8 + 8 + 8 + 7 | 4 |
| 42 | 10 | 11 + 11 + 10 + 10 | 4 |
| 46 | 10 | 10 + 9 + 9 + 9 + 9 | 5 |
| 8 | 10 | 8 | 1 |
| 100 | 10 | 10 × 10 | 10 |

### 4.4 Strategy 3: ROOM_LAYOUT (Theo layout phòng)

**Mục tiêu:** Tôn trọng layout vật lý của phòng (số bàn tối đa, vị trí bàn).

**Input thêm:**
```
RoomLayout {
  MaxTables: 4              // VIP1 chỉ kê được 4 bàn tròn
  TablePositions: [         // Vị trí các bàn
    { Id: "T1", MaxSeats: 10 },
    { Id: "T2", MaxSeats: 10 },
    { Id: "T3", MaxSeats: 10 },
    { Id: "T4", MaxSeats: 12 }
  ]
}
```

**Thuật toán:**
```
Input: guestCount = 31, layout trên

Sắp xếp vị trí theo MaxSeats giảm dần:
  T4(12) → T1(10) → T2(10) → T3(10)

Phân bổ:
  T4: 12 (còn 19)
  T1: 10 (còn 9)
  T2: 9 (còn 0)
  T3: 0 (bỏ qua)

Kết quả: 12 + 10 + 9 (3 bàn)
Bàn T3 không dùng → có thể dùng làm bàn phụ (đồ uống, quà...)
```

### 4.5 Strategy 4: CUSTOM (Khách chọn)

Khách có thể yêu cầu cụ thể:
- "Tôi muốn 3 bàn: 12 + 10 + 9"
- "Tôi muốn 1 bàn dài 20 người + 2 bàn tròn 6 người"

Hệ thống cho phép nhập tay số bàn và số khách/bàn.

### 4.6 Re-allocation Engine (Chia lại khi đổi số khách)

**Tình huống:** 31 → 42 khách (+11)

```
Bước 1: Xác định chiến lược hiện tại
  Strategy = MIN_TABLES

Bước 2: Chia mới với strategy đó
  Cũ (31, MIN_TABLES): 10 + 10 + 11
  Mới (42, MIN_TABLES): 10 + 10 + 10 + 12

Bước 3: So sánh
  Bàn cũ: 3 bàn → Bàn mới: 4 bàn (+1)
  Phân bổ lại món PER_TABLE cho bàn mới

Bước 4: Nếu đang Serving
  - Giữ nguyên bàn cũ (đã phục vụ)
  - Thêm bàn mới cho khách thêm
  - KHÔNG chia lại bàn cũ
```

**Tình huống:** 31 → 25 khách (-6)

```
Bước 1: Chia mới với strategy
  Cũ (31, MIN_TABLES): 10 + 10 + 11
  Mới (25, MIN_TABLES): 10 + 10 + 5

Bước 2: Nếu Pre-service:
  - Cập nhật: bỏ 1 bàn, bàn còn lại 10 + 10 + 5
  - Giảm món PER_PERSON: -6 suất
  - Giảm món PER_TABLE: -1 (bớt 1 bàn)

Bước 3: Nếu During-service:
  - Cảnh báo: "Đã chuẩn bị cho 31 khách"
  - Tính phí cho 6 suất không dùng (nếu đã chế biến)
  - Giữ nguyên bàn (không xóa bàn đã setup)
```

### 4.7 Tham số cấu hình

| Tham số | Mặc định | Mô tả |
|---|---|---|
| `DefaultTableStandard` | 10 | Chuẩn bàn mặc định |
| `MinTableGuests` | 4 | Số khách tối thiểu 1 bàn (dưới → cảnh báo) |
| `MaxTableGuests` | 14 | Số khách tối đa 1 bàn (trên → cảnh báo) |
| `DefaultAllocationStrategy` | MIN_TABLES | Chiến lược mặc định |
| `AllowUnevenLastTable` | true | Cho phép bàn cuối ít hơn chuẩn |
| `PreferEvenDistribution` | false | Ưu tiên chia đều (override MIN_TABLES) |
| `RespectRoomLayout` | true | Tôn trọng layout phòng (nếu có) |

---

## 5. MENU CALCULATION ENGINE V2

### 5.1 Đánh giá V1 → V2

V1 có 4 quy tắc (PER_PERSON, PER_TABLE, PER_ROOM, MANUAL). V2 bổ sung thêm các quy tắc từ thực tế Fine Dining.

### 5.2 Bộ quy tắc hoàn chỉnh

#### 5.2.1 PER_PERSON (Theo người)

**Dùng cho:** Món mỗi người 1 phần riêng.

```
Tôm hùm: 1 con/người
31 khách → 31 con
```

**Biến thể:**
- `PER_PERSON_ADULT`: Chỉ tính người lớn (rượu, bia)
- `PER_PERSON_CHILD`: Chỉ tính trẻ em (nước ngọt trẻ em)
- `PER_PERSON_TOTAL`: Tính tất cả (người lớn + trẻ em × childRate)

#### 5.2.2 PER_TABLE (Theo bàn)

**Dùng cho:** Món chia sẻ giữa bàn.

```
Lẩu: 1 nồi/bàn
4 bàn → 4 nồi
```

#### 5.2.3 PER_ROOM (Theo phòng)

**Dùng cho:** Dịch vụ/món setup 1 lần cho cả phòng.

```
Màn LED: 1 gói/phòng
Trang trí: 1 gói/phòng
```

#### 5.2.4 PER_HOURLY (Theo giờ)

**Dùng cho:** Dịch vụ tính theo thời gian.

```
MC: 3 giờ đầu 800.000đ/giờ, giờ thêm 500.000đ/giờ
Ca sĩ: 2.500.000đ/60 phút

Sự kiện 11:00-15:00 (4 giờ):
  MC = 3×800.000 + 1×500.000 = 2.900.000đ
```

**Cấu hình:**
```
TimeSlotPricing: [
  { Hours: 3, UnitPrice: 800000, Unit: "giờ" },
  { Hours: null, UnitPrice: 500000, Unit: "giờ thêm" }
]
```

#### 5.2.5 PER_PACKAGE (Theo gói)

**Dùng cho:** Gói dịch vụ trọn gói, không tách lẻ.

```
Gói Sinh Nhật VIP: 8.500.000đ/gói
Gói Trang Trí Cưới: 15.000.000đ/gói

→ Giá cố định, không phụ thuộc số khách/số bàn
→ Nếu trong gói có món lẻ → tính theo quy tắc riêng
```

#### 5.2.6 PER_EVENT (Theo sự kiện)

**Dùng cho:** Món/dịch vụ chỉ tồn tại 1 lần cho toàn bộ sự kiện.

```
Bánh cưới: 1 cái/SỰ KIỆN
Rượu Champagne khai vị: 1 chai/SỰ KIỆN
Cổng hoa: 1 cái/SỰ KIỆN

→ Không phụ thuộc phòng (nếu sự kiện dùng 2 phòng, vẫn 1 bánh)
→ Không phụ thuộc số khách
```

**Khác PER_ROOM:** PER_ROOM gắn với 1 phòng vật lý. PER_EVENT gắn với sự kiện logic (có thể dùng nhiều phòng).

#### 5.2.7 PER_ADULT / PER_CHILD (Theo độ tuổi)

**Dùng cho:** Phân biệt giá người lớn / trẻ em.

```
Buffet: 650.000đ/người lớn, 325.000đ/trẻ em
28 người lớn + 4 trẻ em:
  = 28 × 650.000 + 4 × 325.000
  = 18.200.000 + 1.300.000
  = 19.500.000đ
```

#### 5.2.8 PER_TIERED (Chiết khấu số lượng)

**Dùng cho:** Giá giảm dần khi số lượng tăng.

```
Bào ngư:
  1-20 con: 350.000đ/con
  21-40 con: 320.000đ/con
  41+ con: 290.000đ/con

31 khách, PER_PERSON:
  → 31 con × 320.000đ = 9.920.000đ
```

#### 5.2.9 PER_MINIMUM (Số lượng tối thiểu)

**Dùng cho:** Món yêu cầu số lượng tối thiểu.

```
Heo sữa quay: tối thiểu 15 khách/phòng mới đặt
→ Phòng 12 khách: không đặt được
→ Cảnh báo khi thêm vào báo giá
```

### 5.3 Ma trận quy tắc × loại món

| Loại món | PER_PERSON | PER_TABLE | PER_ROOM | PER_HOURLY | PER_PACKAGE | PER_EVENT | PER_ADULT/CHILD | PER_TIERED |
|---|---|---|---|---|---|---|---|---|
| Hải sản đắt | ✓✓✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ |
| Lẩu | ✗ | ✓✓✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| Dịch vụ AV | ✗ | ✗ | ✓✓✓ | ✓✓ | ✓ | ✗ | ✗ | ✗ |
| MC/Ca sĩ | ✗ | ✗ | ✗ | ✓✓✓ | ✗ | ✗ | ✗ | ✗ |
| Buffet | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✓✓✓ | ✗ |
| Trang trí | ✗ | ✗ | ✓✓✓ | ✗ | ✓✓ | ✗ | ✗ | ✗ |
| Bánh cưới | ✗ | ✗ | ✗ | ✗ | ✗ | ✓✓✓ | ✗ | ✗ |
| Rượu | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓✓ | ✓ |

(✓✓✓ = primary, ✓ = có thể dùng)

---

## 6. NHỮNG NGHIỆP VỤ CÒN THIẾU

### 6.1 Waitlist (Danh sách chờ)

**Vấn đề:** Khách muốn đặt VIP 1 nhưng đã có người đặt. Khách sẵn sàng chờ nếu có hủy.

**Giải pháp:**
- Khi phòng đầy → cho khách đăng ký Waitlist
- Khi có hủy → tự động thông báo cho khách đầu danh sách
- Thời gian giữ: 30 phút để khách phản hồi
- Nếu khách không phản hồi → chuyển sang khách tiếp theo

### 6.2 Recurring / Định kỳ

**Vấn đề:** Công ty đặt VIP 1 mỗi thứ 6 tuần cuối cùng của tháng.

**Giải pháp:**
- Tạo báo giá định kỳ: "Mỗi thứ 6 cuối tháng, VIP 1, Set Doanh Nghiệp"
- Hệ thống tự động tạo báo giá cho các kỳ tiếp theo
- Có thể sửa từng kỳ riêng (vd: tháng này thêm món)

### 6.3 Pre-order Nguyên liệu

**Vấn đề:** Báo giá có Tôm hùm Alaska — cần đặt trước 3 ngày.

**Giải pháp:**
- Mỗi MenuItem có `LeadTimeDays` (số ngày cần đặt trước)
- Khi duyệt báo giá → kiểm tra lead time → nếu không đủ → cảnh báo
- Tự động tạo PO (Purchase Order) cho bếp trưởng

### 6.4 Service Recovery (Đền bù sự cố)

**Vấn đề:** Món bị cháy, phục vụ chậm, khách không hài lòng.

**Giải pháp:**
- Nhân viên ghi nhận sự cố: `ServiceIncident`
- Đề xuất đền bù: giảm giá X%, tặng món Y, tặng voucher lần sau
- Manager duyệt đền bù
- Ghi vào lịch sử khách hàng

### 6.5 Phí phục vụ (Service Charge) cho VIP Room

**Vấn đề:** À La Carte VIP có nên tính phí phục vụ không? Bao nhiêu %?

**Khuyến nghị:**
- Fine Dining / Hybrid: 8-10% (phục vụ riêng từng người)
- Sharing: 5% (phục vụ đơn giản hơn)
- Buffet VIP: 5% (khách tự chọn)
- Set Menu: 5-8%
- Cấu hình theo Service Style

### 6.6 Tích hợp với CRM

**Vấn đề:** Khách VIP cần được nhận diện khi gọi điện.

**Giải pháp:**
- Nhập SĐT → tự động hiện tên, lịch sử đặt phòng, món ưa thích, dị ứng
- Phân hạng khách: VIP Platinum / Gold / Silver / New
- Tự động áp dụng ưu đãi theo hạng

### 6.7 Báo cáo vận hành

| Báo cáo | Mô tả |
|---|---|
| Phòng sử dụng theo ngày/tuần/tháng | Tỷ lệ lấp đầy, doanh thu/phòng |
| Món bán chạy theo mùa | Để lên menu template mới |
| Hiệu suất nhân viên | Ai bán được nhiều báo giá nhất, giá trị trung bình |
| Tỷ lệ hủy | Phòng nào hay bị hủy, lý do hủy |
| Lợi nhuận/phòng | Sau khi trừ chi phí nguyên liệu + nhân viên |

---

## 7. NHỮNG TÌNH HUỐNG BIÊN BỔ SUNG

### 7.1 Khách book 2 phòng cùng lúc

**Xử lý:**
- Tạo 1 báo giá với `RoomCount = 2`, `SelectedRooms = [VIP1, VIP2]`
- Menu giống nhau cho cả 2 phòng? → Copy
- Menu khác nhau? → Tách thành 2 section trong cùng báo giá
- Bàn tổng: 1 bàn tổng cho cả 2 phòng (để thanh toán 1 lần)

### 7.2 Sự kiện kéo dài qua nửa đêm

**Xử lý:**
- `EndTime > 23:00` → cảnh báo "Phụ thu ngoài giờ"
- Cấu hình phụ thu: `OvertimeRate = 500.000đ/giờ`
- Nhân viên phục vụ ca đêm → cần phân công riêng

### 7.3 Khách yêu cầu đổi toàn bộ menu ngay trước giờ phục vụ

**Xử lý:**
- Cảnh báo: "Đã chuẩn bị nguyên liệu cho menu cũ"
- Đề xuất: giữ nguyên món đã chuẩn bị, bổ sung món mới
- Tính thêm chi phí phát sinh

### 7.4 Nhân viên nghỉ đột xuất — thiếu người phục vụ

**Xử lý:**
- Cảnh báo: "VIP 1 cần 2 nhân viên, hiện chỉ có 1"
- Đề xuất: điều nhân viên từ phòng khác, hoặc giảm chất lượng phục vụ
- Ghi nhận để tính lương / đền bù

### 7.5 Khách thanh toán 1 phần, phần còn lại ghi nợ

**Xử lý:**
- Cho phép thanh toán từng phần (split payment)
- Phần còn lại → tạo công nợ
- Gửi nhắc nợ tự động sau X ngày

---

## 8. KHUYẾN NGHỊ TRƯỚC KHI LẬP TRÌNH

### 8.1 Kiến trúc

1. **Tách module À La Carte VIP khỏi POS Cà phê** — đừng share entity `DiningTable`
2. **Menu Calculation Engine là service riêng** — gọi được từ Quote, Order, Recalculation
3. **Table Allocation Engine là service riêng** — nhận input (guestCount, strategy, layout) → output (table list)
4. **Approval Workflow dùng State Machine pattern** — mỗi trạng thái có allowedActions, guard conditions
5. **Audit Log mọi thay đổi** — đặc biệt từ THƯƠNG LƯỢNG trở đi

### 8.2 Dữ liệu

1. **Không dùng DiningTable cố định cho bàn phục vụ** — tạo `OrderTable` sinh động
2. **MenuTemplate & RoomPackage là reference data** — không modify inline khi dùng
3. **Mỗi dòng báo giá/đơn phải lưu PricingRule** — không suy từ menu item (có thể bị override)
4. **Lưu toàn bộ lịch sử thương lượng** — `QuoteRevision` table

### 8.3 UI/UX

1. **Calendar View là màn hình chính** — nhân viên mở app là thấy lịch phòng
2. **Wizard tạo báo giá** — step 1: khách hàng → step 2: chọn phòng + giờ → step 3: chọn template/package → step 4: tùy chỉnh → step 5: xác nhận
3. **Sơ đồ bàn kéo thả** — visual table allocation, có thể kéo thả khách giữa các bàn
4. **Side-by-side diff** — khi thương lượng, hiển thị old vs new

### 8.4 Thứ tự triển khai

| Phase | Nội dung | Thời gian dự kiến |
|---|---|---|
| P0 | Quote CRUD + 4 pricing rules + Auto table division + Basic workflow | 2-3 tuần |
| P1 | Calendar View + Approval workflow đầy đủ + Recalculation engine | 2-3 tuần |
| P2 | Menu Template + Room Package + Table Allocation Engine V2 | 2 tuần |
| P3 | Kitchen Integration + Inventory + CRM | 2-3 tuần |
| P4 | Reports + Waitlist + Recurring + Loyalty | 2-3 tuần |

### 8.5 Sai lầm phổ biến cần tránh

1. **Dùng chung bàn giữa POS Cà phê và À La Carte** → HỎNG KIẾN TRÚC
2. **Hardcode 4 pricing rules** → phải extensible (thêm rule mới không cần sửa engine)
3. **Không có audit log** → không truy vết được ai sửa gì khi có tranh chấp
4. **Workflow quá đơn giản** → không phản ánh đúng quy trình thương lượng thực tế
5. **Bỏ qua Service Style** → không phân biệt được Fine Dining vs Sharing
6. **Không có Calendar View** → nhân viên không biết phòng nào trống
7. **Tính giá theo bàn cố định** → không linh hoạt khi số khách thay đổi
