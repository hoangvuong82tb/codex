# KIỂM TRA TOÀN BỘ APP ASMILE BO

## 1. Tóm tắt kết quả

* Thời gian kiểm tra: 2026-05-28, timezone Asia/Saigon.
* Branch/commit nếu lấy được: chưa lấy được. Thư mục `D:\ASMILE-BO` không phải git repository theo lệnh `git rev-parse`.
* Environment: Windows, PowerShell, .NET solution `D:\ASMILE-BO\ASMILE.BO.sln`, WPF/.NET 8 theo output build.
* Database: SQL Server `localhost`, database `ASMILE_BO`, connection string lấy từ `ASMILE.BO.App\appsettings.json`.
* Build: FAIL. `dotnet build .\ASMILE.BO.sln --no-incremental` trả exit code 1.
* Test: FAIL. `dotnet test .\ASMILE.BO.sln` trả exit code 1, unit test fail 2/120, integration test pass 11/11.
* Tổng số lỗi nghiêm trọng: 4 nhóm chính.
* Tổng số warning đáng chú ý: 6 nhóm rủi ro/warning đáng chú ý.
* Trạng thái khuyến nghị: FAIL.

Các lỗi nghiêm trọng:

* Build fail do post-build encoding check phát hiện mojibake trong `ASMILE.BO.App\Views\OperationsCenterView.xaml` dòng 230.
* Unit test fail ở TT99/TT200 compliance mapping.
* Database sau reset trắng chưa sạch dữ liệu nghiệp vụ: vẫn còn chứng từ, bút toán, hàng hóa, khách hàng, nhà cung cấp.
* Vẫn còn mojibake trong UI/service, gồm nhóm view vận hành/ngân hàng/thuế/TSCĐ và message trong service.

## 2. Lệnh đã chạy

| Lệnh | Kết quả |
| --- | --- |
| `cd D:\ASMILE-BO` | OK |
| `dotnet build .\ASMILE.BO.sln --no-incremental` | FAIL, exit code 1 |
| `dotnet test .\ASMILE.BO.sln` | FAIL, exit code 1 |
| `git rev-parse --abbrev-ref HEAD; git rev-parse HEAD` | FAIL, không phải git repository |
| `sqlcmd -S localhost -U sa -P 123456 -d ASMILE_BO -C -Q ...` kiểm tra migration và bảng training data | OK |
| `sqlcmd ...` kiểm tra business/system/training counts | Chạy được một phần; query `SettingDefinitions` fail vì bảng không tồn tại |
| `rg` scan log theo `ERR`, `FTL`, `Exception`, `XamlParseException`, `BindingExpression`, `Invalid object name`, `Duplicate key`, `DbUpdateConcurrencyException`, `BackgroundService failed`, dấu hiệu encoding | OK |
| `rg` scan `.cs`, `.xaml`, `.md`, `.json` theo dấu hiệu mojibake | OK |
| `rg` scan route/viewmodel/service/DI/navigation | OK static scan |
| Scan XAML binding/resource trong các file trọng điểm | OK static scan, chưa smoke test UI thủ công |

## 3. Kết quả build/test

* Build result: FAIL.
* Build warning count: 0 warning theo summary build.
* Build error count: 1 error theo summary build.
* Lỗi build chính: post-build hook `scripts/check-xaml-encoding.ps1` phát hiện lỗi encoding ở `ASMILE.BO.App\Views\OperationsCenterView.xaml` dòng 230, cột `MemoryUsageBytes`, header bị mojibake.
* Vì build fail ở encoding hook, startup app từ build hiện tại chưa kiểm tra được đầy đủ.

Test result:

* Integration tests: pass 11/11.
* Unit tests: fail 2, pass 118, total 120.
* Test warning count: chưa tách riêng được từ output vì lệnh test kết thúc fail; không thấy warning compiler nổi bật trong phần build bắt buộc.

Test fail:

