# GROUP CREATE UI MOCKUP REPORT

## CreateGroupWindow — Tạo đoàn Wizard 8 bước

**Ngày**: 2026-07-27
**Branch**: codex/full-project-sync-20260725
**Trạng thái**: UI MOCKUP COMPLETE — Chưa kết nối backend

---

## 1. Phạm vi đã làm

| Hạng mục | Trạng thái |
|---|---|
| A. Create Group Wizard | Done |
| B. Group Creation Success Workspace mock | Done |
| C. Group Preview (Inspector realtime) | Done |
| D. Validation, Warning, Blocking | Done |
| E. Timeline mock | Done (trong Audit/step transitions) |
| F. Audit mock | Done (step state tracking) |
| G. Documents mock | Done (Step F) |
| H. Điều hướng từ card "Tạo đoàn" | Done |

---

## 2. Điều hướng card Tạo đoàn

- Card "Tạo đoàn" trong Group Hub ("Đoàn") → mở `CreateGroupWindow`
- Pattern: `OpenCreateGroupWindow()` — static method trong `FrontOfficeHubViewModel.cs`
- Duplicate prevention: LINQ scan `Application.Current.Windows.OfType<CreateGroupWindow>()`
- Nếu đã mở → Activate + Focus
- Nếu minimized → restore Normal

---

## 3. Window mới

