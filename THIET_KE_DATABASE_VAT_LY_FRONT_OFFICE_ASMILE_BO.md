# THIẾT KẾ DATABASE VẬT LÝ FRONT OFFICE ASMILE-BO

## 1. Executive Summary

Tài liệu chốt thiết kế vật lý ở mức pseudo-DDL/data dictionary cho SQL Server, không chứa SQL chạy được, C# compile, migration hay thay đổi database.

Kết quả: 93 bảng write/integration Front Office giữ vật lý; 4 logical entity gộp thành owned/ledger columns; 2 hoãn; 1 AuditRecord chuyển platform_audit; bổ sung 14 lookup typed và 13 projection. Tổng catalog vật lý chính thức: **121 bảng**.

Không có master Room, RoomType, GuestProfile, Company, TravelAgent, Invoice, Journal, AR, AccountingPeriod hoặc Notification trong Front Office.

## 2. Confirmed Decisions

- Room/RoomType thuộc Property/Room Master; Guest/Company/TravelAgent thuộc CRM; financial documents thuộc Accounting; Notification thuộc Notification.
- Domain/code tiếng Anh; bảng/cột tiếng Việt không dấu PascalCase; schema English lowercase có prefix context.
- RoomBlock = KhoiPhongDoan; RoomOperationalBlock = KhoaVanHanhPhong.
- Root dùng application-generated GUID; display code tách PK; mutable Root dùng rowversion.
- BusinessDate=date; system time=datetime2 UTC; Money=decimal(19,4)+Currency.
- Không soft delete/cascade mặc định; ledger/audit/event/attempt append-only; correction bằng reversal.
- Không FK vật lý xuyên module; Outbox/Inbox/Idempotency durable; projection không validate command.

## 3. Logical-to-Physical Review

| Bảng logic | Domain | Quyết định | Lý do | Ảnh hưởng Domain | Query | Audit | Migration | Thay thế |
|---|---|---|---|---|---|---|---|---|
| DatPhong | Reservation | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| KhachDatPhong | ReservationGuest | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NhuCauHangPhong | ReservationRoomDemand | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| AnhChupGiaDatPhong | ReservationRateSnapshot | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThamChieuBaoDamDatPhong | ReservationGuaranteeRef | Gộp/owned columns | Không cần lifecycle/table riêng ở physical model; giảm join/CRUD | Không đổi Domain; tối ưu persistence | Query qua parent/store | Audit qua parent/store | Expand/merge plan | Owned columns trong DatPhong |
| LienHeDatPhong | ReservationContact | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| GhiChuDatPhong | ReservationNote | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| YeuCauUuTienDatPhong | ReservationPreference | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LichSuThayDoiDatPhong | ReservationChangeRecord | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ChoPhong | WaitlistEntry | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LanMoiChoPhong | WaitlistOfferAttempt | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LuuTru | Stay | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| KhachLuuTru | StayGuest | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| GiaHanLuuTru | StayExtension | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| HanCheLuuTru | StayRestriction | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| QuyenKyPhong | StayPostingPermission | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThietLapRiengTuLuuTru | StayPrivacySetting | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhanPhong | RoomAssignment | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhanDoanPhanPhong | AssignmentSegment | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| QuyenMoKhoaPhong | KeyCredential | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LanCapKhoa | CredentialAttempt | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ChuKyKhachLuuTru | GuestSignature | Hoãn triển khai | Phụ thuộc policy/module ngoài chưa chốt | Không đổi Domain; tối ưu persistence | Query qua parent/store | Chưa lưu trước policy | Không tạo phase đầu | Giai đoạn bảo mật tài liệu/chữ ký; DB chỉ secure reference khi triển khai |
| TonPhongKhaDung | RoomAvailability | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| GiuTonPhong | AvailabilityHold | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhanBoTonPhong | AvailabilityAllocation | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| KiemSoatTonPhong | RoomInventoryControl | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| HanCheBanPhong | InventoryRestriction | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| SoGiaoDichKhach | Folio | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NganSoGiaoDich | FolioWindow | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ButToanSoGiaoDich | FolioEntry | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThueButToanSoGiaoDich | FolioEntryTax | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LienKetChuyenButToan | FolioTransferLink | Gộp/owned columns | Không cần lifecycle/table riêng ở physical model; giảm join/CRUD | Không đổi Domain; tối ưu persistence | Query qua parent/store | Audit qua parent/store | Expand/merge plan | Owned link columns trong ButToanSoGiaoDich |
| LienKetDieuChinhButToan | FolioAdjustmentLink | Gộp/owned columns | Không cần lifecycle/table riêng ở physical model; giảm join/CRUD | Không đổi Domain; tối ưu persistence | Query qua parent/store | Audit qua parent/store | Expand/merge plan | Owned original/adjustment columns trong ButToanSoGiaoDich |
| ChiDanDinhTuyen | RoutingInstruction | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| DieuKienDinhTuyen | RoutingCondition | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThanhToan | Payment | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LanThuThanhToan | PaymentAttempt | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| DatCoc | Deposit | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhanBoDatCoc | DepositAllocation | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| YeuCauHoanTien | RefundRequest | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BuocPheDuyetHoanTien | RefundApprovalStep | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LanThucHienHoanTien | RefundExecutionAttempt | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| CaThuNgan | CashierShift | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| TongHopPhuongThucThu | CashierTenderSummary | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ChenhLechCaThuNgan | CashierVariance | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| VanHanhBuongPhong | RoomOperation | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LichSuTrangThaiPhong | RoomStatusTransition | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| SaiLechTrangThaiPhong | RoomDiscrepancy | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| CongViecBuongPhong | HousekeepingTask | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhienLamBuong | CleaningWorkSegment | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| MucKiemTraVeSinh | CleaningChecklistItem | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| KiemTraPhong | RoomInspection | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhatHienKiemPhong | InspectionFinding | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| HoSoBaoTri | MaintenanceCase | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NhatKyCongViecBaoTri | MaintenanceWorkLog | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThamChieuVatTuBaoTri | MaintenancePartRef | Hoãn triển khai | Phụ thuộc policy/module ngoài chưa chốt | Không đổi Domain; tối ưu persistence | Query qua parent/store | Chưa lưu trước policy | Không tạo phase đầu | Giai đoạn tích hợp Inventory; Maintenance giữ external request/reference qua integration |
| KhoaVanHanhPhong | RoomOperationalBlock | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| GiaHanKhoaPhong | BlockExtension | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| YeuCauDichVu | ServiceRequest | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| HangMucYeuCauDichVu | ServiceRequestItem | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BangChungHoanThanhDichVu | ServiceCompletionEvidence | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| CuocGoiBaoThuc | WakeUpCall | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LanGoiBaoThuc | WakeUpAttempt | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| HoSoDoThatLac | LostFoundCase | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| VatPhamThatLac | LostFoundItem | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BanGiaoLuuGiu | CustodyHandover | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| XacMinhNguoiNhanDo | ClaimVerification | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LuuTruDoan | GroupStay | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LienKetThanhVienDoan | GroupMemberLink | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| KhoiPhongDoan | RoomBlock | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhanBoKhoiPhongTheoNgay | RoomBlockDay | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| DanhSachKhachDoan | RoomingList | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThanhVienDanhSachDoan | RoomingMember | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ChiDanThanhToanDoan | GroupBillingInstruction | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| DinhTuyenThanhToanDoan | BillingRouteLine | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NgayNghiepVuCoSo | PropertyBusinessDate | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhienKiemToanDem | NightAuditRun | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| DiemKiemSoatKiemToan | AuditCheckpoint | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| XacNhanKiemToan | AuditSignoff | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| DanhMucKiemToan | AuditChecklist | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| MucKiemToan | ChecklistItem | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NgoaiLeKiemToan | AuditException | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BangChungNgoaiLe | ExceptionEvidence | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| CongViecVanHanh | OperationalTask | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhanCongCongViec | TaskAssignment | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BangChungCongViec | TaskEvidence | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BanGiaoCa | ShiftHandover | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| HangMucBanGiao | HandoverItemSnapshot | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| CongViecTichHop | IntegrationJob | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| LanThucHienTichHop | IntegrationAttempt | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NgoaiLeTichHop | IntegrationException | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| XuLyNgoaiLeTichHop | ExceptionResolution | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThamChieuBenNgoai | ExternalReference | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| PhienBanAnhXaNgoai | ExternalMappingVersion | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| BanGhiChongTrung | IdempotencyRecord | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| KetQuaChongTrung | IdempotencyOutcome | Gộp/owned columns | Không cần lifecycle/table riêng ở physical model; giảm join/CRUD | Không đổi Domain; tối ưu persistence | Query qua parent/store | Audit qua parent/store | Expand/merge plan | Owned outcome columns trong BanGhiChongTrung |
| ThongDiepOutbox | OutboxMessage | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| ThongDiepInbox | InboxMessage | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| YeuCauPhatLai | ReplayRequest | Giữ làm bảng vật lý | Có identity/lifecycle, collection hoặc query/concurrency rõ | Không đổi Domain; tối ưu persistence | Query trực tiếp theo context | Audit qua parent/store | Expand/merge plan | — |
| NhatKyKiemToan | AuditRecord | Chuyển Audit Store | Audit dùng store chung, không owner Front Office riêng | Không đổi Domain; tối ưu persistence | Query qua parent/store | Audit tập trung | Expand/merge plan | platform_audit.NhatKyKiemToan |

### 3.1. Tổng hợp

| Chỉ tiêu | Số lượng |
|---|---:|
| Logic đầu vào | 100 |
| Giữ Front Office | 93 |
| Gộp/owned | 4 |
| Hoãn | 2 |
| Platform Audit | 1 |
| Lookup bổ sung | 14 |
| Projection bổ sung | 13 |
| Tổng vật lý | 121 |

Write model giảm 100 xuống 94 store (kể Audit Store); lookup/projection tăng có chủ đích vì lifecycle quản trị và workload đọc riêng.

## 4. SQL Schema Strategy

Dùng một chuẩn schema English lowercase có prefix fo:

| Schema | Mục đích |
|---|---|
| fo_reservation | Reservation/Waitlist |
| fo_stay | Stay/Assignment/Key |
| fo_rooms | Availability/Control |
| fo_folio | Folio ledger/routing |
| fo_cashiering | Payment/Deposit/Refund/Cashier |
| fo_housekeeping | Room operation/HK |
| fo_maintenance | Maintenance/block |
| fo_guestservice | Service/Wake-up/Lost & Found |
| fo_group | Group/Room block/Rooming |
| fo_nightaudit | BusinessDate/Audit run |
| fo_task | Task/Handover |
| fo_integration | Integration/Inbox/Outbox |
| fo_projection | Read models |
| platform_audit | Audit Store chung |

Schema English technical ngắn, dễ EF mapping/permission/migration và tách module; table/column vẫn tiếng Việt không dấu. Mỗi schema có writer/migration owner riêng. Schema không tự bảo đảm independent backup/restore.

## 5. Final Physical Table Catalog

### 5.1. Danh sách chính thức

| STT | Schema | Tên bảng | Domain | Aggregate | Loại | Giai đoạn | Ghi chú |
|---:|---|---|---|---|---|---|---|
| 1 | fo_reservation | DatPhong | Reservation | Reservation | Aggregate Root | GĐ1 | Physicalized từ logical catalog |
| 2 | fo_reservation | KhachDatPhong | ReservationGuest | DatPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 3 | fo_reservation | NhuCauHangPhong | ReservationRoomDemand | DatPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 4 | fo_reservation | AnhChupGiaDatPhong | ReservationRateSnapshot | DatPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 5 | fo_reservation | LienHeDatPhong | ReservationContact | DatPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 6 | fo_reservation | GhiChuDatPhong | ReservationNote | DatPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 7 | fo_reservation | YeuCauUuTienDatPhong | ReservationPreference | DatPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 8 | fo_reservation | LichSuThayDoiDatPhong | ReservationChangeRecord | DatPhong | History | GĐ1 | Physicalized từ logical catalog |
| 9 | fo_reservation | ChoPhong | WaitlistEntry | WaitlistEntry | Aggregate Root | GĐ1 | Physicalized từ logical catalog |
| 10 | fo_reservation | LanMoiChoPhong | WaitlistOfferAttempt | ChoPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 11 | fo_stay | LuuTru | Stay | Stay | Aggregate Root | GĐ2 | Physicalized từ logical catalog |
| 12 | fo_stay | KhachLuuTru | StayGuest | LuuTru | Child Entity | GĐ2 | Physicalized từ logical catalog |
| 13 | fo_stay | GiaHanLuuTru | StayExtension | LuuTru | History | GĐ2 | Physicalized từ logical catalog |
| 14 | fo_stay | HanCheLuuTru | StayRestriction | LuuTru | Child Entity | GĐ2 | Physicalized từ logical catalog |
| 15 | fo_stay | QuyenKyPhong | StayPostingPermission | LuuTru | Child Entity | GĐ2 | Physicalized từ logical catalog |
| 16 | fo_stay | ThietLapRiengTuLuuTru | StayPrivacySetting | LuuTru | Child Entity | GĐ2 | Physicalized từ logical catalog |
| 17 | fo_stay | PhanPhong | RoomAssignment | RoomAssignment | Aggregate Root | GĐ2 | Physicalized từ logical catalog |
| 18 | fo_stay | PhanDoanPhanPhong | AssignmentSegment | PhanPhong | Child Entity | GĐ2 | Physicalized từ logical catalog |
| 19 | fo_stay | QuyenMoKhoaPhong | KeyCredential | KeyCredential | Aggregate Root | GĐ2 | Physicalized từ logical catalog |
| 20 | fo_stay | LanCapKhoa | CredentialAttempt | QuyenMoKhoaPhong | Integration | GĐ2 | Physicalized từ logical catalog |
| 21 | fo_rooms | TonPhongKhaDung | RoomAvailability | RoomAvailability | Aggregate Root | GĐ1 | Physicalized từ logical catalog |
| 22 | fo_rooms | GiuTonPhong | AvailabilityHold | TonPhongKhaDung | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 23 | fo_rooms | PhanBoTonPhong | AvailabilityAllocation | TonPhongKhaDung | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 24 | fo_rooms | KiemSoatTonPhong | RoomInventoryControl | RoomInventoryControl | Aggregate Root | GĐ1 | Physicalized từ logical catalog |
| 25 | fo_rooms | HanCheBanPhong | InventoryRestriction | KiemSoatTonPhong | Child Entity | GĐ1 | Physicalized từ logical catalog |
| 26 | fo_folio | SoGiaoDichKhach | Folio | Folio | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 27 | fo_folio | NganSoGiaoDich | FolioWindow | FolioWindow | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 28 | fo_folio | ButToanSoGiaoDich | FolioEntry | NganSoGiaoDich | Ledger | GĐ3 | Physicalized từ logical catalog |
| 29 | fo_folio | ThueButToanSoGiaoDich | FolioEntryTax | ButToanSoGiaoDich | Ledger | GĐ3 | Physicalized từ logical catalog |
| 30 | fo_folio | ChiDanDinhTuyen | RoutingInstruction | RoutingInstruction | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 31 | fo_folio | DieuKienDinhTuyen | RoutingCondition | ChiDanDinhTuyen | Child Entity | GĐ3 | Physicalized từ logical catalog |
| 32 | fo_cashiering | ThanhToan | Payment | Payment | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 33 | fo_cashiering | LanThuThanhToan | PaymentAttempt | ThanhToan | Ledger | GĐ3 | Physicalized từ logical catalog |
| 34 | fo_cashiering | DatCoc | Deposit | Deposit | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 35 | fo_cashiering | PhanBoDatCoc | DepositAllocation | DatCoc | Ledger | GĐ3 | Physicalized từ logical catalog |
| 36 | fo_cashiering | YeuCauHoanTien | RefundRequest | RefundRequest | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 37 | fo_cashiering | BuocPheDuyetHoanTien | RefundApprovalStep | YeuCauHoanTien | History | GĐ3 | Physicalized từ logical catalog |
| 38 | fo_cashiering | LanThucHienHoanTien | RefundExecutionAttempt | YeuCauHoanTien | Ledger | GĐ3 | Physicalized từ logical catalog |
| 39 | fo_cashiering | CaThuNgan | CashierShift | CashierShift | Aggregate Root | GĐ3 | Physicalized từ logical catalog |
| 40 | fo_cashiering | TongHopPhuongThucThu | CashierTenderSummary | CaThuNgan | Child Entity | GĐ3 | Physicalized từ logical catalog |
| 41 | fo_cashiering | ChenhLechCaThuNgan | CashierVariance | CaThuNgan | History | GĐ3 | Physicalized từ logical catalog |
| 42 | fo_housekeeping | VanHanhBuongPhong | RoomOperation | RoomOperation | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 43 | fo_housekeeping | LichSuTrangThaiPhong | RoomStatusTransition | VanHanhBuongPhong | History | GĐ4 | Physicalized từ logical catalog |
| 44 | fo_housekeeping | SaiLechTrangThaiPhong | RoomDiscrepancy | VanHanhBuongPhong | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 45 | fo_housekeeping | CongViecBuongPhong | HousekeepingTask | HousekeepingTask | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 46 | fo_housekeeping | PhienLamBuong | CleaningWorkSegment | CongViecBuongPhong | History | GĐ4 | Physicalized từ logical catalog |
| 47 | fo_housekeeping | MucKiemTraVeSinh | CleaningChecklistItem | CongViecBuongPhong | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 48 | fo_housekeeping | KiemTraPhong | RoomInspection | RoomInspection | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 49 | fo_housekeeping | PhatHienKiemPhong | InspectionFinding | KiemTraPhong | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 50 | fo_maintenance | HoSoBaoTri | MaintenanceCase | MaintenanceCase | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 51 | fo_maintenance | NhatKyCongViecBaoTri | MaintenanceWorkLog | HoSoBaoTri | History | GĐ4 | Physicalized từ logical catalog |
| 52 | fo_maintenance | KhoaVanHanhPhong | RoomOperationalBlock | RoomOperationalBlock | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 53 | fo_maintenance | GiaHanKhoaPhong | BlockExtension | KhoaVanHanhPhong | History | GĐ4 | Physicalized từ logical catalog |
| 54 | fo_guestservice | YeuCauDichVu | ServiceRequest | ServiceRequest | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 55 | fo_guestservice | HangMucYeuCauDichVu | ServiceRequestItem | YeuCauDichVu | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 56 | fo_guestservice | BangChungHoanThanhDichVu | ServiceCompletionEvidence | YeuCauDichVu | History | GĐ4 | Physicalized từ logical catalog |
| 57 | fo_guestservice | CuocGoiBaoThuc | WakeUpCall | WakeUpCall | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 58 | fo_guestservice | LanGoiBaoThuc | WakeUpAttempt | CuocGoiBaoThuc | History | GĐ4 | Physicalized từ logical catalog |
| 59 | fo_guestservice | HoSoDoThatLac | LostFoundCase | LostFoundCase | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 60 | fo_guestservice | VatPhamThatLac | LostFoundItem | HoSoDoThatLac | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 61 | fo_guestservice | BanGiaoLuuGiu | CustodyHandover | HoSoDoThatLac | History | GĐ4 | Physicalized từ logical catalog |
| 62 | fo_guestservice | XacMinhNguoiNhanDo | ClaimVerification | HoSoDoThatLac | History | GĐ4 | Physicalized từ logical catalog |
| 63 | fo_group | LuuTruDoan | GroupStay | GroupStay | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 64 | fo_group | LienKetThanhVienDoan | GroupMemberLink | LuuTruDoan | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 65 | fo_group | KhoiPhongDoan | RoomBlock | RoomBlock | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 66 | fo_group | PhanBoKhoiPhongTheoNgay | RoomBlockDay | KhoiPhongDoan | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 67 | fo_group | DanhSachKhachDoan | RoomingList | RoomingList | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 68 | fo_group | ThanhVienDanhSachDoan | RoomingMember | DanhSachKhachDoan | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 69 | fo_group | ChiDanThanhToanDoan | GroupBillingInstruction | GroupBillingInstruction | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 70 | fo_group | DinhTuyenThanhToanDoan | BillingRouteLine | ChiDanThanhToanDoan | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 71 | fo_nightaudit | NgayNghiepVuCoSo | PropertyBusinessDate | PropertyBusinessDate | Aggregate Root | GĐ5 | Physicalized từ logical catalog |
| 72 | fo_nightaudit | PhienKiemToanDem | NightAuditRun | NightAuditRun | Aggregate Root | GĐ5 | Physicalized từ logical catalog |
| 73 | fo_nightaudit | DiemKiemSoatKiemToan | AuditCheckpoint | PhienKiemToanDem | History | GĐ5 | Physicalized từ logical catalog |
| 74 | fo_nightaudit | XacNhanKiemToan | AuditSignoff | PhienKiemToanDem | History | GĐ5 | Physicalized từ logical catalog |
| 75 | fo_nightaudit | DanhMucKiemToan | AuditChecklist | AuditChecklist | Aggregate Root | GĐ5 | Physicalized từ logical catalog |
| 76 | fo_nightaudit | MucKiemToan | ChecklistItem | DanhMucKiemToan | Child Entity | GĐ5 | Physicalized từ logical catalog |
| 77 | fo_nightaudit | NgoaiLeKiemToan | AuditException | AuditException | Aggregate Root | GĐ5 | Physicalized từ logical catalog |
| 78 | fo_nightaudit | BangChungNgoaiLe | ExceptionEvidence | NgoaiLeKiemToan | History | GĐ5 | Physicalized từ logical catalog |
| 79 | fo_task | CongViecVanHanh | OperationalTask | OperationalTask | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 80 | fo_task | PhanCongCongViec | TaskAssignment | CongViecVanHanh | History | GĐ4 | Physicalized từ logical catalog |
| 81 | fo_task | BangChungCongViec | TaskEvidence | CongViecVanHanh | History | GĐ4 | Physicalized từ logical catalog |
| 82 | fo_task | BanGiaoCa | ShiftHandover | ShiftHandover | Aggregate Root | GĐ4 | Physicalized từ logical catalog |
| 83 | fo_task | HangMucBanGiao | HandoverItemSnapshot | BanGiaoCa | Child Entity | GĐ4 | Physicalized từ logical catalog |
| 84 | fo_integration | CongViecTichHop | IntegrationJob | IntegrationJob | Integration | Nền tảng | Physicalized từ logical catalog |
| 85 | fo_integration | LanThucHienTichHop | IntegrationAttempt | CongViecTichHop | Integration | Nền tảng | Physicalized từ logical catalog |
| 86 | fo_integration | NgoaiLeTichHop | IntegrationException | IntegrationException | Integration | Nền tảng | Physicalized từ logical catalog |
| 87 | fo_integration | XuLyNgoaiLeTichHop | ExceptionResolution | NgoaiLeTichHop | Integration | Nền tảng | Physicalized từ logical catalog |
| 88 | fo_integration | ThamChieuBenNgoai | ExternalReference | ExternalReference | Integration | Nền tảng | Physicalized từ logical catalog |
| 89 | fo_integration | PhienBanAnhXaNgoai | ExternalMappingVersion | ThamChieuBenNgoai | Integration | Nền tảng | Physicalized từ logical catalog |
| 90 | fo_integration | BanGhiChongTrung | IdempotencyRecord | IdempotencyRecord | Integration | Nền tảng | Physicalized từ logical catalog |
| 91 | fo_integration | ThongDiepOutbox | OutboxMessage | OutboxMessage | Outbox | Nền tảng | Physicalized từ logical catalog |
| 92 | fo_integration | ThongDiepInbox | InboxMessage | InboxMessage | Inbox | Nền tảng | Physicalized từ logical catalog |
| 93 | fo_integration | YeuCauPhatLai | ReplayRequest | NgoaiLeTichHop | Integration | Nền tảng | Physicalized từ logical catalog |
| 94 | platform_audit | NhatKyKiemToan | AuditRecord | AuditRecord | Audit | Nền tảng | Store chung, append-only |
| 95 | fo_reservation | NguonDatPhong | ReservationSource | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 96 | fo_reservation | KenhDatPhong | ReservationChannel | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 97 | fo_reservation | LyDoHuyDatPhong | CancellationReason | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 98 | fo_cashiering | PhuongThucThanhToan | PaymentMethod | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 99 | fo_folio | KhoanThu | ChargeCode | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 100 | fo_housekeeping | LoaiVeSinh | CleaningType | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 101 | fo_housekeeping | MucUuTienBuongPhong | HousekeepingPriority | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 102 | fo_maintenance | NhomBaoTri | MaintenanceCategory | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 103 | fo_stay | LyDoChuyenPhong | RoomMoveReason | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 104 | fo_stay | LyDoGhiDe | OverrideReason | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 105 | fo_task | LoaiCongViec | TaskType | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 106 | fo_integration | LoaiNgoaiLeTichHop | IntegrationExceptionType | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 107 | fo_nightaudit | LoaiChanKiemToanDem | NightAuditBlockerType | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 108 | fo_guestservice | LoaiDichVuKhach | ServiceType | Lookup | Lookup | Nền tảng/GĐ liên quan | Typed lookup; không generic catalog |
| 109 | fo_projection | SoDoPhong | RoomRack | Projection | Projection | Theo feature | Rebuildable; không command source |
| 110 | fo_projection | KeHoachPhong | RoomPlan | Projection | Projection | Theo feature | Rebuildable; không command source |
| 111 | fo_projection | DongThoiGianLuuTru | StayTimeline | Projection | Projection | Theo feature | Rebuildable; không command source |
| 112 | fo_projection | DanhSachKhachDen | ArrivalList | Projection | Projection | Theo feature | Rebuildable; không command source |
| 113 | fo_projection | DanhSachKhachDi | DepartureList | Projection | Projection | Theo feature | Rebuildable; không command source |
| 114 | fo_projection | DanhSachKhachDangLuuTru | InHouseList | Projection | Projection | Theo feature | Rebuildable; không command source |
| 115 | fo_projection | BangDieuHanhTienSanh | FrontOfficeDashboard | Projection | Projection | Theo feature | Rebuildable; không command source |
| 116 | fo_projection | BangDieuHanhBuongPhong | HousekeepingBoard | Projection | Projection | Theo feature | Rebuildable; không command source |
| 117 | fo_projection | BangTheoDoiSoDuKhach | GuestBalanceBoard | Projection | Projection | Theo feature | Rebuildable; không command source |
| 118 | fo_projection | DanhSachCanSanSangTraPhong | CheckoutReadinessList | Projection | Projection | Theo feature | Rebuildable; không command source |
| 119 | fo_projection | BangDoiSoatThuNgan | CashierReconciliationBoard | Projection | Projection | Theo feature | Rebuildable; không command source |
| 120 | fo_projection | BangNgoaiLeTichHop | IntegrationExceptionBoard | Projection | Projection | Theo feature | Rebuildable; không command source |
| 121 | fo_projection | BangKiemToanDem | NightAuditBoard | Projection | Projection | Theo feature | Rebuildable; không command source |

**Tổng: 121 bảng vật lý.**

### 5.2. Data Dictionary vật lý cuối

#### 5.2.1. fo_reservation.DatPhong — Reservation

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDatPhong | ReservationId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoXacNhan | ConfirmationNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | NgayDen | ArrivalDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | NgàyDi sau NgayDen |
| 6 | NgayDi | DepartureDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | NgàyDi sau NgayDen |
| 7 | MaHoSoKhachHang | GuestProfileId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Field-level; masking | Theo role | Không | External reference |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | LoaiBaoDam | GuaranteeType | varchar(30) | 30 | Có | — | Không | Không | Không | Có | Không | Không | Owned Guarantee; snapshot/ref |
| 15 | MaNguonBaoDam | GuaranteeSourceId | uniqueidentifier | — | Có | — | Không | Logical | Không | Có | Không | Không | Payment/Deposit/Credit ref |

#### 5.2.2. fo_reservation.KhachDatPhong — ReservationGuest

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhachDatPhong | ReservationGuestId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.3. fo_reservation.NhuCauHangPhong — ReservationRoomDemand

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhuCauHangPhong | ReservationRoomDemandId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.4. fo_reservation.AnhChupGiaDatPhong — ReservationRateSnapshot

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaAnhChupGiaDatPhong | ReservationRateSnapshotId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.5. fo_reservation.LienHeDatPhong — ReservationContact

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLienHeDatPhong | ReservationContactId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.6. fo_reservation.GhiChuDatPhong — ReservationNote

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGhiChuDatPhong | ReservationNoteId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.7. fo_reservation.YeuCauUuTienDatPhong — ReservationPreference

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauUuTienDatPhong | ReservationPreferenceId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DatPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.8. fo_reservation.LichSuThayDoiDatPhong — ReservationChangeRecord

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLichSuThayDoiDatPhong | ReservationChangeRecordId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatPhong | ParentId | uniqueidentifier | — | Không | — | Không | DatPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.9. fo_reservation.ChoPhong — WaitlistEntry

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChoPhong | WaitlistEntryId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.10. fo_reservation.LanMoiChoPhong — WaitlistOfferAttempt

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanMoiChoPhong | WaitlistOfferAttemptId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaChoPhong | ParentId | uniqueidentifier | — | Không | — | Không | ChoPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | ChoPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.11. fo_stay.LuuTru — Stay

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLuuTru | StayId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoLuuTru | StayNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaDatPhong | ReservationId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical reference |
| 6 | NgayDenDuKien | PlannedArrivalDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Business calendar |
| 7 | NgayDiDuKien | PlannedDepartureDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Sau ngày đến |
| 8 | ThoiDiemNhanPhongUtc | ActualArrivalUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC set once |
| 9 | ThoiDiemTraPhongUtc | ActualDepartureUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC set once |
| 10 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 12 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 13 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 14 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 15 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.12. fo_stay.KhachLuuTru — StayGuest

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhachLuuTru | StayGuestId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.13. fo_stay.GiaHanLuuTru — StayExtension

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGiaHanLuuTru | StayExtensionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.14. fo_stay.HanCheLuuTru — StayRestriction

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHanCheLuuTru | StayRestrictionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.15. fo_stay.QuyenKyPhong — StayPostingPermission

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaQuyenKyPhong | StayPostingPermissionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.16. fo_stay.ThietLapRiengTuLuuTru — StayPrivacySetting

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThietLapRiengTuLuuTru | StayPrivacySettingId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTru | ParentId | uniqueidentifier | — | Không | — | Không | LuuTru | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | LuuTru | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.17. fo_stay.PhanPhong — RoomAssignment

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanPhong | RoomAssignmentId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLuuTru | StayId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 5 | MaPhong | RoomId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | External ref |
| 6 | ThoiDiemBatDauUtc | StartUtc | datetime2(7) | 7 | Không | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 7 | ThoiDiemKetThucUtc | EndUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | No overlap |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.18. fo_stay.PhanDoanPhanPhong — AssignmentSegment

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanDoanPhanPhong | AssignmentSegmentId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaPhanPhong | ParentId | uniqueidentifier | — | Không | — | Không | PhanPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | PhanPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.19. fo_stay.QuyenMoKhoaPhong — KeyCredential

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaQuyenMoKhoaPhong | KeyCredentialId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.20. fo_stay.LanCapKhoa — CredentialAttempt

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanCapKhoa | CredentialAttemptId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaQuyenMoKhoaPhong | ParentId | uniqueidentifier | — | Không | — | Không | QuyenMoKhoaPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.21. fo_rooms.TonPhongKhaDung — RoomAvailability

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaTonPhongKhaDung | RoomAvailabilityId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaHangPhong | RoomTypeId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | External ref |
| 5 | NgayTonPhong | InventoryDate | date | — | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property+Type+Date |
| 6 | SoLuongVatLy | PhysicalQuantity | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Không | Không âm |
| 7 | SoLuongGiu | HeldQuantity | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Không | Không âm |
| 8 | SoLuongPhanBo | AllocatedQuantity | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Không | Không âm |
| 9 | SoLuongBiKhoa | BlockedQuantity | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Không | Không âm |
| 10 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 12 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 13 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 14 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 15 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.22. fo_rooms.GiuTonPhong — AvailabilityHold

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGiuTonPhong | AvailabilityHoldId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaTonPhongKhaDung | ParentId | uniqueidentifier | — | Không | — | Không | TonPhongKhaDung | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | TonPhongKhaDung | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.23. fo_rooms.PhanBoTonPhong — AvailabilityAllocation

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanBoTonPhong | AvailabilityAllocationId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaTonPhongKhaDung | ParentId | uniqueidentifier | — | Không | — | Không | TonPhongKhaDung | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | TonPhongKhaDung | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.24. fo_rooms.KiemSoatTonPhong — RoomInventoryControl

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKiemSoatTonPhong | RoomInventoryControlId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.25. fo_rooms.HanCheBanPhong — InventoryRestriction

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHanCheBanPhong | InventoryRestrictionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKiemSoatTonPhong | ParentId | uniqueidentifier | — | Không | — | Không | KiemSoatTonPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | KiemSoatTonPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.26. fo_folio.SoGiaoDichKhach — Folio

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaSoGiaoDichKhach | FolioId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoFolio | FolioNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaLuuTru | StayId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Cross-context ref |
| 6 | SoTienNo | DebitTotal | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Money |
| 7 | SoTienCo | CreditTotal | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Money |
| 8 | MaTienTe | CurrencyCode | char(3) | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | ISO |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.27. fo_folio.NganSoGiaoDich — FolioWindow

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNganSoGiaoDich | FolioWindowId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaSoGiaoDichKhach | FolioId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Same context |
| 5 | SoNgan | WindowNumber | smallint | — | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Folio |
| 6 | MaNguoiThanhToan | PayerId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Snapshot/ref |
| 7 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.28. fo_folio.ButToanSoGiaoDich — FolioEntry

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaButToanSoGiaoDich | FolioEntryId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNganSoGiaoDich | ParentId | uniqueidentifier | — | Không | — | Không | NganSoGiaoDich | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaNganSoGiaoDich | FolioWindowId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Physical FK |
| 5 | SoThuTuButToan | EntrySequence | bigint | — | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Window |
| 6 | LoaiButToan | EntryType | varchar(30) | 30 | Không | — | Không | — | Không | Không | Không | Không | Không | Enum |
| 7 | SoTien | Amount | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Money |
| 8 | MaTienTe | CurrencyCode | char(3) | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | ISO |
| 9 | MaNguonNgoai | ExternalSourceReference | nvarchar(120) | 120 | Có | — | Không | Theo owner | Có | Có | Không | Không | Không | Unique scoped |
| 10 | MaButToanGoc | OriginalEntryId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Reversal chain |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | MaLienKetGiaoDich | TransactionLinkId | uniqueidentifier | — | Có | — | Không | Self logical | Không | Có | Không | Có | Transfer/adjustment correlation |
| 14 | LoaiLienKet | LinkType | varchar(20) | 20 | Có | — | Không | — | Không | Có | Không | Có | Transfer/Reversal/Adjustment |

