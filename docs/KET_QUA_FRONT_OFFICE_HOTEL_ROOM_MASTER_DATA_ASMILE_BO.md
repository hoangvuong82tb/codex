# KET QUA FRONT OFFICE HOTEL ROOM MASTER DATA ASMILE-BO

## 1. Executive Summary

Da xay dung Hospitality Master Data toi thieu cho Front Office: Property boundary, HotelRoomType, HotelRoom va reference boundary cho Reservation sprint sau. Khong sua Reservation, khong dung Restaurant/Banquet Room, khong lam UI, RatePlan, pricing, availability hay workflow luu tru.

Trang thai: **HOAN THANH CO GIOI HAN**.

## 2. Scope

Da thuc hien domain, application command/query, repository SQL Server/EF Core, reference reader, mapping, DI, migration, LocalDB integration va full build/test. Ngoai scope van duoc giu nguyen.

## 3. Existing Property Discovery

Solution chua co Property aggregate/table master data phu hop de tai su dung. Da tai su dung `PropertyId` strongly typed va `IFrontOfficeExecutionContextAccessor`/`PropertyContext` hien co lam trusted property boundary. Khong tao bang Property moi va khong doan schema legacy.

## 4. Existing Room Model Analysis

Room hien co thuoc Restaurant/Banquet, khong phu hop cho physical hotel room. Hotel domain dung ten rieng `HotelRoomType` va `HotelRoom`, khong co tham chieu den hai module tren.

## 5. Domain Boundary Decision

`HotelRoomType` la master cua loai phong. `HotelRoom` la physical room, chi tham chieu `HotelRoomTypeId`. `PropertyId` bat bien sau khi tao. Cross-property type assignment duoc chan trong application layer bang property-scoped lookup.

## 6. HotelRoomType Domain

Co Code, Name, Description, suc chua standard/maximum, DefaultOccupancy, DisplayOrder, IsActive, audit fields va RowVersion. Domain chap nhan create/update/activate/deactivate va bao ve cac quy tac RT-RULE-001 den RT-RULE-010 lien quan den input/capacity/property immutability.

## 7. HotelRoom Domain

Co RoomNumber, DisplayName, Floor, Building, Zone, Description, OperationalStatus, IsActive, audit fields va RowVersion. Domain chap nhan create/update/change type/change status/activate/deactivate.

## 8. Operational Status

Chi co `Available`, `OutOfOrder`, `OutOfService`. `IsAssignable` chi true khi phong active va `Available`; query assignable cung yeu cau RoomType active.

## 9. Business Rules

Code RoomType unique theo Property; RoomNumber unique theo Property; room type phai ton tai, cung Property va active khi tao/chuyen HotelRoom. Inactive, OutOfOrder va OutOfService khong duoc coi la assignable.

## 10. Commands

Da co create/update/activate/deactivate RoomType; create/update/change type/change operational status/activate/deactivate HotelRoom. Commands lay PropertyId tu execution context, khong nhan PropertyId tu client. Update commands bat buoc ExpectedRowVersion.

## 11. Queries

Da co get by id/code, search va active RoomType; get by id/number, search va assignable HotelRoom. Search dung property scope, paging va sort allowlist; khong dung dynamic raw SQL sort.

## 12. Reference Boundary

`IHotelReservationReferenceReader` cung cap room type reference, room reference va `HotelRoomMatchesRoomTypeAsync`. Reference chi tra safe master-data facts va khong tham chieu Reservation aggregate.

## 13. Property Isolation

Repository lookup/search/reference deu filter `PropertyId` o SQL query. Property khac nhan ket qua null, tranh ro ri thong tin. LocalDB test da xac nhan RoomType/Room cua Property A khong doc duoc boi Property B.

## 14. Authorization

Property authorization duoc enforced bang trusted Front Office execution context. Chua co permission-check cu the cho `FrontOffice.RoomType.*` va `FrontOffice.Room.*`; day la limitation can ket noi voi authorization policy co san truoc khi mo UI/API.

## 15. Audit

Command service stage `FrontOfficeAuditIntent` trong cung persistence session; metadata chi gom PropertyId, khong log raw command hay SQL exception.

## 16. Concurrency

Hai aggregate co RowVersion private setter, SQL Server mapping rowversion, DTO tra token va command update yeu cau token. Persistence session chuyen concurrency failure thanh machine-readable `HOTEL_ROOM_TYPE_CONCURRENCY_CONFLICT` hoac `HOTEL_ROOM_CONCURRENCY_CONFLICT`.

## 17. Persistence Mapping

`HotelRoomType` map `fo.LoaiPhongKhachSan`; `HotelRoom` map `fo.PhongKhachSan`. FK HotelRoomType dung Restrict. Khong dung composite FK `(PropertyId, HotelRoomTypeId)` de tranh tang phuc tap; ung dung bat buoc property-scoped lookup truoc moi gan/chuyen type.

## 18. Database Tables

- `fo.LoaiPhongKhachSan`
- `fo.PhongKhachSan`

