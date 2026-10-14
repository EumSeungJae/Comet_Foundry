Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType=WindowsRuntime]
$null = [Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType=WindowsRuntime]
$null = [Windows.Storage.Streams.InMemoryRandomAccessStream, Windows.Storage.Streams, ContentType=WindowsRuntime]
function Await-Operation($operation, [Type]$resultType) {
    $method = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethodDefinition -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
    $task = $method.MakeGenericMethod($resultType).Invoke($null, @($operation))
    $task.Wait()
    return $task.Result
}
function Await-Action($operation) {
    $method = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and !$_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' } | Select-Object -First 1
    $task = $method.Invoke($null, @($operation))
    $task.Wait()
}
$taskRoot = 'D:\Programming\Comet_Foundry\project_CF'
$pdfPath = Join-Path $taskRoot 'Presentations\Portfolio\CometFoundry_Portfolio_2026-10-14.pdf'
$file = Await-Operation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($pdfPath)) ([Windows.Storage.StorageFile])
$pdf = Await-Operation ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
Write-Output "PDF pages: $($pdf.PageCount)"
for ($index=0; $index -lt $pdf.PageCount; $index++) {
    $page = $pdf.GetPage($index)
    $stream = New-Object Windows.Storage.Streams.InMemoryRandomAccessStream
    Await-Action ($page.RenderToStreamAsync($stream))
    $stream.Seek(0)
    $readStream = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream)
    $target = Join-Path $taskRoot ('ValidationArtifacts\Portfolio-2026-10-14\pdf-page-'+($index+1).ToString('00')+'.png')
    $output = [System.IO.File]::Create($target)
    $readStream.CopyTo($output)
    $output.Dispose()
    $readStream.Dispose()
    $stream.Dispose()
    $page.Dispose()
}