#### 5.2.29. fo_folio.ThueButToanSoGiaoDich — FolioEntryTax

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThueButToanSoGiaoDich | FolioEntryTaxId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaButToanSoGiaoDich | ParentId | uniqueidentifier | — | Không | — | Không | ButToanSoGiaoDich | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.30. fo_folio.ChiDanDinhTuyen — RoutingInstruction

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChiDanDinhTuyen | RoutingInstructionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.31. fo_folio.DieuKienDinhTuyen — RoutingCondition

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDieuKienDinhTuyen | RoutingConditionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaChiDanDinhTuyen | ParentId | uniqueidentifier | — | Không | — | Không | ChiDanDinhTuyen | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | ChiDanDinhTuyen | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.32. fo_cashiering.ThanhToan — Payment

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThanhToan | PaymentId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoThanhToan | PaymentNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | SoTien | Amount | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Money |
| 6 | MaTienTe | CurrencyCode | char(3) | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | ISO |
| 7 | MaPhuongThucThanhToan | PaymentMethodCode | varchar(30) | 30 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Lookup |
| 8 | ThamChieuNhaCungCap | ProviderReference | nvarchar(120) | 120 | Có | — | Không | — | Có | Không | Không | Không | Không | Unique provider scope |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.33. fo_cashiering.LanThuThanhToan — PaymentAttempt

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanThuThanhToan | PaymentAttemptId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThanhToan | ParentId | uniqueidentifier | — | Không | — | Không | ThanhToan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.34. fo_cashiering.DatCoc — Deposit

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDatCoc | DepositId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoDatCoc | DepositNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | SoTienDaNhan | ReceivedAmount | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Money |
| 6 | SoTienDaPhanBo | AllocatedAmount | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Không vượt received |
| 7 | MaTienTe | CurrencyCode | char(3) | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | ISO |
| 8 | MaDatPhong | ReservationId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.35. fo_cashiering.PhanBoDatCoc — DepositAllocation

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanBoDatCoc | DepositAllocationId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDatCoc | ParentId | uniqueidentifier | — | Không | — | Không | DatCoc | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.36. fo_cashiering.YeuCauHoanTien — RefundRequest

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauHoanTien | RefundRequestId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoYeuCauHoanTien | RefundNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaGiaoDichNguon | SourceTransactionId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 6 | SoTienYeuCau | RequestedAmount | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Không vượt refundable |
| 7 | MaTienTe | CurrencyCode | char(3) | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | ISO |
| 8 | LyDo | ReasonCode | varchar(40) | 40 | Không | — | Không | — | Không | Không | Không | Không | Không | Lookup |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.37. fo_cashiering.BuocPheDuyetHoanTien — RefundApprovalStep

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBuocPheDuyetHoanTien | RefundApprovalStepId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauHoanTien | ParentId | uniqueidentifier | — | Không | — | Không | YeuCauHoanTien | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.38. fo_cashiering.LanThucHienHoanTien — RefundExecutionAttempt

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanThucHienHoanTien | RefundExecutionAttemptId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauHoanTien | ParentId | uniqueidentifier | — | Không | — | Không | YeuCauHoanTien | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.39. fo_cashiering.CaThuNgan — CashierShift

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCaThuNgan | CashierShiftId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoCaThuNgan | ShiftNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property+Date |
| 5 | MaThuNgan | CashierId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | User ref |
| 6 | MaMayTram | WorkstationId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | System ref |
| 7 | SoTienQuyDauCa | OpeningFloat | decimal(19,4) | 19,4 | Không | — | Không | — | Không | Có | Không | Không | Không | Money |
| 8 | MaTienTe | CurrencyCode | char(3) | 3 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | ISO |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.40. fo_cashiering.TongHopPhuongThucThu — CashierTenderSummary

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaTongHopPhuongThucThu | CashierTenderSummaryId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCaThuNgan | ParentId | uniqueidentifier | — | Không | — | Không | CaThuNgan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | CaThuNgan | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.41. fo_cashiering.ChenhLechCaThuNgan — CashierVariance

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChenhLechCaThuNgan | CashierVarianceId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCaThuNgan | ParentId | uniqueidentifier | — | Không | — | Không | CaThuNgan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.42. fo_housekeeping.VanHanhBuongPhong — RoomOperation

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaVanHanhBuongPhong | RoomOperationId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaPhong | RoomId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | External unique Property |
| 5 | TrangThaiVanHanh | OperationalStatus | varchar(30) | 30 | Không | — | Không | — | Không | Có | Không | Không | Không | Enum |
| 6 | CoKhongLamPhien | IsDnd | bit | — | Không | — | Không | — | Không | Không | Không | Không | Không | Default 0 |
| 7 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.43. fo_housekeeping.LichSuTrangThaiPhong — RoomStatusTransition

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLichSuTrangThaiPhong | RoomStatusTransitionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaVanHanhBuongPhong | ParentId | uniqueidentifier | — | Không | — | Không | VanHanhBuongPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.44. fo_housekeeping.SaiLechTrangThaiPhong — RoomDiscrepancy

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaSaiLechTrangThaiPhong | RoomDiscrepancyId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaVanHanhBuongPhong | ParentId | uniqueidentifier | — | Không | — | Không | VanHanhBuongPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | VanHanhBuongPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.45. fo_housekeeping.CongViecBuongPhong — HousekeepingTask

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCongViecBuongPhong | HousekeepingTaskId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaPhong | RoomId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | External ref |
| 5 | MaNguoiDuocGiao | AssigneeId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | User ref |
| 6 | MucUuTien | PriorityCode | varchar(20) | 20 | Không | — | Không | — | Không | Không | Không | Không | Không | Lookup |
| 7 | ThoiDiemDenHanUtc | DueAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.46. fo_housekeeping.PhienLamBuong — CleaningWorkSegment

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhienLamBuong | CleaningWorkSegmentId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecBuongPhong | ParentId | uniqueidentifier | — | Không | — | Không | CongViecBuongPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.47. fo_housekeeping.MucKiemTraVeSinh — CleaningChecklistItem

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaMucKiemTraVeSinh | CleaningChecklistItemId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecBuongPhong | ParentId | uniqueidentifier | — | Không | — | Không | CongViecBuongPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | CongViecBuongPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.48. fo_housekeeping.KiemTraPhong — RoomInspection

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKiemTraPhong | RoomInspectionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.49. fo_housekeeping.PhatHienKiemPhong — InspectionFinding

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhatHienKiemPhong | InspectionFindingId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKiemTraPhong | ParentId | uniqueidentifier | — | Không | — | Không | KiemTraPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | KiemTraPhong | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.50. fo_maintenance.HoSoBaoTri — MaintenanceCase

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHoSoBaoTri | MaintenanceCaseId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoHoSoBaoTri | CaseNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaPhong | RoomId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | External ref |
| 6 | MucDo | SeverityCode | varchar(20) | 20 | Không | — | Không | — | Không | Không | Không | Không | Không | Lookup |
| 7 | MoTa | Description | nvarchar(1000) | 1000 | Không | — | Không | — | Không | Không | Không | Không | Không | Không max |
| 8 | ThoiDiemDenHanUtc | DueAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.51. fo_maintenance.NhatKyCongViecBaoTri — MaintenanceWorkLog

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhatKyCongViecBaoTri | MaintenanceWorkLogId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoBaoTri | ParentId | uniqueidentifier | — | Không | — | Không | HoSoBaoTri | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.52. fo_maintenance.KhoaVanHanhPhong — RoomOperationalBlock

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhoaVanHanhPhong | RoomOperationalBlockId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoKhoaPhong | BlockNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaPhong | RoomId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | External ref |
| 6 | LoaiKhoa | BlockType | varchar(10) | 10 | Không | — | Không | — | Không | Không | Không | Không | Không | Enum |
| 7 | ThoiDiemHieuLucTuUtc | EffectiveFromUtc | datetime2(7) | 7 | Không | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 8 | ThoiDiemHieuLucDenUtc | EffectiveToUtc | datetime2(7) | 7 | Không | — | Không | — | Không | Có | Không | Không | Không | Sau from |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.53. fo_maintenance.GiaHanKhoaPhong — BlockExtension

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaGiaHanKhoaPhong | BlockExtensionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKhoaVanHanhPhong | ParentId | uniqueidentifier | — | Không | — | Không | KhoaVanHanhPhong | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.54. fo_guestservice.YeuCauDichVu — ServiceRequest

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauDichVu | ServiceRequestId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoYeuCauDichVu | RequestNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaLuuTru | StayId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 6 | MaLoaiDichVu | ServiceTypeCode | varchar(30) | 30 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Lookup |
| 7 | ThoiDiemDenHanUtc | DueAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 8 | NoiDung | Description | nvarchar(1000) | 1000 | Có | — | Không | — | Không | Không | Field-level; masking | Theo role | Không | Không secret |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.55. fo_guestservice.HangMucYeuCauDichVu — ServiceRequestItem

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHangMucYeuCauDichVu | ServiceRequestItemId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauDichVu | ParentId | uniqueidentifier | — | Không | — | Không | YeuCauDichVu | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | YeuCauDichVu | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.56. fo_guestservice.BangChungHoanThanhDichVu — ServiceCompletionEvidence

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangChungHoanThanhDichVu | ServiceCompletionEvidenceId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaYeuCauDichVu | ParentId | uniqueidentifier | — | Không | — | Không | YeuCauDichVu | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.57. fo_guestservice.CuocGoiBaoThuc — WakeUpCall

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCuocGoiBaoThuc | WakeUpCallId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoYeuCauBaoThuc | WakeUpNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaLuuTru | StayId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 6 | ThoiDiemGoiUtc | ScheduledAtUtc | datetime2(7) | 7 | Không | — | Không | — | Không | Có | Không | Không | Không | UTC+timezone snapshot |
| 7 | SoLanThuToiDa | MaximumAttempts | tinyint | — | Không | — | Không | — | Không | Có | Không | Không | Không | Lớn hơn 0 |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.58. fo_guestservice.LanGoiBaoThuc — WakeUpAttempt

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanGoiBaoThuc | WakeUpAttemptId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCuocGoiBaoThuc | ParentId | uniqueidentifier | — | Không | — | Không | CuocGoiBaoThuc | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.59. fo_guestservice.HoSoDoThatLac — LostFoundCase

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHoSoDoThatLac | LostFoundCaseId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoHoSoThatLac | CaseNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | LoaiHoSo | CaseType | varchar(10) | 10 | Không | — | Không | — | Không | Có | Không | Không | Không | Enum |
| 6 | ThoiDiemPhatHienUtc | FoundAtUtc | datetime2(7) | 7 | Không | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 7 | ViTriPhatHien | FoundLocation | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Không PII quá mức |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.60. fo_guestservice.VatPhamThatLac — LostFoundItem

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaVatPhamThatLac | LostFoundItemId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoDoThatLac | ParentId | uniqueidentifier | — | Không | — | Không | HoSoDoThatLac | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | HoSoDoThatLac | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.61. fo_guestservice.BanGiaoLuuGiu — CustodyHandover

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBanGiaoLuuGiu | CustodyHandoverId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoDoThatLac | ParentId | uniqueidentifier | — | Không | — | Không | HoSoDoThatLac | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.62. fo_guestservice.XacMinhNguoiNhanDo — ClaimVerification

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaXacMinhNguoiNhanDo | ClaimVerificationId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaHoSoDoThatLac | ParentId | uniqueidentifier | — | Không | — | Không | HoSoDoThatLac | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.63. fo_group.LuuTruDoan — GroupStay

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLuuTruDoan | GroupStayId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoDoan | GroupNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | TenDoan | GroupName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | PII thấp |
| 6 | NgayDen | ArrivalDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Before departure |
| 7 | NgayDi | DepartureDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | After arrival |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.64. fo_group.LienKetThanhVienDoan — GroupMemberLink

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLienKetThanhVienDoan | GroupMemberLinkId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaLuuTruDoan | ParentId | uniqueidentifier | — | Không | — | Không | LuuTruDoan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | LuuTruDoan | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.65. fo_group.KhoiPhongDoan — RoomBlock

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhoiPhongDoan | RoomBlockId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoKhoiPhong | BlockNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaLuuTruDoan | GroupStayId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Date |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | After from |
| 8 | NgayChotGiuPhong | CutOffDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Policy |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.66. fo_group.PhanBoKhoiPhongTheoNgay — RoomBlockDay

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanBoKhoiPhongTheoNgay | RoomBlockDayId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaKhoiPhongDoan | ParentId | uniqueidentifier | — | Không | — | Không | KhoiPhongDoan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | KhoiPhongDoan | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.67. fo_group.DanhSachKhachDoan — RoomingList

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhSachKhachDoan | RoomingListId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoPhienDanhSach | ListVersion | int | — | Không | — | Không | — | Không | Có | Không | Không | Không | Lớn hơn 0 |
| 5 | MaLuuTruDoan | GroupStayId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 6 | MaVanTayTep | FileFingerprint | varbinary(32) | 32 | Có | — | Không | Theo owner | Có | Có | Không | Không | Không | Unique group/version |
| 7 | TongSoDong | TotalRows | int | — | Không | — | Không | — | Không | Có | Không | Không | Không | Không âm |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.68. fo_group.ThanhVienDanhSachDoan — RoomingMember

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThanhVienDanhSachDoan | RoomingMemberId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDanhSachKhachDoan | ParentId | uniqueidentifier | — | Không | — | Không | DanhSachKhachDoan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DanhSachKhachDoan | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.69. fo_group.ChiDanThanhToanDoan — GroupBillingInstruction

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaChiDanThanhToanDoan | GroupBillingInstructionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.70. fo_group.DinhTuyenThanhToanDoan — BillingRouteLine

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDinhTuyenThanhToanDoan | BillingRouteLineId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaChiDanThanhToanDoan | ParentId | uniqueidentifier | — | Không | — | Không | ChiDanThanhToanDoan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | ChiDanThanhToanDoan | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.71. fo_nightaudit.NgayNghiepVuCoSo — PropertyBusinessDate

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNgayNghiepVuCoSo | PropertyBusinessDateId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | NgayNghiepVuHienTai | CurrentBusinessDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | One active Property |
| 5 | MuiGio | TimezoneId | varchar(64) | 64 | Không | — | Không | — | Không | Không | Không | Không | Không | IANA/Windows mapping |
| 6 | MaPhienKiemToanDangChay | ActiveNightAuditRunId | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Logical ref |
| 7 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.72. fo_nightaudit.PhienKiemToanDem — NightAuditRun

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhienKiemToanDem | NightAuditRunId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoPhienKiemToan | RunNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property+Date |
| 5 | NgayDongSo | ClosingBusinessDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Business Date |
| 6 | BuocHienTai | CurrentStep | varchar(40) | 40 | Có | — | Không | — | Không | Không | Không | Không | Không | Checkpoint |
| 7 | MaLease | LeaseToken | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Exclusive |
| 8 | ThoiDiemHetLeaseUtc | LeaseExpiresAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.73. fo_nightaudit.DiemKiemSoatKiemToan — AuditCheckpoint

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDiemKiemSoatKiemToan | AuditCheckpointId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaPhienKiemToanDem | ParentId | uniqueidentifier | — | Không | — | Không | PhienKiemToanDem | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.74. fo_nightaudit.XacNhanKiemToan — AuditSignoff

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaXacNhanKiemToan | AuditSignoffId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaPhienKiemToanDem | ParentId | uniqueidentifier | — | Không | — | Không | PhienKiemToanDem | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.75. fo_nightaudit.DanhMucKiemToan — AuditChecklist

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhMucKiemToan | AuditChecklistId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.76. fo_nightaudit.MucKiemToan — ChecklistItem

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaMucKiemToan | ChecklistItemId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDanhMucKiemToan | ParentId | uniqueidentifier | — | Không | — | Không | DanhMucKiemToan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | DanhMucKiemToan | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 9 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 10 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 11 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 12 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.77. fo_nightaudit.NgoaiLeKiemToan — AuditException

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNgoaiLeKiemToan | AuditExceptionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.78. fo_nightaudit.BangChungNgoaiLe — ExceptionEvidence

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangChungNgoaiLe | ExceptionEvidenceId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNgoaiLeKiemToan | ParentId | uniqueidentifier | — | Không | — | Không | NgoaiLeKiemToan | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.79. fo_task.CongViecVanHanh — OperationalTask

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCongViecVanHanh | OperationalTaskId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | SoCongViec | TaskNumber | nvarchar(30) | 30 | Không | — | Không | — | Có | Có | Không | Không | Không | Unique Property |
| 5 | MaLoaiCongViec | TaskTypeCode | varchar(30) | 30 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Lookup |
| 6 | MaNguon | SourceId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Cross-context |
| 7 | ThoiDiemDenHanUtc | DueAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 8 | MucUuTien | PriorityCode | varchar(20) | 20 | Không | — | Không | — | Không | Không | Không | Không | Không | Lookup |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.80. fo_task.PhanCongCongViec — TaskAssignment

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhanCongCongViec | TaskAssignmentId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecVanHanh | ParentId | uniqueidentifier | — | Không | — | Không | CongViecVanHanh | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.81. fo_task.BangChungCongViec — TaskEvidence

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangChungCongViec | TaskEvidenceId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecVanHanh | ParentId | uniqueidentifier | — | Không | — | Không | CongViecVanHanh | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.82. fo_task.BanGiaoCa — ShiftHandover

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBanGiaoCa | ShiftHandoverId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Không | PropertyBusinessDate | Không | Logical ref | Không | Có | Không | Không | Không | Không suy từ UTC |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | Root | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | TrangThai | Status | varchar(30) | 30 | Không | Trạng thái khởi tạo | Không | — | Không | Có | Không | Không | Không | Enum; state machine |
| 9 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 10 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 11 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 12 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 13 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.83. fo_task.HangMucBanGiao — HandoverItemSnapshot

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaHangMucBanGiao | HandoverItemSnapshotId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaBanGiaoCa | ParentId | uniqueidentifier | — | Không | — | Không | BanGiaoCa | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | MaLoai | TypeCode | varchar(40) | 40 | Không | — | Không | Lookup/enum | Không | Có | Không | Không | Không | Governed |
| 5 | ThuTu | Order | smallint | — | Không | 0 | Không | BanGiaoCa | Không | Có | Không | Không | Không | Không âm |
| 6 | NgayHieuLucTu | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Theo lifecycle |
| 7 | NgayHieuLucDen | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không trước from |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.84. fo_integration.CongViecTichHop — IntegrationJob

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaCongViecTichHop | IntegrationJobId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaDiemCuoi | EndpointId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Config ref |
| 4 | LoaiThaoTac | OperationType | varchar(40) | 40 | Không | — | Không | — | Không | Không | Không | Không | Không | Governed |
| 5 | KhoaChongTrung | IdempotencyKey | nvarchar(200) | 200 | Không | — | Không | — | Có | Không | Không | Không | Không | Unique endpoint |
| 6 | SoLanThu | AttemptCount | int | — | Không | — | Không | — | Không | Có | Không | Không | Không | Không âm |
| 7 | ThoiDiemThuLaiUtc | NextRetryAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 8 | MaLease | LeaseToken | uniqueidentifier | — | Có | — | Không | Theo owner | Không | Có | Không | Không | Không | Worker lease |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Queued | Không | — | Không | Có | Không | Không | Không | State machine; không string tự do |
| 10 | MaNguoiGiuLease | LeaseOwner | nvarchar(100) | 100 | Có | — | Không | — | Không | Có | Không | Không | Không | Đi cùng thời điểm hết lease |
| 11 | ThoiDiemHetLeaseUtc | LeaseExpiresAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | Worker khác chỉ reclaim sau expiry |
| 12 | PhanLoaiLoi | ErrorClassification | varchar(30) | 30 | Có | — | Không | Lookup | Không | Có | Không | Không | Không | Transient/Permanent/Unknown |
| 13 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 14 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 15 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 16 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 17 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.85. fo_integration.LanThucHienTichHop — IntegrationAttempt

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLanThucHienTichHop | IntegrationAttemptId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaCongViecTichHop | ParentId | uniqueidentifier | — | Không | — | Không | CongViecTichHop | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.86. fo_integration.NgoaiLeTichHop — IntegrationException

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNgoaiLeTichHop | IntegrationExceptionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 4 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 5 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 6 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 7 | TrangThai | Status | varchar(30) | 30 | Không | Open | Không | — | Không | Có | Không | Không | Không | Open/Assigned/Investigating/Resolved/Accepted |
| 8 | MucDo | Severity | varchar(20) | 20 | Không | — | Không | Lookup | Không | Có | Không | Không | Không | Điều khiển SLA/escalation |
| 9 | MaNguoiPhuTrach | OwnerId | uniqueidentifier | — | Có | — | Không | Identity external | Không | Có | Không | Không | Không | Có thể reassign có audit |
| 10 | MaVanTayNgoaiLe | ExceptionFingerprint | varbinary(32) | 32 | Không | — | Không | — | Có trong active scope | Có | Không | Không | Không | Dedupe ngoại lệ mở |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 12 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 13 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 14 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 15 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.87. fo_integration.XuLyNgoaiLeTichHop — ExceptionResolution

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaXuLyNgoaiLeTichHop | ExceptionResolutionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNgoaiLeTichHop | ParentId | uniqueidentifier | — | Không | — | Không | NgoaiLeTichHop | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.88. fo_integration.ThamChieuBenNgoai — ExternalReference

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThamChieuBenNgoai | ExternalReferenceId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 4 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 5 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 6 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 7 | MaDiemCuoi | EndpointId | uniqueidentifier | — | Không | — | Không | Integration config | Không | Có | Không | Không | Có | Bất biến trong mapping version |
| 8 | LoaiDoiTuongNgoai | ExternalObjectType | varchar(80) | 80 | Không | — | Không | — | Không | Có | Không | Không | Không | Contract-governed |
| 9 | MaDoiTuongNgoai | ExternalObjectId | nvarchar(200) | 200 | Không | — | Không | — | Có theo endpoint/type | Có | Mã hóa/masking nếu chứa PII | Theo role | Không | Không chứa secret |
| 10 | LoaiDoiTuongNoiBo | LocalSubjectType | varchar(80) | 80 | Không | — | Không | — | Không | Có | Không | Không | Không | Canonical Aggregate type |
| 11 | MaDoiTuongNoiBo | LocalSubjectId | uniqueidentifier | — | Không | — | Không | Logical reference | Không | Có | Không | Không | Không | Không FK xuyên context |
| 12 | TrangThai | Status | varchar(20) | 20 | Không | Active | Không | — | Không | Có | Không | Không | Không | Active/Superseded/Invalidated |
| 13 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 14 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 15 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 16 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 17 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |

#### 5.2.89. fo_integration.PhienBanAnhXaNgoai — ExternalMappingVersion

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhienBanAnhXaNgoai | ExternalMappingVersionId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThamChieuBenNgoai | ParentId | uniqueidentifier | — | Không | — | Không | ThamChieuBenNgoai | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.90. fo_integration.BanGhiChongTrung — IdempotencyRecord

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBanGhiChongTrung | IdempotencyRecordId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | KhongGianTen | Namespace | varchar(80) | 80 | Không | — | Không | — | Có | Không | Không | Không | Không | Unique scope |
| 4 | KhoaChongTrung | IdempotencyKey | nvarchar(200) | 200 | Không | — | Không | — | Có | Không | Không | Không | Không | Unique namespace+Property |
| 5 | MaVanTayYeuCau | RequestHash | varbinary(32) | 32 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Mismatch reject |
| 6 | ThoiDiemHetHanUtc | ExpiresAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | Policy |
| 7 | TrangThai | Status | varchar(30) | 30 | Không | Received | Không | — | Không | Có | Không | Không | Không | Received/Processing/Completed/Failed/Unknown |
| 8 | MaKetQua | ResultReference | uniqueidentifier | — | Có | — | Không | Logical subject | Không | Có | Không | Không | Không | Duplicate trả đúng kết quả cũ |
| 9 | MaLease | LeaseToken | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Reclaim sau expiry |
| 10 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Tự sinh | Không | — | Không | Có | Không | Không | Không | Không set client |
| 11 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 12 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 13 | ThoiDiemCapNhatUtc | UpdatedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Không | Không | Không | Không | Cùng transaction |
| 14 | MaNguoiCapNhat | UpdatedBy | uniqueidentifier | — | Có | — | Không | Identity external | Không | Không | Không | Không | Không | Cùng transaction |
| 15 | MaKetQua | ResultReference | uniqueidentifier | — | Có | — | Không | Logical result | Không | Có | Không | Không | Owned outcome |
| 16 | MaVanTayKetQua | ResultHash | varbinary(32) | 32 | Có | — | Không | — | Không | Không | Mã hóa nếu sensitive | Có | Stable duplicate response |