## 19. Indexes

- Unique `(PropertyId, Code)` cho RoomType.
- RoomType: `(PropertyId, IsActive)`, `(PropertyId, DisplayOrder)`.
- Unique `(PropertyId, RoomNumber)` cho HotelRoom.
- HotelRoom: `(PropertyId, HotelRoomTypeId)`, `(PropertyId, OperationalStatus)`, `(PropertyId, IsActive)`.

## 20. Constraints

Primary keys, required identifiers/code/name/room number, unique property-scoped keys, rowversion va Restrict FK da duoc tao trong migration.

## 21. Foreign Keys

`PhongKhachSan.HotelRoomTypeId` -> `LoaiPhongKhachSan.HotelRoomTypeId`, `Restrict`. Application validation bao ve them same-property va active type.

## 22. Migration

Migration moi: `20260718014449_AddFrontOfficeHotelRoomMasterData`. Migration chi chua hai bang, indexes va FK thuoc scope. Khong sua migration Reservation hay migration cu.

## 23. Snapshot Changes

EF scaffold da cap nhat `AppDbContextModelSnapshot.cs` va designer migration theo model hien hanh. Main migration da duoc kiem tra chi co DDL trong scope Hotel Room Master Data.

## 24. Unit Test Results

`dotnet test ASMILE.BO.Tests.Unit --no-build`: **230 passed, 0 failed**.

Chua them bo unit test rieng theo tung ma RT-U/ROOM-U; cac quy tac domain quan trong duoc bao phu bang LocalDB integration test va can tach thanh unit test chi tiet o hardening tiep theo.

## 25. Integration Test Results

`dotnet test ASMILE.BO.Tests.Integration --no-build`: **19 passed, 0 failed**.

Test `HMS_I_001_to_030_clean_localdb_migration_enforces_property_isolation_and_room_assignability` xac nhan clean migration, hai table, property isolation, same code khac Property, assignable query va reference lookup.

## 26. Reference Boundary Tests

Da xac nhan room type/room reference an du lieu Property khac, room Available active la assignable, va room match dung RoomType.

## 27. Architecture Tests

Khong co architecture-test harness rieng moi. Separation da duoc giu trong project references: Core khong phu thuoc EF/WPF/Infrastructure; HotelRoom domain khong tham chieu Restaurant/Banquet.

## 28. DI Validation

Da dang ky scoped repository, reference reader, application service, 10 command handlers va 8 query handlers. Khong co singleton giu DbContext.

## 29. EF Tooling Results

`dotnet ef dbcontext info`: AppDbContext, SqlServer, LocalDB thanh cong.

`dotnet ef migrations list`: co migration `20260718014449_AddFrontOfficeHotelRoomMasterData`; cac migration hien Pending tren design database, khong truy cap production database.

## 30. Build Results

`dotnet build ASMILE.BO.sln --no-restore`: **0 warning, 0 error**. XAML encoding check pass.

## 31. Files Created

- `ASMILE.BO.Core/FrontOffice/HotelMasterData/HotelRooms.cs`
- `ASMILE.BO.Application/FrontOffice/HotelMasterData/HotelRoomMasterDataContracts.cs`
- `ASMILE.BO.Application/FrontOffice/HotelMasterData/HotelRoomMasterDataHandlers.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/HotelMasterData/EfHotelRoomMasterDataRepository.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260718014449_AddFrontOfficeHotelRoomMasterData.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260718014449_AddFrontOfficeHotelRoomMasterData.Designer.cs`

## 32. Files Modified

- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs`

## 33. Package Changes

Khong them package moi.

## 34. Deviations

Property hien la execution boundary, chua phai Property Management aggregate. FK don khong tu bao ve cross-property mismatch; application validation va property-scoped lookup la mandatory protection.

## 35. Existing Issues

`AppDbContext.SaveChangesAsync` co co che retry concurrency chung; can duoc danh gia rieng khi hoan tat hardening concurrency end-to-end cua cac vertical slice.

## 36. Known Limitations

Chua co permission policy wiring cu the, unit test matrix RT-U/ROOM-U day du, hay architecture test harness rieng cho module. Chua co composite FK PropertyId + RoomTypeId.

## 37. Deferred Work

RatePlan, pricing, availability theo ngay, Reservation reference validation wiring, Room assignment workflow, Housekeeping, Maintenance, Check-in/Check-out va WPF deu chua thuc hien.

## 38. Recommended Next Sprint

Ket noi `IHotelReservationReferenceReader` vao Reservation reference validation; bo sung permission policy va unit/architecture tests chi tiet; sau do thiet ke RatePlan rieng truoc availability/pricing.

## 39. Final Status

**HOAN THANH CO GIOI HAN**. Foundation persistence, migration, Property isolation, wrong-property protection, LocalDB evidence va Reservation reference boundary da hoat dong. Cac limitation ve permission wiring va test matrix can duoc hardening truoc khi phat hanh UI/API.
