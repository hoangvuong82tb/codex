# Xác minh database trước migration — Front Office

**Thời điểm xác minh:** 18/07/2026  
**Chế độ:** read-only. Không apply migration, không tạo migration, không sửa source/UI/database.

## 1. Kết luận

Ứng dụng Debug đã dùng đúng database runtime **`ASMILE_BO`** trên SQL Server **`HOANGVUONGPC\SQLEXPRESS`**; không phải `ASMILE_BO_DesignTime`, LocalDB hay database khác.

Tuy nhiên database không chỉ thiếu migration Hotel Stay: source có **11 migration pending**. Vì EF Core áp migration theo chuỗi tuần tự, không an toàn để chạy một lệnh update nhắm riêng Check-in mà không đánh giá toàn bộ 11 migration đó trước.

## 2. Runtime ConnectionString

`ASMILE.BO.App/App.xaml.cs` nạp duy nhất `appsettings.json` từ `AppContext.BaseDirectory`. File trong output Debug và source có cùng SHA-256, vì vậy đây là cấu hình đã được dùng cho lần chạy tái hiện lỗi.

```text
Server=localhost;Database=ASMILE_BO;User Id=sa;Password=***;
TrustServerCertificate=True;MultipleActiveResultSets=True;
```

| Thuộc tính | Giá trị runtime |
| --- | --- |
| Server | `localhost` (SQL Server trả về `HOANGVUONGPC\SQLEXPRESS`) |
| Database | `ASMILE_BO` |
| TrustedConnection / Integrated Security | Không khai báo; có `User Id`, nên dùng SQL authentication, không phải Trusted Connection |
| Encrypt | Không khai báo; provider default áp dụng |
| TrustServerCertificate | `True` |
| MultipleActiveResultSets | `True` |
| ApplicationName | Không khai báo; provider default áp dụng |
| MigrationMode | `Disabled` |
| AllowApplyPendingMigrations | `False` |
| FailOnPendingMigrations | `True` |

## 3. Runtime Database

Đã chạy read-only trên đúng endpoint/configuration ở trên:

```sql
SELECT DB_NAME();
```

Kết quả:

```text
RuntimeDatabase = ASMILE_BO
SqlServer       = HOANGVUONGPC\SQLEXPRESS
Edition         = Express Edition (64-bit)
```

**Kết luận:** connection runtime là đúng `ASMILE_BO`; không cần và không được đổi connection string trong bước này.

## 4. Bảng Front Office được yêu cầu

| Bảng được kiểm tra | Trạng thái | Ghi chú |
| --- | --- | --- |
| `[fo].[DatPhong]` | EXISTS | Reservation table đã có |
| `[fo].[LuuTruKhachSan]` | **MISSING** | Hotel Stay table mà Check-in đang truy vấn |
| `[fo].[LichSuDatPhong]` | EXISTS | Reservation history đã có |
| `[fo].[HotelRoom]` | MISSING | Không phải physical table name của source |
| `[fo].[HotelRoomType]` | MISSING | Không phải physical table name của source |

Hai tên master-data vật lý đúng theo migration source là:

| Physical table thực tế | Trạng thái |
| --- | --- |
| `[fo].[PhongKhachSan]` | EXISTS |
| `[fo].[LoaiPhongKhachSan]` | EXISTS |

Danh sách schema `fo` hiện có: `DatPhong`, `LichSuDatPhong`, `PhongKhachSan`, `LoaiPhongKhachSan`, cùng các technical stores (`fo_hop_thu_den`, `fo_hop_thu_di`, `fo_khoa_idempotency`).

## 5. `__EFMigrationsHistory` trong database

Danh sách theo đúng thứ tự `MigrationId`:

