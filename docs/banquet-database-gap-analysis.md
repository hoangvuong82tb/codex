# Banquet Database Gap Analysis — ASMILE-BO

> Ngày kiểm tra: 2026-07-10 | Đối chiếu Entity ↔ Migration ↔ DbContext ↔ Service usage

---

## 1. Hiện trạng CSDL

### 1.1. Migration hiện có

| Migration | Ngày | Nội dung |
|---|---|---|
| `20260614055304_AddRestaurantAlacarteAndBanquetModules` | 14/06/2026 | Khởi tạo toàn bộ bảng Restaurant + Banquet |
| `20260624021045_AddBanquetMenuPackageLinePricingRule` | 24/06/2026 | Thêm cột `PricingRule` vào `BanquetMenuPackageLines` |

### 1.2. Các bảng Banquet hiện có (12 bảng)

| # | Bảng | Entity | DbSet | Migration | Số cột |
|---|---|---|---|---|---|
| 1 | `BanquetRooms` | `BanquetRoom` | `BanquetRooms` | Có | 10 |
| 2 | `BanquetEvents` | `BanquetEvent` | `BanquetEvents` | Có | 14 |
| 3 | `BanquetMenuPackages` | `BanquetMenuPackage` | `BanquetMenuPackages` | Có | 9 |
| 4 | `BanquetMenuPackageLines` | `BanquetMenuPackageLine` | `BanquetMenuPackageLines` | Có | 8 |
| 5 | `BanquetEventMenus` | `BanquetEventMenu` | `BanquetEventMenus` | Có | 10 |
| 6 | `BanquetServiceLines` | `BanquetServiceLine` | `BanquetServiceLines` | Có | 9 |
| 7 | `BanquetContracts` | `BanquetContract` | `BanquetContracts` | Có | 10 |
| 8 | `BanquetDeposits` | `BanquetDeposit` | `BanquetDeposits` | Có | 11 |
| 9 | `BanquetChecklists` | `BanquetChecklist` | `BanquetChecklists` | Có | 8 |
| 10 | `BanquetFloorPlans` | `BanquetFloorPlan` | `BanquetFloorPlans` | Có | 5 |
| 11 | `BanquetFloorPlanTables` | `BanquetFloorPlanTable` | `BanquetFloorPlanTables` | Có | 10 |
| 12 | `BanquetTimelines` | `BanquetTimeline` | `BanquetTimelines` | Có | 8 |

---

## 2. Entity có DbSet nhưng thiếu cột trong DB

### 2.1. `BanquetMenuPackageLine.PricingRule`

- **Entity**: Có property `PricingRule` (line 460, `RestaurantEntities.cs`)
- **Migration gốc (14/06)**: Không có cột
- **Migration bổ sung (24/06)**: Đã thêm → **OK**

### 2.2. `BanquetEvent` — Thiếu 10+ cột từ yêu cầu nghiệp vụ

Các trường sau **có trong ViewModel nhưng Entity không có**:

| Property cần có | Kiểu DB | Lý do |
|---|---|---|
| `BrideName` | nvarchar(255) NULL | Tiệc cưới: tên cô dâu |
| `GroomName` | nvarchar(255) NULL | Tiệc cưới: tên chú rể |
| `ConferenceName` | nvarchar(255) NULL | Hội nghị: tên hội nghị |
| `BirthdayPersonName` | nvarchar(255) NULL | Sinh nhật: tên người tổ chức |
| `BirthdayDate` | datetime2 NULL | Sinh nhật: ngày sinh |
| `MeetingTopic` | nvarchar(255) NULL | Họp mặt: chủ đề |
| `PartyTheme` | nvarchar(255) NULL | Tiệc: chủ đề |
| `OtherEventName` | nvarchar(255) NULL | Sự kiện khác: tên |
| `ExtraGuestCount` | int NOT NULL DEFAULT 0 | Số khách phát sinh |
| `ExtraTableCount` | int NOT NULL DEFAULT 0 | Số bàn phát sinh |
| `GuestConfirmedCount` | int NOT NULL DEFAULT 0 | Số khách chốt cuối |
| `TableConfirmedCount` | int NOT NULL DEFAULT 0 | Số bàn chốt cuối |
| `SetupTime` | time NULL | Giờ setup phòng |
| `TeardownTime` | time NULL | Giờ dọn phòng |

