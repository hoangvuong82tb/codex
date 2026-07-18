# Banquet Module Audit — ASMILE-BO

> Ngày kiểm tra: 2026-07-10 | Phương pháp: Phân tích tĩnh toàn bộ source code

---

## 1. Executive Summary

### Tổng quan

Module Nhà hàng Tiệc (Banquet) đã có **kiến trúc backend hoàn chỉnh** với Entity → Migration → Service → DTO → API endpoint. Phần **ViewModel đã kết nối với Service** và có đầy đủ command cho CRUD cơ bản. UI (XAML) đã được thiết kế với binding hoàn chỉnh cho form nhập liệu.

Tuy nhiên, module vẫn còn **nhiều khoảng trống quan trọng** cần hoàn thiện trước khi có thể vận hành thực tế.

### Chỉ số

| Chỉ số | Giá trị |
|---|---|
| **Module hoàn thiện tổng thể** | ~**55%** |
| **Tab Tổng quan hoàn thiện** | ~**60%** |
| **Backend hoàn thiện** | ~**80%** |
| **UI hoàn thiện** | ~**65%** |
| **Kết nối UI-Backend** | ~**50%** |
| **Tổng lỗi P0** | 5 |
| **Tổng lỗi P1** | 12 |
| **Tổng lỗi P2** | 18 |
| **Tổng lỗi P3** | 15 |
| **Vị trí mock/hard-code** | 16 |
| **Bảng CSDL thiếu** | 4 |
| **Cột CSDL thiếu** | 8+ |
| **Chức năng UI chưa có backend** | 6 |

### Kết luận

**Chưa thể đưa vào vận hành thực tế.** Cần hoàn thiện ít nhất các hạng mục P0 và P1 trước khi có thể dùng thử nội bộ.

---

## 2. Kiến trúc hiện tại

### Luồng dữ liệu thực tế

```
BanquetRestaurantView.xaml (UI)
  → BanquetRestaurantViewModel (MVVM, CommunityToolkit.Mvvm)
    → IBanquetRestaurantService (interface)
      → BanquetRestaurantService (Infrastructure/Services/RestaurantServices.cs:4037)
        → AppDbContext (EF Core)
          → SQL Server (qua migration)
```

### Các tầng đã implement

| Tầng | Trạng thái | File |
|---|---|---|
| Entity | Hoàn chỉnh | `Core/Entities/RestaurantEntities.cs:389-670` |
| DbContext + Config | Hoàn chỉnh | `Infrastructure/Persistence/AppDbContext.cs:332-343, 3952-4075` |
| Migration | Hoàn chỉnh (2 migrations) | `Migrations/20260614_*`, `Migrations/20260624_*` |
| DTO | Hoàn chỉnh | `Application/DTOs/RestaurantDtos.cs:671-1036` |
| Service Interface | Hoàn chỉnh | `Application/Interfaces/IRestaurantServices.cs:46-73` |
| Service Implementation | Hoàn chỉnh | `Infrastructure/Services/RestaurantServices.cs:4037-4686` |
| DI Registration | Hoàn chỉnh | `Infrastructure/DependencyInjection.cs:159` |
| ViewModel | ~70% | `ViewModels/RestaurantViewModels.cs:9551+`, `BanquetRestaurantViewModel.Partial.cs` |
| View (XAML) | ~65% | `Views/BanquetRestaurantView.xaml`, `Views/Banquet/*.xaml` |

---

## 3. Danh sách màn hình và chức năng

| Tab/Màn hình | ViewModel Tab Index | Trạng thái | Backend | CSDL | Mock | Đánh giá |
|---|---|---|---|---|---|---|
| **Dashboard (Tổng quan)** | 0 | Hoạt động | Có API | Có bảng | Một phần | KPI từ service thật, alert count hard-coded |
| **Lịch phòng** | 1 | UI có, data một phần | Có API GetEvents | Có | Ngày hard-code | Calendar grid có bind Rooms thật nhưng dates cứng |
| **Cài đặt** | 2 | **Chỉ UI, hoàn toàn hard-code** | Không | Không | 100% | Form settings完全不 lưu |
| **Danh sách sự kiện** | 3 | Hoạt động | Có API | Có | Không | Load từ service, filter hoạt động |
| **Vận hành & checklist** | 4 | ViewModel có | Có API | Có | Không | Checklist cross-event trong VM |
| **Báo cáo** | 6 | Một phần | Có API Dashboard | Có | Một phần | KPI từ service, pie chart % hard-code |
| **Phòng tiệc** | 7 | Hoạt động | Có API CRUD | Có | Một phần | Room list từ service, equipment status hard-code |

### Detail tabs (khi chọn sự kiện)

| Detail Tab | SelectedDetailTabIndex | Trạng thái | Backend | Đánh giá |
|---|---|---|---|---|
| **Chọn loại** | 0 | UI hoàn chỉnh | Không cần | Chọn event type, hiển thị cards |
| **Thông tin** | 1 | Hoạt động | SaveEventAsync | Form nhập + live preview, có validation |
| **Thực đơn** | 2 | Hoạt động | AttachMenuPackageAsync | Chọn custom/preset, modal window |
| **Dịch vụ** | 3 | Hoạt động | SaveServiceLineAsync | Thêm/xóa dịch vụ |
| **Checklist** | 4 | Hoạt động | CRUD Checklist | Toggle, thêm, xóa |
| **Timeline** | 5 | Hoạt động | CRUD Timeline | Thêm, xóa mốc thời gian |
| **Tài chính** | 6 | Hoạt động | AddDepositAsync | Xem lịch sử cọc, thêm cọc mới |
| **Hợp đồng** | 7 | Hoạt động | CreateContractAsync | Xem danh sách, tạo hợp đồng |

---

## 4. Phân tích sâu tab Tổng quan (Dashboard + Event Editor)

### 4.1. Dashboard (SelectedTabIndex = 0)

Dashboard là màn hình chính khi mở module, hiển thị:
- **Cột trái (473px)**: Danh sách sự kiện đã lọc + filter chips
- **Cột phải (*)**: Chi tiết sự kiện được chọn hoặc empty state