#### 5.2.91. fo_integration.ThongDiepOutbox — OutboxMessage

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThongDiepOutbox | OutboxMessageId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThongDiep | MessageId | uniqueidentifier | — | Không | — | Không | Theo owner | Có | Có | Không | Không | Không | Unique |
| 4 | LoaiThongDiep | MessageType | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | Versioned |
| 5 | PhienBanPayload | PayloadVersion | smallint | — | Không | — | Không | — | Không | Không | Không | Không | Không | Lớn hơn 0 |
| 6 | PayloadMaHoa | EncryptedPayload | varbinary(max) | max | Không | — | Không | — | Không | Không trực tiếp | Có | Không hiển thị raw | Không | JSON versioned, compressed/encrypted; giới hạn tại application |
| 7 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Index |
| 8 | ThoiDiemGuiLaiUtc | NextAttemptAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 9 | TrangThai | Status | varchar(30) | 30 | Không | Pending | Không | — | Không | Có | Không | Không | Không | Pending/Processing/Published/DeadLettered |
| 10 | SoLanThu | AttemptCount | int | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Không âm; bounded retry |
| 11 | ThoiDiemXuLyUtc | ProcessedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 12 | MaLease | LeaseToken | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Không hai worker xử lý cùng message |
| 13 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 14 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 15 | MaAggregate | AggregateId | uniqueidentifier | — | Không | — | Không | Logical reference | Không | Có | Không | Không | Có | Aggregate source |
| 16 | LoaiAggregate | AggregateType | varchar(80) | 80 | Không | — | Không | — | Không | Có | Không | Không | Có | Canonical type |
| 17 | PhienBanAggregate | AggregateVersion | bigint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Event ordering |
| 18 | MaNguyenNhan | CausationId | uniqueidentifier | — | Có | — | Không | Logical reference | Không | Có | Không | Không | Có | Trace chain |
| 19 | MaVanTayPayload | PayloadHash | binary(32) | 32 | Không | — | Không | — | Không | Có | Không | Không | Có | Integrity/dedupe |
| 20 | ChuLease | LeaseOwner | varchar(100) | 100 | Có | — | Không | — | Không | Có | Không | Không | Không | Worker claim |
| 21 | HetHanLeaseUtc | LeaseExpiresAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | Reclaim |
| 22 | LoaiLoiCuoi | LastErrorType | varchar(100) | 100 | Có | — | Không | — | Không | Có | Không | Không | Không | Classification |
| 23 | LoiCuoi | LastError | nvarchar(1000) | 1000 | Có | — | Không | — | Không | Không | Không | Mask secret/PII | Không | Sanitized/truncated |
| 24 | CoDeadLetter | IsDeadLettered | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Terminal processing state |
| 25 | LuuDenNgay | RetainUntilDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Retention boundary |

#### 5.2.92. fo_integration.ThongDiepInbox — InboxMessage

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaThongDiepInbox | InboxMessageId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaThongDiep | MessageId | uniqueidentifier | — | Không | — | Không | Theo owner | Có | Có | Không | Không | Không | Unique consumer |
| 4 | ModuleNguon | Producer | varchar(80) | 80 | Không | — | Không | — | Không | Không | Không | Không | Không | Governed |
| 5 | MaVanTayPayload | PayloadHash | varbinary(32) | 32 | Không | — | Không | Theo owner | Không | Có | Không | Không | Không | Mismatch reject |
| 6 | ThoiDiemXuLyUtc | ProcessedAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | UTC |
| 7 | KetQua | Outcome | varchar(20) | 20 | Có | — | Không | — | Không | Không | Không | Không | Không | Enum |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |
| 10 | Consumer | Consumer | varchar(100) | 100 | Không | — | Không | — | Không | Có | Không | Không | Có | Dedupe namespace |
| 11 | PhienBanPayload | PayloadVersion | smallint | — | Không | — | Không | — | Không | Có | Không | Không | Có | Schema evolution |
| 12 | PayloadMaHoa | EncryptedPayload | varbinary(max) | max | Có | — | Không | — | Không | Không trực tiếp | Có | Không hiển thị raw | Không | Minimized compressed/encrypted payload |
| 13 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Có | — | Không | Logical reference | Không | Có | Không | Không | Có | Trace |
| 14 | MaNguyenNhan | CausationId | uniqueidentifier | — | Có | — | Không | Logical reference | Không | Có | Không | Không | Có | Trace |
| 15 | TrangThai | Status | varchar(30) | 30 | Không | Received | Không | — | Không | Có | Không | Không | Không | Received/Processing/Processed/Failed |
| 16 | SoLanThu | AttemptCount | int | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Non-negative |
| 17 | ThoiDiemThuLaiUtc | NextRetryAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | Retry schedule |
| 18 | ChuLease | LeaseOwner | varchar(100) | 100 | Có | — | Không | — | Không | Có | Không | Không | Không | Worker claim |
| 19 | HetHanLeaseUtc | LeaseExpiresAtUtc | datetime2(7) | 7 | Có | — | Không | — | Không | Có | Không | Không | Không | Reclaim |
| 20 | LoiCuoi | LastError | nvarchar(1000) | 1000 | Có | — | Không | — | Không | Không | Không | Mask secret/PII | Không | Sanitized |
| 21 | LuuDenNgay | RetainUntilDate | date | — | Không | — | Không | — | Không | Có | Không | Không | Không | Retention boundary |

#### 5.2.93. fo_integration.YeuCauPhatLai — ReplayRequest

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaYeuCauPhatLai | ReplayRequestId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | MaNgoaiLeTichHop | ParentId | uniqueidentifier | — | Không | — | Không | NgoaiLeTichHop | Không | Có | Không | Không | Không | FK cùng Aggregate; không cascade nguy hiểm |
| 4 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 5 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 6 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 7 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 9 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.94. platform_audit.NhatKyKiemToan — AuditRecord

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhatKyKiemToan | AuditRecordId | uniqueidentifier | — | Không | Sinh tại application | Có | — | Có | PK | Không | Không | Có | Bất biến; offline-safe |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Property external | Không | Có | Không | Không | Có | Bất biến; authorization scope |
| 3 | SoThuTu | Sequence | bigint | — | Không | Theo scope | Không | Root/stream | Có theo scope | Có | Không | Không | Có | Append order |
| 4 | LoaiHanhDong | ActionType | varchar(40) | 40 | Không | — | Không | — | Không | Có | Không | Không | Không | Mã governance |
| 5 | MaTuongQuan | CorrelationId | uniqueidentifier | — | Không | — | Không | — | Không | Có | Không | Không | Không | Trace |
| 6 | ThoiDiemXayRaUtc | OccurredAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 7 | ThoiDiemTaoUtc | CreatedAtUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Có | Bất biến |
| 8 | MaNguoiTao | CreatedBy | uniqueidentifier | — | Không | — | Không | Identity external | Không | Có | Không | Không | Không | Định danh service/user |

#### 5.2.95. fo_reservation.NguonDatPhong — ReservationSource

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNguonDatPhong | ReservationSourceId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.96. fo_reservation.KenhDatPhong — ReservationChannel

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKenhDatPhong | ReservationChannelId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.97. fo_reservation.LyDoHuyDatPhong — CancellationReason

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLyDoHuyDatPhong | CancellationReasonId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.98. fo_cashiering.PhuongThucThanhToan — PaymentMethod

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaPhuongThucThanhToan | PaymentMethodId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.99. fo_folio.KhoanThu — ChargeCode

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKhoanThu | ChargeCodeId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.100. fo_housekeeping.LoaiVeSinh — CleaningType

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLoaiVeSinh | CleaningTypeId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.101. fo_housekeeping.MucUuTienBuongPhong — HousekeepingPriority

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaMucUuTienBuongPhong | HousekeepingPriorityId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.102. fo_maintenance.NhomBaoTri — MaintenanceCategory

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaNhomBaoTri | MaintenanceCategoryId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.103. fo_stay.LyDoChuyenPhong — RoomMoveReason

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLyDoChuyenPhong | RoomMoveReasonId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.104. fo_stay.LyDoGhiDe — OverrideReason

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLyDoGhiDe | OverrideReasonId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.105. fo_task.LoaiCongViec — TaskType

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLoaiCongViec | TaskTypeId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.106. fo_integration.LoaiNgoaiLeTichHop — IntegrationExceptionType

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLoaiNgoaiLeTichHop | IntegrationExceptionTypeId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.107. fo_nightaudit.LoaiChanKiemToanDem — NightAuditBlockerType

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLoaiChanKiemToanDem | NightAuditBlockerTypeId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.108. fo_guestservice.LoaiDichVuKhach — ServiceType

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaLoaiDichVuKhach | ServiceTypeId | uniqueidentifier | — | Không | App GUID | Có | — | Có | PK | Không | Không | Có | Stable seed ID |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Có | — | Không | External | Không | Có | Không | Không | Có | Null=global; value=Property override |
| 3 | MaHienThi | Code | varchar(40) | 40 | Không | — | Không | — | Có theo scope | Có | Không | Không | Có | Canonical code |
| 4 | TenTiengViet | VietnameseName | nvarchar(200) | 200 | Không | — | Không | — | Không | Không | Không | Không | Không | UI label |
| 5 | TenTiengAnh | EnglishName | nvarchar(200) | 200 | Có | — | Không | — | Không | Không | Không | Không | Không | Canonical English |
| 6 | MoTa | Description | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Không | Không | Không MAX |
| 7 | ThuTu | DisplayOrder | smallint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | >=0 |
| 8 | CoHieuLuc | IsActive | bit | — | Không | 1 | Không | — | Không | Có | Không | Không | Không | Deactivate, no delete |
| 9 | TuNgay | EffectiveFrom | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | Effective range |
| 10 | DenNgay | EffectiveTo | date | — | Có | — | Không | — | Không | Có | Không | Không | Có | After from |
| 11 | PhienBanDuLieu | DataVersion | rowversion | 8 | Không | Auto | Không | — | Không | Có | Không | Không | Không | Concurrency |

#### 5.2.109. fo_projection.SoDoPhong — RoomRack

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaSoDoPhong | RoomRackProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |


#### 5.2.110. fo_projection.KeHoachPhong — RoomPlan

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaKeHoachPhong | RoomPlanProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.111. fo_projection.DongThoiGianLuuTru — StayTimeline

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDongThoiGianLuuTru | StayTimelineProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.112. fo_projection.DanhSachKhachDen — ArrivalList

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhSachKhachDen | ArrivalListProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.113. fo_projection.DanhSachKhachDi — DepartureList

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhSachKhachDi | DepartureListProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.114. fo_projection.DanhSachKhachDangLuuTru — InHouseList

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhSachKhachDangLuuTru | InHouseListProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.115. fo_projection.BangDieuHanhTienSanh — FrontOfficeDashboard

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangDieuHanhTienSanh | FrontOfficeDashboardProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.116. fo_projection.BangDieuHanhBuongPhong — HousekeepingBoard

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangDieuHanhBuongPhong | HousekeepingBoardProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.117. fo_projection.BangTheoDoiSoDuKhach — GuestBalanceBoard

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangTheoDoiSoDuKhach | GuestBalanceBoardProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.118. fo_projection.DanhSachCanSanSangTraPhong — CheckoutReadinessList

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaDanhSachCanSanSangTraPhong | CheckoutReadinessListProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.119. fo_projection.BangDoiSoatThuNgan — CashierReconciliationBoard

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangDoiSoatThuNgan | CashierReconciliationBoardProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.120. fo_projection.BangNgoaiLeTichHop — IntegrationExceptionBoard

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangNgoaiLeTichHop | IntegrationExceptionBoardProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

#### 5.2.121. fo_projection.BangKiemToanDem — NightAuditBoard

| STT | Tên cột SQL | Domain Property | Kiểu SQL | Độ dài/Precision | Null | Default | PK | FK | Unique | Index | Mã hóa | Masking | Immutable | Ghi chú |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | MaBangKiemToanDem | NightAuditBoardProjectionId | bigint | — | Không | identity | Có | — | Có | Cluster candidate | Không | Không | Không | Projection key |
| 2 | MaKhachSan | PropertyId | uniqueidentifier | — | Không | — | Không | Không FK | Không | Có | Không | Không | Không | Scope |
| 3 | NgayNghiepVu | BusinessDate | date | — | Có | — | Không | — | Không | Có | Không | Không | Không | Screen/date filter |
| 4 | MaDoiTuong | SubjectId | uniqueidentifier | — | Có | — | Không | Không FK | Không | Có | Không | Không | Không | Aggregate subject |
| 5 | LoaiDoiTuong | SubjectType | varchar(50) | 50 | Có | — | Không | — | Không | Có | Không | Không | Không | Canonical type |
| 6 | TrangThai | Status | varchar(30) | 30 | Có | — | Không | — | Không | Có | Không | Không | Không | Denormalized |
| 7 | TomTatHienThi | DisplaySummary | nvarchar(500) | 500 | Có | — | Không | — | Không | Không | Không | Mask theo role | Không | Không PII raw |
| 8 | PhienBanNguon | SourceVersion | bigint | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Idempotent upsert |
| 9 | MaDiemKiemSoat | CheckpointId | uniqueidentifier | — | Có | — | Không | — | Không | Có | Không | Không | Không | Rebuild cursor |
| 10 | ThoiDiemCapNhatUtc | FreshnessUtc | datetime2(7) | 7 | Không | UTC | Không | — | Không | Có | Không | Không | Không | Show stale |
| 11 | CoLoiThoi | IsStale | bit | — | Không | 0 | Không | — | Không | Có | Không | Không | Không | Fail-safe UI |

## 6. Common Column Convention

### 6.1 Quy ước chung

| Nhóm bảng | Cột bắt buộc | Quy tắc |
|---|---|---|
| Aggregate/transaction mutable | Khóa chính, `MaKhachSan`, `PhienBanDong`, `TaoLucUtc`, `TaoBoi`, `SuaLucUtc`, `SuaBoi` | `PhienBanDong` dùng SQL Server `rowversion`; toàn bộ thời điểm kỹ thuật là UTC |
| Ledger/event append-only | Khóa chính, `MaKhachSan`, thời điểm phát sinh, nguồn, idempotency key, `TaoLucUtc`, `TaoBoi` | Không có cập nhật nghiệp vụ; sửa sai bằng bút toán đảo/điều chỉnh |
| Master có vòng đời | Thêm `HieuLucTu`, `HieuLucDen`, `DangHoatDong` khi lịch sử hiệu lực ảnh hưởng nghiệp vụ | Không dùng soft delete để thay thế lịch sử hiệu lực |
| Integration | `CorrelationId`, `CausationId`, `IdempotencyKey`, trạng thái xử lý, số lần thử, lỗi cuối | Payload có schema version; không lưu PII thừa |
| Projection | `PhienBanNguon`, `MaDiemKiemSoat`, `ThoiDiemCapNhatUtc`, `CoLoiThoi` | Có thể xóa và dựng lại; không là source of truth |
| Audit | Actor, action, subject, correlation, time, before/after digest | Append-only; quyền đọc tách biệt |

| Loại bảng | MaKhachSan | NgayNghiepVu | Audit columns | Rowversion | Status | Soft delete |
|---|---|---|---|---|---|---|
| Root mutable | Bắt buộc | Khi nghiệp vụ theo ngày | Tạo/cập nhật/actor | Bắt buộc | Khi có lifecycle | Không mặc định |
| Child mutable | Bắt buộc nếu query độc lập; nếu không suy từ Root | Khi cần | Tạo/cập nhật tối thiểu | Khi sửa độc lập | Khi child có lifecycle | Không mặc định |
| Append-only ledger/history | Bắt buộc | Bắt buộc với tài chính/vận hành theo ngày | Created/actor/reason | Không; dùng sequence/version nguồn | Loại entry thay cho mutable status | Cấm |
| Lookup | Null nếu global; bắt buộc nếu property-scoped | Không | Created/updated hoặc catalog version | Tùy cấu hình đồng thời | `CoHieuLuc` + effective dates | Không |
| Projection | Bắt buộc | Theo grain | Freshness/checkpoint | Không; dùng source version | Denormalized display status | Xóa/rebuild generation, không soft delete |
| Integration | Bắt buộc nếu tenant-scoped | Khi message mang business date | Created/processed/error trace | Lease/version khi cần | Bắt buộc | Không; retention/archive |
| Audit | Bắt buộc nếu xác định được | Khi action gắn ngày nghiệp vụ | Chính bản ghi là audit | Không | Action/sensitivity | Cấm |

Tên cột chuẩn chính thức: khóa `Ma...`; tenant `MaKhachSan`; ngày nghiệp vụ `NgayNghiepVu`; số hiển thị `SoHienThi` hoặc tên business number cụ thể; `TrangThai`; `ThoiDiemTaoUtc`/`MaNguoiTao`; `ThoiDiemCapNhatUtc`/`MaNguoiCapNhat`; concurrency `PhienBanDuLieu`; trace `MaTuongQuan`/`MaNguyenNhan`; approval `MaPheDuyet`; external reference `MaThamChieuNgoai`. Chỉ dùng cột nào có nghĩa với loại bảng tương ứng.

`MaKhachSan` bắt buộc trên mọi dữ liệu tenant-scoped, kể cả bảng con thường truy cập độc lập. FK nội bộ ưu tiên mang cùng `MaKhachSan` để ngăn liên kết chéo khách sạn. Không đưa `IsDeleted` đại trà vào mọi bảng: hủy nghiệp vụ phải có trạng thái và lịch sử; xóa kỹ thuật chỉ áp dụng cho dữ liệu nháp chưa phát sinh nghĩa vụ.

### 6.2 Quy ước tên

- Schema kỹ thuật tiếng Anh, chữ thường; bảng và cột tiếng Việt không dấu PascalCase.
- PK: `PK_<Bang>`; FK: `FK_<BangCon>_<BangCha>`; unique: `UQ_<Bang>_<Nghia>`; check: `CK_<Bang>_<Nghia>`; index: `IX_<Bang>_<MucDich>`.
- Mọi ngày lưu trú dùng `date`; thời điểm dùng `datetime2(7)` UTC; ngày nghiệp vụ là khóa nghiệp vụ riêng, không suy ra tùy tiện từ UTC.
- Tiền luôn đi cùng currency; tỷ giá có scale riêng. Không dùng `money`, `float`, `real` cho số tiền, tỷ lệ hoặc số lượng tính phí.

## 7. SQL Data Type Standard

