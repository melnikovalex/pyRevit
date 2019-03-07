
$ErrorActionPreference = 'Stop';

$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'exe'
  url           = 'https://github.com/eirannejad/pyRevit/releases/download/cli-v0.9.0.0/pyRevit.CLI_0.9.0.0_signed.exe'

  softwareName  = 'pyrevit-cli*'

  checksum      = '0655648B5698FC2B9B22B650CC411AF07302A295E9C0C617B143A13447D689E3'
  checksumType  = 'sha256'

  silentArgs    = "/exenoui /exenoupdates /qn"
  validExitCodes= @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs