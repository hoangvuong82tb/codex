# PHASE 4 IMPLEMENTATION PLAN

## Nguyên tắc điều phối
- NO REWRITE RULE
- NO UI REDESIGN
- BACKWARD COMPATIBLE
- MIGRATION SAFE
- KEEP EXISTING ACCOUNTING FLOW
- KEEP EXISTING SERVICES
- EXTEND INCREMENTALLY
- SMALL SAFE STEPS
- BUILD MUST STAY GREEN
- TESTS MUST STAY GREEN

## Mục tiêu tổng quát
- Hoàn thiện nền tảng enterprise cho compliance, vận hành và mở rộng quy mô mà không phá hành vi kế toán hiện tại.
- Giữ nguyên luồng posting/journal/report/workflow legacy; chỉ bổ sung lớp metadata, orchestration và kiểm soát.
- Triển khai theo bước nhỏ có điểm kiểm thử, rollback và gate rõ ràng.

## Roadmap Phase 4

| Step | Nội dung | Trạng thái | Ghi chú |
|---|---|---|---|
| 4.1 | Multi-company / Multi-branch Hardening | Completed | Đã hoàn thành nền cô lập company/branch và session scope. |
| 4.2 | TT99 Compliance Foundation | Completed | Đã hoàn thành nền mapping/validation/snapshot TT99 theo hướng additive. |
| 4.3 | TT200 Compliance Foundation | Completed | Đã hoàn thành nền mapping/validation/snapshot TT200 theo hướng additive. |
| 4.4 | Enterprise Approval Workflow Foundation | Completed | Đã hoàn tất approval workflow foundation; FK đã harden NoAction để SQL Server không lỗi multiple cascade paths. |
| 4.5 | Advanced Financial Statement Mapping | Completed | Đã hoàn tất template/version/formula/snapshot foundation; FK financial mapping dùng NoAction cho SQL Server. |
| 4.6 | Enterprise Reconciliation Engine | Completed | Đã bổ sung foundation đối soát ngân hàng, tồn kho, AR/AP và snapshot đối soát. |
| 4.7 | E-Invoice Integration Foundation | Completed | Đã hoàn thiện foundation provider/state/retry tracking trên nền e-invoice hiện có. |
| 4.8 | SQL Server Performance Hardening | Completed | Đã harden query/index foundation và giữ hướng SQL Server-first. |
| 4.9 | Advanced Reporting & Export Pipeline | Completed | Đã bổ sung export execution, retention, artifact và report snapshot foundation. |
| 4.10 | Enterprise Data Integrity & Recovery | Completed | Đã bổ sung integrity checkpoint, scan result và repair operation foundation. |
| 4.11 | Compliance Packaging & Legal Output | Completed | Đã bổ sung compliance package, manifest, submission và archive foundation. |
| 4.12 | Phase 4 Stabilization & Production Certification | Completed | Đã kiểm tra build/test và startup Release sau khi áp migration Phase 4. |


## Phase 4 completion note - 2026-05-24
- Phase 4 đã hoàn tất theo hướng additive, không rewrite accounting/posting/report/workflow cũ.
- Đã sửa lỗi startup SQL Server 1785 bằng cách chuẩn hóa FK approval/financial về `NoAction`.
- Đã harden seed bảo mật/permission để tránh duplicate key `administrator` khi DB dev đã có dữ liệu.
- App Release đã mở được sau khi auto-migrate; process PID 25112 còn chạy sau hơn 135 giây theo dõi và log mới không còn lỗi startup/runtime.

## STEP 4.4 — Enterprise Approval Workflow Foundation

### Mục tiêu
- Mục tiêu nghiệp vụ: thiết lập chuỗi phê duyệt doanh nghiệp theo cấp duyệt, ủy quyền và leo thang, có truy vết đầy đủ.
- Mục tiêu kỹ thuật: thêm approval engine additive gắn với workflow hiện hữu, không đổi route/shell và không đổi voucher UI.
- Scope được phép: approval matrix, delegation window, escalation policy, approval audit, permission model theo company/branch.
- Scope cấm: không redesign màn chứng từ, không rewrite workflow posting cũ, không đổi business rule kế toán.

### Backend
- Backend impact: bổ sung metadata approval chain, approval history, delegated approval record, escalation event record.
- Security/compliance impact: mọi hành động duyệt/từ chối/ủy quyền bắt buộc audit + correlation; kiểm tra quyền theo module/action/company/branch.
- Dependency với step khác: phụ thuộc 4.1 (scope), 3.11-3.16 (audit/workflow/telemetry); là nền cho 4.10 và 4.12.