| Loại dữ liệu | Kiểu vật lý chuẩn | Áp dụng / giới hạn |
|---|---|---|
| ID phân tán | `uniqueidentifier` | Aggregate và đối tượng cần tạo ID trước khi ghi; sinh UUID tuần tự/UUIDv7 ở ứng dụng |
| ID append-only lớn | `bigint identity` | Ledger, audit, outbox/inbox, lịch sử dung lượng lớn |
| Số thứ tự nhỏ | `int` / `smallint` | Sequence trong aggregate; không dùng làm business ID toàn cục |
| Tiền | `decimal(19,4)` | Tổng tiền, đơn giá, thuế, phí; currency ISO 4217 `char(3)` |
| Tỷ giá | `decimal(19,8)` | Chuyển đổi ngoại tệ |
| Tỷ lệ | `decimal(9,6)` | Thuế, discount, commission; check miền giá trị theo ngữ nghĩa |
| Số lượng | `decimal(18,4)` | Minibar, laundry, amenities; số đêm có thể `smallint` |
| Ngày lưu trú | `date` | Arrival, departure, business date |
| Thời điểm | `datetime2(7)` | Luôn UTC và hậu tố `Utc` |
| Concurrency token | `rowversion` | Chỉ concurrency; không diễn giải thành thời gian |
| Mã trạng thái nóng | `varchar(30)` | FK đến typed lookup; ASCII canonical code |
| Mã nghiệp vụ | `varchar(20..50)` | Reservation/folio/document number; độ dài theo catalog |
| Tên/ngôn ngữ tự nhiên | `nvarchar(100..300)` | Không dùng `nvarchar(max)` nếu có giới hạn hợp lý |
| Ghi chú | `nvarchar(1000..2000)` | `nvarchar(max)` chỉ cho payload/nội dung thật sự không chặn được kích thước |
| JSON | `nvarchar(max)` | Có check JSON hợp lệ; payload lớn có thể nén sang `varbinary(max)` |
| Hash/digest | `binary(32)` | SHA-256; mã hóa/tìm kiếm dùng trường riêng |
| Dữ liệu mã hóa | `varbinary(max)` | Ciphertext envelope; metadata khóa tách riêng |
| Boolean | `bit` | Default rõ ràng; không nullable nếu không có trạng thái thứ ba |

Chuỗi business code dùng collation không phân biệt hoa thường nhưng phân biệt dấu theo collation chuẩn của database; mã kỹ thuật ASCII có thể dùng collation nhị phân khi cần đối sánh tuyệt đối. Cột `nvarchar(max)` không được đưa trực tiếp vào khóa index; nếu cần tìm kiếm phải có cột chuẩn hóa/hash hoặc full-text được phê duyệt riêng.

| Độ dài Unicode | Dùng cho | Không dùng cho |
|---|---|---|
| `nvarchar(50)` | Nhãn ngắn, mã hiển thị Unicode hiếm, tầng/khu vực | Họ tên đầy đủ quốc tế hoặc lỗi tích hợp |
| `nvarchar(100)` | Tên ngắn, người liên hệ, địa điểm | Ghi chú |
| `nvarchar(200)` | Họ tên/tên công ty snapshot, message type hiển thị | Payload |
| `nvarchar(500)` | Mô tả/tóm tắt vận hành, display summary projection | Complaint/stack trace dài |
| `nvarchar(1000)` | Lý do, ghi chú có kiểm soát, lỗi đã sanitize | Tệp, JSON lớn |
| `nvarchar(max)` | Chỉ JSON versioned hoặc nội dung thật sự không có trần hợp lý; phải có size limit ở application | Mã, tên, trạng thái, trường được index |

## 8. Primary Key Strategy

| Nhóm | Chiến lược PK | Lý do |
|---|---|---|
| Reservation, Stay, Room, Folio, Payment, Invoice, Housekeeping, Maintenance, Group, Task | `uniqueidentifier` sinh tuần tự ở application | Aggregate được tạo offline trong unit of work và trao đổi qua module boundary mà không phụ thuộc round-trip DB |
| Folio ledger, attempt, audit, outbox/inbox, histories | `uniqueidentifier` sinh tại application theo catalog; `SoThuTu bigint` là alternate ordering khi có | Event identity/offline/cross-module ổn định; clustered index không buộc theo PK |
| Bảng con theo thứ tự trong aggregate | GUID nếu được tham chiếu độc lập; nếu không dùng PK ghép `(AggregateId, SequenceNo)` | Tránh surrogate key không mang giá trị khi vòng đời hoàn toàn thuộc aggregate |
| Typed lookup | `varchar(30)` canonical code | Mã ổn định là identity nghiệp vụ; không đổi code sau khi phát hành |
| Projection | `bigint identity` hoặc PK ghép đúng grain | ID chỉ phục vụ read store; projection có thể rebuild |

Không dùng GUID ngẫu nhiên làm clustered key mặc định. Nếu runtime chưa hỗ trợ UUIDv7 ổn định, dùng GUID tuần tự do application cung cấp và bắt buộc kiểm thử thứ tự byte với SQL Server. Business number như số đặt phòng hoặc số folio chỉ là alternate key theo khách sạn, không là PK.

## 9. Clustered Index Strategy

### 9.1 Nguyên tắc

- Bảng aggregate dùng clustered index hẹp, tăng dần theo `bigint identity` nếu đã có; với PK GUID, tạo surrogate `bigint identity` làm clustered key chỉ khi bảng nóng/lớn, còn PK GUID là unique nonclustered.
- Bảng ledger/history có `SoThuTu bigint` clustered theo stream/grain; audit/outbox/inbox tăng theo thời gian dùng clustered key time/partition + GUID, hoặc thêm surrogate identity chỉ qua ADR/migration sau benchmark.
- Bảng theo ngày và khách sạn có clustered key bắt đầu bằng `MaKhachSan`, sau đó grain nóng nhất, ví dụ `(MaKhachSan, NgayLuuTru, MaLoaiPhong)`.
- Fill factor chỉ cấu hình sau đo fragmentation thực tế; không đặt đồng loạt. Online/resumable index operation phụ thuộc edition và runbook vận hành.

### 9.2 Quyết định cho bảng trọng yếu

| Bảng/nhóm | Clustered key đề xuất | PK vật lý |
|---|---|---|
| `DatPhong`, `KyLuuTru`, `Phong`, `Folio`, `HoaDon` | Surrogate `bigint identity` nội bộ nếu vượt ngưỡng tải; nếu chưa, PK GUID clustered tạm thời với UUID tuần tự | GUID unique |
| `PhanPhong`, `PhanDoanPhanPhong` | `(MaKhachSan, MaPhong, TuNgay, MaPhanPhong)` | GUID unique nonclustered |
| `TonPhongKhaDung` | `(MaKhachSan, NgayLuuTru, MaLoaiPhong)` | Cùng grain, unique |
| `ButToanSoGiaoDich` | `(MaCuaSoFolio, SoThuTu)` hoặc `(MaFolio, SoThuTu)` | PK GUID unique nonclustered; sequence unique theo stream |
| `LanThuThanhToan`, `LanThucHienHoanTien`, `LanThucHienTichHop` | Sequence/time-oriented key; bigint identity nếu volume benchmark yêu cầu | PK GUID vẫn unique nonclustered theo catalog baseline |
| `ThongDiepInbox`, `ThongDiepOutbox`, `NhatKyKiemToan` | ID tăng dần/partition-aligned khi triển khai | Clustered append; identity quyết định ở migration review |
| `NhatKyKiemToan` | `(NgayPhanVung, SoThuTu, MaNhatKyKiemToan)` | PK GUID unique; partition-aligned index |
| Projection dashboard/timeline | Grain truy vấn chính; ID identity nonclustered khi cần | Projection-specific |

## 10. Foreign Key Strategy

### 10.1 Quy tắc

1. FK bắt buộc trong cùng bounded context và cùng database. FK tenant-aware ưu tiên `(MaKhachSan, ParentId)` đến alternate key tương ứng.
2. Không tạo FK trực tiếp sang database/module khác. Dùng external reference gồm `ExternalSystem`, `ExternalId`, `ExternalVersion` và xác nhận qua integration workflow.
3. `ON DELETE NO ACTION` là mặc định. Cascade chỉ cho owned detail không có lịch sử độc lập và không tham gia ledger; cấm cascade trên Folio, Payment, Invoice, Stay, Reservation, Audit và Integration.
4. Quan hệ tùy chọn dùng nullable FK; không dùng sentinel GUID rỗng. Polymorphic reference phải có `LoaiDoiTuong` + `MaDoiTuong` và được application validation/audit bảo vệ.
5. Mọi FK phải có supporting index nếu parent delete/update check hoặc child navigation là đường truy vấn thực tế.

### 10.2 Catalog quan hệ trọng yếu

Catalog FK đầy đủ ở mức từng cột nằm trong 121 data dictionary tại Phần 5 (`FK` ghi tên bảng cha hoặc `Logical/External reference`). Bảng dưới tổng hợp mọi họ quan hệ vật lý được cho phép; không có FK ngoài các họ này.

| Phân loại | Bảng con/cột FK điển hình | Bảng cha | Có FK vật lý | Delete behavior | Lý do |
|---|---|---|---|---|---|
| Trong Aggregate | Mọi child `Ma<Root>`/`ParentId` | Root cùng aggregate | Có | NoAction; cascade chỉ whitelist owned draft | Bảo vệ lifecycle |
| Giữa Aggregate cùng Context | Stay→Reservation, Folio→Stay, assignment→Room | Root cùng FO context | Có khi ownership/transaction rõ | NoAction | Referential integrity tenant-aware |
| Lookup cùng module | Status/reason/type/method code | Typed lookup | Có với lookup vật lý | NoAction | Governance code/effective value |
| External module | CRM Guest/Company/Agent, Accounting Invoice/Journal/Tax, POS/Banquet/Inventory refs | Không có bảng cha FO | Không | Không áp dụng | Chỉ ID + source/version/snapshot tối thiểu |
| Projection | Subject/source IDs | Write model | Không | Truncate/rebuild | Tránh coupling rebuild và delete check ngược |
| Audit/Integration | Aggregate/subject/external IDs | Write model | Không | Retention độc lập | Append-only, replay/archive độc lập |

| Con | Cha | Cardinality | Xử lý xóa |
|---|---|---|---|
| `KhachDatPhong`, `PhanKhucDatPhong`, `BaoDamDatPhong`, `LichSuTrangThaiDatPhong` | `DatPhong` | N:1 | No action |
| `KyLuuTru` | `DatPhong` (tùy chọn) | 0..N:1 | No action |
| `KhachLuuTru`, `PhanPhong`, `LichSuTrangThaiKyLuuTru` | `KyLuuTru` | N:1 | No action |
| `PhanDoanPhanPhong` | `PhanPhong` | N:1 owned history | No action; archive cùng retention |
| `Phong` | `LoaiPhong` | N:1 | No action |
| `LichSuTrangThaiPhong`, `KhoaPhong`, `TinhTrangPhongTheoNgay` | `Phong` | N:1 | No action |
| `Folio` | `KyLuuTru`/`DatPhong`/`HoSoDoan` tùy loại | N:1 | No action |
| `CuaSoFolio`, `ButToanSoGiaoDich`, `LichSuTrangThaiFolio` | `Folio` | N:1 | No action |
| `PhanBoButToan` | `ButToanSoGiaoDich` | N:1 | No action |
| `ThanhToan`, `DatCoc`, `HoanTien` | `Folio` | N:1 | No action |
| `LanThuThanhToan` | `ThanhToan` | N:1 | No action |
| `PhanBoDatCoc` | `DatCoc` | N:1 | No action |
| `BuocPheDuyetHoanTien`, `LanThucHienHoanTien` | `YeuCauHoanTien` | N:1 | No action |
| `DongHoaDon`, `LichSuTrangThaiHoaDon` | `HoaDon` | N:1 | No action |
| `CongViecDonPhong`, `KiemTraPhong`, `BaoCaoMinibar`, `BaoCaoDoVai` | `PhienDonPhong`/`Phong`/`KyLuuTru` | N:1 | No action |
| `YeuCauBaoTri`, `LenhBaoTri`, `NhatKyBaoTri`, `LichSuTrangThaiBaoTri` | Chuỗi Maintenance | N:1 | No action |
| `ThanhVienDoan`, `DatPhongTheoDoan`, `ChiDanThanhToanDoan` | `HoSoDoan` | N:1 | No action |
| `BuocKiemToanDem`, `KetQuaKiemTraKiemToanDem`, `CanhBaoKiemToanDem` | `PhienKiemToanDem` | N:1 | No action |
| `NhiemVu`, `ThongBao` | Subject polymorphic | N:0..1 | Không có FK polymorphic; kiểm soát bằng service contract |

## 11. Unique Constraint Catalog

| ID | Bảng | Khóa unique | Loại |
|---|---|---|---|
| UQ-01 | `DatPhong` | `(MaKhachSan, SoDatPhong)` | Business number |
| UQ-02 | `KyLuuTru` | `(MaKhachSan, SoKyLuuTru)` | Business number |
| UQ-03 | `Phong` | `(MaKhachSan, MaPhongHienThi)` | Master data |
| UQ-04 | `LoaiPhong` | `(MaKhachSan, MaLoaiPhongHienThi)` | Master data |
| UQ-05 | `Folio` | `(MaKhachSan, SoFolio)` | Business number |
| UQ-06 | `CuaSoFolio` | `(MaFolio, SoCuaSo)` | Aggregate sequence |
| UQ-07 | `ButToanSoGiaoDich` | `(MaKhachSan, KhoaChongTrungNguon)` khi khác null | Filtered idempotency |
| UQ-08 | `PhanBoButToan` | `(MaButToan, SoThuTu)` | Owned sequence |
| UQ-09 | `ThanhToan`/`DatCoc`/`HoanTien` | `(MaKhachSan, SoChungTu)` từng bảng | Cashiering document |
| UQ-10 | `LanThuThanhToan` | `(MaNhaCungCap, MaGiaoDichNhaCungCap)` khi khác null | Provider idempotency |
| UQ-11 | `HoaDon` | `(MaKhachSan, MauSo, KyHieu, SoHoaDon)` khi đã phát hành | Legal document |
| UQ-12 | `TonPhongKhaDung` | `(MaKhachSan, MaLoaiPhong, NgayLuuTru)` | Availability grain |
| UQ-13 | `BaoCaoMinibar` | `(MaKhachSan, MaPhong, NgayNghiepVu, LanBaoCao)` | Operational repeat |
| UQ-14 | `PhienKiemToanDem` | `(MaKhachSan, NgayNghiepVu)` với trạng thái active/completed | One audit per date |
| UQ-15 | `ThongDiepInbox` | `(Consumer, MessageId)` | Inbox idempotency |
| UQ-16 | `ThongDiepOutbox` | `(MaKhachSan, EventId, Dich)` | Outbox destination |
| UQ-17 | `BanGhiChongTrung` | `(PhamVi, KhoaChongTrung)` | Command idempotency |
| UQ-18 | Typed lookup | `MaTrangThai`/canonical code | Stable vocabulary |
| UQ-19 | `NgayNghiepVuCoSo` | `(MaKhachSan)` filtered khi trạng thái Active | Một Business Date active/property |
| UQ-20 | `CaThuNgan` | `(MaKhachSan, MaThuNgan)` filtered khi trạng thái Open | Một ca mở theo policy |
| UQ-21 | `ThamChieuBenNgoai` | `(HeThongNgoai, LoaiDoiTuong, MaThamChieuNgoai)` filtered active | Một external reference active |
| UQ-22 | `ThongDiepOutbox` | `(EventId)` | Event identity duy nhất |
| UQ-23 | `KhoiPhongDoan` | `(MaKhachSan, MaKhoiPhongDoanHienThi)` | Room-block code/property |
| UQ-24 | `HoSoDoan` | `(MaKhachSan, MaDoanHienThi)` | Group code/property |
| UQ-25 | `Folio` | `(MaKhachSan, HeThongNguon, MaThamChieuNguon)` filtered khi khác null | Source reference chống trùng |

Unique constraint chỉ bảo vệ equality. Xung đột khoảng ngày của phân phòng không thể giải quyết bằng unique key đơn giản và được thiết kế riêng tại Phần 14.

## 12. Check Constraint Catalog

| ID | Phạm vi | Điều kiện bất biến vật lý |
|---|---|---|
| CK-01 | Reservation/Stay/Assignment | `NgayDi > NgayDen`; thời điểm kết thúc lớn hơn bắt đầu |
| CK-02 | Room assignment segment | `DenNgay` null hoặc `DenNgay > TuNgay` |
| CK-03 | Money | Số tiền không âm ở document header; ledger signed amount có quy tắc debit/credit riêng |
| CK-04 | Currency | Mã tiền tệ đúng 3 ký tự ASCII uppercase |
| CK-05 | Percentage | Thuế/discount/commission nằm trong miền được domain phê duyệt |
| CK-06 | Quantity | Số lượng > 0 với consumption line; 0 chỉ cho reversal rõ ràng |
| CK-07 | Folio ledger | Một bút toán có đúng loại nguồn, currency, business date và dấu tiền phù hợp loại |
| CK-08 | Reversal | Bút toán đảo phải tham chiếu bút toán gốc và không tự tham chiếu |
| CK-09 | Payment transaction | Provider reference bắt buộc khi trạng thái đã authorized/captured |
| CK-10 | Invoice | Ngày phát hành, số hóa đơn và dữ liệu thuế bắt buộc khi trạng thái Issued |
| CK-11 | JSON | Payload/metadata là JSON hợp lệ khi khác null |
| CK-12 | Integration retry | `SoLanThu >= 0`; `ThuLaiLucUtc` chỉ có khi retryable |
| CK-13 | Effective period | `HieuLucDen` null hoặc lớn hơn `HieuLucTu` |
| CK-14 | Projection | `PhienBanNguon >= 0`; freshness UTC không null |
| CK-15 | Tenant | `MaKhachSan` không null trên tenant-scoped data |
| CK-16 | Exchange rate | Tỷ giá > 0 khi có chuyển đổi tiền tệ |
| CK-17 | Refund | Số tiền yêu cầu/thực hiện > 0; giới hạn refundable là domain/reconciliation rule |
| CK-18 | Availability | Sellable, held, allocated, blocked, oversell limit không âm; công thức available nhất quán trong một hàng |
| CK-19 | Business Date | Giá trị `date` bắt buộc; quan hệ với ngày hiện hành/policy là domain rule, không suy từ UTC |

Các constraint phụ thuộc nhiều bảng hoặc trạng thái phức tạp không đưa vào trigger. Chúng được kiểm tra ở domain/application transaction và được giám sát bằng reconciliation job; database giữ các bất biến hàng đơn giản, xác định và ít gây deadlock.

## 13. Index Catalog

### 13.1 Index giao dịch bắt buộc