#### Danh sách sự kiện

| Thành phần | Binding/Command | Trạng thái |
|---|---|---|
| Search box | `EventSearchText` → FilterEvents() | Hoạt động |
| Status filter | `SelectedEventStatusFilter` → FilterEvents() | Hoạt động |
| Type filter | `SelectedEventTypeFilter` → FilterEvents() | Hoạt động |
| Room filter | `SelectedEventRoomFilterId` → FilterEvents() | Hoạt động |
| Event list | `FilteredEvents` ItemsControl | Hoạt động |
| Event count | `FilteredEvents.Count` | Hoạt động |
| Pager | `EventsPager` TablePagerControl | Có binding |
| "Thêm mới" button | `NewEventCommand` | Hoạt động |

#### Filter options

```csharp
// RestaurantViewModels.cs ~line 44-48
EventStatusFilterOptions = ["Tất cả", "Draft", "Hold", "Confirmed", "Preparing", "Completed", "Cancelled"];
EventTypeFilterOptions = ["Tất cả", "Wedding", "Conference", "Birthday", "Meeting", "Party", "Other"];
```

**Vấn đề**: Các option này được hard-code trong ViewModel, không lấy từ danh mục trong DB.

### 4.2. Event Editor (khi chọn sự kiện hoặc tạo mới)

#### Header bar

| Nút | Command | Trạng thái |
|---|---|---|
| Lưu | `SaveEventCommand` | Hoạt động, có validation |
| Xóa | `CancelSelectedEventCommand` | Hoạt động (đổi status thành Cancelled) |
| Sửa | `SaveEventCommand` | Trùng với Lưu — **P1: 2 nút cùng command** |
| In HĐ | `CreateContractCommand` | Hoạt động |

#### Tab "Chọn loại" (DetailIndex=0)

6 event type cards với MouseBinding `SelectEventTypeCommand`. Mỗi card có icon, tên tiếng Việt và tên tiếng Anh.

| Event Type | UI Label | Binding |
|---|---|---|
| Wedding | Tiệc Cưới | `EventType = "Wedding"` |
| Conference | Hội nghị | `EventType = "Conference"` |
| Birthday | Sinh nhật | `EventType = "Birthday"` |
| Meeting | Họp mặt | `EventType = "Meeting"` |
| Party | Tiệc / Party | `EventType = "Party"` |
| Other | Sự kiện khác | `EventType = "Other"` |

#### Tab "Thông tin" (DetailIndex=1)

Đây là **tab quan trọng nhất** — nơi nhập toàn bộ thông tin khách và sự kiện.

**Form bên trái:**

| # | Trường UI | Binding VM | Lưu DB? | Ghi chú |
|---|---|---|---|---|
| 1 | Tên khách hàng | `EventCustomerName` | Có | `BanquetEvent.CustomerName` |
| 2 | Số điện thoại | `EventPhone` | Có | `BanquetEvent.Phone` |
| 3 | Loại-specific field | `BrideName`/`GroomName`/etc. | **KHÔNG** | **P0: Không có trong SaveBanquetEventRequest** |
| 4 | Ngày tổ chức | `EventDate` | Có | `BanquetEvent.EventDate` |
| 5 | Phòng / Sảnh | `SelectedEventRoomId` | Có | `BanquetEvent.BanquetRoomId` |
| 6 | Trạng thái | `EventStatus` | Có | `BanquetEvent.Status` |
| 7 | Giờ bắt đầu — Kết thúc | `StartTimeText`/`EndTimeText` | Có | Parse sang TimeSpan |
| 8 | Số khách — Số bàn | `GuestCount`/`TableCount` | Có | `BanquetEvent.GuestCount/TableCount` |
| 9 | Khách PS — Bàn PS | `ExtraGuestCount`/`ExtraTableCount` | **KHÔNG** | **P0: Không có trong entity** |
| 10 | Ghi chú | `EventNote` | Có | `BanquetEvent.Note` |

**Preview bên phải:** Mirror của form, hiển thị real-time binding để xác nhận trước khi lưu.

#### Validation (ValidateEventEditor)

```csharp
// BanquetRestaurantViewModel.Partial.cs:461-483
- Tên khách hàng không được rỗng
- Số khách > 0
- Số bàn > 0
- Giờ bắt đầu < Giờ kết thúc
- Số khách/bàn không vượt sức chứa phòng
```

**Vấn đề**:
- Không validate số điện thoại
- Không validate trùng lịch phòng (chỉ có ở backend)
- Không kiểm tra khách hàng trùng

#### Backend validation (ValidateEventScheduleAsync)

```csharp
// RestaurantServices.cs:4202-4228
- Ngày tổ chức không được default
- Giờ bắt đầu < Giờ kết thúc
- Số khách > 0
- Số bàn > 0
- Không vượt sức chứa phòng
- Không trùng lịch phòng (cùng phòng, cùng ngày, overlap thời gian, status != Cancelled)
```

### 4.3. Các trường bị thiếu trong DB

Các trường sau **chỉ tồn tại trong ViewModel**, không có trong `SaveBanquetEventRequest` và không được lưu:

| Trường ViewModel | Kiểu | Entity thiếu | Mức độ |
|---|---|---|---|
| `BrideName` | string | `BanquetEvent.BrideName` | P0 |
| `GroomName` | string | `BanquetEvent.GroomName` | P0 |
| `ConferenceName` | string | `BanquetEvent.ConferenceName` | P0 |
| `BirthdayPersonName` | string | `BanquetEvent.BirthdayPersonName` | P0 |
| `BirthdayDate` | DateTime | `BanquetEvent.BirthdayDate` | P0 |
| `MeetingTopic` | string | `BanquetEvent.MeetingTopic` | P0 |
| `PartyTheme` | string | `BanquetEvent.PartyTheme` | P0 |
| `OtherEventName` | string | `BanquetEvent.OtherEventName` | P0 |
| `ExtraGuestCount` | int | `BanquetEvent.ExtraGuestCount` | P0 |
| `ExtraTableCount` | int | `BanquetEvent.ExtraTableCount` | P0 |

---

