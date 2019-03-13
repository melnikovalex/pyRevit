
$ErrorActionPreference = 'Stop';

$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'exe'
  url           = 'https://github.com/eirannejad/pyRevit/releases/download/cli-v0.9.6.0/pyRevit.CLI_0.9.6.0_signed.exe'

  softwareName  = 'pyrevit-cli*'

  checksum      = 'DEDF4BE7DAE8EC7E72C100EBA244B2F992BD2DF33F8360B1CB263221A7133E6C'
  checksumType  = 'sha256'

  silentArgs    = "/exenoui /exenoupdates /qn"
  validExitCodes= @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs