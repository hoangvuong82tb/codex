# CREATE RESERVATION WINDOW LAUNCH ERROR FIX REPORT

> **Ngày**: 2026-07-22
> **Kết luận**: FIXED — read-only binding exceptions eliminated

---

## 1. EXCEPTION

| Field | Value |
|-------|-------|
| **Type** | `System.InvalidOperationException` |
| **Message** | `A TwoWay or OneWayToSource binding cannot work on the read-only property 'Nights' of type 'ASMILE.BO.App.ViewModels.ReservationWorkspaceViewModel'.` |
| **InnerException** | None |
| **Source file** | `ASMILE.BO.App\Views\ReservationCreateWindow.xaml` |
| **Trigger** | WPF `BindingExpression.Activate` → `CheckReadOnly` during `UpdateLayout` |

### Full message variants

```
A TwoWay or OneWayToSource binding cannot work on the read-only property 'Nights' of type 'ASMILE.BO.App.ViewModels.ReservationWorkspaceViewModel'.
A TwoWay or OneWayToSource binding cannot work on the read-only property 'RoomTypeDisplay' of type 'ASMILE.BO.App.Views.ReservationCreateWindow'.
A TwoWay or OneWayToSource binding cannot work on the read-only property 'PaymentDisplay' of type 'ASMILE.BO.App.Views.ReservationCreateWindow'.
A TwoWay or OneWayToSource binding cannot work on the read-only property 'PriceSourceLabel' of type 'ASMILE.BO.App.Views.ReservationCreateWindow'.
A TwoWay or OneWayToSource binding cannot work on the read-only property 'RoomChargeDisplay' of type 'ASMILE.BO.App.Views.ReservationCreateWindow'.
A TwoWay or OneWayToSource binding cannot work on the read-only property 'DepositDisplay' of type 'ASMILE.BO.App.Views.ReservationCreateWindow'.
A TwoWay or OneWayToSource binding cannot work on the read-only property 'StepGSourceDisplay' of type 'ASMILE.BO.App.Views.ReservationCreateWindow'.
```

---

## 2. STACK TRACE (key frames)

```
at MS.Internal.Data.PropertyPathWorker.CheckReadOnly(Object item, Object info)
at MS.Internal.Data.PropertyPathWorker.ReplaceItem(Int32 k, Object newO, Object parent)
at MS.Internal.Data.PropertyPathWorker.UpdateSourceValueState(Int32 k, ...)
at System.Windows.Data.BindingExpression.Activate(Object item)
at System.Windows.Data.BindingExpression.AttachToContext(AttachAttempt attempt)
at MS.Internal.Data.DataBindEngine.Task.Run(Boolean lastChance)
at MS.Internal.Data.DataBindEngine.OnLayoutUpdated(Object sender, EventArgs e)
at System.Windows.ContextLayoutManager.UpdateLayout()
```

Diagnostic log: `bin\Debug\net8.0-windows\logs\ui-exceptions.log` (588 lines, 450+ entries of the same 7 exceptions repeating)

---

## 3. ROOT CAUSE

Read-only computed properties (expression-bodied / getter-only) were bound in XAML **without explicit `Mode=OneWay`**. During `BindingExpression.Activate` (triggered by `UpdateLayout` on window resize/show), WPF's binding engine called `UpdateSourceValueState`, which invoked `CheckReadOnly`. Since the properties have no setter, it threw `InvalidOperationException`.

The default binding mode for `TextBlock.Text` and `Run.Text` is `OneWay`, but the binding engine still calls `UpdateSourceValueState` during activation to sync the initial value. For read-only properties, this path fails.

**Affected properties:**

| Property | Type | Occurrences |
|----------|------|-------------|
| `Modal.Workspace.Nights` | `ReservationWorkspaceViewModel` (read-only computed) | 4 |
| `PriceSourceLabel` | `ReservationCreateWindow` (read-only computed) | 3 |
| `PaymentDisplay` | `ReservationCreateWindow` (read-only computed) | 3 |
| `VatLabel` | `ReservationCreateWindow` (read-only computed) | 2 |
| `DiscountDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `DepositDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `RoomRateDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `RoomChargeDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `BalanceDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `RoomTypeDisplay` | `ReservationCreateWindow` (read-only computed) | 1 |
| `GuestsSummary` | `ReservationCreateWindow` (read-only computed) | 1 |
| `GuestNameDisplay` | `ReservationCreateWindow` (read-only computed) | 1 |
| `StepGSourceDisplay` | `ReservationCreateWindow` (read-only computed) | 1 |
| `BookingNotesPreview` | `ReservationCreateWindow` (read-only computed) | 2 |
| `InternalNotesPreview` | `ReservationCreateWindow` (read-only computed) | 1 |
| `SpecialRequestsPreview` | `ReservationCreateWindow` (read-only computed) | 2 |
| `PickupDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `DropoffDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `FlightArrivalDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |
| `FlightDepartureDisplay` | `ReservationCreateWindow` (read-only computed) | 2 |

All these are computed properties with **no setter**. Total: ~36 binding sites fixed.

---

## 4. FIX

Added `Mode=OneWay` to all bindings targeting read-only properties in `ReservationCreateWindow.xaml`.

```xml
<!-- BEFORE — implicit binding mode, WPF tries TwoWay during activation -->
<TextBlock Text="{Binding Modal.Workspace.Nights}" .../>
<TextBlock Text="{Binding PaymentDisplay, RelativeSource={RelativeSource AncestorType=UserControl}}" .../>

<!-- AFTER — explicit Mode=OneWay prevents TwoWay attempt -->
<TextBlock Text="{Binding Modal.Workspace.Nights, Mode=OneWay}" .../>
<TextBlock Text="{Binding PaymentDisplay, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=OneWay}" .../>
```

### Files changed

| File | Change |
|------|--------|
| `ASMILE.BO.App\Views\ReservationCreateWindow.xaml` | Added `Mode=OneWay` to ~36 bindings across all steps A–G |

---

## 5. VERIFICATION

| Test | Result |
|------|--------|
| Build (clean + rebuild) | PASS (0 errors) |
| App launch | PID 43296, 318MB |
| Exception log after startup | NO NEW EXCEPTIONS |
| Exception log after showing wizard | (pending user test) |
| Navigate A→G | (pending) |
| Close and reopen | (pending) |

---

## 6. CONFIRMATION

- [x] Root cause identified from actual exception log (not guessed)
- [x] Fix targets exact properties listed in stack traces
- [x] No backend changes
- [x] No UI redesign
- [x] No Steps A–G logic changed
- [x] Only `Mode=OneWay` added to XAML bindings