## 5. Ma trận UI – ViewModel – API – Database

### 5.1. Tab "Thông tin" — Form nhập liệu chính

| UI Field | ViewModel Property | DTO Field | API Endpoint | Entity Property | DB Column | Status |
|---|---|---|---|---|---|---|
| Tên khách hàng | `EventCustomerName` | `SaveBanquetEventRequest.CustomerName` | SaveEventAsync | `BanquetEvent.CustomerName` | `CustomerName` (nvarchar 255) | Hoàn chỉnh |
| Số điện thoại | `EventPhone` | `SaveBanquetEventRequest.Phone` | SaveEventAsync | `BanquetEvent.Phone` | `Phone` (nvarchar 40) | Hoàn chỉnh |
| Cô dâu | `BrideName` | **Thiếu** | **Thiếu** | **Thiếu** | **Thiếu** | **Chỉ có UI** |
| Chú rể | `GroomName` | **Thiếu** | **Thiếu** | **Thiếu** | **Thiếu** | **Chỉ có UI** |
| Ngày tổ chức | `EventDate` | `SaveBanquetEventRequest.EventDate` | SaveEventAsync | `BanquetEvent.EventDate` | `EventDate` (datetime2) | Hoàn chỉnh |
| Phòng/Sảnh | `SelectedEventRoomId` | `SaveBanquetEventRequest.BanquetRoomId` | SaveEventAsync | `BanquetEvent.BanquetRoomId` | `BanquetRoomId` (uniqueidentifier) | Hoàn chỉnh |
| Trạng thái | `EventStatus` | `SaveBanquetEventRequest.Status` | SaveEventAsync | `BanquetEvent.Status` | `Status` (nvarchar 40) | Hoàn chỉnh |
| Giờ bắt đầu | `StartTimeText` | Parse → `SaveBanquetEventRequest.StartTime` | SaveEventAsync | `BanquetEvent.StartTime` | `StartTime` (time) | Hoàn chỉnh |
| Giờ kết thúc | `EndTimeText` | Parse → `SaveBanquetEventRequest.EndTime` | SaveEventAsync | `BanquetEvent.EndTime` | `EndTime` (time) | Hoàn chỉnh |
| Số khách | `GuestCount` | `SaveBanquetEventRequest.GuestCount` | SaveEventAsync | `BanquetEvent.GuestCount` | `GuestCount` (int) | Hoàn chỉnh |
| Số bàn | `TableCount` | `SaveBanquetEventRequest.TableCount` | SaveEventAsync | `BanquetEvent.TableCount` | `TableCount` (int) | Hoàn chỉnh |
| Khách PS | `ExtraGuestCount` | **Thiếu** | **Thiếu** | **Thiếu** | **Thiếu** | **Chỉ có UI** |
| Bàn PS | `ExtraTableCount` | **Thiếu** | **Thiếu** | **Thiếu** | **Thiếu** | **Chỉ có UI** |
| Ghi chú | `EventNote` | `SaveBanquetEventRequest.Note` | SaveEventAsync | `BanquetEvent.Note` | `Note` (nvarchar 1000) | Hoàn chỉnh |
| Loại sự kiện | `EventType` | `SaveBanquetEventRequest.EventType` | SaveEventAsync | `BanquetEvent.EventType` | `EventType` (nvarchar 60) | Hoàn chỉnh |
| Mã KH (CustomerId) | — | `SaveBanquetEventRequest.CustomerId` | SaveEventAsync | `BanquetEvent.CustomerId` | `CustomerId` (int?) | Có binding gián tiếp |

### 5.2. Các tab khác

| Tab | UI Field | Backend Method | Status |
|---|---|---|---|
| Thực đơn | Custom menu items | `AttachMenuPackageAsync` / `DetachMenuPackageAsync` | Hoàn chỉnh |
| Dịch vụ | Service lines | `SaveServiceLineAsync` / `DeleteServiceLineAsync` | Hoàn chỉnh |
| Checklist | Checklist items | `SaveChecklistItemAsync` / `ToggleChecklistAsync` / `DeleteChecklistItemAsync` | Hoàn chỉnh |
| Timeline | Timeline items | `SaveTimelineItemAsync` / `DeleteTimelineItemAsync` | Hoàn chỉnh |
| Tài chính | Deposits | `AddDepositAsync` | Hoàn chỉnh |
| Hợp đồng | Contracts | `CreateContractAsync` | Hoàn chỉnh |

---

## 6. Danh sách dữ liệu mock và hard-code

### 6.1. Bảng tổng hợp

