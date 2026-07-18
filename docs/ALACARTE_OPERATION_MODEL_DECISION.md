# ALACARTE_OPERATION_MODEL_DECISION.md

## Quyết định mô hình vận hành — Nhà hàng À La Carte VIP

Ngày: 24/06/2026

---

# TÓM TẮT QUYẾT ĐỊNH

**Chọn: MÔ HÌNH C — HYBRID (Suất nền + Món lẻ + Dịch vụ phòng)**

Đây là mô hình chuẩn của nhà hàng VIP cao cấp tại Việt Nam và quốc tế. Cho phép linh hoạt tối đa trong khi giữ được cấu trúc giá và kiểm soát chi phí.

---

# PHÂN TÍCH 3 MÔ HÌNH

---

## MÔ HÌNH A: BÁN MÓN LẺ (À LA CARTE THUẦN)

### Mô tả

Như POS Bữa sáng nhưng trong phòng VIP. Mỗi món được thêm thủ công, không có suất định sẵn. Nhân viên chọn từng món từ thực đơn, set PricingRule cho từng món.

### Quy trình bán hàng

```
Khách đến → Chọn phòng → Nhập số khách
  → Nhân viên tự chọn từng món từ menu
  → Set PricingRule cho từng món (PerPerson/PerTable/PerRoom)
  → Chia bàn → Báo giá → Duyệt → Phục vụ → Thanh toán
```

### Quy trình phục vụ

- Mỗi món được gọi riêng
- Có thể thêm/hủy món linh hoạt
- Bếp nhận từng món riêng lẻ

### PricingRule

- Nhân viên PHẢI chọn rule cho TỪNG món
- Dễ sai: chọn PerTable thay vì PerPerson
- Không có fallback từ suất

### Chia bàn

- Tính từ tổng số khách
- Mỗi món phân phối theo PricingRule đã chọn

### Thêm/Giảm khách

- Phải tính lại TỪNG MÓN thủ công
- Dễ bỏ sót món
- Không có automation

### Thêm món phát sinh

- Đơn giản: chọn món → set rule → thêm vào bàn

### Thanh toán

- Tổng hợp tất cả món từ các bàn + dịch vụ

### Ưu điểm

| # | Ưu điểm |
|---|---|
| 1 | Linh hoạt tối đa — khách muốn gì gọi đó |
| 2 | Không phụ thuộc vào suất định sẵn |
| 3 | Phù hợp khách walk-in, không đặt trước |
| 4 | Đơn giản về mặt data model (không cần entity Suất) |

### Nhược điểm

| # | Nhược điểm |
|---|---|
| 1 | **Chậm**: Nhân viên phải chọn từng món, set rule cho từng món. Với 10+ món → mất thời gian |
| 2 | **Dễ sai**: PricingRule chọn sai → số lượng sai → thiếu món hoặc dư món |
| 3 | **Không chuyên nghiệp**: Nhà hàng VIP không thể để nhân viên "tự nghĩ" ra menu |
| 4 | **Không có chuẩn hóa**: Mỗi nhân viên làm một kiểu, không consistent |
| 5 | **Khó kiểm soát chi phí**: Không biết giá vốn trung bình/suất |
| 6 | **Không bán được giá cao**: Khách VIP muốn "gói", không muốn "gọi món lẻ" |

### Kết luận: **LOẠI** — Không phù hợp nhà hàng VIP.

---

## MÔ HÌNH B: BÁN SUẤT (SET MENU THUẦN)

### Mô tả

Như Nhà hàng Tiệc nhưng theo phòng. Chỉ bán các suất định sẵn (VIP A, VIP B, VIP C...). Không cho phép thay đổi món trong suất. Không cho phép gọi món lẻ.

### Quy trình bán hàng

```
Khách đến → Chọn phòng → Chọn suất → Nhập số khách
  → Hệ thống tự tính: Suất × Số khách = Tổng
  → Chia bàn → Báo giá → Duyệt → Phục vụ → Thanh toán
```

### Quy trình phục vụ

- Bếp nhận ticket theo suất
- Mỗi bàn nhận cùng 1 set món
- Không thay đổi được

### PricingRule

- Mỗi món trong suất có PricingRule cố định
- Không cần nhân viên chọn
- Không thể sai

### Chia bàn

- Tự động từ số khách
- Món PerPerson phân phối đều

### Thêm/Giảm khách

- Đơn giản: GuestCount × Suất → tự động tính lại
- Chỉ cần rebuild bàn

### Thêm món phát sinh

- **Không được phép** trong suất thuần
- Nếu khách muốn thêm → không làm được

### Thanh toán

- Tổng = Suất × Khách + Dịch vụ

### Ưu điểm