```text
20260421153713_InitialCreate
20260424054559_AddCustomersModule
20260424072605_AddDepartmentPeopleModule
20260424084053_AddWarehouseDepartmentAndResponsible
20260426091500_AddApprovalStatusForStockVouchers
20260427034808_AddStockDetailManualColumns
20260427054537_AddReceiptReceiverName
20260517090000_AddLedgerAccounts
20260520051037_AddAccountingCoreEntities
20260520083954_FixBusinessDocumentAmountPrecision
20260520093157_AddStockVoucherPosting
20260520145845_AddLedgerAccountMetadata
20260520150643_AlignPostingMappingsToDetailAccounts
20260520153302_AddArApPersistedTransactions
20260520155935_AddImportEngine
20260521020906_AddInventoryCosting
20260521023835_AddReportEngine
20260521031817_AddAccountingStandardEngine
20260521041101_AddCashFundModule
20260521043833_AddBankingModule
20260521051106_AddFixedAssetModule
20260521053544_AddToolAssetModule
20260521072800_AddVatModule
20260521080623_AddFinancialStatementEngine
20260521093739_AddAuditTrailAndConcurrency
20260522131611_AddEnterpriseShellAndPermissions
20260522133345_AddEnterpriseOperationsAndSecurityUi
20260522144345_AddMultiCompanyFoundation
20260523042803_AddBackgroundSchedulerEngine
20260523053846_EnhanceEnterpriseAuditTrail
20260523062650_CompleteEnterpriseAuditLifecycle
20260523065326_AddEnterpriseRetentionAndArchival
20260523075514_AddEnterpriseTelemetryAndDiagnostics
20260523094333_AddWorkflowResilienceAndRecovery
20260523103649_AddEnterpriseReliabilityAndDistributedCoordination
20260523111335_AddEnterpriseMultiCompanyBranchHardening
20260523145315_AddTt99ComplianceFoundation
20260523153700_AddTt200ComplianceFoundation
20260524030741_AddEnterpriseApprovalWorkflowFoundation
20260524060743_AddAdvancedFinancialStatementMappingFoundation
20260524061631_AddPhase4ExecutionFoundationStep46To412
20260525031433_AddTrainingDataRunHistory
20260525113000_AddTrainingDataRunStepDetails
20260525133000_AddTrainingDataMarkers
20260527091500_AddStockReceiptDetailVatColumns
20260528095952_AddBreakfastCafeSalesModule
20260528183000_AddMenuRecipeModule
20260529152901_AddPreparationAreaToMenuItem
20260607022536_AddBreakfastCafePosTablesAndKitchenTickets
20260614055304_AddRestaurantAlacarteAndBanquetModules
20260615090000_AddRecipeIngredientSubstitutes
20260617031602_AddRestaurantQuotes
20260617043114_AddRestaurantQuoteGuestCount
20260618100620_AddAlacarteVipP0QuoteAndOrderTables
20260620075759_AddRestaurantServingGroupsAndSupplements
20260624021045_AddBanquetMenuPackageLinePricingRule
20260624082548_AddQuoteChangeRequest
20260624091944_AddQuoteChangeRequestRowVersion
20260710033555_CompleteBanquetOverviewEventFields
20260710040633_AddBanquetEventMenuCustomColumns
20260711043948_AddBanquetMenuGroups
20260713032500_AddBanquetRoomLayout
20260715083035_AddBanquetServiceUnits
20260715083738_AddBanquetQuantificationAndSettings
20260715085020_AddBanquetFloorPlanAndDepositReceipt
20260715100639_AddBanquetCommercialControls
20260715101214_AddBanquetPurchaseRequests
20260716085141_AddBanquetServiceCosting
20260717102111_CreateFrontOfficeFoundationTechnicalStores
20260717105902_CreateFrontOfficeProcessManagerFoundation
20260717124748_HardenFrontOfficeProcessManagerRuntime
20260717143000_AddFrontOfficeProcessWorkerDeadLetters
20260718011133_AddFrontOfficeReservation
20260718014449_AddFrontOfficeHotelRoomMasterData
```

Tổng số migration đã áp dụng: **74**.

## 6. Đối chiếu với source hiện tại

Source có 85 migration; 74 đã có trong database và 11 chưa có. Không có migration nào trong database bị thiếu file source.

### Migration có trong source nhưng chưa có trong `ASMILE_BO`

```text
20260522221500_Phase3Step32SqlPerformanceScaling
20260522223000_Phase3Step33EnterprisePermissions
20260522224500_Phase3Step34EInvoiceFoundation
20260522230000_Phase3Step35SchedulerEngine
20260523010000_AddFiscalPeriodAndBranchEngine
20260523012000_AddEnterpriseNotificationCenter
20260523014000_AddEnterpriseSearchIndex
20260629114500_AddParentCategoryToMenuCategory
20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices
20260718092912_AddFrontOfficeHotelStayFoundation
20260718095823_AddFrontOfficeHotelStayCheckIn
```

### Migration Front Office cần cho lỗi Check-in

1. `20260718092912_AddFrontOfficeHotelStayFoundation` — tạo `[fo].[LuuTruKhachSan]`, `[fo].[LichSuLuuTruKhachSan]` và sequence `fo.TrinhTuSoLuuTru`.
2. `20260718095823_AddFrontOfficeHotelStayCheckIn` — bổ sung index/constraint cho Check-in trên bảng Hotel Stay đã được tạo ở bước 1.

## 7. Đánh giá an toàn trước khi apply

| Tiêu chí | Đánh giá |
| --- | --- |
| Đúng database mục tiêu | PASS — `ASMILE_BO` |
| Nhầm LocalDB / DesignTime | NO |
| Cần sửa connection trước | NO |
| Chỉ thiếu migration Front Office | **NO** — còn 9 migration non-FO pending trước/giữa chuỗi |
| An toàn để tự động apply ngay | **NO** |

Không nên chạy `database update` ngay trên database này. Một lệnh EF Core thông thường sẽ áp **toàn bộ 11 migration pending theo thứ tự**, không chỉ hai migration Front Office. Đây là thay đổi schema có phạm vi lớn hơn việc sửa Check-in.

## 8. Khuyến nghị bước tiếp theo

1. Giữ nguyên connection `ASMILE_BO`; không đổi sang LocalDB hoặc `ASMILE_BO_DesignTime`.
2. Review nội dung/up-down và dependency của toàn bộ 11 migration pending trong môi trường staging có snapshot/backup tương đương.
3. Xác nhận chủ sở hữu database chấp thuận phạm vi đầy đủ 11 migration.
4. Chỉ sau khi được phê duyệt, áp chuỗi migration theo quy trình release có backup và kế hoạch rollback.
5. Sau khi `[fo].[LuuTruKhachSan]` tồn tại, chạy lại Reception → Check-in ở Debug và Release để xác nhận list/empty state thay generic dialog.

**Không có migration nào được apply trong bước xác minh này.**