| Mã | File | Vị trí | Nội dung | Loại | Ảnh hưởng | Cách xử lý |
|---|---|---|---|---|---|---|
| MOCK-01 | `BanquetRestaurantView.xaml:124` | Line 124 | `Text="3"` (alert badge count) | Hard-code | P2 — UI hiển thị sai số lượng alert | Bind vào `ImportantAlertCount` |
| MOCK-02 | `BanquetRestaurantView.xaml:125` | Line 125 | `Text="Administrator"` | Hard-code | P2 — Username giả | Bind vào current user từ auth service |
| MOCK-03 | `BanquetRestaurantView.xaml:650-652` | Lines 650-652 | "Âm thanh 8/8", "Ánh sáng 8/8", "LED 7/8", "Máy lạnh 8/8" | Hard-code | P2 — Equipment status giả | Load từ DB hoặc bỏ nếu chưa có tính năng |
| MOCK-04 | `BanquetRestaurantView.xaml:652` | Line 652 | "Kiểm tra ánh sáng sân khấu Sảnh Diamond trước 15h 15/06" | Hard-code | P3 — Ghi chú mẫu | Load từ dữ liệu thật hoặc để trống |
| MOCK-05 | `BanquetRestaurantView.xaml:663-669` | Lines 663-669 | "14/06", "15/06", ... "20/06" | Hard-code | P1 — Calendar dates cố định | Bind vào `OverviewCalendarRows` đã có sẵn trong VM |
| MOCK-06 | `BanquetRestaurantView.xaml:694` | Line 694 | "Tiệc cưới 42%", "Sinh nhật 21%", "Hội nghị 18%" | Hard-code | P2 — Pie chart % giả | Tính từ dữ liệu thật |
| MOCK-07 | `BanquetRestaurantView.xaml:701-713` | Lines 701-713 | Toàn bộ form Cài đặt (tên, mã, địa chỉ, điện thoại, email, website) | Hard-code | P1 — Settings không lưu được | Tạo entity `BanquetSettings` + CRUD |
| MOCK-08 | `RestaurantViewModels.cs:48` | Line 48 | `PaymentMethodOptions = ["Cash", "Transfer", "Card", "Debt"]` | Hard-code list | P2 — Danh mục cứng | Load từ DB hoặc config |
| MOCK-09 | `RestaurantViewModels.cs:44` | Line 44 | `EventTypeOptions = ["Wedding", "Conference", "Birthday", "Meeting", "Party", "Other"]` | Hard-code list | P2 — Danh mục cứng | Load từ bảng danh mục `EventTypes` |
| MOCK-10 | `RestaurantViewModels.cs:45` | Line 45 | `EventStatusOptions = ["Draft", "Hold", "Confirmed", "Preparing", "Completed", "Cancelled"]` | Hard-code list | P1 — Trạng thái cứng | Load từ bảng `EventStatuses` |
| MOCK-11 | `RestaurantViewModels.cs:46` | Line 46 | `ServiceTypeOptions = ["MC", "Singer", "LedScreen", ...]` | Hard-code list | P2 — Danh mục cứng | Load từ DB |
| MOCK-12 | `BanquetRestaurantViewModel.Partial.cs:223-234` | Lines 223-234 | `ServiceTypeNames` dictionary | Hard-code map | P3 — Display name cứng | Load từ bảng danh mục |
| MOCK-13 | `RestaurantServices.cs:1387-1400` | Lines 1387-1400 | `SeedDefaultExtraServicesAsync` — 11 extra services hard-coded | Seed data | P3 — Hợp lệ cho seed | Đây là seed data chấp nhận được |
| MOCK-14 | `RestaurantServices.cs:4494-4495` | Lines 4494-4495 | `AddDefaultChecklist` — 9 checklist items hard-coded | Seed data | P3 — Hợp lệ cho default | OK vì đây là template khởi tạo |
| MOCK-15 | `RestaurantServices.cs:4498-4523` | Lines 4498-4523 | `AddDefaultTimeline` — 5 timeline items hard-coded | Seed data | P3 — Hợp lệ cho default | OK vì đây là template khởi tạo |
| MOCK-16 | `RestaurantViewModels.cs:9577` | Line 9577 | `eventDate = DateTime.Today.AddDays(1)` | Default value | P3 — Giá trị mặc định hợp lý | OK |

### 6.2. Phân loại mock data

| Loại | Số lượng | Cần xử lý ngay |
|---|---|---|
| Hard-code UI (cần bỏ) | 7 | MOCK-01 → MOCK-07 |
| Hard-code list (cần chuyển sang DB) | 5 | MOCK-08 → MOCK-12 |
| Seed data (chấp nhận được) | 3 | MOCK-13 → MOCK-15 |
| Default value (hợp lý) | 1 | MOCK-16 |

---

## 7. Phân tích CSDL

### 7.1. Các bảng hiện có (qua migration)

| Bảng | Entity | Mục đích | Số cột |
|---|---|---|---|
| `BanquetRooms` | `BanquetRoom` | Phòng tiệc/sảnh | 10 |
| `BanquetEvents` | `BanquetEvent` | Sự kiện tiệc | 14 |
| `BanquetMenuPackages` | `BanquetMenuPackage` | Gói thực đơn | 9 |
| `BanquetMenuPackageLines` | `BanquetMenuPackageLine` | Dòng thực đơn trong gói | 8 |
| `BanquetEventMenus` | `BanquetEventMenu` | Thực đơn gắn vào sự kiện | 10 |
| `BanquetServiceLines` | `BanquetServiceLine` | Dịch vụ gắn vào sự kiện | 9 |
| `BanquetContracts` | `BanquetContract` | Hợp đồng tiệc | 10 |
| `BanquetDeposits` | `BanquetDeposit` | Đặt cọc tiệc | 11 |
| `BanquetChecklists` | `BanquetChecklist` | Checklist công việc | 8 |
| `BanquetFloorPlans` | `BanquetFloorPlan` | Sơ đồ mặt bằng | 5 |
| `BanquetFloorPlanTables` | `BanquetFloorPlanTable` | Bàn trong sơ đồ | 10 |
| `BanquetTimelines` | `BanquetTimeline` | Mốc thời gian | 8 |
| `DiningRooms` | `DiningRoom` | Phòng ăn (Alacarte) | 9 |
| `DiningTables` | `DiningTable` | Bàn ăn (Alacarte) | 10 |
| `TableReservations` | `TableReservation` | Đặt bàn (Alacarte) | 11 |
| `AlacarteOrders` | `AlacarteOrder` | Đơn hàng (Alacarte) | 19 |
| `AlacarteOrderLines` | `AlacarteOrderLine` | Dòng đơn hàng | 17 |
| `RestaurantExtraServices` | `RestaurantExtraService` | Dịch vụ bổ sung | 10 |
| `RestaurantOrderExtraServices` | `RestaurantOrderExtraService` | Dịch vụ trong đơn | 8 |
| `RestaurantContracts` | `RestaurantContract` | Hợp đồng (Alacarte) | 10 |
| `RestaurantDeposits` | `RestaurantDeposit` | Đặt cọc (Alacarte) | 11 |
| `RestaurantQuotes` | `RestaurantQuote` | Báo giá | 19+ |
| `RestaurantQuoteLines` | `RestaurantQuoteLine` | Dòng báo giá | 9 |
| `RestaurantQuoteTables` | `RestaurantQuoteTable` | Bàn trong báo giá | 8 |
| ... | ... | ... | ... |

### 7.2. Các bảng còn thiếu