### UI
- UI impact: thêm thành phần additive trong màn hiện có (panel trạng thái duyệt, lịch sử duyệt, người duyệt hiện tại); không đổi bố cục shell.

### Migration
- Migration impact: additive-only cho bảng approval matrix/history/delegation/escalation metadata.
- Nhóm migration dự kiến: nhóm riêng cho approval core và index lookup.
- Rollback-safe: migration có cờ trạng thái, rollback không làm mất dữ liệu chứng từ cũ.

### Test strategy
- Unit: approval chain ordering, delegation validity, escalation trigger, permission deny override.
- Integration: company/branch scope isolation, audit correlation, workflow compatibility với posting flow hiện có.
- Regression: không đổi behavior chứng từ legacy khi không bật policy mới.

### Risks
- Rủi ro lớn: ma trận duyệt phức tạp gây sai tuyến; quyền chồng chéo giữa role cũ và role scoped.
- Performance impact: danh sách phê duyệt và lịch sử có thể tăng nhanh, cần paging + index từ đầu.

### Definition of Done
- Hoàn thành khi approval engine chạy song song workflow cũ, có audit đầy đủ, không đổi hành vi kế toán.
- Rollback strategy: tắt feature flag approval nâng cao và quay về flow duyệt cũ mà không cần data rewrite.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.5 — Advanced Financial Statement Mapping

### Mục tiêu
- Mục tiêu nghiệp vụ: chuẩn hóa bản đồ báo cáo tài chính TT99/TT200, hỗ trợ thay đổi biểu mẫu theo phiên bản pháp lý.
- Mục tiêu kỹ thuật: thêm lớp template động, hierarchy báo cáo và mapping versioning trên report foundation hiện có.
- Scope được phép: template metadata, statement hierarchy, mapping version lifecycle, compatibility rule.
- Scope cấm: không rewrite report engine, không đổi format output legacy đã ổn định.

### Backend
- Backend impact: thêm cấu trúc mapping phiên bản, rule resolve line-item và tham chiếu account group.
- Security/compliance impact: khóa phiên bản theo kỳ kê khai; audit thay đổi mapping và lịch sử phát hành template.
- Dependency với step khác: kế thừa 4.2/4.3; dữ liệu đầu ra dùng cho 4.11.

### UI
- UI impact: thêm màn cấu hình mapping dạng additive trong khu vực quản trị/report; không đổi shell.

### Migration
- Migration impact: additive-only cho template/mapping version tables và index theo company/standard/version.
- Nhóm migration dự kiến: 1 migration mapping core + 1 migration index tối ưu truy vấn.
- Rollback-safe: giữ bản mapping cũ hoạt động bằng cơ chế active version fallback.

### Test strategy
- Unit: resolve hierarchy, version precedence, mapping conflict detection.
- Integration: report generation với nhiều version, company isolation, backward compatibility.
- Regression: các report hiện có vẫn chạy khi chưa kích hoạt template động.

### Risks
- Rủi ro lớn: sai ánh xạ biểu mẫu làm lệch số liệu báo cáo pháp lý.
- Performance impact: truy vấn hierarchy sâu có thể chậm nếu thiếu materialized projection.

### Definition of Done
- Có versioned mapping TT99/TT200 chạy ổn định, report cũ không bị ảnh hưởng, audit thay đổi đầy đủ.
- Rollback strategy: chuyển active version về bản trước trong cùng schema additive.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.6 — Enterprise Reconciliation Engine

### Mục tiêu
- Mục tiêu nghiệp vụ: bổ sung nền đối soát bank/inventory/AR/AP để phát hiện lệch và khóa bằng chứng đối soát.
- Mục tiêu kỹ thuật: tạo engine auto-match + anomaly detection + reconciliation snapshot.
- Scope được phép: rule đối soát, điểm số khớp, snapshot theo kỳ, metadata xử lý ngoại lệ.
- Scope cấm: không rewrite engine kế toán lõi, không thay flow reconcile legacy đã dùng.

### Backend
- Backend impact: persisted reconciliation run, match candidate, anomaly record, snapshot record.
- Security/compliance impact: mỗi kết quả đối soát phải có audit và nguồn dữ liệu truy nguyên được.
- Dependency với step khác: dùng dữ liệu từ AR/AP/bank/inventory hiện có; đầu vào cho 4.10 integrity.