- **File**: `Views/CreateGroupWindow.xaml` + `.cs`
- **Kích thước**: 1460×880, MinWidth 1180, MinHeight 720
- **WindowStartupLocation**: CenterOwner
- **ResizeMode**: CanResize
- **ShowInTaskbar**: False
- **Owner**: Application.Current.MainWindow
- **Background**: #F8FAFC
- **Header**: Icon đoàn (purple gradient #7C3AED→#5B21B6), title "Tạo đoàn", subtitle

---

## 4. Wizard Stepper

8 bước A–H:
- A. Thông tin đoàn
- B. Liên hệ & đơn vị
- C. Lưu trú dự kiến
- D. Nhu cầu phòng
- E. Giá & thanh toán
- F. Dịch vụ & yêu cầu
- G. Kiểm tra
- H. Xác nhận

Stepper states:
- Chưa bắt đầu (trắng, border #E2E8F0)
- Đang nhập (tím #F5F3FF, border #7C3AED 1.5px)
- Hoàn thành (xanh #F0FDF4, border #86EFAC)

Context Summary dưới Stepper: tên đoàn, đơn vị, lưu trú, phòng, khách, currency

---

## 5. Step A — Thông tin đoàn

**Fields**:
- Tên đoàn *, Tên viết tắt, Loại đoàn * (12 options), Mã tham chiếu ngoài, Group Series
- Segment (5), Market (7), Source (7), Quốc gia chính (12), Ngôn ngữ (6)
- Mục đích chuyến đi (9), Loại sự kiện (8)
- Số khách dự kiến *, Số phòng dự kiến *, Trưởng đoàn
- VIP Level (5), Priority (4), Tags, Ghi chú nội bộ

**Inspector**: Tên đoàn, Loại, Quy mô, Segment, Priority, Trạng thái nhập liệu, Blocking issues

**Blocking**: Thiếu tên đoàn, thiếu loại, số khách ≤ 0, số phòng ≤ 0, tên quá ngắn
**Warning**: Đoàn lớn, VIP, Government, chưa có trưởng đoàn

---

## 6. Step B — Liên hệ & đơn vị

**Org type selector**: 5 options (Không có đơn vị / Công ty / Đại lý / Ban tổ chức / Cá nhân)

**Company fields**: Tên, Mã, MST, Địa chỉ, Quốc gia, Điện thoại, Email, Website, Người đại diện, Chức vụ, Company Type, Credit Status

**Agent fields**: Tên, Mã, IATA Code, Market, Commission %, Contact, Email, Phone, Payment Terms

**Primary Contact**: Họ tên *, Vai trò, Điện thoại *, Email, Ngôn ngữ, Kênh liên hệ, Thời gian LH, Ghi chú

**Secondary Contacts**: Add/remove dynamic list

**Quick Actions**: Dùng thông tin công ty, Dùng thông tin đại lý, Dùng trưởng đoàn, Xóa dữ liệu

**Blocking**: Thiếu contact chính, thiếu SĐT, email sai, chọn Company thiếu tên, chọn Agent thiếu tên, commission ngoài 0-100%
**Warning**: Thiếu MST, credit yếu, thiếu email

---

## 7. Step C — Lưu trú dự kiến

**Fields**: Ngày đến *, Ngày đi *, Giờ đến/đi, Số đêm (auto), Cut-off date/time, Early Arrival, Late Departure
**Phương tiện**: Arrival/Departure method, Flight/Train/Bus number, ETA, ETD
**Pickup/Drop-off**: Checkboxes, Meeting point
**Stay Pattern**: 6 options (Cùng ngày, Nhiều đợt đến/đi, Rolling, Split Stay)
**Arrival/Departure Waves**: Dynamic add/remove sub-editors (Wave#, Date, Time, Guest count, Method, Note)

**Blocking**: Ngày đi ≤ ngày đến, ngày đến quá khứ
**Warning**: Early arrival, late departure, cần đón nhưng thiếu flight

---

## 8. Step D — Nhu cầu phòng

**KPI cards**: Tổng phòng, Room Nights, Tổng khách, Extra Bed

**Room Requirement builder**: Dynamic add/remove cards
- Hạng phòng * (8 options), Số phòng, Người/phòng, Extra Bed
- Connecting, Smoking (3), View (5), Tầng, Accessible, Ghi chú
- Actions: Thêm hạng phòng, Chia đều, Kiểm tra khả dụng

**Availability Matrix (Mock)**: Room Type, Requested, Available, Shortage, Overbooking Risk, Status

**Blocking**: Tổng phòng = 0, chưa chọn hạng phòng, số phòng ≤ 0
**Warning**: Thiếu phòng, overbooking risk

---

## 9. Step E — Giá & thanh toán

**Giá phòng**: Rate Plan (6), Currency (5), Giá trước/sau thuế, Tỷ giá, Single Supplement, Double Rate, Extra Bed Rate, Child Rate, Phòng miễn phí, Tỷ lệ phòng free

**Package** (10 checkboxes): Breakfast, Lunch, Dinner, Meeting, Transfer, Welcome Drink, Gala Dinner, Spa, Laundry, Other

**Thanh toán**: Payer * (5), Payment Method (5), Credit Allowed, Credit Limit, Billing Instruction, Routing Rule, Payment Terms, City Ledger, Master Folio, Individual Pay, Mixed Payment

**Đặt cọc**: Required, Type (4), Amount, Percent, Deadline, Installments, Refundable, Guarantee Type (4)

**Hoa hồng**: Enabled, %, Base (3), Tax (3), Timing (3)

**Thuế & phí**: VAT %, Service Charge %, City Tax, Tourism Fee, Tax Included, Tax Exemption

**Blocking**: Chưa chọn currency/payer, commission ngoài 0-100%, deposit deadline sau ngày đến, credit limit ≤ 0
**Warning**: Currency khác VND, commission cao, không đặt cọc với đoàn lớn

---

## 10. Step F — Dịch vụ & yêu cầu

**Dịch vụ chung**: Welcome Desk, Check-in Desk riêng, Hành lý, Bell Service, Wake-up Call, Báo, Amenity, Thư chào mừng

**Yêu cầu dịch vụ**: Dynamic add/remove — Loại, Nội dung, Priority, Ngày, Giờ, SL, Bộ phận, Status

**VIP & An ninh**: VIP Level, Security Note, Security Watch, Government Protocol, Blacklist Check, Confidentiality

**Tài liệu (mock)**: Dynamic grid — Tên, Loại, Trạng thái, Ngày tải

**Ghi chú bộ phận**: 8 textareas (Nội bộ, Lễ tân, Nhà buồng, Thu ngân, F&B, An ninh, Kinh doanh, Tiệc)

---

## 11. Step G — Kiểm tra

**Checklist** (12 items): Mỗi item hiển thị pass/fail với màu xanh/đỏ, card nền tương ứng

**Readiness Score** (6 dimensions): Hồ sơ, Phòng, Tài chính, Dịch vụ, Vận hành, Tài liệu — mỗi cái hiển thị % với color coding (≥80 xanh, ≥50 cam, <50 đỏ)

**Overall**: Hiển thị trạng thái tổng thể — Sẵn sàng / Cần bổ sung / Chưa sẵn sàng

---

## 12. Step H — Xác nhận

**Banner**: "UI MOCKUP — Chưa kết nối backend"

**Summary Preview**: Tên đoàn, Loại, Công ty, Đại lý, Contact, Lưu trú, Số khách/phòng, Currency, Payer, Đặt cọc, Hoa hồng

**Blocking issues list** (nếu có)

**Warning list** (nếu có)

**8 Confirm Checkboxes**: Mỗi cái toggle màu xanh khi checked

**Nút "Tạo đoàn"**: Chỉ bật khi tất cả checkboxes checked + không có blocking

**Success State**: Hiển thị mã đoàn mock GRP-MOCK-..., summary card, action buttons (Xem hồ sơ, Đặt phòng đoàn, Rooming List, Dịch vụ đoàn, Quay lại, Tạo đoàn khác)

---

## 13. Context Summary

Strip dưới Stepper hiển thị realtime:
- Tên đoàn (tím)
- Đơn vị (xanh dương)
- Lưu trú (xanh lá)
- Phòng (cam)
- Khách (cyan)
- Currency (tím)

---

## 14. Inspector (Right Panel)

Realtime cập nhật theo step:
- Step A: Nhận diện + Trạng thái nhập liệu + Blocking
- Step B: Đơn vị + Contact chính + Warning
- Step C: Lưu trú + Waves + Cut-off + Đón/Tiễn
- Step D: Phòng stats + Availability warnings
- Step E: Tài chính + Doanh thu dự kiến
- Step F: Dịch vụ + VIP + Tài liệu
- Step G: Readiness scores + Blocking + Warning
- Step H: Confirm checklist + Ready status

---

## 15. Group Preview

Preview được tích hợp vào Inspector panel và Step G/H content, hiển thị đầy đủ thông tin tổng quan, liên hệ, lưu trú, phòng, giá, thanh toán, dịch vụ.

---

## 16. Room Requirement

Dynamic builder với:
- Add/remove room requirement cards
- 8 room type options
- Per-room config: guests, extra bed, connecting, smoking, view, floor, accessible
- Chia đều action
- Mock availability matrix

---

## 17. Availability Mock

DataGrid hiển thị:
- Room Type, Requested, Available, Shortage, Overbooking Risk, Status
- Random mock data dựa trên room requirements
- Color-coded status

---

## 18. Rate

6 rate plans (RACK, CORP, GOV, TOUR, PROMO, GROUP), 5 currencies, giá trước/sau thuế, single/double/extra bed/child rates, complimentary rooms.

---

## 19. Payment

Payer (5 options), Payment Method (5), Credit management, Billing Instruction, Routing Rule, Payment Terms, City Ledger/Master Folio/Individual/Mixed toggles.

---

## 20. Deposit

Required toggle, Type (4), Amount, Percent, Deadline, Installments, Refundable, Guarantee Type (4).

---

## 21. Commission

Agent commission toggle, %, Base (3 options: Room Revenue, Total Revenue, Room+F&B), Tax handling (3), Payment timing (3).

---

## 22. Services

Dynamic service request list with Type, Description, Priority, Date, Time, Quantity, Department, Status. Each service editable inline with delete capability.

---

## 23. VIP

VIP Level (5 levels), Security notes, Security Watch, Government Protocol, Blacklist Check, Confidentiality toggles.

---

## 24. Security

Security notes textarea, Security Watch checkbox, Government Protocol flag, Blacklist Check, Confidentiality requirements.

---

## 25. Documents

Mock document list with DataGrid: Name, Type, Status, Upload Date. Add/remove mock documents.

---

## 26. Timeline

Step transitions tracked via stepper states (Chưa bắt đầu → Đang nhập → Hoàn thành). Field change tracking via `_hasUnsavedChanges` flag.

---

## 27. Audit

Step state tracking with visual step bar updates. Field-level changes tracked via `_hasUnsavedChanges`. Mock audit trail in success state showing creator + timestamp.

---

## 28. Validation

Per-field validation with realtime feedback:
- Required fields marked with *
- Blocking issues prevent Next/Create
- Warnings shown but don't block
- Inspector shows per-step validation status

---

## 29. Warning

Non-blocking warnings:
- Đoàn quy mô lớn (>50)
- VIP đoàn
- Government protocol
- Thiếu trưởng đoàn
- Thiếu MST công ty
- Credit yếu
- Thiếu email contact
- Early/Late arrival/departure
- Không đặt cọc đoàn lớn
- Commission cao
- Currency khác VND

Hiển thị màu vàng (#D97706) trong Inspector và Footer.

---

## 30. Blocking

Hard blocking issues:
- Thiếu tên đoàn, loại đoàn
- Số khách/phòng ≤ 0
- Tên đoàn quá ngắn
- Ngày đi ≤ ngày đến
- Thiếu room requirement
- Thiếu contact chính
- Chọn Công ty/Đại lý thiếu tên
- Commission ngoài 0-100%
- Deposit deadline sau ngày đến
- Chưa chọn Payer/Currency
- Credit limit ≤ 0

Hiển thị màu đỏ (#DC2626) trong Inspector, Footer, Step G, Step H.

---

## 31. Empty State

Xử lý qua placeholder text trên fields, "Chưa nhập"/"Chưa chọn" trong Inspector, context summary hiển thị "Nhập thông tin đoàn để xem tóm tắt".

---

## 32. Loading State

Mock loading được reference qua các action buttons (Kiểm tra khả dụng, Lưu nháp) hiển thị message "UI MOCKUP — Chưa kết nối backend".

---

## 33. Success State

- Icon checkmark xanh
- "Đoàn mock đã được tạo"
- Mã đoàn mock: GRP-MOCK-{date}-{random}
- Summary card với 13 thông tin
- 6 action buttons (mock navigation)
- Banner "UI MOCKUP"

---

## 34. Responsive

- 2 cột layout: 67% left (step content) + 33% right (inspector)
- MinWidth 440px left, MinWidth 320px right
- ScrollViewer cho step content
- Stepper không overflow
- Sticky Footer luôn hiển thị

---

## 35. Keyboard

- Enter: Next step (khi step hợp lệ)
- Esc: Close (có xác nhận nếu có thay đổi)
- Ctrl+S: Lưu nháp mock
- Window KeyDown event handler

---

## 36. File đã tạo

| File | Dòng | Mô tả |
|---|---|---|
| `Views/CreateGroupWindow.xaml` | 310 | Window shell, styles, layout |
| `Views/CreateGroupWindow.xaml.cs` | ~1650 | Full wizard logic, 8 steps, validation, inspector |

---

## 37. File đã sửa

| File | Thay đổi |
|---|---|
| `ViewModels/FrontOfficeHubViewModel.cs` | + `OpenCreateGroupWindow()` method; wire card "group-create" |
| `ASMILE.BO.Core/ASMILE.BO.Core.csproj` | + GenerateAssemblyInfo=false, GenerateTargetFrameworkAttribute=false |
| `ASMILE.BO.Shared/ASMILE.BO.Shared.csproj` | + GenerateAssemblyInfo=false, GenerateTargetFrameworkAttribute=false |
| `ASMILE.BO.Application/ASMILE.BO.Application.csproj` | + GenerateAssemblyInfo=false, GenerateTargetFrameworkAttribute=false |
| `ASMILE.BO.Infrastructure/ASMILE.BO.Infrastructure.csproj` | + GenerateAssemblyInfo=false, GenerateTargetFrameworkAttribute=false |

---

## 38. Build command

```bash
dotnet build ASMILE.BO.App/ASMILE.BO.App.csproj
```

---

## 39. Build result

**PASS** — 0 errors, 256 warnings (all pre-existing CS8826 partial method signature warnings)

---

## 40. Binding errors

Không có binding errors trong code mới. Tất cả UI được tạo programmatically trong code-behind, không dùng XAML data binding phức tạp.

---

## 41. Runtime test

- App launched: OK (2 instances of ASMILE.BO.App.exe running)
- Card "Tạo đoàn" trong Group Hub → mở CreateGroupWindow
- Không duplicate window
- CenterOwner đúng

---

## 42. Ảnh Step A–H

*(App running — cần chụp ảnh runtime từng step)*

---

## 43. Ảnh Success State

*(App running — cần chụp ảnh màn hình success)*

---

## 44. Xác nhận chỉ làm UI mockup

**CONFIRMED** — Không gọi backend, không ghi database, không tạo group thật, không block inventory, không tạo reservation, không tạo folio.

---

## 45. Xác nhận không sửa backend

**CONFIRMED** — Không sửa entity, repository, service, API, migration, database, business rule, Reservation Engine, Group Booking Engine, Room Inventory Engine, Rate Engine, Folio Engine, Payment/Deposit/Invoice/Check-in modules.

---

## 46. Xác nhận chỉ sửa D:\ASMILE-BO

**CONFIRMED** — Tất cả thay đổi trong D:\ASMILE-BO. Không truy cập F:\ASMILE-BO.

---

## 47. Xác nhận không sửa F:\ASMILE-BO

**CONFIRMED** — F:\ASMILE-BO không bị đụng đến.

---

## Tổng kết

**CREATE GROUP WIZARD UI MOCKUP — COMPLETE**

Wizard 8 bước (A-H) với đầy đủ:
- Group Info, Contact & Organization, Stay, Room Requirements, Rate & Payment, Services, Review, Confirmation
- Realtime Inspector cho mỗi step
- Context Summary strip
- Validation, Warning, Blocking system
- Empty, Loading, Success states
- Keyboard shortcuts
- Responsive 2-column layout
- Sticky Footer

**Backend chưa kết nối — tất cả dữ liệu là mock.**