| Test | Module liên quan | Stack trace ngắn | Ghi chú |
| --- | --- | --- | --- |
| `ASMILE.BO.Tests.Unit.TT99ComplianceTests.ValidateComplianceAsync_GeneratesIssuesForMissingMapping` | Compliance/account mapping TT99 | Expected issues empty to contain rule `TT99_MISSING_MAPPING`, tại `ASMILE.BO.Tests.Unit\TT99ComplianceTests.cs:59` | Validator không sinh issue như test kỳ vọng |
| `ASMILE.BO.Tests.Unit.TT200ComplianceTests.ValidateComplianceAsync_GeneratesIssuesForMissingMapping` | Compliance/account mapping TT200 | Expected issues empty to contain rule `TT200_MISSING_MAPPING`, tại `ASMILE.BO.Tests.Unit\TT200ComplianceTests.cs:61` | Validator không sinh issue như test kỳ vọng |

## 4. Kết quả kiểm tra database/migration

* Migration status: kiểm tra trực tiếp được bằng `sqlcmd`.
* 5 migration mới nhất đã có trong `__EFMigrationsHistory`:
  * `20260527091500_AddStockReceiptDetailVatColumns`
  * `20260525133000_AddTrainingDataMarkers`
  * `20260525113000_AddTrainingDataRunStepDetails`
  * `20260525031433_AddTrainingDataRunHistory`
  * `20260524061631_AddPhase4ExecutionFoundationStep46To412`
* TrainingData tables: tồn tại đủ `TrainingDataRuns`, `TrainingDataRunSteps`, `TrainingDataMarkers`.
* Lỗi `Invalid object name 'TrainingDataMarkers'`: chưa thấy trong DB hiện tại vì bảng đã tồn tại; log cũ vẫn cần theo dõi nếu có môi trường khác chưa migrate.
* Reset state: FAIL. Dữ liệu nghiệp vụ chưa reset trắng.
* System data retained: có dữ liệu system còn lại.
* Business data cleared: FAIL.

Số liệu DB kiểm tra trực tiếp:

| Nhóm | Bảng | Count | Kết luận |
| --- | --- | ---: | --- |
| Business | `BusinessDocuments` | 2 | Chưa sạch |
| Business | `JournalHeaders` | 6 | Chưa sạch |
| Business | `Items` | 23 | Chưa sạch |
| Business | `Customers` | 6 | Chưa sạch |
| Business | `Suppliers` | 5 | Chưa sạch |
| System | `AppUsers` | 1 | Còn system data |
| System | `AppRoles` | 4 | Còn system data |
| System | `AppPermissions` | 34 | Còn system data |
| System | `AppSettings` | 0 | Không có setting runtime |
| Training | `TrainingDataRuns` | 26 | Có history training |
| Training | `TrainingDataRunSteps` | 285 | Có history step |
| Training | `TrainingDataMarkers` | 213 | Có marker |

Ghi chú DB:

* Query kiểm tra `SettingDefinitions` báo `Invalid object name 'SettingDefinitions'`. Chưa xác nhận đây là bảng bắt buộc hay chỉ là tên cũ/không còn dùng.
* Không chạy reset dữ liệu vì yêu cầu là kiểm tra, không thay đổi workflow/dữ liệu.

## 5. Kết quả kiểm tra log

