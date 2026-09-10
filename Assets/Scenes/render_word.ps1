$docxPath = 'D:\My project (2)\Assets\Scenes\output\琉璃愈心_药师七佛盲盒交互装置设计文献综述.docx'
$pdfPath = 'D:\My project (2)\Assets\Scenes\tmp\docx_render\琉璃愈心_药师七佛盲盒交互装置设计文献综述.pdf'
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