### 2.3. `Customer` — [NotMapped] properties

| Property | Trạng thái | Kiểu DB cần |
|---|---|---|
| `Email` | **[NotMapped]** — không lưu DB | nvarchar(255) NULL |
| `TaxCode` | **[NotMapped]** — không lưu DB | nvarchar(50) NULL |
| `CreatedAt` | **[NotMapped]** — không lưu DB | datetime2 NOT NULL |
| `UpdatedAt` | Không có trong entity | datetime2 NOT NULL |

### 2.4. `BanquetEvent` — Thiếu audit columns

| Property cần có | Kiểu DB |
|---|---|
| `CreatedBy` | nvarchar(120) NULL |
| `UpdatedBy` | nvarchar(120) NULL |
| `RowVersion` | rowversion (concurrency token) |

### 2.5. `BanquetChecklist` — Thiếu audit columns

| Property cần có | Kiểu DB |
|---|---|
| `CreatedAt` | datetime2 NOT NULL |
| `UpdatedAt` | datetime2 NOT NULL |

### 2.6. `BanquetTimeline` — Thiếu audit columns

| Property cần có | Kiểu DB |
|---|---|
| `CreatedAt` | datetime2 NOT NULL |
| `UpdatedAt` | datetime2 NOT NULL |

---

## 3. Bảng cần bổ sung

### 3.1. `EventTypes` (P1)

```sql
CREATE TABLE EventTypes (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(255) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NOT NULL DEFAULT 0,
    CONSTRAINT UQ_EventTypes_Code UNIQUE (Code)
);
```

### 3.2. `EventStatuses` (P1)

```sql
CREATE TABLE EventStatuses (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(255) NOT NULL,
    FromStatuses nvarchar(500) NULL, -- JSON array of allowed previous statuses, null = any
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NOT NULL DEFAULT 0,
    CONSTRAINT UQ_EventStatuses_Code UNIQUE (Code)
);
```

### 3.3. `EventStatusHistories` (P1)

```sql
CREATE TABLE EventStatusHistories (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BanquetEventId uniqueidentifier NOT NULL,
    FromStatus nvarchar(50) NULL, -- NULL when creating
    ToStatus nvarchar(50) NOT NULL,
    ChangedBy nvarchar(120) NULL,
    ChangedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
    Reason nvarchar(1000) NULL,
    CONSTRAINT FK_EventStatusHistories_BanquetEvents FOREIGN KEY (BanquetEventId)
        REFERENCES BanquetEvents(Id) ON DELETE CASCADE
);
CREATE INDEX IX_EventStatusHistories_BanquetEventId ON EventStatusHistories(BanquetEventId);
CREATE INDEX IX_EventStatusHistories_ChangedAt ON EventStatusHistories(ChangedAt);
```

### 3.4. `BanquetSettings` (P1)

```sql
CREATE TABLE BanquetSettings (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
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
    CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt datetime2 NOT NULL DEFAULT SYSDATETIME()
);
```

### 3.5. `ServiceTypes` (P2)

```sql
CREATE TABLE ServiceTypes (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(255) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NOT NULL DEFAULT 0,
    CONSTRAINT UQ_ServiceTypes_Code UNIQUE (Code)
);
```

### 3.6. `EventAttachments` (P2)

```sql
CREATE TABLE EventAttachments (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BanquetEventId uniqueidentifier NOT NULL,
    FileName nvarchar(500) NOT NULL,
    FilePath nvarchar(1000) NOT NULL,
    ContentType nvarchar(100) NULL,
    FileSize bigint NULL,
    UploadedBy nvarchar(120) NULL,
    UploadedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
    Note nvarchar(500) NULL,
    CONSTRAINT FK_EventAttachments_BanquetEvents FOREIGN KEY (BanquetEventId)
        REFERENCES BanquetEvents(Id) ON DELETE CASCADE
);
```

### 3.7. `EventNotes` (P2)