| Mức độ | Nhóm lỗi | Số lượng | File/Module | Ghi chú |
| --- | --- | ---: | --- | --- |
| Error | XAML binding read-only `PermissionSelectionRow.IsAssigned`/`IsDangerous` | Nhiều dòng | `logs\asmile-bo-20260524.log`, Security/Admin permission UI | Runtime log ghi lỗi binding TwoWay/OneWayToSource vào property read-only |
| Error | Training Data UI binding `CompletionSummaryItem.Value` | Nhiều dòng lịch sử | `ASMILE.BO.App\bin\Release\net8.0-windows\logs\asmile-bo-20260526.log` và bản copy | Current XAML đã thấy dùng OneWay, nhưng chưa smoke test UI vì build fail |
| Error | XamlParseException | Nhiều dòng lịch sử | `logs\asmile-bo-20260423.log`, `logs\asmile-bo-20260501.log`, release logs | Có lỗi parse/runtime XAML lịch sử, chưa xác nhận đã hết trong app chạy hiện tại |
| Error | Migration/FK cascade | Ít nhất 1 nhóm | `logs\asmile-bo-20260524.log` | Lỗi FK `ApprovalHistory_ApprovalWorkflows_WorkflowId` có thể do migration cũ |
| Error | DbContext concurrency | Ít nhất 1 nhóm | log lịch sử | Có lỗi second operation on same DbContext, cần kiểm tra background/job/service lifecycle |
| Error | DI missing service | Ít nhất 1 nhóm lịch sử | log lịch sử | Missing `IEnterpriseConsistencyValidatorService` khi activate `FailureRecoveryService`; current DI compile nhưng startup chưa verify được |
| Warning | Mojibake/encoding | Nhiều file | UI XAML và service message | Build hiện fail vì encoding; còn nhiều chuỗi lỗi tiếng Việt |
| Not verified | BackgroundService failed/StopHost hiện tại | Chưa kiểm tra được runtime hiện tại | Background scheduler | Build fail nên chưa xác nhận startup runtime hiện tại sạch log |

## 6. Kết quả kiểm tra module