| # | Ưu điểm |
|---|---|
| 1 | **Chuẩn hóa**: Mọi nhân viên bán cùng một cách |
| 2 | **Nhanh**: Chọn suất → xong |
| 3 | **Không sai PricingRule**: Đã định nghĩa sẵn trong suất |
| 4 | **Chuyên nghiệp**: Đúng chuẩn nhà hàng VIP |
| 5 | **Kiểm soát chi phí**: Biết chính xác giá vốn mỗi suất |
| 6 | **Dễ bán**: Khách VIP quen với "gói", không quen "gọi món" |

### Nhược điểm

| # | Nhược điểm |
|---|---|
| 1 | **Cứng nhắc**: Khách không ăn tôm → không đổi được |
| 2 | **Không gọi thêm được**: Khách muốn thêm 1 món đặc biệt → từ chối? |
| 3 | **Không phù hợp khách khó tính**: VIP hay có yêu cầu riêng |
| 4 | **Không tận dụng được upsell**: Không bán thêm được rượu, món đặc biệt |
| 5 | **Khó cạnh tranh**: Nhà hàng khác cho đổi món, mình không → mất khách |

### Kết luận: **LOẠI** — Quá cứng nhắc cho VIP.

---

## MÔ HÌNH C: HYBRID (SUẤT NỀN + MÓN LẺ + DỊCH VỤ PHÒNG)

### Mô tả

**Suất là nền tảng** — định nghĩa menu chính với PricingRule chuẩn. Trên nền đó:
- **Thay đổi món**: Khách không ăn tôm → đổi sang cua (giá chênh lệch tự động tính)
- **Thêm món lẻ**: Gọi thêm rượu, món đặc biệt, đồ uống
- **Thêm dịch vụ phòng**: MC, âm thanh, trang trí

### Quy trình bán hàng

```
Khách đến → Chọn phòng → Chọn suất nền → Nhập số khách
  → Hệ thống tự bung món từ suất + PricingRule
  → Nhân viên điều chỉnh:
      • Đổi món (nếu khách yêu cầu)
      • Thêm món lẻ (upsell rượu, đặc sản)
      • Thêm dịch vụ phòng (MC, âm thanh)
  → Chia bàn → Báo giá → Duyệt → Phục vụ → Thanh toán
```

### Quy trình phục vụ

- Món trong suất → gửi bếp theo bàn (PricingRule đúng sẵn)
- Món lẻ → gửi bếp theo bàn (PricingRule do nhân viên chọn lúc thêm)
- Dịch vụ phòng → không gửi bếp, giữ ở Bàn Tổng

### PricingRule

- **Món trong suất**: Kế thừa từ `BanquetMenuPackageLine.PricingRule ?? BanquetMenuPackage.PricingMode`
- **Món lẻ thêm vào**: User chọn từ dropdown (PerPerson/PerTable/Manual)
- **Dịch vụ**: Luôn PerRoom
- **Món đổi**: Copy PricingRule từ món gốc trong suất

### Chia bàn

- Tự động từ số khách (TableAllocationEngine)
- Món suất + món lẻ đều phân phối theo PricingRule

### Thêm/Giảm khách

- Rebuild toàn bộ: bàn + món suất + món lẻ
- Món suất tự động tính lại (PerPerson × GuestCount mới)
- Món lẻ tự động tính lại theo PricingRule
- QuoteChangeService.ApplyFullRebuildAsync

### Thêm món phát sinh

- Trong lúc phục vụ: thêm món lẻ, thêm dịch vụ
- Món phát sinh gán vào bàn cụ thể

### Thanh toán

- Bàn Tổng tổng hợp: suất + món lẻ + dịch vụ + VAT

### Ưu điểm

| # | Ưu điểm |
|---|---|
| 1 | **Chuẩn hóa + Linh hoạt**: Suất làm nền, vẫn cho phép tùy biến |
| 2 | **Nhanh**: Chọn suất → 80% menu đã có sẵn. Chỉ thêm 20% tùy biến |
| 3 | **PricingRule đúng**: Món trong suất kế thừa rule chuẩn. Món lẻ user chọn |
| 4 | **Upsell**: Bán thêm rượu, món đặc biệt → tăng doanh thu |
| 5 | **Chuyên nghiệp**: Đúng chuẩn nhà hàng VIP 5 sao |
| 6 | **Kiểm soát chi phí**: Biết giá vốn suất nền + biên lợi nhuận món lẻ |
| 7 | **Phù hợp mọi đối tượng khách**: Khách dễ tính → suất. Khách khó tính → suất + đổi món |
| 8 | **Tận dụng module Định lượng**: Mỗi món trong suất đã có recipe, giá vốn |

### Nhược điểm