### UI
- UI impact: tab đối soát additive với lưới ảo hóa, bộ lọc theo module/company/branch.

### Migration
- Migration impact: additive-only cho reconciliation entities + index theo kỳ, trạng thái, nguồn.
- Nhóm migration dự kiến: reconciliation core + index tối ưu anomaly query.
- Rollback-safe: không xóa dữ liệu chứng từ, chỉ rollback metadata đối soát.

### Test strategy
- Unit: matching rule, anomaly scoring, snapshot hash consistency.
- Integration: đối soát bank/inventory/AR/AP trên dữ liệu mẫu lớn, isolation company/branch.
- Regression: không làm đổi số liệu journal/report khi chạy đối soát.

### Risks
- Rủi ro lớn: rule auto-match sai gây false-positive/false-negative.
- Performance impact: so khớp tập lớn cần batching, pagination và throttling.

### Definition of Done
- Có pipeline đối soát đa module, lưu snapshot và anomaly rõ ràng, không đổi hành vi nghiệp vụ cũ.
- Rollback strategy: tắt auto-match nâng cao, giữ manual reconciliation path cũ.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.7 — E-Invoice Integration Foundation

### Mục tiêu
- Mục tiêu nghiệp vụ: tạo nền tích hợp hóa đơn điện tử linh hoạt theo nhiều nhà cung cấp.
- Mục tiêu kỹ thuật: chuẩn hóa provider abstraction, signing lifecycle, state machine, sync/retry và archive.
- Scope được phép: adapter contract, lifecycle state, retry policy, provider isolation, correlation.
- Scope cấm: không hard-code provider, không tích hợp cloud observability ngoài scope.

### Backend
- Backend impact: chuẩn hóa provider contract và persisted sync/attempt/state history.
- Security/compliance impact: bảo toàn metadata ký và trạng thái pháp lý; audit toàn bộ thao tác phát hành/hủy/đồng bộ.
- Dependency với step khác: dùng nền từ 3.4 và telemetry/workflow từ 3.14-3.16; đầu ra phục vụ 4.11.

### UI
- UI impact: bổ sung trạng thái e-invoice và retry hành động dạng additive ở màn liên quan; không đổi shell.

### Migration
- Migration impact: additive-only cho provider settings, sync history, signing metadata.
- Nhóm migration dự kiến: provider abstraction + state machine metadata + index retry.
- Rollback-safe: có fallback provider disabled, không ảnh hưởng hóa đơn đã phát hành.

### Test strategy
- Unit: state transition, retry backoff, provider failover contract.
- Integration: đồng bộ trạng thái, xử lý timeout/cancellation, isolation theo company.
- Regression: luồng hóa đơn cũ tiếp tục dùng được khi chưa bật provider abstraction mới.

### Risks
- Rủi ro lớn: sai state machine dẫn đến sai trạng thái pháp lý.
- Performance impact: sync hàng loạt dễ nghẽn nếu không batching và giới hạn song song.

### Definition of Done
- Provider abstraction hoạt động ổn định, retry/sync có audit + telemetry, không hard-code nhà cung cấp.
- Rollback strategy: chuyển về provider mode trước đó qua cấu hình và giữ lịch sử đồng bộ.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.8 — SQL Server Performance Hardening

### Mục tiêu
- Mục tiêu nghiệp vụ: đảm bảo hệ thống vận hành ổn định trên SQL Server khi dữ liệu tăng lớn.
- Mục tiêu kỹ thuật: tối ưu index, query shape, execution plan, paging chuẩn, compiled query và cache strategy.
- Scope được phép: tối ưu đọc/ghi trọng điểm theo SQL Server; chuẩn hóa quy ước truy vấn phân trang.
- Scope cấm: không tối ưu theo SQLite làm giảm chất lượng SQL Server; SQLite/InMemory chỉ giữ cho test compatibility.

### Backend
- Backend impact: thêm index/filtered index, refactor truy vấn nặng sang projection + compiled query khi phù hợp.
- Security/compliance impact: không thay đổi cơ chế isolation/filter hiện có; giữ đầy đủ audit trail.
- Dependency với step khác: hỗ trợ trực tiếp 4.9, 4.10, 4.12.

### UI
- UI impact: không đổi layout; chỉ cải thiện độ phản hồi qua API/query tối ưu.

### Migration
- Migration impact: additive-only cho index, statistics tuning metadata nếu cần.
- Nhóm migration dự kiến: index batch theo module (search/audit/workflow/report).
- Rollback-safe: index có thể rollback độc lập, không ảnh hưởng schema nghiệp vụ.

