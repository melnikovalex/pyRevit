$productId = '{4F364726-0CD9-4E0C-A2F6-1FC42DE4CF7C}'
$silentArgs = '/qn /norestart'
$validExitCodes = @(0)
$msiArgs = "/X$productId $silentArgs"
Start-ChocolateyProcessAsAdmin "$msiArgs" 'msiexec'