| Bảng cần thêm | Mục đích | Mức độ |
|---|---|---|
| `EventTypes` | Danh mục loại sự kiện | P1 |
| `EventStatuses` | Danh mục trạng thái + quy tắc chuyển đổi | P1 |
| `EventStatusHistories` | Lịch sử thay đổi trạng thái | P1 |
| `ServiceTypes` | Danh mục loại dịch vụ | P2 |
| `BanquetSettings` | Cấu hình module (tên, địa chỉ, email, website,...) | P1 |
| `EventChangeLogs` | Audit log thay đổi sự kiện | P1 |
| `CustomerSources` | Danh mục nguồn khách hàng | P2 |
| `EventAttachments` | File đính kèm sự kiện | P2 |

### 7.3. Các cột còn thiếu trong bảng hiện có

| Bảng | Cột thiếu | Kiểu dữ liệu | Mục đích |
|---|---|---|---|
| `BanquetEvents` | `BrideName` | nvarchar(255) | Tên cô dâu |
| `BanquetEvents` | `GroomName` | nvarchar(255) | Tên chú rể |
| `BanquetEvents` | `ConferenceName` | nvarchar(255) | Tên hội nghị |
| `BanquetEvents` | `BirthdayPersonName` | nvarchar(255) | Tên người sinh nhật |
| `BanquetEvents` | `BirthdayDate` | datetime2 | Ngày sinh |
| `BanquetEvents` | `MeetingTopic` | nvarchar(255) | Chủ đề họp mặt |
| `BanquetEvents` | `PartyTheme` | nvarchar(255) | Chủ đề tiệc |
| `BanquetEvents` | `OtherEventName` | nvarchar(255) | Tên sự kiện khác |
| `BanquetEvents` | `ExtraGuestCount` | int | Số khách phát sinh |
| `BanquetEvents` | `ExtraTableCount` | int | Số bàn phát sinh |
| `BanquetEvents` | `GuestConfirmedCount` | int | Số khách chốt cuối |
| `BanquetEvents` | `TableConfirmedCount` | int | Số bàn chốt cuối |
| `BanquetEvents` | `SetupTime` | time | Giờ setup |
| `BanquetEvents` | `TeardownTime` | time | Giờ dọn |
| `Customers` | `Email` | nvarchar(255) | Email — **đang [NotMapped]** |
| `Customers` | `TaxCode` | nvarchar(50) | MST — **đang [NotMapped]** |
| `Customers` | `CreatedAt` | datetime2 | Ngày tạo — **đang [NotMapped]** |
| `Customers` | `UpdatedAt` | datetime2 | Ngày cập nhật — **thiếu** |

### 7.4. Quan hệ sai hoặc thiếu

| Quan hệ | Vấn đề |
|---|---|
| `BanquetEvent` → `Customer` | `CustomerId` là `int?` nhưng Customer dùng `int Id` — đúng |
| `BanquetEvent.Menus` → `BanquetEventMenu` | Cascade delete — có thể gây mất dữ liệu khi xóa event **P1** |
| `BanquetEvent.Services` → `BanquetServiceLine` | Cascade delete — có thể gây mất dữ liệu khi xóa event **P1** |
| `BanquetEvent.Contracts` → `BanquetContract` | Cascade delete — nguy hiểm **P0** |
| `BanquetEvent.Deposits` → `BanquetDeposit` | Cascade delete — nguy hiểm **P0** |
| `BanquetEvent.ChecklistItems` → `BanquetChecklist` | Cascade delete — OK |
| `BanquetEvent.TimelineItems` → `BanquetTimeline` | Cascade delete — OK |
| `BanquetEvent.FloorPlans` → `BanquetFloorPlan` | Cascade delete — OK |

### 7.5. Index và constraint cần bổ sung

| Bảng | Index/Constraint | Mục đích |
|---|---|---|
| `BanquetEvents` | Index on `Phone` | Tìm kiếm sự kiện theo SĐT |
| `BanquetEvents` | Index on `EventDate` | Lọc theo ngày (đã có composite index với Room) |
| `BanquetEvents` | Unique constraint on `(BanquetRoomId, EventDate, StartTime, EndTime)` | Chống trùng lịch ở DB level |
| `Customers` | Unique index on `Phone` | Chống trùng khách hàng |
| `BanquetDeposits` | Index on `DepositDate` | Báo cáo theo ngày |
| `BanquetContracts` | Index on `SignDate` | Báo cáo theo ngày ký |
| `BanquetChecklists` | Cần thêm column `CreatedAt`, `UpdatedAt` | Audit cơ bản |

### 7.6. Migration còn thiếu

1. **Migration thêm event-type-specific fields** — Thêm 10 cột vào `BanquetEvents`
2. **Migration sửa Customer** — Thêm `Email`, `CreatedAt`, `UpdatedAt` vào bảng `Customers`
3. **Migration thêm `BanquetSettings`** — Bảng cấu hình module
4. **Migration thêm `EventStatusHistories`** — Bảng lịch sử trạng thái
5. **Migration thêm `EventTypes`** — Bảng danh mục loại sự kiện

---

## 8. Các lỗi nghiệp vụ

### P0 — Nghiêm trọng

| Mã | Mô tả | Vị trí | Ảnh hưởng | Giải pháp |
|---|---|---|---|---|
| **BUG-001** | **Event-type specific fields không được lưu** (BrideName, GroomName, ConferenceName...) | VM: `BanquetRestaurantViewModel.Partial.cs`; DTO: `SaveBanquetEventRequest` thiếu field | Người dùng nhập thông tin cô dâu chú rể nhưng không được lưu — mất dữ liệu | Thêm field vào Entity, DTO, Migration |
| **BUG-002** | **ExtraGuestCount, ExtraTableCount không được lưu** | VM: field tồn tại; Entity `BanquetEvent` thiếu | Số liệu PS không lưu — sai nghiệp vụ | Thêm cột vào Entity + Migration |
| **BUG-003** | **Cascade delete trên BanquetContract và BanquetDeposit từ BanquetEvent** | `AppDbContext.cs:3976-3977` | Xóa event → mất toàn bộ hợp đồng và cọc — **mất dữ liệu tài chính** | Đổi sang `Restrict` hoặc `NoAction`, hoặc soft delete event |
| **BUG-004** | **Customer.Email [NotMapped] — không lưu vào DB** | `Customer.cs:48` | Không lưu được email khách hàng | Bỏ `[NotMapped]`, thêm migration |
| **BUG-005** | **Không có trường khách chốt (GuestConfirmedCount/TableConfirmedCount)** | Entity `BanquetEvent` thiếu | Không phân biệt được số dự kiến và số chốt | Thêm cột vào Entity |