```sql
CREATE TABLE EventNotes (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    BanquetEventId uniqueidentifier NOT NULL,
    Content nvarchar(max) NOT NULL,
    CreatedBy nvarchar(120) NULL,
    CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME(),
    IsInternal bit NOT NULL DEFAULT 1, -- true = internal note, false = visible to customer
    CONSTRAINT FK_EventNotes_BanquetEvents FOREIGN KEY (BanquetEventId)
        REFERENCES BanquetEvents(Id) ON DELETE CASCADE
);
```

### 3.8. `CustomerSources` (P2)

```sql
CREATE TABLE CustomerSources (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(255) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NOT NULL DEFAULT 0,
    CONSTRAINT UQ_CustomerSources_Code UNIQUE (Code)
);
```

---

## 4. Quan hệ cần sửa

### 4.1. Cascade delete → Restrict/NoAction (P0)

**Hiện tại:**
```csharp
// AppDbContext.cs:3976-3977
entity.HasMany(x => x.Contracts).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
entity.HasMany(x => x.Deposits).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Cascade);
```

**Cần sửa thành:**
```csharp
entity.HasMany(x => x.Contracts).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Restrict);
entity.HasMany(x => x.Deposits).WithOne(x => x.Event).HasForeignKey(x => x.BanquetEventId).OnDelete(DeleteBehavior.Restrict);
```

**Lý do:** Xóa sự kiện không được xóa hợp đồng và lịch sử đặt cọc. Cần soft delete event thay vì hard delete.

### 4.2. BanquetServiceLine → Supplier (P3)

```csharp
// Hiện tại: SetNull
entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
// OK — giữ nguyên
```

---

## 5. Index và constraint cần bổ sung

| Bảng | Index/Constraint | Mục đích | Mức độ |
|---|---|---|---|
| `BanquetEvents` | `IX_BanquetEvents_Phone` | Tìm sự kiện theo SĐT | P2 |
| `BanquetEvents` | `IX_BanquetEvents_EventDate` | Lọc theo ngày (hiện chỉ có composite index với Room) | P2 |
| `BanquetEvents` | `UQ_BanquetEvents_RoomDateTime` on `(BanquetRoomId, EventDate, StartTime, EndTime)` | Chống trùng lịch ở DB level (cần filter `Status != 'Cancelled'`) | P1 |
| `BanquetEvents` | `IX_BanquetEvents_Status` | Lọc theo trạng thái | P2 |
| `Customers` | `UQ_Customers_Phone` | Chống trùng SĐT khách hàng | P1 |
| `BanquetDeposits` | `IX_BanquetDeposits_DepositDate` | Báo cáo cọc theo ngày | P2 |
| `BanquetContracts` | `IX_BanquetContracts_SignDate` | Báo cáo hợp đồng theo ngày ký | P2 |

---

## 6. Kiểu dữ liệu kiểm tra

| Bảng | Cột | Kiểu hiện tại | Đánh giá |
|---|---|---|---|
| `BanquetEvents` | `Phone` | nvarchar(40) | OK — SĐT dạng chuỗi |
| `BanquetMenuPackages` | `Price` | decimal(18,2) | OK |
| `BanquetMenuPackageLines` | `Qty` | decimal(18,3) | OK — cho phép số lẻ |
| `BanquetServiceLines` | `Amount` | decimal(18,2) | OK |
| `BanquetServiceLines` | `VatRate` | decimal(9,4) | OK — VAT có thể lẻ |
| `BanquetDeposits` | `Amount` | decimal(18,2) | OK |
| `BanquetContracts` | `TotalAmount` | decimal(18,2) | OK |
| `BanquetEvents` | `EventDate` | datetime2 | OK |
| `BanquetEvents` | `StartTime` | time | OK |
| `BanquetEvents` | `EndTime` | time | OK |

**Lưu ý:** 
- `datetime2` không có timezone — cần đảm bảo toàn bộ app dùng UTC
- Service code dùng `DateTime.UtcNow` → OK
- Nhưng `EventDate` lưu ngày tổ chức (local date) → có thể bị lệch nếu khác timezone

---

## 7. Migration cần tạo (theo thứ tự an toàn)