### Test strategy
- Unit: query helper/pagination semantics.
- Integration: benchmark truy vấn hot path trên SQL Server, kiểm tra plan regression.
- Regression: đảm bảo không đổi kết quả nghiệp vụ, không phá provider test hiện tại.

### Risks
- Rủi ro lớn: index sai thứ tự cột gây regression ghi dữ liệu.
- Performance impact: trade-off giữa tốc độ đọc và chi phí ghi phải được đo trước khi rollout.

### Definition of Done
- Hot-path query đạt ngưỡng mục tiêu p95, build/test xanh, không degrade SQL Server vì provider test.
- Rollback strategy: rollback từng nhóm index và bật lại truy vấn cũ qua feature toggle khi cần.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.9 — Advanced Reporting & Export Pipeline

### Mục tiêu
- Mục tiêu nghiệp vụ: đáp ứng xuất báo cáo dung lượng lớn, ổn định và truy vết được lịch sử phát hành.
- Mục tiêu kỹ thuật: streaming export, background export jobs, retention phiên bản snapshot báo cáo.
- Scope được phép: pipeline export bất đồng bộ, quản lý job xuất, lưu metadata/hash/manifest.
- Scope cấm: không rewrite report center hiện có, không đổi contract file output đã dùng.

### Backend
- Backend impact: bổ sung export job orchestrator, chunked writer, snapshot version index.
- Security/compliance impact: metadata xuất phải có người thực hiện, thời điểm, hash, phạm vi dữ liệu.
- Dependency với step khác: dựa trên 4.8 performance; đầu ra gói hóa cho 4.11.

### UI
- UI impact: thêm trạng thái tiến độ export và lịch sử export dạng additive trong màn báo cáo/operations.

### Migration
- Migration impact: additive-only cho export job/history/manifest/versioning metadata.
- Nhóm migration dự kiến: export pipeline core + retention index.
- Rollback-safe: giữ path export cũ làm fallback.

### Test strategy
- Unit: streaming writer integrity, chunk boundary, hash verification.
- Integration: export dataset lớn, cancellation/resume, retention cleanup.
- Regression: format Excel/PDF/CSV hiện có không thay đổi.

### Risks
- Rủi ro lớn: memory spike khi export lớn nếu fallback sang load-all.
- Performance impact: cần throttle job đồng thời để tránh tranh chấp tài nguyên với workflow khác.

### Definition of Done
- Export lớn chạy qua pipeline nền ổn định, có manifest/hash, không block UI và không đổi output legacy.
- Rollback strategy: tắt background export path mới và dùng export trực tiếp cũ theo module.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.10 — Enterprise Data Integrity & Recovery

### Mục tiêu
- Mục tiêu nghiệp vụ: tăng độ tin cậy dữ liệu bằng kiểm tra nhất quán và quy trình khôi phục có kiểm soát.
- Mục tiêu kỹ thuật: consistency validator, repair workflow, recovery orchestration, corruption detection.
- Scope được phép: detector + recommendation + replay-safe recovery metadata.
- Scope cấm: không chạy destructive auto-fix, không thay logic kế toán gốc.

### Backend
- Backend impact: bổ sung integrity check result, repair action log, recovery attempt timeline.
- Security/compliance impact: tất cả thao tác repair/recovery phải audit + correlation + permission guard.
- Dependency với step khác: dùng dữ liệu từ 4.4-4.9; là gate quan trọng cho 4.12.

### UI
- UI impact: thêm bảng vi phạm nhất quán và hành động khuyến nghị trong Operations Center.

### Migration
- Migration impact: additive-only cho integrity/recovery metadata.
- Nhóm migration dự kiến: integrity core + recommendation/index.
- Rollback-safe: rollback metadata không tác động dữ liệu hạch toán đã ghi.

### Test strategy
- Unit: detection rules, recommendation ranking, corruption signature detection.
- Integration: recovery orchestration flow, replay-safe validation, company/branch isolation.
- Regression: không thay đổi kết quả accounting flow khi chạy integrity scan.

### Risks
- Rủi ro lớn: rule quá nghiêm gây cảnh báo giả, ảnh hưởng vận hành.
- Performance impact: scan dữ liệu lớn có thể kéo dài, cần lịch chạy nền và cắt lô.

### Definition of Done
- Có engine phát hiện/ghi nhận/tư vấn khôi phục rõ ràng, an toàn, không mutate dữ liệu kế toán tự động.
- Rollback strategy: vô hiệu hóa repair action nâng cao, giữ detector ở chế độ chỉ đọc.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.11 — Compliance Packaging & Legal Output

