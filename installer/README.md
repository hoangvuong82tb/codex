# ASMILE BO Windows installer

The NSIS source is `ASMILE-BO.nsi`. Build from a verified Release publish directory with:

```powershell
.\scripts\build_commercial_installer.ps1 `
  -PublishDirectory .\artifacts\publish `
  -OutputDirectory .\artifacts\installer `
  -Version 1.0.0.0 `
  -Publisher 'ASMILE BO'
```

The builder first runs `verify_commercial_package.ps1`, copies the publish tree to a unique temporary staging directory, generates an exact file inventory for installation/uninstallation, compiles the installer, and then runs `verify_commercial_installer.ps1`. The package verifier requires a self-contained `win-x64` publish that includes both .NET and Windows Desktop runtimes; framework-dependent publishes are rejected. The installer verifier extracts the NSIS archive with 7-Zip into a unique temporary directory and compares the exact payload paths and SHA-256 hashes against the verified publish (allowing only the app executable to change when production signing is enabled). It also checks executable version, production signatures, and the adjacent third-party notice. Provide `-SevenZipPath` when 7-Zip is not on `PATH` or installed in its standard Program Files location. The builder refuses to overwrite an existing installer. The uninstaller deletes only files from that inventory and removes directories only when empty; it must never recursively remove the install directory. App settings, logs, exports, backups, and business documents must remain in their configured user-data locations, outside Program Files.

During install/upgrade, the installer first backs up the exact managed payload and uninstaller, exports the prior uninstall registry key, snapshots the Start Menu shortcut, and writes an active transaction marker. It replaces application files and updates install metadata only while that marker is active. A recoverable write failure rolls back immediately. If the installer process is interrupted, the next setup reads the protected recovery pointer and restores the previous files, registry metadata, and shortcut before continuing. If recovery cannot be completed, setup aborts and retains recovery data; do not launch the application until recovery is resolved. Removing the active marker is the commit point; after that point, a leftover journal is cleanup-only and must not roll back the committed version.

The test-only fault-injection harness validates both write-failure rollback and forced process termination after metadata has changed. It uses unique temporary install paths and unique HKCU registry keys; it is not a substitute for elevated clean-machine, upgrade, or power-loss testing.

For a production-signed artifact, provide a code-signing certificate already provisioned in the Windows certificate store and an HTTPS RFC 3161 timestamp endpoint:

```powershell
.\scripts\build_commercial_installer.ps1 `
  -PublishDirectory .\artifacts\publish `
  -OutputDirectory .\artifacts\installer `
  -Version 1.0.0.0 `
  -Publisher 'ASMILE BO' `
  -Production `
  -SigningCertificateThumbprint '<certificate thumbprint>' `
  -TimestampUrl 'https://<approved-timestamp-service>'
```

Production mode signs the app executable in the staging copy and the resulting setup, then verifies both signatures. It does not change the original publish directory. Keep certificate private keys out of the repository and build logs. The build emits a versioned NSIS third-party notice alongside the installer.

To verify that a missing production certificate fails before creating the output directory, run the negative gate test against a verified publish and the pinned tools:

```powershell
.\scripts\test_commercial_installer_production_gate.ps1 `
  -PublishDirectory .\artifacts\publish `
  -MakensisPath 'C:\Program Files (x86)\NSIS\makensis.exe' `
  -SevenZipPath 'C:\Program Files\7-Zip\7z.exe'
```

This test uses a synthetic thumbprint that must not exist in the certificate store; it never signs an artifact and asserts that no output directory is created.

To regression-test that the package verifier accepts a self-contained publish and rejects a framework-dependent one, run:

```powershell
.\scripts\test_commercial_package_runtime_gate.ps1 `
  -SelfContainedPublishDirectory .\artifacts\publish
```

The test creates a temporary `win-x64` framework-dependent publish, asserts that `verify_commercial_package.ps1` rejects it at the runtime gate, and removes only that unique temporary directory.

## Release gate still required

- Build with NSIS 3.13 and archive its verified distribution hash, installer source hash, build log, and final SHA-256.
- Run `verify_commercial_installer.ps1` against the final signed setup and exact publish; archive its `COMMERCIAL_INSTALLER=PASS` output with the release evidence.
- Review the applicable NSIS and bundled-plugin licenses for the chosen commercial distribution; retain required notices.
- Complete clean-machine install, first-run setup, repair/upgrade, uninstall, and reinstall tests on supported Windows versions. Specifically confirm settings, logs, exports, backups, and business documents survive upgrades and uninstall.
- Test locked-file behavior, insufficient permissions, interrupted installation, and rollback/recovery on supported Windows versions. The test harness covers payload/uninstaller and simulated HKCU install-metadata recovery after forced process termination; elevated production-registry/Start Menu behavior, hard power loss, and clean-machine upgrade/uninstall UAT remain required.
- Approve the product EULA, publisher identity, support/update policy, and signing certificate before customer distribution.