### Migration 1: Sửa cascade delete + Thêm cột BanquetEvent

```text
Migration: FixBanquetCascadeDeleteAndAddEventFields
- ALTER BanquetContracts DROP FK → ADD FK ON DELETE NO ACTION
- ALTER BanquetDeposits DROP FK → ADD FK ON DELETE NO ACTION  
- ALTER BanquetEvents ADD BrideName, GroomName, ..., ExtraTableCount, GuestConfirmedCount, TableConfirmedCount, SetupTime, TeardownTime
```

### Migration 2: Sửa Customer

```text
Migration: AddCustomerEmailAndTimestamps
- ALTER Customers ADD Email nvarchar(255) NULL
- ALTER Customers ADD CreatedAt datetime2 NOT NULL DEFAULT SYSDATETIME()
- ALTER Customers ADD UpdatedAt datetime2 NOT NULL DEFAULT SYSDATETIME()
```

### Migration 3: Thêm bảng danh mục

```text
Migration: AddBanquetReferenceTables
- CREATE TABLE EventTypes (...)
- CREATE TABLE EventStatuses (...)
- CREATE TABLE ServiceTypes (...)
- CREATE TABLE CustomerSources (...)
```

### Migration 4: Thêm bảng nghiệp vụ

```text
Migration: AddBanquetOperationalTables
- CREATE TABLE EventStatusHistories (...)
- CREATE TABLE BanquetSettings (...)
- CREATE TABLE EventAttachments (...)
- CREATE TABLE EventNotes (...)
```

### Migration 5: Bổ sung index và constraint

```text
Migration: AddBanquetMissingIndexes
- CREATE INDEX IX_BanquetEvents_Phone ON BanquetEvents(Phone)
- CREATE UNIQUE INDEX UQ_Customers_Phone ON Customers(Phone) WHERE Phone <> ''
- ALTER BanquetEvents ADD CONSTRAINT UQ_BanquetEvents_RoomDateTime ...
- ALTER BanquetEvents ADD RowVersion rowversion
```

---

## 8. DTO chứa field nhưng Entity không có

| DTO | Field | Entity tương ứng | Trạng thái |
|---|---|---|---|
| `BanquetEventDto` | `RoomName` | Computed từ `Room.Name` | OK — tính từ navigation property |
| `BanquetEventDto` | `MenuAmount` | Computed từ `Menus.Sum(m => m.Amount)` | OK — tính từ child collection |
| `BanquetEventDto` | `ServiceAmount` | Computed từ `Services.Sum(s => s.Amount)` | OK |
| `BanquetEventDto` | `DepositAmount` | Computed từ `Deposits.Sum(d => d.Amount)` | OK |
| `BanquetEventDto` | `TotalAmount` | Computed = MenuAmount + ServiceAmount | OK |
| `BanquetEventDto` | `RemainingAmount` | Computed = TotalAmount - DepositAmount | OK |
| `BanquetMenuPackageDto` | `LineCount` | Computed từ `Lines.Count` | OK |

---

## 9. Entity có field nhưng DTO không map

| Entity Field | DTO thiếu | Ảnh hưởng |
|---|---|---|
| `BanquetMenuPackage.Note` | Không có trong DTO (chỉ có trong entity) | P3 — Không hiển thị được ghi chú gói thực đơn |
| `BanquetServiceLine.SupplierId` | Có trong `SaveRequest` nhưng không trong `BanquetServiceLineDto` | P3 — Không hiển thị được NCC |
| `BanquetTimeline.Description` | Có trong DTO | OK |
| `BanquetFloorPlan.*` | Không có DTO | P2 — Chưa có API/DTO cho FloorPlan |

---

## 10. Tổng kết

| Hạng mục | Số lượng |
|---|---|
| Bảng hiện có | 12 |
| Bảng cần thêm | 8 |
| Cột cần thêm vào bảng hiện có | 18 |
| Quan hệ cần sửa | 2 |
| Index cần thêm | 6 |
| Constraint cần thêm | 2 |
| Migration cần tạo | 5 |

---

*Báo cáo được tạo bởi Claude Code analysis ngày 2026-07-10*