### Mục tiêu
- Mục tiêu nghiệp vụ: đóng gói hồ sơ pháp lý TT99/TT200 sẵn sàng kiểm toán và nộp theo quy định.
- Mục tiêu kỹ thuật: legal package generator gồm XML/PDF manifest, compliance archive, metadata ký số.
- Scope được phép: package manifest, checksum, versioning, submission metadata.
- Scope cấm: không tích hợp nộp trực tiếp cổng cơ quan thuế, không thay workflow báo cáo hiện có.

### Backend
- Backend impact: thêm package manifest, bundle history, integrity metadata và trace correlation.
- Security/compliance impact: chuẩn hóa provenance dữ liệu, bất biến gói đã phát hành.
- Dependency với step khác: phụ thuộc 4.5, 4.7, 4.9, 4.10.

### UI
- UI impact: thêm màn xem lịch sử gói pháp lý và trạng thái chuẩn bị ký số theo dạng additive.

### Migration
- Migration impact: additive-only cho package/archive/submission metadata tables.
- Nhóm migration dự kiến: legal package core + index theo kỳ/pháp nhân/trạng thái.
- Rollback-safe: package metadata tách rời, rollback không ảnh hưởng report nguồn.

### Test strategy
- Unit: manifest generation, checksum stability, metadata completeness.
- Integration: package build từ snapshot/report/export thực tế, replay và archive compatibility.
- Regression: output báo cáo nguồn không bị thay đổi bởi bước đóng gói.

### Risks
- Rủi ro lớn: thiếu metadata pháp lý làm gói không hợp lệ cho kiểm toán.
- Performance impact: đóng gói batch lớn cần streaming/IO throttle.

### Definition of Done
- Tạo được gói TT99/TT200 có manifest và truy vết đầy đủ, không phá luồng báo cáo hiện tại.
- Rollback strategy: giữ chế độ xuất tài liệu rời nếu legal package path gặp sự cố.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

## STEP 4.12 — Phase 4 Stabilization & Production Certification

### Mục tiêu
- Mục tiêu nghiệp vụ: chốt chất lượng vận hành trước phát hành production.
- Mục tiêu kỹ thuật: memory/concurrency validation, long-session test, migration/rollback certification, smoke checklist.
- Scope được phép: hardening, tuning, test matrix cuối kỳ, release gate.
- Scope cấm: không mở feature nghiệp vụ mới, không redesign UI, không rewrite engine cũ.

### Backend
- Backend impact: tối ưu cuối kỳ theo số liệu đo được; không đổi contract nghiệp vụ.
- Security/compliance impact: xác nhận đầy đủ audit, permission, retention, compliance gate trước release.
- Dependency với step khác: là bước tổng hợp, phụ thuộc hoàn thành 4.4–4.11.

### UI
- UI impact: chỉ sửa lỗi nhỏ ảnh hưởng vận hành; không thay đổi thiết kế tổng thể.

### Migration
- Migration impact: không tạo migration mới trừ khi bắt buộc sửa lỗi an toàn additive.
- Nhóm migration dự kiến: chỉ cho hotfix schema nhỏ có rollback script kèm theo.
- Rollback-safe: chứng nhận rollback toàn phase theo checkpoint.

### Test strategy
- Unit/integration/regression full run toàn solution.
- Performance soak: long-session, workload hỗn hợp, cancellation storm, retry stress.
- Operational smoke: checklist mở app, login, post/reverse, report/export, scheduler, recovery.

### Risks
- Rủi ro lớn: lỗi cạnh tranh tài nguyên ở tải cao chỉ xuất hiện khi soak dài.
- Performance impact: bước này có thể phát hiện bottleneck mới yêu cầu tinh chỉnh vòng cuối.

### Definition of Done
- Hoàn tất chứng nhận build/test/perf/smoke theo tiêu chí phát hành, không có regression nghiêm trọng.
- Rollback strategy: xác nhận rollback thành công tại mọi checkpoint release.

### Validation
```powershell
dotnet build .\ASMILE.BO.sln --no-incremental
dotnet test .\ASMILE.BO.sln --no-build
```

# PHASE 4 EXECUTION ORDER

1. 4.4 Approval Foundation  
2. 4.5 Financial Statement Mapping  
3. 4.6 Reconciliation Engine  
4. 4.7 E-Invoice Foundation  
5. 4.8 SQL Server Hardening  
6. 4.9 Reporting & Export Pipeline  
7. 4.10 Data Integrity & Recovery  
8. 4.11 Compliance Packaging  
9. 4.12 Stabilization & Certification

