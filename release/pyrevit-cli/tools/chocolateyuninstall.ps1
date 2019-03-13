$productId = '{A72CCEB0-FD16-472D-8FF1-5215981985F3}'
$silentArgs = '/qn /norestart'
$validExitCodes = @(0)
$msiArgs = "/X$productId $silentArgs"
Start-ChocolateyProcessAsAdmin "$msiArgs" 'msiexec'
