$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$src = Join-Path $PSScriptRoot 'src'
$ico = Join-Path $src 'app.ico'

function Draw-Icon([int]$n) {
    $bmp = New-Object Drawing.Bitmap $n, $n, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $r = $n * 0.22
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $path.AddArc($n - $r * 2 - 1, 0, $r * 2, $r * 2, 270, 90)
    $path.AddArc($n - $r * 2 - 1, $n - $r * 2 - 1, $r * 2, $r * 2, 0, 90)
    $path.AddArc(0, $n - $r * 2 - 1, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $blue = [Drawing.Color]::FromArgb(37, 99, 235)
    $g.FillPath((New-Object Drawing.SolidBrush $blue), $path)
    $x = $n * 0.25; $w = $n * 0.5; $top = $n * 0.18; $bottom = $n * 0.82; $eh = $n * 0.18
    $white = New-Object Drawing.SolidBrush ([Drawing.Color]::White)
    $g.FillRectangle($white, $x, $top + $eh / 2, $w, $bottom - $top - $eh)
    $g.FillEllipse($white, $x, $bottom - $eh, $w, $eh)
    $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(191, 219, 254))), $x, $top, $w, $eh)
    $pen = New-Object Drawing.Pen $blue, ([Math]::Max(1, $n * 0.045))
    foreach ($y in @(($top + ($bottom - $top - $eh) / 3), ($top + 2 * ($bottom - $top - $eh) / 3))) {
        $g.DrawArc($pen, $x, $y, $w, $eh, 0, 180)
    }
    $g.Dispose()
    $bmp
}

function Dib-Bytes($bmp) {
    $n = $bmp.Width
    $ms = New-Object IO.MemoryStream
    $bw = New-Object IO.BinaryWriter $ms
    $bw.Write([int]40); $bw.Write([int]$n); $bw.Write([int]($n * 2)); $bw.Write([int16]1); $bw.Write([int16]32)
    $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)
    for ($y = $n - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $n; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([byte]$c.B); $bw.Write([byte]$c.G); $bw.Write([byte]$c.R); $bw.Write([byte]$c.A)
        }
    }
    $stride = [int]([Math]::Ceiling($n / 32.0) * 4)
    $bw.Write((New-Object byte[] ($stride * $n)))
    $bw.Flush()
    , $ms.ToArray()
}

if (-not (Test-Path $ico)) {
    $sizes = 16, 20, 24, 32, 40, 48, 64, 256
    $images = New-Object 'Collections.Generic.List[byte[]]'
    foreach ($n in $sizes) {
        $bmp = Draw-Icon $n
        if ($n -eq 256) {
            $ms = New-Object IO.MemoryStream
            $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png)
            $images.Add($ms.ToArray())
        } else {
            $images.Add([byte[]](Dib-Bytes $bmp))
        }
    }
    $out = New-Object IO.MemoryStream
    $bw = New-Object IO.BinaryWriter $out
    $bw.Write([int16]0); $bw.Write([int16]1); $bw.Write([int16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $n = $sizes[$i]
        $bw.Write([byte]($n % 256)); $bw.Write([byte]($n % 256)); $bw.Write([byte]0); $bw.Write([byte]0)
        $bw.Write([int16]1); $bw.Write([int16]32); $bw.Write([int]$images[$i].Length); $bw.Write([int]$offset)
        $offset += $images[$i].Length
    }
    foreach ($img in $images) { $bw.Write($img) }
    $bw.Flush()
    [IO.File]::WriteAllBytes($ico, $out.ToArray())
}

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /optimize+ /codepage:65001 `
    /out:"$(Join-Path $PSScriptRoot 'HighBackup.exe')" `
    /win32icon:"$ico" /win32manifest:"$(Join-Path $src 'app.manifest')" /resource:"$ico,HighBackup.app.ico" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Security.dll /r:System.Xml.dll `
    "$src\*.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Built HighBackup.exe'