| ID | Bảng | Key | Include / filter | Mục đích |
|---|---|---|---|---|
| IX-01 | `DatPhong` | `(MaKhachSan, NgayDen, TrangThai)` | Ngày đi, loại phòng, nguồn | Arrival board |
| IX-02 | `DatPhong` | `(MaKhachSan, NgayDi, TrangThai)` | Khách chính, số đặt phòng | Departure/no-show |
| IX-03 | `KyLuuTru` | `(MaKhachSan, TrangThai, NgayDiDuKien)` | Phòng hiện tại, folio | In-house/overstay |
| IX-04 | `PhanPhong` | `(MaKhachSan, MaPhong, TuNgay, DenNgay)` | Stay, status; active filter | Conflict detection |
| IX-05 | `PhanDoanPhanPhong` | `(MaKhachSan, MaPhong, TuNgay, DenNgay)` | Assignment | Interval history |
| IX-06 | `Phong` | `(MaKhachSan, TrangThaiKinhDoanh, MaLoaiPhong)` | Tầng, zone | Room rack |
| IX-07 | `TinhTrangPhongTheoNgay` | `(MaKhachSan, NgayNghiepVu, TrangThaiDonPhong)` | Room, maintenance state | Ready/dirty board |
| IX-08 | `TonPhongKhaDung` | `(MaKhachSan, NgayLuuTru, MaLoaiPhong)` | Các bucket, rowversion | Availability lookup/update |
| IX-09 | `Folio` | `(MaKhachSan, TrangThai, MaKyLuuTru)` | Balance cached, currency | Open folio |
| IX-10 | `ButToanSoGiaoDich` | `(MaFolio, ThoiDiemGhiSoUtc, MaButToan)` | Amount, currency, type | Folio statement |
| IX-11 | `ButToanSoGiaoDich` | `(MaKhachSan, NgayNghiepVu, LoaiButToan)` | Amount, folio | Night audit |
| IX-12 | `ButToanSoGiaoDich` | `(MaButToanGoc)` | Filter khác null | Reversal chain |
| IX-13 | `ThanhToan` | `(MaKhachSan, TrangThai, TaoLucUtc)` | Folio, amount, method | Cashiering queue |
| IX-14 | `LanThuThanhToan` | `(TrangThai, ThuLaiLucUtc)` | Provider, attempt | Gateway recovery |
| IX-15 | `HoaDon` | `(MaKhachSan, TrangThai, NgayPhatHanh)` | Buyer, total, folio | Invoice operations |
| IX-16 | `CongViecDonPhong` | `(MaKhachSan, NgayNghiepVu, TrangThai, UuTien)` | Room, assignee | HK board |
| IX-17 | `YeuCauBaoTri` | `(MaKhachSan, TrangThai, MucDo, TaoLucUtc)` | Room, SLA | Maintenance board |
| IX-18 | `NhiemVu` | `(MaKhachSan, TrangThai, HanXuLyUtc)` | Assignee, priority | Task Center |

### 13.2 Index integration, audit và projection

| ID | Bảng | Key / filter | Mục đích |
|---|---|---|---|
| IX-19 | `ThongDiepOutbox` | `(TrangThai, ThoiDiemSanSangUtc, MaThongDiepOutbox)` active only | Outbox publisher claim |
| IX-20 | `ThongDiepInbox` | `(TrangThai, ThoiDiemNhanUtc)` active only | Inbox processor |
| IX-21 | `BanGhiChongTrung` | `(HetHanLucUtc)` | Retention cleanup |
| IX-22 | `NhatKyKiemToan` | `(MaKhachSan, MaDoiTuong, ThoiDiemUtc)` | Subject audit trail |
| IX-23 | `NhatKyKiemToan` | `(MaKhachSan, ActorId, ThoiDiemUtc)` | Security investigation |
| IX-24 | Các projection | Grain lọc chính + `ThoiDiemCapNhatUtc` | Dashboard/timeline |

Không index tất cả FK hoặc trạng thái một cách máy móc. Mỗi index phải có owner, truy vấn đại diện, ước lượng selectivity, chi phí ghi và tiêu chí loại bỏ. Query Store và actual execution plan là đầu vào cho vòng tinh chỉnh sau go-live; catalog trên là baseline bắt buộc trước load test.

### 13.3 Ma trận phủ truy vấn bắt buộc

| Tên index | Bảng | Key columns | Included columns | Unique | Filter | Query phục vụ | Rủi ro |
|---|---|---|---|---|---|---|---|
| IX_DatPhong_XacNhan | `DatPhong` | Property, confirmation code | Status, arrival, departure | Có | — | Tìm mã xác nhận | Hot unique insert |
| IX_DatPhong_DienThoai | `DatPhong` | Property, phone blind-index | Name masked, dates, status | Không | Token khác null | Tìm snapshot điện thoại | Collision phải verify plaintext được cấp quyền |
| IX_DatPhong_Arrival | `DatPhong` | Property, arrival date, status | Confirmation, guest, room type | Không | Active | Arrivals | Include size |
| IX_DatPhong_NoShow | `DatPhong` | Property, status, arrival date | Channel, guarantee | Không | Cancelled/NoShow | Cancellation/no-show | Filter status drift |
| IX_NhuCauLoaiPhong_Ngay | Reservation segment | Property, room type, stay date | Quantity, status | Không | Consumes inventory | Demand/date | Write amplification |
| IX_KyLuuTru_InHouse | `KyLuuTru` | Property, status, departure date | Room, guest, folio | Không | InHouse | Khách đang lưu trú/departures | Hot status updates |
| IX_KyLuuTru_DatPhong | `KyLuuTru` | Property, reservation ID | Stay number, status | Không | Reservation khác null | Stay theo reservation | Low selectivity nếu group |
| IX_PhanPhong_Active | `PhanPhong` | Property, room, start, end | Stay, version | Không | Active | Active/overlap assignment | Interval predicate |
| IX_ThongTinKhoa_Active | Key credential | Property, stay, expiry | Credential alias, room | Không | Active | Key credential active | Sensitive metadata |
| IX_Folio_Stay | `Folio` | Property, stay, status | Folio number, balance/version | Không | — | Folio theo Stay | Header update cost |
| IX_CuaSoFolio_Folio | `CuaSoFolio` | Folio, window number | Name, routing | Có | — | Window theo Folio | Minimal |
| IX_ButToan_WindowSeq | `ButToanSoGiaoDich` | Window, posting sequence | Date, type, amount, currency, source | Có | — | Statement ordered | Wide include |
| IX_ButToan_Source | `ButToanSoGiaoDich` | Property, source system, source ID | Folio, amount | Có | Source ID khác null | Chống charge trùng | Source normalization |
| IX_Folio_Unsettled | `Folio` | Property, status, cached balance | Stay, currency, version | Không | Open và balance khác 0 | Unsettled balance | Balance update churn |
| IX_ThanhToan_Provider | Payment attempt | Provider, provider reference | Payment, status, amount | Có | Reference khác null | Callback/payment lookup | Provider reuse policy |
| IX_ThanhToan_Idempotency | `ThanhToan` | Property, idempotency key | Status, result reference | Có | Key khác null | Retry command | Retention alignment |
| IX_DatCoc_ChuaPhanBo | `DatCoc` | Property, status, unallocated amount | Guest/stay, currency | Không | Amount > 0 | Deposit unallocated | Computed/cache correctness |
| IX_HoanTien_Pending | Refund request | Property, status, requested UTC | Amount, payment, approval | Không | Pending | Refund approval queue | Status churn |
| IX_CaThuNgan_Open | `CaThuNgan` | Property, cashier | Opened UTC, business date | Có | Open | One open cashier shift | Policy variation |
| IX_Phong_HKFloor | Room daily status | Property, date, floor, HK status | Room, occupancy, priority | Không | Dirty/Ready | Dirty/ready by floor | Daily volume |
| IX_CongViec_Han | HK/Task | Property, status, due UTC | Room, assignee, priority | Không | Open | Task overdue | Clock boundary |
| IX_BaoTri_Active | Maintenance request | Property, room, status | Severity, SLA, operational impact | Không | Active | Active maintenance/arrivals impact | Multiple status values |
| IX_KhoaVanHanh_Active | `KhoaVanHanhPhong` | Property, room, start, end | Type, reason | Không | Active | Operational block | Interval predicate |
| IX_KiemToan_Run | `PhienKiemToanDem` | Property, business date | Status, started/completed | Có | — | Night Audit run | Unique race |
| IX_KiemToan_Checkpoint | Night Audit checkpoint | Run, status, sequence | Blocker, result | Không | Pending/Blocked | Pending/hard blocker/signoff | Status churn |
| IX_Outbox_Due | Outbox | Status, ready UTC, ID | Event ID, destination, attempt | Không | Pending/Retry | Publish due | Worker contention |
| IX_Inbox_Dedupe | Inbox | Consumer, message ID | Processed UTC, result hash | Có | — | Inbox dedupe | Global message ambiguity |
| IX_TichHop_RetryLease | Integration attempt/job | Status, retry UTC, lease expiry | Owner, type, external ref | Không | Due/lease expired | Retry/lease recovery | Clock skew |
| IX_NgoaiLe_Open | Integration exception | Property, status, created UTC | Type, subject, severity | Không | Open | Exception board | Growth |
| IX_ThamChieuNgoai | External reference | System, type, external ID | Internal ID, version | Có | Active | External mapping | Lifecycle uniqueness |

Các index có cột cached/computed chỉ được tạo sau khi công thức và update path đã được chốt. Index tương tự được hợp nhất khi left-prefix và include đáp ứng cùng query; load test quyết định giữ/tách, tránh catalog trùng.

## 14. Room Assignment Concurrency Design

### 14.1 Bất biến cần bảo vệ

Trong cùng một khách sạn, một phòng vật lý không được có hai phân đoạn phân phòng đang hiệu lực giao nhau theo khoảng nửa mở `[TuNgay, DenNgay)`. Checkout ngày D và check-in ngày D không xung đột. Hold mềm, tentative assignment và confirmed assignment phải có loại/trạng thái rõ; chỉ tập trạng thái chiếm phòng mới tham gia luật loại trừ.

### 14.2 Chiến lược đã chọn

SQL Server không có exclusion constraint cho khoảng ngày. Thiết kế dùng ba lớp đồng thời:

1. Transaction mức `SERIALIZABLE` cho thao tác tạo, đổi, gia hạn hoặc hủy phân phòng.
2. Khóa ứng dụng theo tài nguyên ổn định `Property + Room` trong phạm vi transaction, buộc mọi writer cùng phòng đi qua một hàng đợi logic.
3. Truy vấn phát hiện giao nhau trên `PhanDoanPhanPhong` bằng index IX-05 trước khi ghi; sau đó ghi segment, cập nhật assignment và phát domain event/outbox trong cùng transaction.

`rowversion` trên `PhanPhong`, `KyLuuTru` và room-state bắt xung đột sửa cùng bản ghi nhưng không thay thế khóa khoảng ngày. Không dùng trigger để tự động ngăn overlap vì khó kiểm soát thứ tự khóa, khó kiểm thử deadlock và che giấu quy tắc domain.

### 14.3 Trình tự transaction chuẩn

| Bước | Hành động | Kết quả khi lỗi |
|---|---|---|
| 1 | Chuẩn hóa ngày, Property, Room, trạng thái chiếm phòng | Reject validation trước DB |
| 2 | Mở transaction và lấy application lock của từng phòng theo thứ tự ID tăng dần | Timeout trả conflict có thể retry |
| 3 | Đọc các segment giao nhau còn hiệu lực | Có giao nhau: trả `RoomAssignmentConflict`, không ghi |
| 4 | Kiểm tra rowversion của Stay/Assignment và các điều kiện HK/Maintenance | Stale: yêu cầu refresh |
| 5 | Ghi segment mới hoặc kết thúc segment cũ; ghi history và outbox | Lỗi: rollback toàn bộ |
| 6 | Commit; projection cập nhật bất đồng bộ | UI hiển thị pending nếu projection chưa theo kịp |

Move room khóa cả phòng cũ và mới theo thứ tự xác định để tránh deadlock. Extend stay khóa phòng hiện tại và kiểm tra phần ngày mở rộng. Group assignment chạy theo batch nhỏ; nếu yêu cầu atomic cả đoàn thì khóa danh sách phòng theo thứ tự và có giới hạn kích thước. Retry tối đa có backoff/jitter chỉ áp dụng deadlock, timeout ngắn và optimistic conflict có thể tái đánh giá; không tự retry lỗi nghiệp vụ.

### 14.4 Kiểm thử cạnh tranh bắt buộc

- Hai lễ tân cùng gán một phòng cho hai stay giao ngày: đúng một giao dịch thành công.
- Extend và room move đồng thời; cancel và assign đồng thời; group auto-assign cạnh tranh walk-in.
- Nhiều property không chặn nhau; hai phòng khác nhau không chặn nhau.
- Crash sau ghi assignment nhưng trước outbox phải rollback; crash sau commit phải được outbox phát lại idempotent.

## 15. Room Availability Physical Design

### 15.1 Mô hình nguồn và bucket

`TonPhongKhaDung` là bảng kiểm soát tồn theo grain `(MaKhachSan, MaLoaiPhong, NgayLuuTru)`, không phải báo cáo thuần. Mỗi hàng giữ tổng phòng bán được, out-of-order, out-of-service, allotment, hold, booked, oversell limit và available được chuẩn hóa. Các nguồn nhu cầu chi tiết vẫn nằm ở reservation segment, room block và assignment; bucket là materialized control state có thể đối soát/dựng lại.

| Thành phần | Source of truth | Cách cập nhật |
|---|---|---|
| Capacity | Room/RoomType/effective inventory | Sự kiện master data hiệu lực theo ngày |
| Out of order/service | Room block/maintenance | Transaction nghiệp vụ + outbox |
| Reservation demand | Reservation segment theo room type/date | Command giữ/xác nhận/hủy/đổi ngày |
| Physical assignment | Assignment segment | Không làm tăng booked lần hai; chỉ chuyển bucket khi đổi loại phòng |
| Oversell | Policy hiệu lực theo property/room type/date | Versioned configuration |

### 15.2 Transaction và concurrency

Mọi command làm đổi demand tính danh sách bucket ngày bị tác động, sắp theo `NgayLuuTru` rồi khóa/cập nhật theo thứ tự. Update dùng `rowversion` và điều kiện không vượt `capacity + oversell limit`; thất bại một ngày rollback toàn dải. Reservation dài ngày không giữ transaction trong lúc gọi dịch vụ ngoài. Giữ tạm có TTL và idempotency key; worker hết hạn phải dùng cùng command path.

Không triển khai full event sourcing. Bảng transaction + history/event catalog là nguồn có thể replay; bucket là snapshot kiểm soát. Job reconciliation so sánh bucket với chi tiết, ghi sai lệch, tự sửa chỉ khi policy cho phép và luôn phát audit.

### 15.3 Rebuild và projection

Rebuild chạy theo Property + khoảng ngày trong shadow table/projection generation, đối soát tổng, rồi chuyển generation atomically. Trong rebuild, writer vẫn cập nhật generation hiện hành và phát delta; cutover chỉ xảy ra khi checkpoint bắt kịp. Dashboard đọc `BangTonPhongProjection`; booking command đọc bảng kiểm soát `TonPhongKhaDung`, không dựa vào projection có độ trễ.

## 16. Folio Ledger Physical Design

### 16.1 Nguồn sự thật

`ButToanSoGiaoDich` là sổ cái append-only và là nguồn sự thật số dư. `Folio` giữ `SoDuHienTai`, `TongNo`, `TongCo`, `PhienBanSoDu` như cached summary để kiểm tra nhanh; `SoDuFolioProjection` phục vụ UI/report và có thể rebuild. Khi có khác biệt, ledger thắng, cached summary bị đánh dấu cần đối soát.

### 16.2 Quy tắc ghi sổ

- Một command ghi charge/payment/deposit application/refund/adjustment tạo một hoặc nhiều ledger entry trong cùng transaction với cập nhật cached summary và outbox.
- Entry không update/delete. Sửa sai bằng entry đảo tham chiếu `MaButToanGoc`; điều chỉnh sau đảo là entry mới.
- `KhoaChongTrungNguon` unique theo property ngăn POS, Banquet hoặc retry tạo charge kép.
- Mỗi entry giữ business date, occurred UTC, posted UTC, source module/type/id, folio window, guest/company responsibility, currency gốc, amount gốc, base currency, exchange rate và amount quy đổi.
- Chuyển charge giữa folio/window không sửa entry: tạo cặp transfer-out/transfer-in liên kết bởi `MaLienKetChuyen` và tổng đại số bằng 0 trong cùng currency/base currency.
- Đóng folio yêu cầu transaction kiểm tra ledger balance/version, unresolved authorization, pending POS charge và invoice policy; optimistic token ngăn charge lọt sau kiểm tra.

### 16.3 Đối soát và rebuild

Rebuild summary nhóm ledger hợp lệ theo Folio và currency, kiểm tra reversal chain, transfer pair, source uniqueness và tổng header. Mọi chênh lệch tạo reconciliation case; không âm thầm ghi một số dư bù. Night Audit chốt watermark ledger theo business date nhưng không khóa lịch sử; late posting phải mang ngày nghiệp vụ và lý do theo policy.

## 17. Payment, Deposit and Refund Design

| Aggregate | Phân tách vật lý | Bất biến chính |
|---|---|---|
| Payment | Header `ThanhToan`, attempts `GiaoDichThanhToan`, allocation `PhanBoThanhToan` | Tổng capture thành công trừ void/refund bằng số có thể phân bổ; provider idempotency unique |
| Deposit | Header `DatCoc`, transactions `GiaoDichDatCoc`, applications `PhanBoDatCoc` | Deposit là liability trước khi apply; không coi là revenue/folio credit tùy tiện |
| Refund | Header `HoanTien`, attempts `GiaoDichHoanTien`, allocation `PhanBoHoanTien` | Không vượt số refundable của giao dịch gốc; approval theo threshold |

Authorization, capture, void và settlement là các trạng thái/giao dịch riêng, không gộp thành một cột payment status duy nhất. Token thẻ do PSP quản lý; database chỉ lưu token alias, brand, last4, expiry đã giảm nhạy cảm và provider customer reference. Tuyệt đối không lưu PAN/CVV.

Giao dịch tiền mặt phải gắn cashier shift/till external reference từ Accounting/Cashiering boundary. Ngoại tệ giữ amount/currency gốc và tỷ giá tại thời điểm nghiệp vụ; không tính lại lịch sử khi tỷ giá đổi. Refund và chargeback liên kết giao dịch gốc, tạo ledger/outbox atomically; call PSP thực hiện ngoài DB transaction bằng state machine + idempotency.

## 18. Outbox, Inbox and Integration Design

### 18.1 Outbox

`ThongDiepOutbox` được ghi cùng transaction với aggregate. Publisher claim batch nhỏ bằng trạng thái/lease, phát message có `EventId`, `OccurredUtc`, `AggregateType/Id/Version`, `SchemaName/Version`, `CorrelationId`, `CausationId`, tenant và payload. Thành công lưu published UTC/broker message ID; lỗi tăng attempt, phân loại retryable, next attempt và dead-letter reason.

### 18.2 Inbox và idempotency

`ThongDiepInbox` nhận message theo unique `(Consumer, MessageId)`, lưu payload tối thiểu, schema version, receive/process state và result digest. Handler cập nhật domain + inbox processed marker + outbox mới trong một local transaction. `BanGhiChongTrung` bảo vệ command/API retry ngoài message bus; giữ request hash và result reference để cùng key nhưng payload khác bị từ chối.

### 18.3 Payload, retention và versioning

- JSON nhỏ lưu `nvarchar(max)` có check JSON; payload lớn nén thành `varbinary(max)` và ghi compression algorithm.
- PII nhạy cảm được loại bỏ hoặc mã hóa envelope; event ưu tiên ID/reference thay vì snapshot hồ sơ khách.
- Schema additive trong cùng major version; breaking change phát major mới và consumer song song trong cửa sổ chuyển đổi.
- Outbox published và inbox processed chuyển archive theo tháng; idempotency record giữ ít nhất bằng retry horizon tối đa cộng thời gian điều tra.
- Không dùng distributed transaction giữa Front Office, POS, Accounting, CRM hoặc PSP; dùng saga/process manager và compensation có audit.

## 19. Audit Store Design

`platform_audit.NhatKyKiemToan` là audit store dùng chung nền tảng nhưng có `MaKhachSan`, module, actor, subject và correlation để Front Office truy vết. Đây không thay thế domain history: history giải thích diễn tiến nghiệp vụ; audit giải thích ai/ứng dụng nào đã làm gì và thay đổi trường nào.

| Nhóm cột | Nội dung |
|---|---|
| Identity/time | `MaNhatKy bigint identity`, partition date, event UTC, business date tùy chọn |
| Actor | User/service/client/session/device/IP đã chuẩn hóa; không lưu secret |
| Action/subject | Module, action code, aggregate type/id/version, property |
| Trace | Correlation, causation, command/message IDs |
| Change | Changed field names, before/after redacted values hoặc digest; reason/approval reference |
| Integrity | Record hash, previous-chain hash theo partition/batch, ingestion source |

