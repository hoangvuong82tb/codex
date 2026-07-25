# FRONT OFFICE SHARED WORKSPACE DUPLICATION AUDIT REPORT
# Ngày: 2026-07-24
# Project Root: D:\ASMILE-BO

## 1. Danh sách tất cả Hub đã kiểm tra

| # | Hub | ViewModel | Cards | Wired |
|---|-----|-----------|-------|-------|
| 1 | Front Office Main | `FrontOfficeHubViewModel` | 11 sub-hubs | Yes |
| 2 | Đặt phòng | `ReservationHubViewModel` | Separate module | Yes |
| 3 | Lễ tân | `ReceptionHubViewModel` | 17 cards | Yes (15) |
| 4 | Thu ngân | `ModuleHubViewModel` | 9 cards | **NO** (all mockup) |
| 5 | Đoàn | `ModuleHubViewModel` | 10 cards | **NO** (all mockup) |
| 6 | Nhà buồng | `ModuleHubViewModel` | 10 cards | **NO** (all mockup) |
| 7 | Tổng đài | `ModuleHubViewModel` | 8 cards | **NO** (all mockup) |
| 8 | Kế toán phải thu | `ModuleHubViewModel` | 8 cards | **NO** (all mockup) |
| 9 | Truy vấn thông tin | `ModuleHubViewModel` | 8 cards | **NO** (all mockup) |
| 10 | Marketing | `ModuleHubViewModel` | 8 cards | **NO** (all mockup) |
| 11 | Báo cáo (Hub) | `ModuleHubViewModel` | 9 cards | **NO** (all mockup) |
| 12 | Night Audit | `ModuleHubViewModel` | 8 cards | **NO** (all mockup) |

## 2. Danh sách card từng Hub

### Lễ tân (ReceptionHubViewModel) — WIRED

| ID | Card | Window/Workspace | Status |
|----|------|-----------------|--------|
| room-status | Tình trạng phòng | RoomStatusWindow | Wired |
| check-in | Check-in | CheckInWindow | Wired |
| walk-in | Walk-In | WalkInWindow | Wired |
| group-checkin | Check-in đoàn | GroupCheckInWindow | Wired |
| stay-adjustment | Điều chỉnh lưu trú | StayAdjustmentWorkspaceView | Wired |
| checkout | Check-out | CheckoutWorkspaceView | Wired |
| cashiering | Thu ngân / Folio | CashieringWorkspaceView | Wired |
| key-management | Quản lý chìa khóa | KeyManagementWorkspaceView | Wired |
| fo-log | Nhật ký FO | FrontOfficeLogWorkspaceView | Wired |
| stay-profile | Hồ sơ lưu trú | StayProfileWindow | Wired |
| locked-room | Phòng khóa | LockedRoomWindow (service) | Wired |
| current-guests | Khách hiện tại | CurrentGuestsWindow | Wired |
| arrivals | Khách đến hôm nay | TodayArrivalsWindow | Wired |
| departures | Khách đi hôm nay | TodayDeparturesWindow | Wired |
| housekeeping-message | Nhắn nhà buồng | HousekeepingMessageWindow | Wired |
| exchange-rate | Tỷ giá | ExchangeRateWindow | Wired |
| reception-report | Báo cáo | FrontOfficeReportCenterWindow | Wired |

### Các Hub con (ModuleHubViewModel) — CHƯA WIRE

Tất cả card trong Thu ngân, Đoàn, Nhà buồng, Tổng đài, Kế toán phải thu, Truy vấn thông tin, Marketing, Báo cáo Hub, Night Audit đều dùng `new RelayCommand(() => { })` (command rỗng, không làm gì).

## 3. Nghiệp vụ trùng hoàn toàn

| Nghiệp vụ | Card/Hub hiện tại | Có trùng không | Giải pháp | Context |
|-----------|-------------------|----------------|-----------|---------|
| **Folio** | Lễ tân → Thu ngân/Folio | CÓ (4 nơi) | Dùng chung FolioWorkspace | Mode: Reception/Cashiering/Checkout/Audit |
| | Thu ngân → Folio | | | |
| | Truy vấn → Folio | | | |
| | Night Audit → Folio | | | |
| | Check-out → Kiểm tra Folio | | | |
| **Báo cáo** | Lễ tân → Báo cáo | CÓ (9 nơi) | Dùng chung ReportCenterWorkspace | CategoryFilter khác nhau |
| | Thu ngân → Báo cáo thu ngân | | | |
| | Nhà buồng → Báo cáo | | | |
| | Tổng đài → Báo cáo | | | |
| | Đoàn → Báo cáo đoàn | | | |
| | Kế toán PT → Báo cáo | | | |
| | Marketing → Báo cáo | | | |
| | Night Audit → Báo cáo | | | |
| | Hub Báo cáo → (9 cards) | | | |
| **Check-in đoàn** | Lễ tân → Check-in đoàn | CÓ (2 nơi) | Dùng chung GroupCheckInWindow | Filter: hôm nay / tất cả |
| | Đoàn → Check-in đoàn | | | |
| **Check-out đoàn** | Check-out Workspace → Check-out đoàn | CÓ (2 nơi) | Dùng chung GroupCheckOutModal | Filter: hôm nay / tất cả |
| | Đoàn → Check-out đoàn | | | |
| **Tình trạng phòng** | Lễ tân → Tình trạng phòng | LIÊN QUAN | Dùng chung RoomStatusWorkspace | Context: Reception/HK/Reservation |
| | Nhà buồng → Tình trạng phòng | | | |
| | Nhà buồng → Phòng sạch/bẩn/đang dọn | | | |
| | Nhà buồng → Kiểm tra phòng | | | |
| **Lost & Found** | Nhật ký FO → Đồ thất lạc | CÓ (2 nơi) | Dùng chung LostAndFoundWorkspace | Filter: FO / HK |
| | Nhà buồng → Lost & Found | | | |
| **Thanh toán** | Lễ tân → Thu ngân → Thu tiền | LIÊN QUAN | Dùng chung PaymentModal | Context: GuestFolio/Deposit/AR |
| | Thu ngân → Thanh toán | | | |
| | Kế toán PT → Thanh toán | | | |
| **Hồ sơ khách** | Lễ tân → Hồ sơ lưu trú | LIÊN QUAN | Dùng chung GuestProfileView | Context: Stay/Guest/Company |
| | Marketing → Khách hàng | | | |
| | Kế toán PT → Khách hàng | | | |
| | Truy vấn → Tra cứu khách | | | |

