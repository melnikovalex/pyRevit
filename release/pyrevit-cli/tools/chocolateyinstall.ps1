
$ErrorActionPreference = 'Stop';

$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'exe'
  url           = 'https://github.com/eirannejad/pyRevit/releases/download/cli-v0.9.7.0/pyRevit.CLI_0.9.7.0_signed.exe'

  softwareName  = 'pyrevit-cli*'

  checksum      = '9F176E1D0394A2706CB63973B96E437974C025D31313192B4C79B9D72D210470'
  checksumType  = 'sha256'

  silentArgs    = "/exenoui /exenoupdates /qn"
  validExitCodes= @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs