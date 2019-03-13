# Publishing a new version:

Open `tools/chocolateyinstall.ps1`
Edit `$url` to the download path of the new exe installer

Run `checksum -t sha256 -f ./new/installer` on the new installer and grab checksum
Replace `checksum` with new checksum

Opne `pyrevit-cli.nuspec`
Update `version` and `releaseNotes`

Open `tools/chocolatelyuninstall.ps1`
Update `productId` with new product id of the installer

Run `choco-test.sh` and test the install

export choco api key to `$chocoapikey` env var
export choco package name to `$chocopkg` env var (e.g. pyrevit-cli.0.9.0.0.nupkg)

Run `choco-push.sh`