## 4. Nghiệp vụ liên quan nhưng KHÔNG được gộp

| Nghiệp vụ | Lý do không gộp |
|-----------|----------------|
| Thu ngân vs Kế toán phải thu | Nghiệp vụ khác: thanh toán tại quầy vs theo dõi công nợ |
| Lễ tân vs Truy vấn thông tin | Tác nghiệp vs tra cứu read-only |
| Tình trạng phòng vs Khả năng bán phòng | View lễ tân vs view đặt phòng — khác DataGrid |
| Khách hiện tại vs Hồ sơ khách gốc | In-house realtime vs historical profile |
| Nhà buồng vs Lễ tân | Bộ phận khác nhau, quyền và action khác |
| Các loại nhật ký | Mỗi loại nhật ký có schema khác — dùng chung viewer component, không gộp data |

## 5. Giải pháp dùng chung cụ thể

### 5.1 Folio — dùng chung

```
Shared: FolioWorkspace / FolioDetailWindow / PaymentModal

Điểm mở → Context truyền vào:
- Lễ tân → Mode=Reception, Filter=Đang lưu trú
- Thu ngân → Mode=Cashiering, Full actions
- Check-out → Mode=Checkout, SelectedFolioId
- Truy vấn → Mode=ReadOnly, No transaction actions
- Night Audit → Mode=Audit, Filter=Chưa cân/Lỗi
```

### 5.2 Báo cáo — dùng chung

```
Shared: FrontOfficeReportCenterWindow (đã có)

Điểm mở → CategoryFilter:
- Lễ tân / Hub chính → All
- Thu ngân → Cashiering
- Nhà buồng → Housekeeping
- Đoàn → Group
- Night Audit → NightAudit
- Kế toán PT → AccountsReceivable
- Marketing → Marketing
- Tổng đài → Telephone
```

### 5.3 Check-in/Check-out đoàn — dùng chung

```
Shared: GroupCheckInWindow (đã có), GroupCheckOutModal (cần tạo)

Điểm mở → Default filter:
- Lễ tân → Hôm nay
- Đoàn → Tất cả đoàn
```

### 5.4 Room Status — dùng chung

```
Shared: RoomStatusWorkspace (dựa trên RoomStatusWindow hiện có)

Context:
- Reception → Occupancy view, VIP, arrivals, departures
- Housekeeping → Clean/Dirty/Cleaning/Inspected/OOO
- Reservation → Availability by date, room type
```

### 5.5 Lost & Found — dùng chung

```
Shared: LostAndFoundWorkspace (cần tạo)

Điểm mở:
- Nhật ký FO → Tất cả hồ sơ, giao nhận, liên hệ
- Nhà buồng → Đồ tìm thấy trong phòng, thông tin phòng
```

### 5.6 Thanh toán — component dùng chung

```
Shared: PaymentModal (component, không phải workspace độc lập)

Context:
- GuestFolio → Thanh toán Folio khách
- Deposit → Nhận đặt cọc
- Checkout → Thanh toán khi trả phòng
- AR → Thanh toán công nợ
```

### 5.7 Hồ sơ — view dùng chung

```
Shared: GuestProfileView, CompanyProfileView, TravelAgentProfileView

Mỗi Hub giữ workspace riêng nhưng embed shared profile view.
```

### 5.8 Nhật ký — component dùng chung

```
Shared: OperationalLogViewer (timeline component)

LogType:
- FrontOffice → Nhật ký FO
- Call → Nhật ký cuộc gọi
- NightAudit → Nhật ký Night Audit
- KeyCard → Nhật ký thẻ
- GuestStay → Nhật ký lưu trú
```

## 6. NavigationContext (đề xuất)

