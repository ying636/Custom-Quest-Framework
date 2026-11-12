param([Parameter(Mandatory = $true)][string]$Evidence)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common
$taskDrawingReferences = @([System.Drawing.Bitmap].Assembly.Location,[System.Drawing.Rectangle].Assembly.Location) + @(Get-ChildItem -LiteralPath (Split-Path -Parent ([System.Drawing.Bitmap].Assembly.Location)) -Filter 'System.Private.Windows.*.dll' | Select-Object -ExpandProperty FullName)
Add-Type -ReferencedAssemblies $taskDrawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class PanoramaPixelComparison
{
    public static double[] Compare(string before, string after, int left, int top, int right, int bottom)
    {
        using Bitmap a = new Bitmap(before);
        using Bitmap b = new Bitmap(after);
        if(a.Size != b.Size) throw new InvalidOperationException("Screenshot dimensions differ");
        Rectangle all = new Rectangle(0, 0, a.Width, a.Height);
        BitmapData da = a.LockBits(all, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData db = b.LockBits(all, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            byte[] ba = new byte[da.Stride * a.Height];
            byte[] bb = new byte[db.Stride * b.Height];
            Marshal.Copy(da.Scan0, ba, 0, ba.Length);
            Marshal.Copy(db.Scan0, bb, 0, bb.Length);
            long count = 0, changed = 0, aboveOne = 0, aboveThree = 0, sum = 0;
            int maximum = 0;
            int minX = a.Width, minY = a.Height, maxX = -1, maxY = -1;
            long outsideCount = 0, outsideChanged = 0;
            for(int y = 160; y < a.Height - 100; y++)
                for(int x = 60; x < a.Width - 700; x++)
                {
                    int offset = y * da.Stride + x * 4;
                    int d0 = Math.Abs(ba[offset] - bb[offset]);
                    int d1 = Math.Abs(ba[offset+1] - bb[offset+1]);
                    int d2 = Math.Abs(ba[offset+2] - bb[offset+2]);
                    int d = Math.Max(d0, Math.Max(d1, d2));
                    if(x >= left && x < right && y >= top && y < bottom)
                    {
                        count++;
                        if(d > 0) { changed++; minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
                        if(d > 1) aboveOne++;
                        if(d > 3) aboveThree++;
                        sum += d0+d1+d2;
                        maximum = Math.Max(maximum,d);
                    }
                    else { outsideCount++; if(d > 1) outsideChanged++; }
                }
            return new double[]{ count, changed, aboveOne, aboveThree, maximum, count == 0 ? 0 : sum/(count*3d), outsideCount, outsideChanged, minX, minY, maxX, maxY };
        }
        finally { a.UnlockBits(da); b.UnlockBits(db); }
    }
}
'@
$taskCameras = @{}
Get-Content -LiteralPath (Join-Path $Evidence 'Cameras.jsonl') -Encoding utf8 | ForEach-Object {
    $taskCamera = $_ | ConvertFrom-Json
    $taskCameras[$taskCamera.Name] = $taskCamera
}
$taskRows = foreach ($taskBefore in Get-ChildItem -LiteralPath $Evidence -Filter '*_Before.png' -File) {
    $taskName = $taskBefore.BaseName.Substring(0,$taskBefore.BaseName.Length-7)
    $taskCamera = $taskCameras[$taskName]
    if (-not $taskCamera) { throw ('Missing camera evidence: ' + $taskName) }
    $taskAfter = Join-Path $Evidence ($taskName + '_After.png')
    $taskInset = if ($taskName.StartsWith('Pool_') -and -not $taskName.EndsWith('_NoHaze')) { 16 } else { 3 }
    $taskLeft = [int][Math]::Ceiling(((-200 - $taskCamera.ViewLeft) / ($taskCamera.ViewRight - $taskCamera.ViewLeft)) * $taskCamera.Width) + $taskInset
    $taskRight = [int][Math]::Floor((($taskCamera.MapSizeX + 200 - $taskCamera.ViewLeft) / ($taskCamera.ViewRight - $taskCamera.ViewLeft)) * $taskCamera.Width) - $taskInset
    $taskTop = [int][Math]::Ceiling(($taskCamera.ViewTop - $taskCamera.MapSizeZ - 200) / ($taskCamera.ViewTop - $taskCamera.ViewBottom) * $taskCamera.Height) + $taskInset
    $taskBottom = [int][Math]::Floor(($taskCamera.ViewTop + 200) / ($taskCamera.ViewTop - $taskCamera.ViewBottom) * $taskCamera.Height) - $taskInset
    $taskComparison = [PanoramaPixelComparison]::Compare($taskBefore.FullName,$taskAfter,$taskLeft,$taskTop,$taskRight,$taskBottom)
    [pscustomobject]@{
        Name=$taskName; OriginalPixels=$taskComparison[0]; ChangedPixels=$taskComparison[1]; AboveOne=$taskComparison[2]; AboveThree=$taskComparison[3]
        MaximumChannelDifference=$taskComparison[4]; MeanChannelDifference=$taskComparison[5]; OutsidePixels=$taskComparison[6]; OutsideChangedAboveOne=$taskComparison[7]
        OriginalRectPixels=@($taskLeft,$taskTop,$taskRight,$taskBottom)
        FilterInsetPixels=$taskInset
        DifferenceBounds=@($taskComparison[8],$taskComparison[9],$taskComparison[10],$taskComparison[11])
    }
}
$taskRows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $Evidence 'PixelComparison.json') -Encoding utf8
$taskRows | Select-Object Name,OriginalPixels,ChangedPixels,MaximumChannelDifference,OutsideChangedAboveOne | Format-Table -AutoSize
if (@($taskRows | Where-Object { $_.OriginalPixels -gt 0 -and $_.ChangedPixels -gt 0 }).Count -gt 0) { throw 'Some original-region pixels changed; inspect PixelComparison.json.' }
