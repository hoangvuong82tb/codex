# REISSUE KEY CARD — UI IMPLEMENTATION REPORT

**Project:** D:\ASMILE-BO  
**Module:** Front Office → Quản lý chìa khóa → Cấp lại thẻ  
**Window:** ReissueKeyCardWindow  
**Date:** 2026-07-25  
**Build:** PASS (0 errors)

---

## 1. Card & Command
`KeyManagementWorkspaceView.xaml.cs` → `OnReissue` handler mở `ReissueKeyCardWindow` với duplicate protection.

## 2. ReissueKeyCardWindow
- 1580×900, MinWidth 1360
- Workspace: Left (Guest Browser 280px) | Center (5 tabs) | Right (Inspector 260px) | Bottom Action Bar
- Khác với IssueKeyCardWindow: workflow thay thế thẻ, không phải cấp mới

## 3. Header
Icon thay thế (đỏ #DC2626), tiêu đề "Cấp lại thẻ", subtitle

## 4. Summary Strip
Khách, Phòng, Res, Đến, Đi, Thẻ, Trạng thái, VIP badge

## 5. Guest Browser (Left)
- 300 mock guests, filter status, search, pager 20/50/100
- Mỗi card: Room (font 15) + loại phòng, status badge + "Cần cấp lại" badge (nếu có), Name + VIP, dates + balance, số thẻ
- Cần cấp lại: badge cam "Cần cấp lại" cho khách có thẻ Lost/Broken/Expired/Disabled

## 6. Center Tabs (5 tabs)
- **Tab 1 — Thông tin khách**: Tên, Res, Stay, Phòng, Đến, Đi, Công ty, Membership, VIP
- **Tab 2 — Thẻ hiện tại**: Card list với status badge màu (Active=xanh, Lost=đỏ, Broken=cam, Expired=xám), thông tin chi tiết từng thẻ, click để chọn thẻ cần cấp lại
- **Tab 3 — Cấp lại**: Hiển thị thẻ đang chọn (đỏ, nổi bật), 8 lý do (Mất thẻ, Hỏng, Khử từ, Quên trong phòng, Hư hại, An ninh, Khách yêu cầu, Khác), 4 chế độ (Thay thế ngay, Cấp thêm, Thẻ tạm, Thẻ khẩn cấp), Thời hạn + Encoder
- **Tab 4 — Xử lý thẻ cũ**: 6 quy tắc (Vô hiệu hóa ngay, Giữ hoạt động, Sau 30 phút, Khi check-out, Yêu cầu trả thẻ, Cần phê duyệt) với mô tả chi tiết
- **Tab 5 — Lịch sử**: Timeline + Audit Trail (cập nhật sau mỗi lần cấp lại)

## 7. Inspector (Right)
- Thông tin lưu trú, thẻ (tổng, cần cấp lại, đang chọn)
- Cảnh báo: cần cấp lại, VIP, thẻ bị mất, nhiều thẻ
- Blocking: đã check-out, chưa đến ngày

## 8. Bottom Action Bar
Thẻ cũ, Lý do, Chế độ, Xử lý thẻ cũ, Encoder + blocking count + nút Hủy/Cấp lại thẻ

## 9. Mock Data
- 300 guests, 700 cards (Active, Lost, Broken, Expired, Returned, Disabled)

## 10. Files
| File | Status |
|------|--------|
| `Views/ReissueKeyCardWindow.xaml` | Created |
| `Views/ReissueKeyCardWindow.xaml.cs` | Created |
| `Views/KeyManagementWorkspaceView.xaml.cs` | Modified (wire OnReissue) |

## 11. Build & Runtime
- Build: PASS (0 errors)
- No backend/database modifications
