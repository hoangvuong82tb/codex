[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ConnectionString,
    [int]$ExpectedReservationCount = 10,
    [switch]$RequirePhase2B6
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$connection.Open()
try {
    function Invoke-Scalar([string]$Sql) {
        $cmd = $connection.CreateCommand(); $cmd.CommandText = $Sql; $cmd.ExecuteScalar()
    }
    function Get-ColumnSet([string]$Schema, [string]$Table) {
        $cmd = $connection.CreateCommand()
        $cmd.CommandText = "SELECT c.name FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=@schema AND t.name=@table"
        [void]$cmd.Parameters.Add('@schema',[System.Data.SqlDbType]::NVarChar,128); $cmd.Parameters['@schema'].Value=$Schema
        [void]$cmd.Parameters.Add('@table',[System.Data.SqlDbType]::NVarChar,128); $cmd.Parameters['@table'].Value=$Table
        $reader=$cmd.ExecuteReader(); $set=[System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        while($reader.Read()){[void]$set.Add([string]$reader.GetValue(0))}; $reader.Close(); $set
    }
    function Test-Table([string]$Schema, [string]$Table) {
        [int](Invoke-Scalar "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=N'$Schema' AND t.name=N'$Table'") -eq 1
    }
    function Assert-Table([string]$Schema, [string]$Table) {
        $present = Test-Table $Schema $Table
        Write-Output "$Schema.$Table=$present"
        if($RequirePhase2B6 -and -not $present){ throw "Missing $Schema.$Table" }
        $present
    }
    function Assert-Columns([string]$Label, [System.Collections.Generic.HashSet[string]]$Columns, [string[]]$Names) {
        foreach($name in $Names) {
            $present = $Columns.Contains($name)
            Write-Output "$Label.$name=$present"
            if($RequirePhase2B6 -and -not $present){ throw "Missing $Label.$name" }
        }
    }
    function Assert-UniqueIndex([string]$Schema, [string]$Table, [string]$IndexName) {
        $qualified = "$Schema.$Table"
        $present = [int](Invoke-Scalar "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'$Schema.$Table') AND name=N'$IndexName' AND is_unique=1")
        Write-Output "$IndexName=$present"
        if($RequirePhase2B6 -and $present -ne 1){ throw "Missing unique index $IndexName on $qualified" }
    }
    $reservations = [int](Invoke-Scalar "SELECT COUNT_BIG(*) FROM fo.DatPhong")
    $folioEntries = [int](Invoke-Scalar "SELECT COUNT_BIG(*) FROM fo.ButToanFolio")
    $folios = [int](Invoke-Scalar "SELECT COUNT_BIG(*) FROM fo.SoFolio")
    Write-Output "RESERVATIONS=$reservations FOLIOS=$folios FOLIO_ENTRIES=$folioEntries"
    if($reservations -ne $ExpectedReservationCount){ throw "Reservation count drift: expected $ExpectedReservationCount, got $reservations" }

    $reservationColumns=Get-ColumnSet 'fo' 'DatPhong'
    $entryColumns=Get-ColumnSet 'fo' 'ButToanFolio'
    Assert-Columns 'fo.DatPhong' $reservationColumns @('GuestId','GuaranteeType','DepositStatus','DepositAmount')
    Assert-Columns 'fo.ButToanFolio' $entryColumns @('RowVersion','IdempotencyKey')
    Assert-UniqueIndex 'fo' 'ButToanFolio' 'UQ_fo_ButToanFolio_Idempotency'

    if($RequirePhase2B6) {
        $stayColumns = Get-ColumnSet 'fo' 'LuuTruKhachSan'
        $guestTable = Assert-Table 'fo' 'HoSoKhach'
        $null = Assert-Table 'fo' 'LuuTruKhachSan'
        Assert-Columns 'fo.LuuTruKhachSan' $stayColumns @('GuestId')
        $roomColumns = Get-ColumnSet 'fo' 'PhongKhachSan'
        $null = Assert-Table 'fo' 'PhongKhachSan'
        Assert-Columns 'fo.PhongKhachSan' $roomColumns @('HousekeepingStatus')

        foreach($table in @('ThanhToan','PhanBoThanhToan','LichSuThanhToan','NgayKinhDoanh')) { $null = Assert-Table 'fo' $table }
        $paymentColumns = Get-ColumnSet 'fo' 'ThanhToan'
        Assert-Columns 'fo.ThanhToan' $paymentColumns @('HotelPaymentId','PropertyId','HotelFolioId','PaymentNumber','PaymentMethod','Amount','Status','Currency','RowVersion')
        $allocationColumns = Get-ColumnSet 'fo' 'PhanBoThanhToan'
        Assert-Columns 'fo.PhanBoThanhToan' $allocationColumns @('HotelPaymentAllocationId','HotelPaymentId','PropertyId','HotelFolioId','ChargeFolioEntryId','Amount')
        $historyColumns = Get-ColumnSet 'fo' 'LichSuThanhToan'
        Assert-Columns 'fo.LichSuThanhToan' $historyColumns @('PaymentHistoryId','HotelPaymentId','PropertyId','Action','OccurredAtUtc')
        $businessDateColumns = Get-ColumnSet 'fo' 'NgayKinhDoanh'
        Assert-Columns 'fo.NgayKinhDoanh' $businessDateColumns @('BusinessDateId','PropertyId','BusinessDate','Status','OpenedAtUtc','ClosedAtUtc','RowVersion')
        Assert-UniqueIndex 'fo' 'ThanhToan' 'UQ_fo_ThanhToan_SoThanhToan'
        Assert-UniqueIndex 'fo' 'NgayKinhDoanh' 'UQ_fo_NgayKinhDoanh_CoSo_Ngay'
        Write-Output 'PHASE2B6_REQUIRED_CHECKS=PASS'
    } else {
        Write-Output 'PHASE2B6_REQUIRED_CHECKS=SKIPPED'
    }
    $hasReservationColumns = $reservationColumns.Contains('GuaranteeType') -and $reservationColumns.Contains('DepositStatus') -and $reservationColumns.Contains('DepositAmount')
    if ($hasReservationColumns) {
        $nullGuarantees=[int](Invoke-Scalar "SELECT COUNT(*) FROM fo.DatPhong WHERE GuaranteeType IS NULL AND DepositStatus IS NULL AND DepositAmount IS NULL")
    } else {
        $nullGuarantees=0
    }
    Write-Output "UNCHANGED_RESERVATION_ROWS=$nullGuarantees"
    Write-Output 'VALIDATION_PASS'
}
finally { $connection.Dispose() }
