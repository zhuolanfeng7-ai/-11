$docxPath = (Get-ChildItem -LiteralPath 'D:\My project (2)\Assets\Scenes\output' -Filter '*.docx' | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
$pdfPath = 'D:\My project (2)\Assets\Scenes\tmp\render_classics\review.pdf'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $pdfPath) | Out-Null
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($docxPath, $false, $true)
    $doc.ExportAsFixedFormat($pdfPath, 17)
    $doc.Close($false)
}
finally {
    $word.Quit()
}
Write-Output $pdfPath
