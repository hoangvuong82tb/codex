# ISSUE KEY CARD — UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Quản lý chìa khóa → Cấp thẻ  
**Window:** IssueKeyCardWindow  
**Date:** 2026-07-25  
**Build:** PASS (0 errors)

---

## 1. Card & Command
`KeyManagementWindow.xaml.cs` → `OnIssueKey` handler mở `IssueKeyCardWindow` với duplicate protection.

## 2. IssueKeyCardWindow
- 1520×900, MinWidth 1300
- Workspace layout: Left (Guest Browser 220px) | Center (5 tabs) | Right (Inspector 260px) | Bottom Action Bar

## 3. Header
Icon khóa vàng (#F59E0B), tiêu đề "Cấp thẻ phòng", subtitle

## 4. Summary Strip
Khách, Phòng, Loại phòng, Res, Đến, Đi, Trạng thái, Thẻ (hiện có/tối đa), VIP badge

## 5. Guest Browser (Left)
- 200 mock guests, search + status filter (In-House, Arrival, Checked-Out)
- Pager: 20/50/100
- Mỗi card: Room, Name, VIP/Membership badge, Status, dates, card count
- Click để chọn → cập nhật tất cả tabs + inspector + bottom bar

## 6. Center Tabs (5 tabs)
- **Tab 1 — Thông tin khách**: Tên, Res, Stay, Phòng, Đến, Đi, Công ty, Membership, Quốc tịch, Balance
- **Tab 2 — Thẻ đang hoạt động**: DataGrid 8 cột (Số thẻ, Loại, Ngày cấp, Người cấp, Hết hạn, Trạng thái, Encoder, SD cuối), 500 mock cards
- **Tab 3 — Cấp thẻ mới**: Loại thẻ (Guest/Extra/VIP/Staff/Housekeeping), Số lượng (1-4), Thời hạn (Checkout/Custom), Encoder (1/2/3), Tùy chọn (Thu hồi thẻ cũ, Tự kích hoạt, Yêu cầu ký nhận, In phiếu)
- **Tab 4 — Quyền truy cập**: 9 khu vực (Phòng, Thang máy, Parking, Gym, Pool, Spa, Lounge, Meeting, Restaurant)
- **Tab 5 — Lịch sử**: Timeline (Cấp thẻ, Kích hoạt, Check-in)

## 7. Inspector (Right)
- Thông tin lưu trú (Khách, Phòng, Res, Stay, Trạng thái)
- Khóa cửa (Online Mock, Encoder)
- Thẻ (đang hoạt động / tối đa)
- Cảnh báo: Balance lớn, VIP, Check-out hôm nay
- Blocking: Đã check-out, Chưa đến ngày, Đạt max thẻ

## 8. Bottom Action Bar
- Thẻ hiện có, Loại, Số lượng, Hết hạn, Encoder
- Blocking/Warning count badges
- Nút Hủy + Cấp thẻ (disabled nếu có blocking)
- Issue confirmation dialog (MessageBox mockup)

## 9. Mock Data
- 200 guests (In-House, Arrival, Checked-Out, VIP, multi-membership)
- 500 cards (Active, Expired, Lost, Returned, Disabled)

## 10. Files
| File | Status |
|------|--------|
| `Views/IssueKeyCardWindow.xaml` | Created |
| `Views/IssueKeyCardWindow.xaml.cs` | Created |
| `Views/KeyManagementWindow.xaml.cs` | Modified (wire OnIssueKey) |

## 11. Build & Runtime
- Build: PASS (0 errors)
- No backend/database modifications
