# RESTAURANT ALACARTE DATA/BINDING FIX

## Ngày: 2026-06-14

---

## Nguyên nhân màn trống

| Vấn đề | Root cause |
|---|---|
| **Tất cả tab trống** | `RefreshAsync()` load từ DB, DB trống → mọi collection rỗng. Không có fallback. |
| **Bootstrap không hiển thị** | Tạo data qua service (DB) rồi gọi `RefreshAsync()` → service call fail hoặc return empty → UI vẫn trống. Contracts bị xóa khi RefreshAsync lần 2. |
| **Dashboard KPIs = 0** | `Dashboard` chỉ load từ `_service.GetDashboardAsync()` → DB trống → 0 hết |
| **Read-only property crash** | `AlacarteServiceItem.Price`, `.Name`, `.Unit` và `AlacarteAlertItem.Message`, `.Time`, `.Type` chỉ có `{ get; }` → WPF binding lỗi |

---

## Đã sửa

### 1. ViewModel — AlacarteRestaurantViewModel

| Thay đổi | Chi tiết |
|---|---|
| **`LoadAsync()`** | Constructor gọi `RefreshAsync()` → nếu Rooms + Tables đều rỗng → tự gọi `PopulateSampleDataInMemory()` |
| **`PopulateSampleDataInMemory()`** | Tạo toàn bộ dữ liệu mẫu TRỰC TIẾP vào ObservableCollection (không qua DB): 6 phòng, 15 bàn, 3 đặt bàn, 1 đơn mở (5 món), 3 hợp đồng |
| **`RefreshAsync()`** | Wrap service calls trong try-catch. KHÔNG xóa Contracts/AlacarteServices. Luôn gọi `InitDefaultServices()`, `PopulateSampleContracts()`, `RefreshAlerts()`, `UpdateDashboardFromLocal()` |
| **`BootstrapSampleDataAsync()`** | Gọi `PopulateSampleDataInMemory()` trực tiếp + hiển thị status kèm count |
| **`UpdateDashboardFromLocal()`** | Tính Dashboard KPIs từ local collections nếu Dashboard từ DB = 0 |
| **Read-only fix** | `AlacarteServiceItem.Name/Price/Unit` → `{ get; set; }`. `AlacarteAlertItem.Message/Time/Type` → `{ get; set; }` |

### 2. Dữ liệu mẫu tự động

| Collection | Count | Nguồn |
|---|---|---|
| `Rooms` | 6 (VIP 1, VIP 2, Tầng 1, Tầng 2, Sân vườn, Ngoài trời) | In-memory |
| `Tables` | 15 (A01-A05, B01-B06, C01-C04) | In-memory, link tới Rooms |
| `Reservations` | 3 (Nguyễn Văn An, Trần Thị Bích, Hoàng Văn Nam) | In-memory |
| `OpenOrders` | 1 (A04 - VIP 1 - 2406900đ - 5 món) | In-memory |
| `Contracts` | 3 (HD-ALC-0001 → 0003) | In-memory |
| `AlacarteServices` | 12 dịch vụ | In-memory (constructor) |
| `Alerts` | Dynamic (từ Reservations + OpenOrders) | In-memory |

---

## Dữ liệu nào là DB thật, dữ liệu nào sample

| Dữ liệu | Loại |
|---|---|
| Rooms, Tables | **Sample tạm** — nếu DB có sẵn thì lấy từ DB qua `_service.GetRoomsAsync()` |
| Reservations | **Sample tạm** — nếu DB có sẵn thì lấy từ DB |
| OpenOrders | **Sample tạm** — luôn có ít nhất 1 đơn mẫu nếu DB trống |
| Contracts | **Luôn sample** — không có service/DB cho Contracts |
| AlacarteServices | **Luôn sample** — local list |
| Alerts | **Dynamic** — tính từ Reservations + OpenOrders |
| Dashboard KPIs | **DB + fallback local** — ưu tiên DB, fallback local computation |

---

## Binding fixes

| XAML binding | Trạng thái |
|---|---|
| `{Binding Rooms}` → chip filter + section | OK |
| `{Binding Tables}` → table cards | OK (4 status DataTriggers) |
| `{Binding Reservations}` → đặt bàn cards | OK |
| `{Binding OpenOrders}` → đơn mở cards | OK |
| `{Binding Contracts}` → hợp đồng cards | OK |
| `{Binding AlacarteServices}` → dịch vụ grid | OK |
| `{Binding Alerts}` → cảnh báo list | OK |
| `{Binding SelectedOrder.Lines}` → order lines | OK |
| `{Binding Dashboard.*}` → KPI cards | OK |
| `OpenOrderCommand` → click bàn mở đơn | OK |
| `SelectRoomCommand` → click room chip | OK |

---

## Build status

**PASS** — 0 errors, 0 warnings