| Module | Route/View/ViewModel | Service/DI | Empty DB Safe | Binding/Log Issue | Trạng thái | Ghi chú |
| --- | --- | --- | --- | --- | --- | --- |
| Tổng quan | Có `dashboard`/`DashboardViewModel`/`DashboardView.xaml` | ViewModel đăng ký DI | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi riêng trong scan trọng điểm | Warning | Chưa smoke test UI thủ công |
| Virtual File Center | Có `virtual-file-center`/`VirtualExplorerViewModel`/`VirtualExplorerView.xaml` | ViewModel đăng ký DI | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi riêng | Warning | Static check OK, chưa smoke test UI |
| Quản lý xuất nhập tồn | Có `inventory`/`InventoryMovementViewModel`/`InventoryMovementView.xaml` | Service inventory có trong infrastructure | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi binding trọng điểm | Warning | Static check OK |
| Công nợ phải thu | Có `ar`/`ReceivableViewModel`/`ReceivableView.xaml` | Service liên quan AR/AP compile | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi binding trọng điểm | Warning | Static check OK |
| Công nợ phải trả | Có `ap`/`PayableViewModel`/`PayableView.xaml` | Service liên quan AP compile | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi binding trọng điểm | Warning | Static check OK |
| Quỹ & Ngân hàng | Có `cash-bank`/`CashBankViewModel`/`CashBankView.xaml` | `ICashFundService`, `IBankingService` có DI | Chưa kiểm tra runtime với DB trống | Có binding rủi ro tên `AmCount`; service banking còn mojibake | Warning | Cần xác minh property đúng là `Amount` hay `AmCount` |
| TSCĐ & CCDC | Có `asset-management`/`AssetManagementViewModel`/`AssetManagementView.xaml` | Service asset có trong DI/module compile | Chưa kiểm tra runtime với DB trống | Có binding rủi ro `AdjustmentAmCount`, `DepreciationAmCount` | Warning | Cần đối chiếu property VM/DTO |
| Thuế & BCTC | Có `tax-financial`/`TaxFinancialReportingViewModel`/`TaxFinancialReportingView.xaml` | `ITaxService`, `IFinancialStatementService`, `IClosingService` có DI | Chưa kiểm tra runtime với DB trống | Có binding rủi ro `AmCount`; `ClosingService` còn mojibake | Warning | Test TT99/TT200 fail liên quan compliance/mapping |
| Sổ cái | Có `general-ledger`/`GeneralLedgerViewModel`/`GeneralLedgerView.xaml` | Service ledger compile | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi binding trọng điểm | Warning | Static check OK |
| Quản lý khách hàng | Có `customers`/`CustomerPageViewModel`/`CustomerPage.xaml` | ViewModel DI OK | DB hiện vẫn còn 6 khách hàng, chưa đúng reset trắng | Chưa thấy lỗi riêng | Warning | Empty DB chưa kiểm tra được vì DB không trắng |
| Quản lý nhà cung cấp | Có `suppliers`/`SupplierPageViewModel`/`SupplierPage.xaml` | ViewModel DI OK | DB hiện vẫn còn 5 nhà cung cấp | Chưa thấy lỗi riêng | Warning | Empty DB chưa kiểm tra được |
| Quản lý hàng hóa | Có `items`/`ItemsViewModel`/`ItemsView.xaml` | ViewModel DI OK | DB hiện vẫn còn 23 hàng hóa | Chưa thấy lỗi riêng | Warning | Empty DB chưa kiểm tra được |
| Quản lý kho | Có `warehouses`/`WarehousesViewModel`/`WarehousesView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | TrainingDataService còn tên kho mojibake | Warning | Static check OK |
| Bộ phận & Nhân sự | Có `departments`/`DepartmentPeoplePageViewModel`/`DepartmentPeoplePage.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Migration seed cũ có tên bộ phận mojibake | Warning | Cần xử lý seed/migration nếu hiển thị ra UI |
| Phiếu thu | Có `payment-receipt`/`PaymentReceiptsViewModel`/`PaymentReceiptsView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi riêng | Warning | Static check OK |
| Phiếu chi | Có `payment-voucher`/`PaymentVouchersViewModel`/`PaymentVouchersView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi riêng | Warning | Static check OK |
| Phiếu nhập | Có `receipts`/`StockReceiptsViewModel`/`StockReceiptsView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi riêng | Warning | Static check OK |
| Phiếu xuất | Có `issues`/`StockIssuesViewModel`/`StockIssuesView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Chưa thấy lỗi riêng | Warning | Static check OK |
| Cài đặt hệ thống | Có `settings`/`SettingsViewModel`/`SettingsView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Query `SettingDefinitions` không tồn tại khi kiểm tra DB | Warning | Cần xác định schema setting đúng |
| Quản trị bảo mật | Có `security-admin` và `authorization`/`SecurityAdminViewModel`/`SecurityAdminView.xaml` | Permission/navigation services có DI | Chưa kiểm tra runtime với DB trống | Log cũ có binding read-only permission row | Warning | Cần smoke test sau khi build pass |
| Trung tâm kiểm tra hệ thống | Có `audit-center`/`AuditCenterViewModel`/`AuditCenterView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Scan còn dấu hiệu mojibake trong view audit | Warning | Static check có rủi ro encoding |
| Trung tâm vận hành | Có `operations-center`/`OperationsCenterViewModel`/`OperationsCenterView.xaml` | ViewModel DI OK | Chưa kiểm tra runtime với DB trống | Build fail trực tiếp tại view này do encoding | Error | Phải sửa trước khi bàn giao |
| Kỳ kế toán | Có `fiscal-periods`/`FiscalPeriodManagementViewModel`/`FiscalPeriodManagementView.xaml` | Closing/fiscal services compile | Chưa kiểm tra runtime với DB trống | Có rủi ro encoding trong nhóm view đã scan | Warning | Static check chưa đủ kết luận runtime |
| Trung tâm thông báo | Có `notification-center`/`NotificationTaskCenterViewModel`/`NotificationTaskCenterView.xaml` | Background scheduler/notification DI có | Chưa kiểm tra runtime với DB trống | Scan còn dấu hiệu mojibake trong view notification | Warning | Background runtime chưa verify do build fail |
| Dữ liệu mẫu | Có `training-data`/`TrainingDataViewModel`/`TrainingDataView.xaml` | Training services có DI | Chưa kiểm tra UI với DB trống | Log cũ có binding `CompletionSummaryItem.Value`; current XAML có OneWay | Warning | Service/test có coverage nhưng app runtime chưa smoke test |

Ghi chú chung module:

* Route/viewmodel/navigation registration kiểm tra bằng static scan trong `NavigationService.cs` và DI registration.
* Không module nào được xác nhận OK tuyệt đối vì build hiện fail và chưa smoke test UI thủ công.
* Empty DB safe chưa xác nhận được thực tế vì DB hiện tại không ở trạng thái trắng.