### P1 — Cao

| Mã | Mô tả | Vị trí |
|---|---|---|
| **BUG-101** | **Không có state machine cho trạng thái** — có thể chuyển từ "Completed" về "Draft" | `UpdateEventStatusAsync` không kiểm tra transition rule |
| **BUG-102** | **Không lưu lịch sử thay đổi trạng thái** | Thiếu bảng `EventStatusHistories` |
| **BUG-103** | **Không chống tạo trùng khách hàng theo SĐT** | Không kiểm tra khi lưu event |
| **BUG-104** | **Không tự động tạo Customer khi nhập tên + SĐT mới** | `BanquetEvent.CustomerId` không được set |
| **BUG-105** | **Nút "Sửa" và "Lưu" trùng command** `SaveEventCommand` | XAML line 253 trong `BanquetRestaurantView.xaml` |
| **BUG-106** | **Không có chức năng xóa event thực sự** — chỉ đổi status | Không có `DeleteEventAsync` |
| **BUG-107** | **Không có xác nhận trước khi rời trang khi IsDirty** | Chỉ có alert khi chuyển tab bằng NavigateTab |
| **BUG-108** | **Không kiểm tra quyền (authorization)** | Service không có `[Authorize]` |
| **BUG-109** | **EventNo generator dùng CountAsync — race condition** | `GenerateEventNoAsync` đếm số event cùng ngày |
| **BUG-110** | **Settings tab hoàn toàn hard-code — không lưu được** | `BanquetRestaurantView.xaml:699-714` |
| **BUG-111** | **Calendar dates trong XAML hard-code 14/06-20/06** | `BanquetRestaurantView.xaml:663-669` |
| **BUG-112** | **Không có setup/teardown time buffer trong conflict detection** | `ValidateEventScheduleAsync` |

### P2 — Trung bình (danh sách rút gọn)

| Mã | Mô tả |
|---|---|
| **BUG-201** | EventTypeOptions, EventStatusOptions, ServiceTypeOptions hard-code trong VM |
| **BUG-202** | Không có search khách hàng theo SĐT từ popup/tooltip |
| **BUG-203** | Không tự động điền thông tin khi chọn khách cũ |
| **BUG-204** | Không hiển thị lịch sử đặt tiệc của khách |
| **BUG-205** | Không có loading indicator riêng cho từng nút (chỉ có IsBusy chung) |
| **BUG-206** | Không chống double-click trên nút Lưu |
| **BUG-207** | Không có phân trang server-side (lọc toàn bộ events về client) |
| **BUG-208** | Không có export Excel/PDF |
| **BUG-209** | MenuPackage chỉ có Attach/Detach, không có Update số lượng riêng cho event |
| **BUG-210** | Không tự động suggest số bàn từ số khách (TableStandard = 10 cố định) |
| **BUG-211** | Không có validation số điện thoại format |
| **BUG-212** | BanquetEventDto.GetEventsAsync luôn tính TotalAmount từ Menus+Services (N+1 query) |
| **BUG-213** | Không hỗ trợ multi-tenant filtering rõ ràng (CompanyId/BranchId nullable) |
| **BUG-214** | Không có phiếu thu riêng cho deposit (chỉ có bảng BanquetDeposit) |
| **BUG-215** | Room form thiếu nút lưu/thêm mới rõ ràng |
| **BUG-216** | Không có cảnh báo "phòng sắp trùng lịch" (chỉ báo trùng chính xác) |
| **BUG-217** | Không tự động tính lại giá khi đổi số bàn/khách |
| **BUG-218** | Thiếu tooltip giải thích cho các trường như "Khách PS", "Bàn PS" |

---

## 9. Các vấn đề UI/UX

### 9.1. Bố cục tab Tổng quan

**Ưu điểm:**
- Layout 2 cột rõ ràng: danh sách sự kiện + chi tiết
- Event cards có màu sắc theo trạng thái
- Form bên trái + preview bên phải cho phép xem trước thay đổi
- Filter chips hoạt động tốt

**Vấn đề:**
1. **Thiếu thanh action bar cố định** — nút Lưu/Xóa nằm trong header, dễ bị scroll mất
2. **Form "Thông tin" dài** — cần scroll, nên chia thành sections có collapsible
3. **Thiếu stepper/progress indicator** — không thấy rõ hồ sơ đang ở bước nào
4. **Thiếu badge cảnh báo** — không có "còn thiếu thông tin X trước khi chốt"
5. **Nút Lưu và Sửa giống hệt nhau** — gây nhầm lẫn

### 9.2. Đề xuất cải tiến UI

1. **Action bar cố định ở bottom** với các nút: Lưu nháp, Lưu & Chốt, Hủy
2. **Chia form thành sections** với header rõ ràng: "Thông tin khách", "Thông tin tiệc", "Yêu cầu đặc biệt"
3. **Thêm stepper** hiển thị tiến độ hồ sơ (Tư vấn → Báo giá → Giữ chỗ → Cọc → HĐ →...)
4. **Tự động suggest số bàn** khi nhập số khách (với TableStandard configurable)
5. **Popup tìm khách cũ** khi nhập SĐT
6. **Tooltip cho mọi trường** giải thích ý nghĩa

---

## 10. Các vấn đề kiến trúc và code quality

### 10.1. ViewModel

| Vấn đề | Chi tiết |
|---|---|
| **ViewModel quá lớn** | `BanquetRestaurantViewModel` split thành 2 file (RestaurantViewModels.cs + Partial.cs) với tổng ~3000+ dòng |
| **Single Responsibility vi phạm** | VM chứa: dashboard, calendar, event editor, rooms, checklist, timeline, finance, contracts, reports, settings |
| **Code-behind sạch** | `BanquetRestaurantView.xaml.cs` hầu như không có code nghiệp vụ — tốt |

