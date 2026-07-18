# Training Data UI Polish

## Muc tieu

Nang cap man `training-data` thanh "Onboarding du lieu mau" cho nha hang/khach san, chi thay doi UI/UX va state trinh bay. Khong doi logic seeding/reset/preflight/safety gate.

## Wireframe

------------------------------------------------------------
[Icon] Onboarding du lieu mau                 [Status badges]
       Khoi tao du lieu demo cho nha hang/khach san

------------------------------------------------------------
| Chon mo hinh demo                            | Preflight   |
| [ Nha hang ] [ Khach san ] [ KS + NH ]       | DB/Env/Risk |
|                                              | Confirm txt |
| Hanh dong chinh                              |             |
| [Nap demo] [Nap nen] [Kiem tra]              | Preview     |
|                                              | doi tac ... |
| Quan ly du lieu mau                          | kho ...     |
| [Xoa mau] [Chi xoa du lieu mau]              |             |
|                                              | Checklist   |
| Vung nguy hiem                               | migration   |
| [Reset trang] [Xoa toan bo nghiep vu]        | markers     |
------------------------------------------------------------
| Canh bao (alert list)                                      |
------------------------------------------------------------
| Nhat ky thao tac (empty state / compact grid)              |
------------------------------------------------------------

## Icon strategy

- PNG icon:
  - Header: `Windows_Icon/Icons/applications/data.ico`
  - Profile Nha hang: `Windows_Icon/Icons/applications/commerce.ico`
  - Profile Khach san: `Windows_Icon/Icons/applications/office.ico`
  - Profile Full: `Windows_Icon/Icons/applications/dataverse.ico`
- Glyph (`Segoe MDL2 Assets`) cho status/warning/checklist/log.

## Hanh vi chinh

- Card profile co selected state (vien accent + nen nhat + dau tick).
- Tach 3 nhom thao tac:
  - Onboarding an toan.
  - Quan ly du lieu mau.
  - Vung nguy hiem.
- Vung nguy hiem dung style canh bao rieng, khong trung style nut thuong.
- Warnings hien thi dang alert list, khong dung DataGrid cho danh sach ngan.
- Log co empty state va bang compact khi co du lieu.
- Completion card hien thi thong ke sau seed va quick actions dang disabled neu chua wire route.

## Luu y ky thuat

- Giu route `training-data`.
- Giu command/service contract hien co.
- Khong sua backend business logic.
- Khong them dependency UI moi.

## Step 9 completed (2026-05-25)

- Hoan tat production-grade polish cho man onboarding du lieu mau theo layout 2 cot (main/sidebar) voi visual hierarchy ro rang.
- Header da co status badge bind state that:
  - environment
  - database
  - selected profile
  - state (ready/locked/warning/migration required)
- Demo profile cards da nang cap thanh onboarding cards thuc:
  - icon/title/description/chips/stats bind tu DTO preview that
  - selected state ro rang (border tint/check badge)
- Preview card da doi thanh mini dashboard, bind 100% tu `TrainingDataDemoPreviewDto`.
- Preflight card da doi sang key-value + risk/destructive badges, co migration warning card rieng.
- Action groups da tach ro:
  - onboarding an toan
  - quan ly du lieu mau
  - vung nguy hiem (danger style rieng)
- Warnings da bo DataGrid, thay bang alert stack bind tu warnings that.
- Nhat ky thao tac co empty state compact va bang log virtualized khi co du lieu.
- Completion state co summary + quick actions disabled an toan voi tooltip module chua kha dung.
- Khong thay doi workflow backend training-data/reset/accounting.

### Build/Test

- `dotnet build .\ASMILE.BO.sln --no-incremental`: pass, 0 warnings, 0 errors.
- `dotnet test .\ASMILE.BO.sln`: pass, 127/127 tests (Unit 119, Integration 8).