## 7. Kết quả kiểm tra Training Data

* Preflight: có `GetPreflightAsync` trong `TrainingDataService`; chưa chạy UI preflight vì build fail.
* Safety gate: có các luồng reset `ResetToBlank`, `ResetTrainingOnly`; chưa thực thi reset vì yêu cầu kiểm tra, không thay đổi dữ liệu.
* Marker: `TrainingDataMarkers` tồn tại trong DB; service có xử lý khi marker schema missing bằng warning `TRAINING_MARKER_TABLE_MISSING`.
* Hospitality profiles: tìm thấy `RestaurantDemo`, `HotelDemo`, `RestaurantHotelFullDemo`.
* Reset: chưa xác nhận end-to-end bằng UI; DB hiện tại chứng minh reset trắng chưa đạt vì business data vẫn còn.
* UI binding: current `TrainingDataView.xaml` đã thấy binding `CompletionSummaryItem.Value` dùng `Mode=OneWay`, nhưng log release cũ vẫn có lỗi; cần smoke test sau khi build pass.
* Docs: chưa kiểm tra tài liệu onboarding/training-data đầy đủ; chỉ kiểm tra code/test/schema liên quan.
* Không còn mojibake trong module này: chưa đạt. `TrainingDataService.cs` vẫn có chuỗi tên kho tiếng Việt bị mojibake.

## 8. Kết quả kiểm tra encoding/mojibake

| File | Vấn đề | Mức độ | Gợi ý xử lý |
| --- | --- | --- | --- |
| `ASMILE.BO.App\Views\OperationsCenterView.xaml` | Build hook phát hiện header bị mojibake tại dòng 230 | Critical | Sửa đúng UTF-8 theo nội dung nghiệp vụ, chạy lại build |
| `ASMILE.BO.App\Views\CashBankView.xaml` | Có binding/chuỗi nghi ngờ bị sửa sai chữ trong tên property `AmCount` | High | Đối chiếu VM/DTO, sửa đúng property nếu là `Amount`; không sửa đoán |
| `ASMILE.BO.App\Views\TaxFinancialReportingView.xaml` | Có binding nghi ngờ `AmCount` | High | Đối chiếu property thật trong VM/DTO |
| `ASMILE.BO.App\Views\AssetManagementView.xaml` | Có binding nghi ngờ `AdjustmentAmCount`, `DepreciationAmCount` | High | Đối chiếu property thật trong VM/DTO |
| `ASMILE.BO.App\Views\AuditCenterView.xaml` | Scan còn dấu hiệu mojibake/chữ sai trong text tiếng Việt | Medium | Sửa thủ công theo ngữ cảnh UI sau khi xác minh |
| `ASMILE.BO.App\Views\NotificationTaskCenterView.xaml` | Scan còn dấu hiệu mojibake/chữ sai trong text tiếng Việt | Medium | Sửa thủ công theo ngữ cảnh UI sau khi xác minh |
| `ASMILE.BO.App\Views\WorkspaceSettingsView.xaml` | Nhóm view từng được scan có dấu hiệu encoding/chữ sai | Medium | Re-scan từng dòng và sửa có kiểm chứng |
| `ASMILE.BO.App\Views\FiscalPeriodManagementView.xaml` | Nhóm view từng được scan có dấu hiệu encoding/chữ sai | Medium | Re-scan từng dòng và sửa có kiểm chứng |
| `ASMILE.BO.App\Views\SecurityAdminView.xaml` | Nhóm view từng được scan có dấu hiệu encoding/chữ sai, log cũ có binding permission | Medium | Kiểm tra text và binding sau build pass |
| `ASMILE.BO.App\Views\AssetManagementView.xaml` | Nhóm view từng được scan có dấu hiệu encoding/chữ sai | Medium | Kiểm tra text hiển thị người dùng |
| `ASMILE.BO.Infrastructure\Services\BankingService.cs` | Nhiều exception/log message tiếng Việt bị mojibake | High | Sửa thủ công message user-facing/log-facing, lưu UTF-8 |
| `ASMILE.BO.Infrastructure\Services\ClosingService.cs` | Nhiều exception/log message tiếng Việt bị mojibake | High | Sửa thủ công message kế toán, chạy test TT99/TT200 sau sửa |
| `ASMILE.BO.Infrastructure\Services\PermissionService.cs` | Message permission tiếng Việt bị mojibake | Medium | Sửa message và smoke test security admin |
| `ASMILE.BO.Infrastructure\Services\TrainingDataService.cs` | Một số tên kho/seed tiếng Việt bị mojibake | Medium | Sửa seed text và kiểm tra reset/training data |
| `ASMILE.BO.Infrastructure\Persistence\Migrations\*.cs` | Một số migration seed cũ có text mojibake | Low/Medium | Chỉ sửa nếu migration chưa apply hoặc có chiến lược data correction; tránh rewrite migration đã apply nếu không cần |