Dependency gates:
- Gate A: 4.4 + 4.5 hoàn tất trước khi bật compliance packaging.
- Gate B: 4.8 phải đạt ngưỡng query trước khi rollout 4.9 diện rộng.
- Gate C: 4.10 phải chạy ổn định trước 4.12 release certification.

Recommended migration grouping:
- Nhóm 1: approval + mapping metadata (4.4–4.5).
- Nhóm 2: reconciliation + e-invoice state (4.6–4.7).
- Nhóm 3: performance index + export pipeline metadata (4.8–4.9).
- Nhóm 4: integrity/recovery + legal package metadata (4.10–4.11).

Smoke-test checkpoints:
- Checkpoint 1 sau nhóm 1.
- Checkpoint 2 sau nhóm 2.
- Checkpoint 3 sau nhóm 3.
- Checkpoint 4 trước 4.12 release gate.

Rollback checkpoints:
- Rollback window theo từng nhóm migration, có script revert và xác nhận data invariants.

Release milestones:
- M1: Foundation complete (4.4–4.6).
- M2: Compliance data flow complete (4.7–4.9).
- M3: Integrity + legal package ready (4.10–4.11).
- M4: Production certification (4.12).

# PHASE 4 GLOBAL RISKS

- Compliance risk: sai mapping/version TT99/TT200 gây lệch hồ sơ pháp lý.
- SQL performance risk: index/query tuning chưa đủ cho tải lớn theo kỳ cao điểm.
- Provider risk: e-invoice provider thay đổi chính sách hoặc SLA không ổn định.
- Operational risk: rollout nhiều module đồng thời làm tăng rủi ro regression chéo.
- Data integrity risk: cảnh báo giả từ validator gây nhiễu vận hành nếu rule chưa chuẩn.

Quy tắc “không làm gì” toàn Phase:
- Không redesign UI shell.
- Không rewrite accounting engine/posting engine/journal engine.
- Không thay database provider khỏi SQL Server production.
- Không tách microservices/distributed queue ngoài phạm vi.

# PHASE 4 PRODUCTION READINESS CHECKLIST

- Build xanh ổn định ở Debug/Release.
- Test unit/integration/regression/smoke đạt ngưỡng đã chốt.
- Migration additive có rollback script và checkpoint kiểm tra dữ liệu.
- Permission/audit/correlation bao phủ hành động rủi ro cao.
- Export/compliance package có hash/manifest và khả năng truy nguyên.
- Recovery playbook có diễn tập thành công.
- Vận hành có dashboard theo dõi lỗi, hiệu năng, retry, timeout.

# PHASE 4 TESTING STRATEGY

Testing matrix theo milestone:
- M1 (4.4–4.6): unit + integration + isolation.
- M2 (4.7–4.9): contract + load + streaming + retry.
- M3 (4.10–4.11): integrity + replay + legal package consistency.
- M4 (4.12): full regression + soak + smoke + rollback drill.

Nhóm test trọng tâm:
- Unit: rule, mapping, state machine, hash/manifest.
- Integration: cross-module flow theo company/branch.
- Regression: bảo toàn accounting behavior legacy.
- Performance: p95/p99 query/export/reconciliation.
- Smoke: nghiệp vụ quan trọng đầu-cuối sau mỗi nhóm rollout.

# PHASE 4 MIGRATION STRATEGY

- Chỉ additive migration, không xóa cột/bảng cũ trong Phase 4.
- Mỗi nhóm migration có:
  - script apply
  - script rollback
  - schema checkpoint
  - data invariant checklist
- Ưu tiên migration nhỏ theo domain để giảm rủi ro lock/timeout.
- Rollout theo môi trường: dev -> staging -> pre-prod -> production.
- Mọi migration phải qua build xanh và smoke-test checkpoint trước khi lên môi trường kế tiếp.

# Phase 4 Success Criteria

- Hoàn tất 4.1–4.12 theo nguyên tắc additive, tương thích ngược và không phá workflow hiện hữu.
- Build/test giữ trạng thái xanh xuyên suốt toàn bộ phase.
- TT99/TT200, e-invoice, export pháp lý và integrity/recovery đạt mức sẵn sàng production theo phạm vi kế hoạch.
- SQL Server vận hành ổn định ở tải lớn, có metric và rollback boundary rõ ràng.