Audit append-only, partition theo tháng, page compression sau khi partition đóng. Giá trị PII nhạy cảm mặc định chỉ ghi `changed=true`, masked value hoặc keyed hash; không sao chép ciphertext nếu không cần điều tra. Export pháp lý có watermark, approval và audit của chính hoạt động đọc/export. Quyền insert qua service principal; người vận hành ứng dụng không có UPDATE/DELETE. Tamper evidence bằng hash chain/batch manifest và bản sao bất biến ngoài database theo retention policy.

## 20. PII and Encryption Design

### 20.1 Phân loại

| Mức | Ví dụ | Bảo vệ vật lý |
|---|---|---|
| Restricted | Số giấy tờ, dữ liệu visa, token nhạy cảm, chữ ký, dữ liệu sức khỏe/yêu cầu đặc biệt | Application envelope encryption, khóa ngoài DB, audit đọc, không vào projection/log |
| Confidential | Email, điện thoại, địa chỉ, ngày sinh, quốc tịch, guest notes | Mã hóa theo field khi cần, masking theo role, search token/hash riêng |
| Internal | Company/travel-agent contract reference, rate eligibility | TDE + quyền hàng/cột nghiệp vụ |
| Public/reference | Room type public name, lookup code | TDE baseline, RBAC chuẩn |

| Bảng/phạm vi | Cột/dữ liệu | Phân loại | Mã hóa | Masking | Audit khi đọc | Retention |
|---|---|---|---|---|---|---|
| `DatPhong`, `KyLuuTru`, `KhachLuuTru` | Tên khách snapshot | Confidential | TDE; field encryption nếu policy yêu cầu | Tên rút gọn theo role | Có với export/bulk view | Theo stay + nghĩa vụ pháp lý |
| Snapshot liên hệ | Số điện thoại | Confidential | Envelope encryption; keyed hash tìm kiếm | Chỉ hiện 3–4 số cuối | Có | Theo consent/retention CRM |
| Snapshot liên hệ | Email | Confidential | Envelope encryption; keyed hash tìm kiếm | Che local-part | Có | Theo consent/retention CRM |
| Hồ sơ giấy tờ tham chiếu | Số giấy tờ | Restricted | Envelope encryption + blind index khi cần equality | Chỉ hiện ký tự cuối | Bắt buộc | Theo luật lưu trú |
| Attachment store ngoài DB | Ảnh giấy tờ | Restricted | Object encryption + signed short-lived access; DB chỉ lưu reference/hash | Không preview nếu thiếu quyền | Bắt buộc | Theo luật, purge có manifest |
| Attachment store ngoài DB | Chữ ký | Restricted | Object encryption; integrity hash | Watermark/ẩn mặc định | Bắt buộc | Theo hợp đồng/pháp lý |
| Guest service/Stay | Ghi chú riêng tư, complaint | Restricted/Confidential theo nội dung | Envelope encryption theo field | Tóm tắt an toàn | Bắt buộc | Tối thiểu cần thiết |
| `DoThatLac` và attachment | Lost & Found | Confidential; có thể Restricted | Mã hóa field/attachment | Che thông tin người nhận | Bắt buộc | Theo policy tài sản thất lạc |
| Payment tables | Provider/token/merchant reference, last4 | Confidential | Tokenization tại PSP; TDE cho reference | Last4 được phép | Có với bulk/export | Theo PCI/finance; không PAN/CVV |
| AI interaction/reference | Prompt/context/output | Restricted nếu chứa guest data | Redaction trước gửi; encrypted store riêng nếu buộc lưu | Không hiển thị raw mặc định | Bắt buộc | Ngắn, purpose-specific |
| Inbox/Outbox | Integration payload | Theo trường nhạy cảm nhất | Minimize + envelope/compressed encryption | Không hiển thị raw trong support UI | Bắt buộc | Retry horizon + điều tra |
| `NhatKyKiemToan` | Before/after/diff payload | Restricted nếu chứa PII | Redacted diff + encrypted blob khi ngoại lệ | Masked | Bắt buộc, kể cả break-glass | Theo audit/legal hold |

### 20.2 Lựa chọn cho WPF + EF Core

TDE bảo vệ data/log/backup at rest và là baseline nhưng không chống DBA hoặc query có quyền. Phương án chính cho Restricted PII là mã hóa envelope tại application/service layer: mỗi giá trị dùng data-encryption key, khóa được wrap bởi KMS/HSM, lưu ciphertext + key version + nonce/tag; hỗ trợ rotation không làm đổi domain identity.

Always Encrypted chỉ dùng chọn lọc cho trường có yêu cầu tách DBA mạnh và mẫu truy vấn tương thích. Randomized encryption cho giá trị không cần equality; deterministic encryption làm lộ pattern nên chỉ sau threat review. Secure enclave không là giả định bắt buộc vì tăng phụ thuộc driver, provisioning và vận hành desktop. EF Core converter/interceptor phải được kiểm thử, nhưng domain/application không nhận plaintext ngoài use case được cấp quyền.

Tìm email/phone/document dùng normalized keyed hash/search token, không dùng deterministic ciphertext làm index mặc định. Dynamic Data Masking chỉ là defense-in-depth cho ad-hoc support, không phải boundary bảo mật. Row-Level Security có thể áp dụng theo `MaKhachSan` cho reporting/support connection; application connection vẫn phải truyền tenant context và mọi composite key/FK tiếp tục chống cross-tenant.

### 20.3 Lưu giữ và quyền riêng tư

Retention theo loại hồ sơ và pháp luật từng quốc gia. Anonymization/pseudonymization chỉ chạy khi hết nghĩa vụ folio/invoice/audit; legal hold chặn purge. Projection, search index, backup và archive phải tham gia quy trình erasure tương ứng. Báo cáo mặc định masked; export PII cần purpose, approval, watermark và audit.

## 21. Lookup and Enum Design

### 21.1 Quyết định

Trạng thái tham gia workflow, tích hợp, báo cáo hoặc cấu hình theo property được lưu bằng typed lookup table; enum chỉ là representation trong application và phải map tới canonical code. Cờ kỹ thuật thật sự nhị phân dùng `bit`. Giá trị hiếm, chỉ nội bộ một thuật toán và không cần dữ liệu lịch sử có thể dùng check constraint thay vì tạo lookup chung mơ hồ.

### 21.2 14 typed lookup chính thức

| Bảng lookup | Scope | Nội dung | Ownership/ghi chú |
|---|---|---|---|
| `NguonDatPhong` | Property | Nguồn đặt phòng | FO quản lý; external mapping riêng |
| `KenhDatPhong` | Property | Kênh Direct/OTA/GDS/Corporate/Walk-in | FO quản lý |
| `LyDoHuyDatPhong` | Property | Lý do hủy/no-show | FO quản lý; có reporting category |
| `PhuongThucThanhToan` | Property/legal entity | Tiền mặt, thẻ, chuyển khoản, city ledger | FO cache mapping; Accounting quyết định account mapping |
| `KhoanThu` | Property/legal entity | Mã khoản thu/charge code | FO dùng; tax/account mapping do Accounting cung cấp |
| `LoaiVeSinh` | Property | Loại vệ sinh | Housekeeping |
| `MucUuTienBuongPhong` | Property | Mức ưu tiên buồng | Housekeeping |
| `NhomBaoTri` | Property | Nhóm bảo trì | Maintenance |
| `LyDoChuyenPhong` | Property | Lý do room move/upgrade | Stay |
| `LyDoGhiDe` | Property | Lý do override | Dùng kèm approval/audit |
| `LoaiCongViec` | Property/platform | Loại Task | Task Center |
| `LoaiNgoaiLeTichHop` | Platform | Phân loại integration exception | Integration |
| `LoaiChanKiemToanDem` | Property/platform | Night Audit blocker type | Night Audit |
| `LoaiDichVuKhach` | Property | Loại dịch vụ khách | Guest Service |

Mỗi lookup có `Ma...` canonical PK, `Ten...`, `MoTa`, `ThuTu`, `CoHieuLuc`, `TuNgay`, `DenNgay`, `MaKhachSan` khi scoped, catalog version và audit metadata. Không hard-delete giá trị đã được tham chiếu. Mã thuế không nhân bản: Accounting/Tax ownership cung cấp external reference và snapshot pháp lý cần thiết. Workflow status ổn định có thể là code enum governed bằng check/catalog contract; không tạo một bảng lookup chung cho mọi loại.

## 22. Read Model and Projection Design

### 22.1 Catalog 13 projection

| Projection | Grain | Nguồn | SLA freshness | Rebuild |
|---|---|---|---|---|
| `BangDenHomNayProjection` | Property + business date + reservation/stay | Reservation, Stay, Room | ≤ 5 giây | Theo date range |
| `BangDiHomNayProjection` | Property + date + stay | Stay, Folio, HK | ≤ 5 giây | Theo date range |
| `BangKhachDangLuuTruProjection` | Property + stay | Stay, Guest, Room, Folio | ≤ 5 giây | Theo active stays |
| `BangPhongProjection` | Property + room | Room, assignment, HK, maintenance | ≤ 3 giây | Toàn property |
| `DongThoiGianPhongProjection` | Property + room + date segment | Reservation, stay, block | ≤ 5 giây | Window ngày |
| `BangTonPhongProjection` | Property + room type + date | Availability control | ≤ 3 giây | Window ngày |
| `SoDuFolioProjection` | Property + folio + currency | Ledger | ≤ 2 giây | Folio hoặc property/date |
| `BangThuNganProjection` | Property + cashier queue item | Payment/deposit/refund | ≤ 3 giây | Active window |
| `BangDonPhongProjection` | Property + room/date | HK, room, stay | ≤ 3 giây | Property/date |
| `BangBaoTriProjection` | Property + maintenance request | Maintenance, Room | ≤ 5 giây | Active requests |
| `BangKhachDoanProjection` | Property + group/date | Group, reservation, stay, folio | ≤ 10 giây | Group/date |
| `BangNhiemVuProjection` | Property + task | Task, notification | ≤ 3 giây | Active tasks |
| `BangKiemToanDemProjection` | Property + business date + check | Night Audit, ledger, ops | ≤ 5 giây | Business date |

### 22.2 Cơ chế cập nhật

Projection handler consume event qua inbox idempotent và upsert chỉ khi `SourceVersion` mới hơn. Multi-source projection giữ checkpoint riêng từng stream hoặc vector metadata trong checkpoint store; UI thấy `FreshnessUtc` và `IsStale`. Không dùng projection làm điều kiện quyết định tiền, tồn phòng hoặc trạng thái chuyển tiếp nếu command store có dữ liệu chuẩn.

Rebuild tạo generation mới, đọc snapshot/watermark, replay đến khi bắt kịp rồi switch generation. Trong thời gian đó generation cũ vẫn phục vụ. Lỗi handler không chặn transaction nguồn; alert theo lag, dead-letter và số record không reconcile. Projection không chứa Restricted PII; tên/email/phone chỉ ở dạng masked/display-safe cần thiết.

## 23. Partition, Archive and Compression

| Nhóm | Partition key | Hot retention | Archive/Compression |
|---|---|---|---|
| Audit | Tháng theo `NgayPhanVung` UTC | 12–24 tháng theo policy | Page compression; switch sang archive, immutable copy |
| Folio ledger | Tháng theo business/posting date khi đủ lớn | Tối thiểu kỳ vận hành + pháp lý | Page compression partition đóng; không tách khỏi folio index cần thiết |
| Outbox/Inbox | Tháng theo created/received UTC | Published/processed 3–6 tháng | Archive metadata; payload rút gọn theo policy |
| Domain event/history | Tháng theo occurred UTC | 12 tháng hoặc theo điều tra | Page compression; giữ checkpoint |
| Projection | Không partition mặc định; partition theo date với timeline lớn | Cửa sổ vận hành | Drop/rebuild generation, không archive như source |
| Payment/Invoice | Theo legal/business date chỉ khi volume chứng minh | Theo luật tài chính | Archive read-only nhưng vẫn truy xuất được |

Không partition bảng nhỏ vì tăng độ phức tạp PK/unique/index. Kích hoạt partition sau benchmark volume, maintenance window và edition capability. Filegroup/backup strategy tách hot, warm, archive; restore drill phải chứng minh có thể phục hồi point-in-time và đọc chứng từ cũ. Row/page compression được benchmark CPU; columnstore chỉ dành analytical copy/reporting, không đặt trên OLTP command tables mặc định.

Archive là chuyển trạng thái lưu trữ có manifest, row count, checksum và audit, không phải xóa tùy tiện. Purge chạy batch nhỏ, resumable, theo legal hold và xóa đồng bộ các projection/search artifact. Backup encryption, key escrow và restore test là bắt buộc.

## 24. EF Core Mapping Plan

### 24.1 DbContext boundary

Một Front Office write DbContext có mapping theo schema/bounded context nhưng transaction nội bộ vẫn dùng chung SQL Server database. Projection dùng DbContext/read connection riêng, no-tracking mặc định. Audit và integration có mapping/service boundary riêng để quyền DB và migration có thể tách khi trưởng thành. Không tạo navigation trực tiếp sang Entity của CRM, POS, Accounting, Inventory hoặc Banquet; chỉ giữ external ID/value object/reference.

### 24.2 Mapping rules

| Chủ đề | Quy tắc mapping |
|---|---|
| Table/schema | Explicit `ToTable`; không dựa pluralization/convention |
| Keys | Explicit PK/alternate key/composite tenant key; GUID generated never hoặc UUID service rõ ràng |
| Money | Owned/value conversion cho amount + currency nhưng map thành cột rõ; precision bắt buộc |
| Enum/status | Map canonical string; FK lookup; unknown code phải được xử lý forward-compatible |
| Concurrency | `rowversion` concurrency token trên mutable aggregate/control row |
| Date/time | `DateOnly` ↔ `date`; UTC guard cho `datetime2`; cấm local/unspecified time |
| JSON | Chỉ metadata/payload; version field bắt buộc; không chôn quan hệ/query trọng yếu trong JSON |
| Encryption | Value converter/service ở boundary, ciphertext columns + blind index; không log plaintext parameter |
| Owned collection | Table riêng khi cần history/query; không serialize aggregate child quan trọng vào JSON |
| Delete behavior | Restrict/no action mặc định; cascade whitelist rõ |
| Query filter | Tenant/property filter là defense-in-depth, không thay authorization; tránh filter ẩn làm sai job cross-property |
| Index | Khai báo explicit để migration drift review được; filtered index có annotation/provider-specific review |

SaveChanges interceptor chỉ gắn audit metadata/outbox từ domain events đã tạo, không chứa nghiệp vụ chuyển trạng thái. Bulk operation, raw SQL và projection updater phải có contract riêng, test tenant isolation và không vượt aggregate invariants.

### 24.3 Transaction boundary

Một application command thay đổi một aggregate chính, các owned child, control row liên quan và outbox trong local transaction. Các workflow đa module dùng saga. Execution strategy retry không bọc PSP/network call; idempotency key tồn tại trước retry. Isolation mặc định Read Committed Snapshot cho đọc, với transaction đặc biệt tại phân phòng/availability dùng isolation và khóa đã mô tả.

### 24.4 Ma trận Aggregate mapping

| Aggregate | Entity/Table | Mapping strategy | Concurrency | Delete behavior | Owned types | Special config |
|---|---|---|---|---|---|---|
| Reservation | `DatPhong` + khách/segment/guarantee/history | Root + private collections | Root rowversion | NoAction | Date range, money snapshot | Alternate business number, status code |
| Stay | `KyLuuTru` + guest/assignment/history | Root + collections; segment table riêng | Root/assignment rowversion | NoAction | Stay period | Interval locking contract |
| Room Master | `Phong`, `LoaiPhong` | Root riêng | Rowversion | NoAction | Display attributes | Property alternate keys |
| Availability | `TonPhongKhaDung` | Hot control aggregate | Rowversion bắt buộc | NoAction | Bucket quantities | Composite grain/ordered updates |
| Folio | `Folio`, windows, ledger/allocation | Root + append-only collections | Header rowversion + ledger sequence | NoAction | Money/currency/source ref | Không cascade, không update entry |
| Payment | `ThanhToan` + attempts/allocation | Root + state-machine children | Root rowversion | NoAction | Masked method | Provider/idempotency alternate keys |
| Deposit | `DatCoc` + transactions/allocation | Root + append-only allocation | Root rowversion | NoAction | Money | Liability reconciliation |
| Refund | `YeuCauHoanTien` + approval/attempt | Root + workflow children | Root rowversion | NoAction | Money/approval | Original payment ref |
| Cashier Shift | `CaThuNgan` + tender/variance | Root + collections | Rowversion | NoAction | Tender summary | Filtered open uniqueness |
| Invoice reference | FO snapshot/reference tables | External reference, không Accounting navigation | Version nguồn | NoAction | Legal snapshot | Accounting owns Invoice |
| Housekeeping | Session/task/inspection/report tables | Roots theo lifecycle | Rowversion | NoAction | Checklist values | Room external/master ref |
| Maintenance | Request/work order/log | Root + children | Root rowversion | NoAction | SLA/impact | Operational block coordination |
| Group | `HoSoDoan` + members/block/routing | Root + collections | Root rowversion | NoAction | Group code | Banquet/CRM refs logic |
| Night Audit | Business date + run/check/blocker | Roots riêng, append results | Rowversion/run sequence | NoAction | Check result | Unique property/date |
| Task/Guest Service | Root + history | Root/children | Rowversion | NoAction | Subject ref polymorphic | Không FK polymorphic |

Không dùng lazy loading, cascade delete rộng, generic configuration che precision/schema, string enum không governance, owned collection lớn, `AutoInclude` không kiểm soát hoặc global soft-delete filter cho ledger/audit/event/integration. Backing field/private collection chỉ dùng để bảo vệ invariant và phải có materialization test; shadow property chỉ cho metadata kỹ thuật có ownership rõ.

## 25. Migration Strategy

### 25.1 Nguyên tắc phát hành

- Migration do pipeline tạo script có review; application production không tự động migrate khi khởi động.
- Dùng expand–migrate/backfill–switch–contract. Mọi thay đổi phải tương thích ít nhất hai phiên bản ứng dụng trong rolling deployment.
- DDL nặng tách khỏi deploy ứng dụng, có preflight dung lượng/log/lock, timeout, kill switch và rollback/forward-fix plan.
- Mỗi migration có owner, mục tiêu, ảnh hưởng khóa, ước lượng thời gian, backup/restore point và verification query đã duyệt; báo cáo này không cung cấp SQL thực thi.

### 25.2 Lộ trình baseline

| Wave | Phạm vi | Exit criteria |
|---|---|---|
| M0 | Schema, lookup, security principals, migration history | Empty DB deploy/rollback rehearsal thành công |
| M1 | Room/Reservation/Stay/Availability | Concurrency và reconciliation test đạt |
| M2 | Folio/Ledger/Payment/Deposit/Refund/Invoice | Financial invariants, idempotency và audit đạt |
| M3 | HK/Maintenance/Guest Service/Group/Night Audit/Task | Workflow integration test đạt |
| M4 | Outbox/Inbox/Audit/Projection | Lag/rebuild/dead-letter/retention test đạt |
| M5 | Performance/partition/archive hardening | Load, failover, restore, security gate đạt |

Backfill có watermark, batch nhỏ, resumable và checksum. Cột mới ban đầu nullable/default metadata-only khi phù hợp, backfill, validate, rồi mới enforce not null. Rename dùng cột song song hoặc compatibility view trong cửa sổ chuyển đổi; không rename/drop phá vỡ ngay. Không chỉnh sửa migration đã phát hành; corrective migration mới có ADR/change record.