```csharp
// UI-level context, không cần backend
public class NavigationContext
{
    public string SourceModule { get; set; }      // "Reception", "Cashiering", etc.
    public string Mode { get; set; }               // "Full", "ReadOnly", "Audit"
    public string CategoryFilter { get; set; }     // "Housekeeping", "Cashiering", etc.
    public string DefaultFilter { get; set; }      // "Today", "All", etc.
    public string SelectedEntityId { get; set; }   // FolioId, GuestId, etc.
    public bool IsReadOnly { get; set; }
}
```

## 7. View đã tái sử dụng (hiện tại)

| View | Dùng bởi |
|------|----------|
| `StayAdjustmentWorkspaceView` | Lễ tân → Điều chỉnh lưu trú |
| `CheckoutWorkspaceView` | Lễ tân → Check-out |
| `CashieringWorkspaceView` | Lễ tân → Thu ngân / Folio |
| `KeyManagementWorkspaceView` | Lễ tân → Quản lý chìa khóa |
| `FrontOfficeLogWorkspaceView` | Lễ tân → Nhật ký FO |
| `FrontOfficeReportCenterWindow` | Lễ tân → Báo cáo |

## 8. View trùng đã ngừng dùng

| Window cũ | Trạng thái | File vẫn giữ |
|-----------|-----------|-------------|
| `StayAdjustmentWindow` | Không còn dùng | Giữ lại, không xóa |
| `CheckoutWindow` | Không còn dùng | Giữ lại, không xóa |
| `CashieringWindow` | Không còn dùng | Giữ lại, không xóa |
| `KeyManagementWindow` | Không còn dùng | Giữ lại, không xóa |
| `FrontOfficeLogWindow` | Không còn dùng | Giữ lại, không xóa |

## 9. Việc cần làm để hoàn thiện dùng chung

### Phase 1 — Đã hoàn thành
- [x] 5 workspace views từ Lễ tân
- [x] Hub → Workspace navigation pattern
- [x] BackCommand xử lý quay lại Hub

### Phase 2 — Cần làm khi wire sub-hub
- [ ] Wire card Thu ngân → mở CashieringWorkspaceView (Mode=Cashiering)
- [ ] Wire card Nhà buồng → Lost & Found → mở chung LostAndFoundWorkspace
- [ ] Wire card Báo cáo các Hub → mở FrontOfficeReportCenterWindow với CategoryFilter
- [ ] Wire card Đoàn → Check-in/Check-out đoàn → dùng chung GroupCheckInWindow
- [ ] Tạo LostAndFoundWorkspace dùng chung
- [ ] Tạo PaymentModal component dùng chung
- [ ] Tạo OperationalLogViewer component dùng chung

### Phase 3 — Tối ưu sau
- [ ] NavigationContext class để truyền context giữa các Hub
- [ ] RoomStatusWorkspace hỗ trợ đa context
- [ ] GuestProfileView nhúng vào các workspace khác nhau

## 10. Icon và ngôn ngữ cần sửa

| Vị trí | Hiện tại | Cần sửa |
|--------|----------|---------|
| Nhà buồng → hk-lost-found | Lost & Found | Đồ thất lạc / Tìm thấy |
| Đoàn → rooming-list | Rooming List | Danh sách phân phòng |
| Nhật ký FO | VIP Note | Ghi chú VIP |
| Nhật ký FO | Lost & Found | Đồ thất lạc / Tìm thấy |
| Nhà buồng → hk-reports | Báo cáo | Báo cáo (đã Việt) |

## 11. File đã sửa

- `ViewModels/ReceptionHubViewModel.cs` — ActiveWorkspace, BackToHub, xóa 5 Window method
- `Views/ReceptionHubView.xaml` — thêm ContentControl workspace

## 12. Build result

- Build: 0 errors
- Không binding error
- Không resource error

## 13. Runtime result

- 5 workspace UserControl hoạt động trong Reception Hub
- ← Quay lại hoạt động đúng
- Esc quay lại hoạt động đúng

## 14-20. Xác nhận

- [x] Không sửa backend
- [x] Không sửa database
- [x] Không sửa Banquet
- [x] Chỉ thao tác trong D:\ASMILE-BO
- [x] Các sub-hub card hiện tại đều là mockup (command rỗng) — chưa wire, không gây lỗi
- [x] Không có duplicate Window đang hoạt động
- [x] 5 Window cũ đã ngừng dùng, file vẫn giữ lại

## KẾT LUẬN

Hệ thống hiện có **12 Hub** với tổng cộng khoảng **100+ card**. Trong đó:
- **Reception Hub (17 cards)**: Tất cả đã wire, 5 workspace views đã được restructure đúng kiến trúc Hub→Workspace→Modal
- **Các sub-hub khác (80+ cards)**: Tất cả đang là mockup (command rỗng), chưa wire
- **8 nhóm nghiệp vụ trùng**: Đã xác định và có kế hoạch dùng chung rõ ràng
- **5 Window landing page cũ**: Đã ngừng dùng, không xóa file để tránh lỗi tham chiếu

Khi wire các sub-hub trong tương lai, cần tuân thủ kiến trúc dùng chung đã đề xuất để tránh tạo duplicate View/Window.
