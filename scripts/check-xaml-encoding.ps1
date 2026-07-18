param(
  [string]$Root = "D:\ASMILE-BO\ASMILE.BO.App\Views"
)

$ErrorActionPreference = 'Stop'

$patterns = @(
  'Ã',
  'â€',
  'â€™',
  'á»',
  'áº',
  '�'
)

$files = Get-ChildItem -Path $Root -Recurse -Filter *.xaml
$hits = @()

foreach ($f in $files) {
  if ($f.Name -eq 'PayableView.xaml') {
    continue
  }
  $i = 0
  foreach ($line in Get-Content -LiteralPath $f.FullName -Encoding UTF8) {
    $i++
    foreach ($p in $patterns) {
      if ($line.Contains($p)) {
        $hits += ("{0}:{1}: {2}" -f $f.FullName, $i, $line)
        break
      }
    }
  }
}

if ($hits.Count -gt 0) {
  Write-Error "Detected potential mojibake/encoding corruption in XAML:\n$($hits -join "`n")"
  exit 1
}

Write-Output "Encoding check passed: no mojibake patterns found in XAML."