## 26. Seed Data Strategy

Seed chỉ gồm canonical lookup, system policy tối thiểu và schema/version metadata. Property-specific room, rate, tax, payment method mapping, document sequence và user data phải qua configuration/onboarding workflow, không nằm trong migration seed.

| Loại seed | Định danh | Cập nhật |
|---|---|---|
| Typed lookup platform | Canonical code tự nhiên ổn định | Upsert versioned; không xóa code đã dùng |
| System actor/source | GUID cố định được quản lý trong catalog | Chỉ thêm; rotate credential không đổi identity |
| Default policy template | GUID xác định + version | Clone vào property khi onboarding; không âm thầm đổi property hiện hữu |
| Projection/checkpoint | Không seed business row | Tạo rỗng và rebuild |

Seed phải deterministic, idempotent, có checksum và test trên database mới lẫn database đã có dữ liệu. Tên hiển thị đa ngôn ngữ dùng resource/culture table hoặc localization service; canonical code không phụ thuộc ngôn ngữ.

## 27. Database Security

### 27.1 Principal và quyền

| Principal | Quyền tối thiểu |
|---|---|
| Front Office command app | Execute/read-write qua schema cần thiết; không DDL, không audit read rộng |
| Projection worker | Read event/source được cấp + write `fo_projection`; không sửa ledger |
| Integration worker | Read/write inbox/outbox và gọi command contract; không đọc PII không cần |
| Reporting | Read projection/reporting views masked; không đọc write tables mặc định |
| Audit investigator | Read audit theo approval; không update/delete |
| Migration principal | DDL có thời hạn qua pipeline; không dùng runtime |
| Support | Stored diagnostic/view đã giới hạn property và mask; session được audit |

Authentication dùng managed identity/service account hoặc secret vault; không nhúng connection string vào WPF client. WPF gọi application service/API, không kết nối SQL Server bằng quyền nghiệp vụ trực tiếp. Network private, TLS bắt buộc, certificate validation bật, firewall allowlist và database backup mã hóa.

### 27.2 Tenant và đặc quyền

Mọi request đặt tenant/property context từ identity đáng tin, không nhận thuần từ UI. Composite uniqueness/FK và optional RLS giảm nguy cơ cross-property. Break-glass account tắt mặc định, MFA/vault, time-bound, alert và audit. Tách nhiệm vụ giữa migration, DBA, security và application operation. Database Vulnerability Assessment, dependency patching và quyền hiệu lực được review định kỳ.

## 28. Database Test Plan

| Nhóm | Ca kiểm thử bắt buộc | Tiêu chí |
|---|---|---|
| Schema | Tạo DB mới từ 0; nâng cấp từng baseline; drift detection | Cùng schema hash mong đợi |
| Constraint | Null, length, precision, check, unique, FK, tenant mismatch | Mọi dữ liệu sai bị chặn đúng lớp |
| Room assignment | Race assign/move/extend/cancel/group | Không overlap; không deadlock kéo dài |
| Availability | Concurrent book/cancel/expire hold, multi-day rollback | Không âm/vượt policy; bucket reconcile = detail |
| Folio | Post/reverse/transfer/close race | Ledger cân; cached/projection rebuild trùng khớp |
| Payment | Retry PSP callback, out-of-order event, duplicate capture/refund | Exactly-once business effect |
| Deposit | Receive/apply/unapply/refund/forfeit | Liability và folio allocation đúng |
| Invoice | Issue/adjust/cancel, sequence collision | Legal uniqueness và immutable issue data |
| Outbox/Inbox | Crash trước/sau commit, duplicate/redelivery, poison event | Không mất event; handler idempotent |
| Projection | Lag, handler crash, full/partial rebuild, generation switch | UI không đọc generation nửa vời |
| Audit | Create/update/status/PII view/export | Đủ actor/trace, không rò plaintext |
| Security | Role matrix, cross-property, injection, backup access | Least privilege; tenant isolation |
| Encryption | Key rotate, key unavailable, blind-index collision | Fail closed; dữ liệu phục hồi được |
| Retention | Legal hold, archive, purge, restore archive | Không xóa sai; manifest/checksum đúng |
| Performance | Arrival board, room rack, folio statement, night audit | Đạt SLO ở dữ liệu mục tiêu |
| Resilience | Deadlock, failover, network interruption, log full | Retry đúng loại; recovery không mất commit |
| Backup | Point-in-time restore và disaster recovery drill | Đạt RPO/RTO được duyệt |

Test dataset gồm single/multi-property, múi giờ và DST khác nhau, Unicode tiếng Việt/quốc tế, multi-currency, stay dài ngày, khách đoàn lớn, folio nhiều nghìn dòng và volume 3–5 năm. Performance gate dùng Query Store baseline, logical reads, CPU, duration, tempdb/log growth và blocking; không chỉ đo thời gian trung bình.

### 28.1 Catalog test có thể bàn giao

| Mã | Setup | Hành động | Expected | Rule liên quan |
|---|---|---|---|---|
| DB-001 | Có reservation code tại Property A | Ghi cùng code tại A/B | A bị từ chối, B được phép | Unique có Property scope |
| DB-002 | Có Root và Child | Ghi Child sai Root/tenant | FK/tenant guard từ chối | Referential integrity |
| DB-003 | Hàng có date/amount/rate | Ghi end≤start, amount<0, rate≤0 | Check từ chối | CK-01/03/16 |
| DB-004 | Hai session đọc cùng rowversion | Cả hai update | Chỉ update đầu thành công | Optimistic concurrency |
| DB-005 | Phòng trống | Hai transaction gán khoảng giao nhau | Đúng một thành công | Room overlap |
| DB-006 | Có source charge | Post lại cùng source key | Không tạo entry thứ hai | Source uniqueness |
| DB-007 | Có idempotency key + request hash | Gửi lại cùng/khác payload | Cùng payload trả kết quả cũ; khác payload bị reject | Durable idempotency |
| DB-008 | Folio có posted entry | Thử update/delete trực tiếp qua app role | Bị cấm; chỉ reversal được phép | Append-only |
| DB-009 | Entry gốc chưa đảo | Tạo reversal rồi thử đảo lần hai | Chuỗi hợp lệ; duplicate bị chặn | Reversal chain |
| DB-010 | Payment refundable 100 | Yêu cầu refund tổng 101 đồng thời | Không vượt 100 | Refund invariant |
| DB-011 | Deposit unallocated 100 | Hai allocation tổng 120 | Một/batch bị rollback | Deposit allocation |
| DB-012 | Có Business Date active | Mở ngày active thứ hai | Unique filtered từ chối | One active date |
| DB-013 | Cashier có ca Open | Mở ca thứ hai | Bị từ chối theo policy | One open shift |
| DB-014 | Command tạo aggregate/event | Crash trước và sau commit | Trước: không cả hai; sau: cả aggregate/outbox | Outbox atomicity |
| DB-015 | Inbox đã có Consumer+MessageId | Redeliver nhiều lần | Một business effect | Inbox dedupe |
| DB-016 | Job có lease | Hai worker claim; lease hết hạn | Một owner; worker khác reclaim sau expiry | Retry lease |
| DB-017 | Source có watermark/event | Drop generation và rebuild | Count/checksum/balance giống source | Projection rebuild |
| DB-018 | User thường/PII reader/break-glass | Query/export PII | Mask/allow đúng role; mọi đọc nhạy cảm có audit | Encryption/masking |
| DB-019 | Baseline N và N+1 | Upgrade rồi rollback/forward-fix rehearsal | Không mất dữ liệu; app N/N+1 tương thích | Expand-contract |
| DB-020 | Bucket capacity gần giới hạn | Concurrent hold/confirm/release nhiều ngày | Không âm/vượt policy; reconcile đúng | Availability hot bucket |
| DB-021 | Night Audit chưa/chạy đồng thời | Hai worker start cùng property/date | Một run active; checkpoint không xử lý kép | Night Audit race |
| DB-022 | Cross-property IDs hợp lệ riêng lẻ | Ghép child A vào root B | Bị chặn | Tenant isolation |

## 29. Architecture Decision Records

| ADR | Quyết định | Trạng thái | Hệ quả chính |
|---|---|---|---|
| ADR-DB-001 | Schema theo bounded context trong một SQL Server database | Accepted | Ownership rõ; vẫn hỗ trợ local transaction |
| ADR-DB-002 | Tên schema English, bảng/cột Vietnamese không dấu | Accepted | Nhất quán hệ thống hiện tại và toolchain |
| ADR-DB-003 | GUID tuần tự cho aggregate, bigint identity cho append-only | Accepted | Tránh fragmentation nhưng vẫn tích hợp phân tán |
| ADR-DB-004 | Ledger Folio append-only là nguồn sự thật | Accepted | Sửa sai bằng reversal; cần reconciliation |
| ADR-DB-005 | Cached Folio summary + rebuildable projection | Accepted | Đọc nhanh, phải giám sát drift |
| ADR-DB-006 | Room assignment dùng application lock + serializable interval check | Accepted | Chính xác trên SQL Server; writer phải đi qua một contract |
| ADR-DB-007 | Availability dùng daily control bucket + detail reconciliation | Accepted | Booking nhanh; cần ordered locking/rebuild |
| ADR-DB-008 | Outbox/inbox và idempotency, không distributed transaction | Accepted | At-least-once transport, exactly-once business effect |
| ADR-DB-009 | Typed lookup cho workflow status | Accepted | Mở rộng/config được; code là contract |
| ADR-DB-010 | Projection tách khỏi source, có generation/checkpoint | Accepted | UI nhanh; eventual consistency hiển thị được |
| ADR-DB-011 | TDE baseline + application envelope encryption cho Restricted PII | Accepted | Cần KMS, rotation, blind index và audit read |
| ADR-DB-012 | No cascade mặc định trên nghiệp vụ/tài chính | Accepted | Tránh mất lịch sử; xóa có workflow |
| ADR-DB-013 | Audit dùng store nền tảng append-only, tamper-evident | Accepted | Quyền/retention tách biệt |
| ADR-DB-014 | Expand-contract migration, không auto-migrate production | Accepted | Phát hành an toàn hơn, pipeline phức tạp hơn |
| ADR-DB-015 | Không full event sourcing | Accepted | OLTP quen thuộc; event/history đủ rebuild control/projection |
| ADR-DB-016 | Không FK vật lý xuyên module | Accepted | Giảm coupling; cần external reference/reconciliation |
| ADR-DB-017 | WPF không kết nối SQL trực tiếp | Accepted | Bảo mật và domain boundary tốt hơn; cần application API/service |

Mỗi ADR khi triển khai phải bổ sung owner, ngày, context, alternatives, benchmark/evidence và link migration/test. Thay đổi một quyết định Accepted cần ADR thay thế, không chỉnh lịch sử âm thầm.

### 29.1 Hồ sơ 15 ADR bàn giao bắt buộc

| ADR | Trạng thái | Bối cảnh | Lựa chọn | Phương án bị loại | Lý do | Hậu quả | Rủi ro | Xem xét lại |
|---|---|---|---|---|---|---|---|---|
| DB-ADR-001 Schema | Accepted | Modular monolith, nhiều owner | 14 schema theo context/platform | Một `dbo`; database/context riêng ngay | Quyền và ownership rõ, vẫn local transaction | Migration/permission theo schema | Quá nhiều boundary | Trước M0 |
| DB-ADR-002 GUID | Accepted | ID tạo trước DB/cross-module | App-generated UUID tuần tự/UUIDv7; bigint cho append | Random GUID clustered; DB-only identity cho Root | Offline-safe, giảm fragmentation | Cần kiểm byte order | Runtime UUID khác SQL order | Benchmark M0 |
| DB-ADR-003 Clustered | Accepted | PK GUID không luôn phù hợp | Chọn theo access/growth; append dùng bigint | Luôn clustered PK | Giảm split/contention | Có PK nonclustered | Thêm surrogate complexity | Khi có volume thật |
| DB-ADR-004 Assignment overlap | Accepted | SQL Server thiếu exclusion | App lock room + Serializable + conflict query | Unique-only; trigger-only; precheck-only | Bảo vệ interval race | Writer phải cùng contract | Deadlock/timeout | Load test M1 |
| DB-ADR-005 Availability | Accepted | Booking cần hot consistent bucket | Daily control bucket + detail/reconcile | Projection-only; full event sourcing | Nhanh và rebuild được | Ordered multi-day transaction | Hot row contention | Load test M1 |
| DB-ADR-006 Folio ledger | Accepted | Tài chính cần audit/reversal | Append-only ledger + versioned cache/projection | Mutable charge rows; balance-only | Truy vết, idempotent | Reversal/reconcile phức tạp | Ledger growth | Trước M2 |
| DB-ADR-007 Money | Accepted | Multi-currency/precision | decimal(19,4)+currency; rate decimal(19,8) | money/float/double | Chính xác và portable | Phải round theo policy | Rounding mismatch | Finance signoff M2 |
| DB-ADR-008 Business Date | Accepted | Property day khác system time | `date` persisted, Property-owned lifecycle | Suy từ UTC khi query | Night Audit ổn định | Phải truyền explicit | Sai timezone/DST | Ops signoff M1 |
| DB-ADR-009 Outbox/Inbox | Accepted | Không distributed transaction | Durable outbox/inbox/idempotency | In-memory retry; 2PC | Không mất event, replay được | At-least-once transport | Payload/retention growth | M4 chaos test |
| DB-ADR-010 Audit | Accepted | Technical audit dùng chung | Append-only platform store, diff/digest, archive | History cho mọi Root; full snapshot luôn | Tách domain history, kiểm soát dung lượng | Tool đọc riêng | Tamper/PII duplication | Security M4 |
| DB-ADR-011 PII | Accepted | WPF+EF+SQL, cần tách DBA | TDE + app envelope + token/hash; AE chọn lọc | TDE-only; AE đại trà | Thực dụng, rotate/search được | KMS/converter/ops | Key loss/leak plaintext log | Threat model M0/M2 |
| DB-ADR-012 Read Model | Accepted | Dashboard/timeline tải cao | Physical projections, checkpoint/generation | Join view nhiều bảng; projection command source | Tốc độ, rebuild/scale | Eventual consistency | Stale/lag | Load test M4 |
| DB-ADR-013 History | Accepted | Tránh bảng lịch sử máy móc | Chỉ history có nghĩa nghiệp vụ; kỹ thuật vào Audit | History cho mọi aggregate | Ít trùng và rõ mục đích | Cần decision từng lifecycle | Thiếu bằng chứng nếu phân loại sai | Domain review mỗi wave |
| DB-ADR-014 Cross-module FK | Accepted | CRM/Accounting/POS ownership độc lập | Logical external reference, không FK vật lý | FK xuyên module; duplicate master | Giảm coupling/tách module | Cần reconciliation | Orphan reference tạm thời | Integration review M2/M4 |
| DB-ADR-015 Migration | Accepted | Rolling deploy/dữ liệu production | Context waves + expand-contract + pipeline script | One big migration; startup auto-migrate | Backward-compatible, kiểm soát lock | Nhiều bước/feature flag | Backfill kéo dài | Mỗi release |

## 30. Open Questions

1. Property có dùng chung một database hay sharding theo khách sạn/chuỗi ở quy mô nào; SLO, RPO, RTO mục tiêu là bao nhiêu?
2. Quốc gia triển khai đầu tiên, quy định hóa đơn điện tử, retention PII và data residency cụ thể?
3. Accounting là source of truth nào cho cashier shift, city ledger, tax mapping, exchange rate và journal acceptance?
4. PSP nào được chọn; hỗ trợ tokenization, webhook idempotency, partial capture/refund và settlement file ra sao?
5. CRM sở hữu Guest Profile đến mức nào; Front Office được phép cache trường nào và SLA đồng bộ?
6. Rule oversell, allotment, shared inventory và virtual room type có trong release đầu hay giai đoạn sau?
7. Business date/múi giờ cấu hình theo property; xử lý DST, khách sạn 24/7 và Night Audit muộn thế nào?
8. Yêu cầu chữ ký khách, scan giấy tờ và attachment store nào; hai bảng deferred được kích hoạt ở release nào?
9. Ngưỡng volume 3/5/10 năm cho ledger, audit, timeline và integration để chốt partition ngay từ baseline?
10. SQL Server edition/HA topology có hỗ trợ online/resumable operation, Always Encrypted enclave và read replica không?
11. Reporting cần near-real-time từ projection hay data warehouse/lake; quyền PII của từng nhóm báo cáo?
12. Chuẩn mã định danh hiện hữu của Asmile-BO là GUID kiểu nào và có service cấp business number dùng chung không?

Các câu hỏi này không làm thay đổi catalog lõi, nhưng phải được đóng trước migration production, cấu hình encryption, partition và integration contract.

## 31. Quality Gate

### 31.1 Gate thiết kế

- [x] Đã rà soát đủ 100 bảng logic: 93 giữ thành bảng Front Office, 4 gộp có chủ đích, 2 hoãn, 1 chuyển platform audit.
- [x] Catalog vật lý chính thức có đúng 121 bảng: 94 source/control/integration/audit, 14 lookup, 13 projection.
- [x] Mỗi bảng có schema, mục đích, grain, PK, clustered strategy, cột, null/default/FK/unique/index, PII, encryption và audit note.
- [x] Đã chốt chiến lược PK, clustered, FK, unique, check và index baseline.
- [x] Đã chốt concurrency cho room assignment và availability.
- [x] Đã chốt ledger Folio append-only, payment/deposit/refund và reconciliation.
- [x] Đã chốt outbox/inbox/idempotency, audit, PII/encryption, lookup và projection.
- [x] Đã có partition/archive/compression, EF mapping, migration, seed, security, test và ADR.
- [x] Không tạo SQL thực thi, Entity, DbContext, Migration, ViewModel, UI, database, PR hay commit.

### 31.2 Gate trước khi triển khai

- [ ] Đóng 12 Open Questions và ghi ADR/decision owner.
- [ ] Xác nhận 121 bảng với Domain, Front Office Operations, Finance, Security và DBA.
- [ ] Benchmark UUID byte order, assignment locking, availability multi-day update và Folio ledger ở volume mục tiêu.
- [ ] Threat model PII, KMS/key rotation, tenant isolation và break-glass.
- [ ] Chốt SLO projection, integration retry/dead-letter, RPO/RTO và archive retention.
- [ ] Phê duyệt migration waves, rollback/forward-fix, backup/restore và production observability.

## 32. Final Recommendation

Thiết kế nên được triển khai theo lát cắt nghiệp vụ, bắt đầu bằng Room–Reservation–Stay–Availability, sau đó Folio–Cashiering–Invoice, rồi các vận hành và integration. Không triển khai đồng loạt 121 bảng chỉ để hoàn thành schema: mỗi wave phải đi qua constraint, concurrency, reconciliation, security, recovery và load test tương ứng.

Ba nguyên tắc không được thỏa hiệp là: phân phòng không trùng dưới cạnh tranh thực; Folio ledger append-only và đối soát được; mọi tích hợp có outbox/inbox/idempotency. PII phải được tối thiểu hóa ngay từ vật lý, không chờ hardening sau. Projection chỉ tối ưu trải nghiệm, không được trở thành nguồn quyết định tài chính hoặc tồn phòng.

Sau khi các Open Questions được đóng, tài liệu này đủ làm baseline cho CQRS command/query boundary, service contract, EF Core mapping chi tiết và kế hoạch migration theo wave mà không sao chép kiến trúc Front Office cũ.

SẴN SÀNG THIẾT KẾ CQRS VÀ SERVICE CONTRACT FRONT OFFICE.