### 10.2. Service

| Vấn đề | Chi tiết |
|---|---|
| **Service layer phẳng** | Không có Repository pattern, service gọi trực tiếp DbContext |
| **Không có Unit of Work** | Mỗi method tự gọi SaveChangesAsync |
| **Thiếu transaction** | Các thao tác liên quan (SaveEvent + SyncContracts) không wrapped trong transaction |
| **GetEventsAsync** | Dùng AsNoTracking().AsQueryable() rồi tính MenuAmount bằng Sum trong LINQ — có thể chậm với nhiều events |

### 10.3. Database

| Vấn đề | Chi tiết |
|---|---|
| **Status là string** | Dùng nvarchar thay vì FK đến bảng danh mục — khó kiểm soát giá trị hợp lệ |
| **Không có soft delete** | Cascade delete có thể gây mất dữ liệu |
| **Thiếu CreatedBy/UpdatedBy** | BanquetEvent không có audit user |
| **Không có RowVersion** | Không có concurrency control khi 2 user cùng sửa |

---

## 11. Các rủi ro dữ liệu và bảo mật

| Rủi ro | Mức độ | Mô tả |
|---|---|---|
| Mất hợp đồng khi xóa event | **P0** | Cascade delete BanquetContract |
| Mất lịch sử cọc khi xóa event | **P0** | Cascade delete BanquetDeposit |
| Trùng lịch phòng | P1 | Chỉ kiểm tra ở backend, không có unique constraint DB |
| Race condition tạo mã event | P1 | `GenerateEventNoAsync` dùng CountAsync |
| Không phân quyền | P1 | Không có authorization check |
| Không chống double submit | P2 | Không disable nút khi đang saving |
| Data không mã hóa | P3 | SĐT khách hàng lưu plain text |

---

## 12. Chức năng còn thiếu để vận hành thực tế

1. **Quản lý khách hàng đầy đủ** — Email, giới tính, ngày sinh, nguồn, hạng, công nợ
2. **Tìm khách cũ theo SĐT** — Auto-fill thông tin
3. **Chống trùng khách** — Cảnh báo khi SĐT đã tồn tại
4. **State machine cho trạng thái** — Quy tắc chuyển đổi có kiểm soát
5. **Lịch sử thay đổi** — Audit log mọi thay đổi
6. **In hợp đồng** — Xuất PDF/Word
7. **Gửi hợp đồng qua email** — Email service
8. **Quản lý đặt cọc** — Phiếu thu riêng
9. **Phân quyền** — Theo vai trò (Lễ tân, Kinh doanh, Quản lý, Kế toán)
10. **Quản lý công nợ** — Theo dõi công nợ khách hàng
11. **Báo cáo** — Doanh thu, công suất phòng, top khách hàng
12. **File đính kèm** — Upload ảnh, file scan hợp đồng
13. **Ghi chú nội bộ** — Note riêng cho từng event
14. **Nhật ký chăm sóc khách** — CRM cơ bản

---

## 13. Đề xuất CSDL bổ sung

### 13.1. Bảng `EventTypes`

```sql
CREATE TABLE EventTypes (
    Id int IDENTITY PRIMARY KEY,
    Code nvarchar(50) NOT NULL UNIQUE,
    Name nvarchar(255) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NOT NULL DEFAULT 0
);
```

### 13.2. Bảng `EventStatuses`

```sql
CREATE TABLE EventStatuses (
    Id int IDENTITY PRIMARY KEY,
    Code nvarchar(50) NOT NULL UNIQUE,
    Name nvarchar(255) NOT NULL,
    FromStatuses nvarchar(500) NULL, -- JSON array of allowed previous statuses
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NOT NULL DEFAULT 0
);
```

### 13.3. Bảng `EventStatusHistories`

```sql
CREATE TABLE EventStatusHistories (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    BanquetEventId uniqueidentifier NOT NULL FK,
    FromStatus nvarchar(50) NOT NULL,
    ToStatus nvarchar(50) NOT NULL,
    ChangedBy nvarchar(120) NULL,
    ChangedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
    Reason nvarchar(1000) NULL
);
```

### 13.4. Bảng `BanquetSettings`

```sql
CREATE TABLE BanquetSettings (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    CompanyId uniqueidentifier NULL,
    BranchId uniqueidentifier NULL,
    RestaurantName nvarchar(255) NOT NULL,
    RestaurantCode nvarchar(50) NOT NULL,
    Address nvarchar(500) NULL,
    Phone nvarchar(40) NULL,
    Email nvarchar(255) NULL,
    Website nvarchar(500) NULL,
    LogoPath nvarchar(1000) NULL,
    DefaultTableStandard int NOT NULL DEFAULT 10,
    DefaultStartTime time NOT NULL DEFAULT '17:00',
    DefaultEndTime time NOT NULL DEFAULT '21:00',
    SetupBufferMinutes int NOT NULL DEFAULT 60,
    TeardownBufferMinutes int NOT NULL DEFAULT 60,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NOT NULL
);
```

### 13.5. Các cột cần thêm vào `BanquetEvents`

```sql
ALTER TABLE BanquetEvents ADD
    BrideName nvarchar(255) NULL,
    GroomName nvarchar(255) NULL,
    ConferenceName nvarchar(255) NULL,
    BirthdayPersonName nvarchar(255) NULL,
    BirthdayDate datetime2 NULL,
    MeetingTopic nvarchar(255) NULL,
    PartyTheme nvarchar(255) NULL,
    OtherEventName nvarchar(255) NULL,
    ExtraGuestCount int NOT NULL DEFAULT 0,
    ExtraTableCount int NOT NULL DEFAULT 0,
    GuestConfirmedCount int NOT NULL DEFAULT 0,
    TableConfirmedCount int NOT NULL DEFAULT 0,
    SetupTime time NULL,
    TeardownTime time NULL,
    CreatedBy nvarchar(120) NULL,
    UpdatedBy nvarchar(120) NULL,
    RowVersion rowversion NULL;
```

### 13.6. Các cột cần thêm vào `Customers`