| # | Nhược điểm |
|---|---|
| 1 | **Phức tạp hơn 2 mô hình kia**: Cần xử lý cả suất + món lẻ |
| 2 | **Cần UI rõ ràng**: Phân biệt "món trong suất" và "món gọi thêm" |
| 3 | **Đổi món cần logic giá chênh lệch**: Tôm (350k) → Cua (420k) → +70k/khách |
| 4 | **Cần audit trail**: Biết món nào từ suất, món nào thêm sau |

### Kết luận: **CHỌN** — Đây là mô hình chuẩn.

---

# SO SÁNH 3 MÔ HÌNH

| Tiêu chí | A: Món lẻ | B: Suất thuần | C: Hybrid |
|---|---|---|---|
| Tốc độ tạo báo giá | Chậm (5-10 phút) | Nhanh (1 phút) | Nhanh (2-3 phút) |
| Độ linh hoạt | Tối đa | Không | Cao |
| PricingRule đúng | Dễ sai | Luôn đúng | Đúng (suất) + User chọn (lẻ) |
| Chuẩn hóa menu | Không | Có | Có (nền suất) |
| Upsell | Có | Không | Có |
| Phù hợp khách VIP | Thấp | Trung bình | **Cao** |
| Phù hợp khách walk-in | Cao | Thấp | Cao |
| Độ phức tạp triển khai | Thấp | Thấp | Trung bình |
| Khả năng kiểm soát chi phí | Thấp | Cao | Cao |

---

# KIẾN TRÚC ĐỀ XUẤT: HYBRID VỚI SUẤT LÀM NỀN

## Cấu trúc Quote Line với Hybrid

```
Quote
├── QuoteSetLine (suất nền)
│     ├── ServingSetId = "VIP B"
│     ├── GuestCount = 47
│     ├── UnitPrice = 1,500,000 đ/khách
│     └── TotalPrice = 70,500,000 đ
│
├── QuoteFoodLine (món lẻ — thêm/thay đổi)
│     ├── "Rượu vang đỏ" × 5 chai (Manual)
│     ├── "Cua hoàng đế" thay "Tôm hùm" (PerPerson, chênh +70k)
│     └── "Súp bào ngư" thêm mới (PerPerson)
│
├── QuoteServiceLine (dịch vụ phòng)
│     ├── "MC" × 1 (PerRoom)
│     └── "Âm thanh" × 1 (PerRoom)
│
└── TablePlan (chia bàn)
      └── 4 bàn × 10 + 1 bàn × 7
```

## Quy tắc vàng

| Quy tắc | Mô tả |
|---|---|
| **Suất là nền** | Mọi báo giá VIP bắt đầu từ 1 suất. Có thể là suất "Tự do" (0 món) cho khách walk-in |
| **Đổi món giữ PricingRule** | Đổi Tôm (PerPerson) → Cua (PerPerson). Không đổi rule |
| **Thêm món phải chọn PricingRule** | User chọn từ dropdown |
| **Dịch vụ luôn PerRoom** | Không cho phép chọn rule khác |
| **Tự động tính chênh lệch giá** | Giá mới - giá cũ → cộng vào tổng |
| **Audit mọi thay đổi** | QuoteAuditLog ghi: món nào đổi, món nào thêm |

## PricingRule trong Hybrid

```
Món từ suất → PricingRule từ PackageLine (không cho sửa)
Món đổi     → Copy PricingRule từ món gốc (không cho sửa)
Món lẻ      → User chọn từ dropdown [PerPerson, PerTable, Manual]
Dịch vụ     → Luôn PerRoom (không có dropdown)
```

---

# KẾT LUẬN

**Chọn Mô hình C — Hybrid.**

Đây không phải là "ý kiến" — đây là cách mà mọi nhà hàng VIP cao cấp đang vận hành:

- **Park Hyatt, InterContinental, JW Marriott**: Set menu + optional upgrades
- **Nhà hàng Trung Hoa VIP (Crystal Jade, Royal Pavilion)**: Package + live seafood add-ons
- **Nhà hàng Nhật VIP (Nobu, Umenokoji)**: Omakase base + à la carte additions
- **Nhà hàng Việt VIP (TUNG, Esta, La Maison)**: Tasting menu + wine pairing

Tất cả đều dùng Hybrid. Không ai dùng món lẻ thuần (quá chợ). Không ai dùng set menu cứng (quá nhà máy).

**Lộ trình triển khai cho ASMILE:**

1. **P0**: Hoàn thiện Suất (CRUD BanquetMenuPackage + tích hợp vào Quote flow)
2. **P1**: Hỗ trợ thêm món lẻ vào quote có suất, đổi món trong suất
3. **P2**: Tự động tính chênh lệch giá khi đổi món
4. **P3**: Upsell suggestion (AI đề xuất món thêm dựa trên suất đã chọn)