## 9. Kết quả kiểm tra XAML binding

| File | Binding/Resource issue | Mức độ | Gợi ý xử lý |
| --- | --- | --- | --- |
| `ASMILE.BO.App\Views\TrainingDataView.xaml` | Log cũ lỗi binding read-only `CompletionSummaryItem.Value`; current XAML dùng `Mode=OneWay` | Medium | Smoke test UI sau khi build pass để xác nhận hết lỗi |
| `ASMILE.BO.App\Views\SecurityAdminView.xaml` | Log cũ lỗi binding read-only `PermissionSelectionRow.IsAssigned` và `IsDangerous` | High | Kiểm tra binding mode hoặc property setter trong VM |
| `ASMILE.BO.App\Views\CashBankView.xaml` | Binding property nghi ngờ sai tên `AmCount` | High | Đối chiếu với VM; nếu property không tồn tại sẽ gây binding fail/runtime UI không hiển thị số tiền |
| `ASMILE.BO.App\Views\TaxFinancialReportingView.xaml` | Binding property nghi ngờ sai tên `AmCount` | High | Đối chiếu với VM/report DTO |
| `ASMILE.BO.App\Views\AssetManagementView.xaml` | Binding property nghi ngờ sai tên `AdjustmentAmCount`, `DepreciationAmCount` | High | Đối chiếu với VM/DTO |
| `ASMILE.BO.App\Views\DashboardView.xaml` | Static scan chưa phát hiện lỗi cụ thể | Not verified | Chưa smoke test UI thủ công |
| `ASMILE.BO.App\Views\GeneralLedgerView.xaml` | Static scan chưa phát hiện lỗi cụ thể | Not verified | Chưa smoke test UI thủ công |
| `ASMILE.BO.App\Views\InventoryMovementView.xaml` | Static scan chưa phát hiện lỗi cụ thể | Not verified | Chưa smoke test UI thủ công |
| `ASMILE.BO.App\Views\ReceivableView.xaml` | Static scan chưa phát hiện lỗi cụ thể | Not verified | Chưa smoke test UI thủ công |
| `ASMILE.BO.App\Views\PayableView.xaml` | Static scan chưa phát hiện lỗi cụ thể | Not verified | Chưa smoke test UI thủ công |
| `ASMILE.BO.App\Views\SettingsView.xaml` | Static scan chưa phát hiện lỗi cụ thể; DB setting schema có nghi vấn | Warning | Xác minh schema setting và UI setting sau build pass |

Ghi chú XAML:

* Build đã đi tới XAML compile và fail ở post-build encoding hook, không thấy lỗi compile-time StaticResource nổi bật trong output build.
* Static scan không thay thế được UI smoke test. Hiện chưa smoke test UI thủ công vì build fail.

## 10. Rủi ro còn lại

