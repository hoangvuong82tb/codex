# CREATE RESERVATION WIZARD V4 REPORT

> **Ngày**: 2026-07-21
> **Kết luận**: WIZARD 7 BƯỚC A–G COMPLETE

---

## 1. LÝ DO BỎ 3 TAB

Thiết kế 3 tab (Khách hàng / Đặt phòng / Bổ sung) nhồi 69 trường vào 3 trang dài → không phải luồng nghiệp vụ PMS. Người dùng yêu cầu wizard từng bước giống ứng dụng tham khảo.

## 2. KIẾN TRÚC WIZARD

```
┌──────────────────────────────────────────────────────────┐
│ [A]▸[B]▸[C]▸[D]▸[E]▸[F]▸[G]   ← Thanh tiến trình 7 bước │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Nội dung bước hiện tại                                  │
│  (mỗi lần chỉ hiển thị 1 bước)                           │
│                                                          │
├──────────────────────────────────────────────────────────┤
│  Bước A/G              [Hủy] [←Quay lại] [Tiếp→] [Tạo]  │
└──────────────────────────────────────────────────────────┘
```

## 3. 7 BƯỚC

| Bước | Tên | Nội dung chính |
|------|-----|---------------|
| **A** | Thông tin lưu trú | Ngày đến/đi, giờ, số đêm, người lớn/trẻ em, hạng phòng, số phòng, mã bữa ăn |
| **B** | Quét giấy tờ | Quét CCCD/Hộ chiếu (disabled), kết quả scan, nút bỏ qua |
| **C** | Hồ sơ khách | Danh xưng, họ tên, giới tính, ngày sinh, quốc tịch, ĐT, email, giấy tờ, khách đi cùng |
| **D** | Thông tin đặt phòng | Nguồn, kênh, công ty, đại lý, market segment, rate code, sale |
| **E** | Tài chính | Giá phòng, tiền tệ, giảm giá, đặt cọc, phương thức TT, guarantee |
| **F** | Ghi chú | Ghi chú đặt phòng, special request, chuyến bay, đón/tiễn |
| **G** | Xác nhận | Tổng hợp toàn bộ dữ liệu, trạng thái validation, nút "Sửa" quay về bước |

## 4. THANH TRẠNG THÁI

| Trạng thái | Màu | Icon |
|-----------|------|------|
| Chưa bắt đầu | Trắng, viền xám | — |
| Đang nhập | Xanh nhạt, viền xanh | ● |
| Hoàn tất | Xanh lá nhạt, viền xanh lá | ✓ |

Có mũi tên ▸ giữa các bước.

## 5. DỮ LIỆU KHI CHUYỂN BƯỚC

- `CurrentStep` là int (0-6) trong Window code-behind
- ViewModel (`ReservationModalWorkspaceViewModel`) được giữ nguyên, không tạo lại
- Dữ liệu nhập ở bước trước được giữ khi quay lại
- Có thể bấm trực tiếp vào bước đã mở khóa (≤ CurrentStep+1)

## 6. NAVIGATION

| Vị trí | Hành vi |
|--------|--------|
| Bấm step trên thanh | Nhảy đến bước đó (nếu đã mở khóa) |
| ← Quay lại | CurrentStep-- |
| Tiếp theo → | CurrentStep++ |
| Bước G: "Sửa" | Quay về đúng bước (A, C, D) |

## 7. FILE MODIFIED

| File | Change |
|------|--------|
| `Views/ReservationCreateWindow.xaml` | Complete rewrite — wizard 7 bước |
| `Views/ReservationCreateWindow.xaml.cs` | +WizardStep class, +CurrentStep, +navigation commands |

## 8. BUILD & RUNTIME

| Check | Result |
|-------|--------|
| Build | PASS (0 errors) |
| App launched | YES — PID 51456, ~153MB |
| 7 steps render | YES |
| Step navigation | Working |
| Backend unchanged | ✓ |

---

**WIZARD V4 — 7 BƯỚC A–G COMPLETE**
