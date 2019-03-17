$productId = '{D5505166-C49A-436F-B25A-9FBD0632F6F0}'
$silentArgs = '/qn /norestart'
$validExitCodes = @(0)
$msiArgs = "/X$productId $silentArgs"
Start-ChocolateyProcessAsAdmin "$msiArgs" 'msiexec'