```sql
-- Bỏ [NotMapped] trên Email và thêm migration
ALTER TABLE Customers ADD
    Email nvarchar(255) NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSDATETIME();
```

---

## 14. Kế hoạch cải thiện theo giai đoạn

### Giai đoạn 1: Hoàn thiện tab Tổng quan (2-3 tuần)

- [ ] Thêm 10+ cột event-type-specific vào Entity + Migration **(P0)**
- [ ] Thêm ExtraGuestCount/ExtraTableCount + GuestConfirmedCount/TableConfirmedCount **(P0)**
- [ ] Fix cascade delete thành Restrict/NoAction cho Contract và Deposit **(P0)**
- [ ] Thêm Email, CreatedAt, UpdatedAt vào Customer **(P0)**
- [ ] Thêm state machine validation cho trạng thái **(P1)**
- [ ] Thêm lookup customer theo SĐT + auto-fill **(P1)**
- [ ] Tạo bảng EventStatusHistories **(P1)**
- [ ] Thêm validation SĐT format, chống trùng khách **(P1)**
- [ ] Thêm loading state + chống double-click cho các nút **(P2)**
- [ ] Xóa toàn bộ hard-code UI (alert count, username, dates, settings) **(P1-P2)**
- [ ] Thêm tooltip cho các trường **(P3)**

### Giai đoạn 2: Hoàn thiện phòng và lịch phòng (1-2 tuần)

- [ ] Fix calendar bind vào ViewModel dates thay vì hard-code **(P1)**
- [ ] Thêm setup/teardown buffer vào conflict detection **(P1)**
- [ ] Thêm unique constraint DB level cho lịch phòng
- [ ] Thêm tính năng "giữ phòng tạm" với timeout
- [ ] Thêm calendar view thật (không hard-code)

### Giai đoạn 3: Thực đơn và dịch vụ (1-2 tuần)

- [ ] Cho phép custom menu lưu vào BanquetEventMenu
- [ ] Tự động tính lại giá khi đổi số bàn
- [ ] Tạo bảng ServiceTypes
- [ ] Load danh mục loại dịch vụ từ DB

### Giai đoạn 4: Đặt cọc và hợp đồng (2-3 tuần)

- [ ] Tạo phiếu thu riêng khi nhận cọc
- [ ] In hợp đồng (PDF/Word)
- [ ] Gửi hợp đồng qua email
- [ ] Phụ lục hợp đồng
- [ ] Thanh lý hợp đồng

### Giai đoạn 5: Checklist, timeline và vận hành (1-2 tuần)

- [ ] Phân công nhân sự cho timeline items
- [ ] Gửi notification khi đến hạn checklist
- [ ] Dashboard vận hành ngày

### Giai đoạn 6: Nghiệm thu, thanh lý và báo cáo (2-3 tuần)

- [ ] Tính năng nghiệm thu
- [ ] Thanh lý hợp đồng
- [ ] Báo cáo doanh thu, công suất, khách hàng
- [ ] Phân quyền đầy đủ

---

## 15. Danh sách công việc đề xuất

- [ ] **P0** — Thêm cột event-type-specific vào `BanquetEvent` entity + migration
  - File: `Core/Entities/RestaurantEntities.cs`, `Application/DTOs/RestaurantDtos.cs`, `SaveBanquetEventRequest`
  - Phụ thuộc: Không
  - Tiêu chí: Lưu và load được BrideName, GroomName,...

- [ ] **P0** — Thêm ExtraGuestCount, ExtraTableCount vào entity
  - File: `Core/Entities/RestaurantEntities.cs`, DTO
  - Phụ thuộc: Không
  - Tiêu chí: Lưu được số liệu PS

- [ ] **P0** — Sửa cascade delete BanquetContract, BanquetDeposit → Restrict
  - File: `Infrastructure/Persistence/AppDbContext.cs:3976-3977`
  - Phụ thuộc: Không
  - Tiêu chí: Xóa event không xóa contract/deposit

- [ ] **P0** — Thêm Email, CreatedAt, UpdatedAt vào Customer entity + migration
  - File: `Core/Entities/Customer.cs`, migration mới
  - Phụ thuộc: Không
  - Tiêu chí: Email lưu được vào DB

- [ ] **P1** — Thêm state machine validation cho UpdateEventStatusAsync
  - File: `Infrastructure/Services/RestaurantServices.cs:4161-4169`
  - Phụ thuộc: Tạo bảng EventStatuses
  - Tiêu chí: Không cho chuyển trạng thái sai luật

- [ ] **P1** — Xóa hard-code UI: alert count, username, calendar dates, settings form
  - File: `Views/BanquetRestaurantView.xaml`
  - Phụ thuộc: MOCK-01 → MOCK-07
  - Tiêu chí: UI hiển thị dữ liệu thật từ binding

- [ ] **P1** — Tạo bảng EventStatusHistories + audit log
  - File: Entity mới, migration mới
  - Phụ thuộc: Không
  - Tiêu chí: Mỗi lần đổi trạng thái đều được ghi log

- [ ] **P1** — Tạo bảng BanquetSettings + CRUD
  - File: Entity + DTO + Service + Migration + UI binding
  - Phụ thuộc: Không
  - Tiêu chí: Settings lưu và load được

- [ ] **P1** — Implement tìm khách theo SĐT + auto-fill
  - File: `BanquetRestaurantViewModel.Partial.cs`, `IBanquetRestaurantService`
  - Phụ thuộc: CustomerService
  - Tiêu chí: Nhập SĐT → hiện danh sách khách khớp → chọn → tự động điền

- [ ] **P2** — Chuyển danh mục cứng sang DB (EventTypes, ServiceTypes)
  - File: Entity mới, migration, service, UI
  - Phụ thuộc: Không
  - Tiêu chí: ComboBox load từ DB

- [ ] **P2** — Implement print contract (PDF)
  - File: Service mới hoặc thư viện in
  - Phụ thuộc: Contract data
  - Tiêu chí: Xuất được file PDF hợp đồng

---

*Báo cáo được tạo bởi Claude Code analysis ngày 2026-07-10*