* Không thể xác nhận app startup sạch vì build hiện fail.
* Không thể xác nhận runtime log hiện tại sạch vì chưa chạy được app từ build pass.
* Không thể xác nhận empty state sau reset trắng vì DB hiện tại không trắng.
* Vẫn còn rủi ro binding silent fail trong WPF nếu property sai tên nhưng không làm build fail.
* Vẫn còn rủi ro background scheduler/hosted service fail khi app startup do log cũ có DbContext concurrency và DI missing service.
* Vẫn còn rủi ro môi trường khác thiếu migration training-data nếu chưa apply migration `AddTrainingDataMarkers`.
* Có nguy cơ sửa encoding hàng loạt sai nghĩa nghiệp vụ nếu tự động replace không theo ngữ cảnh.

## 11. Việc cần sửa ưu tiên

### Critical

* Sửa lỗi encoding trong `OperationsCenterView.xaml` làm build fail.
* Sửa hoặc cập nhật logic/test TT99 và TT200 để test compliance mapping pass đúng nghiệp vụ.
* Xác định và thực hiện quy trình reset trắng dữ liệu nghiệp vụ; hiện DB vẫn còn dữ liệu business.

### High

* Kiểm tra và sửa toàn bộ mojibake user-facing trong `BankingService.cs`, `ClosingService.cs`, `PermissionService.cs`, `TrainingDataService.cs`.
* Đối chiếu các binding nghi ngờ `AmCount`/`AdjustmentAmCount`/`DepreciationAmCount` với ViewModel/DTO thực tế.
* Kiểm tra lại binding permission row trong `SecurityAdminView.xaml` để không binding TwoWay vào read-only property.
* Chạy lại app startup sau khi build pass và scan log mới.

### Medium

* Re-scan thủ công 9 file XAML người dùng đã nêu: `OperationsCenterView.xaml`, `TaxFinancialReportingView.xaml`, `SecurityAdminView.xaml`, `AssetManagementView.xaml`, `CashBankView.xaml`, `FiscalPeriodManagementView.xaml`, `WorkspaceSettingsView.xaml`, `NotificationTaskCenterView.xaml`, `AuditCenterView.xaml`.
* Kiểm tra lại UI route từng module bằng smoke test nếu có automation hoặc thao tác thủ công.
* Xác minh schema setting đúng, đặc biệt việc không có bảng `SettingDefinitions`.
* Kiểm tra background scheduler bằng startup runtime và log mới.

### Low

* Xem xét text mojibake trong migration seed cũ; chỉ sửa bằng data correction/migration mới nếu text đó còn hiển thị cho người dùng.
* Bổ sung script scan encoding vào CI để chặn lỗi lặp lại trước khi commit/build release.
* Bổ sung checklist binding XAML cho property read-only và property không tồn tại.

## 12. Kết luận

App chưa đủ điều kiện bàn giao khách mới.

Lý do: build hiện tại FAIL do lỗi encoding, unit test FAIL ở TT99/TT200, database sau reset trắng chưa sạch dữ liệu nghiệp vụ, và vẫn còn nhiều dấu hiệu mojibake/binding/runtime log cần xử lý. Các module đã có route/view/viewmodel/DI ở mức static scan, nhưng chưa thể xác nhận runtime ổn định vì build không pass và chưa smoke test UI thủ công.

Điều kiện tối thiểu trước khi bàn giao:

* Build pass với `dotnet build .\ASMILE.BO.sln --no-incremental`.
* Test pass hoặc có quyết định nghiệp vụ rõ ràng cho 2 test TT99/TT200 đang fail.
* DB reset trắng thật sự sạch business data nhưng giữ system data cần thiết.
* Không còn mojibake trong các view/service user-facing.
* Chạy startup app và scan log mới không còn lỗi XAML binding, DbContext concurrency, DI missing service, invalid object name hoặc background service fail.

---

## Stabilization Phase 0 - Baseline & Safety (20260528_091901)

- Thời gian: 2026-05-28T09:19:02.3741287+07:00
- Database backup: $backupFile
- Baseline report: D:\ASMILE-BO\KIEMTRAAPP_BEFORE_FIX.md
- Connection string: Server=localhost;Database=ASMILE_BO;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=True;
- Environment: Windows PowerShell, .NET SDK 10.0.100, app target .NET 8 WPF.
- Code changes: none in phase 0.
- Checkpoint: PASS.
