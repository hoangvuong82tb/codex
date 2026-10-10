Unicode true
!ifdef ASMILE_INSTALLER_TEST_MODE
  RequestExecutionLevel user
!else
  RequestExecutionLevel admin
!endif
SetCompressor /SOLID zlib
CRCCheck force
ManifestDPIAware true

!ifndef APP_VERSION
  !error "APP_VERSION is required (four numeric components)."
!endif
!ifndef PUBLISHER
  !error "PUBLISHER is required."
!endif
!ifndef PAYLOAD_INCLUDE
  !error "PAYLOAD_INCLUDE is required."
!endif
!ifndef OUTPUT_PATH
  !error "OUTPUT_PATH is required."
!endif

!define APP_NAME "ASMILE BO"
!define APP_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\ASMILE-BO"
!define APP_EXE "ASMILE.BO.App.exe"

Var RollbackDir

Name "${APP_NAME} ${APP_VERSION}"
Caption "${APP_NAME} Setup"
OutFile "${OUTPUT_PATH}"
InstallDir "$PROGRAMFILES64\ASMILE BO"
InstallDirRegKey HKLM "${APP_KEY}" "InstallLocation"
ShowInstDetails show
ShowUnInstDetails show

VIProductVersion "${APP_VERSION}"
VIAddVersionKey /LANG=1033 "ProductName" "${APP_NAME}"
VIAddVersionKey /LANG=1033 "CompanyName" "${PUBLISHER}"
VIAddVersionKey /LANG=1033 "FileDescription" "${APP_NAME} Setup"
VIAddVersionKey /LANG=1033 "FileVersion" "${APP_VERSION}"
VIAddVersionKey /LANG=1033 "LegalCopyright" "Copyright (c) ${PUBLISHER}"
VIAddVersionKey /LANG=1033 "ProductVersion" "${APP_VERSION}"

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WordFunc.nsh"
!include "StrFunc.nsh"
!include "FileFunc.nsh"
!insertmacro VersionCompare
${StrStr}
${UnStrStr}
!include "${PAYLOAD_INCLUDE}"

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Vietnamese"

Function .onInit
!ifdef ASMILE_INSTALLER_TEST_MODE
  SetShellVarContext current
!else
  SetShellVarContext all
!endif
  Call RecoverInterruptedInstall
  ${If} $R0 == "0"
!ifndef ASMILE_INSTALLER_TEST_MODE
    MessageBox MB_ICONSTOP|MB_OK "A previous installation was interrupted and could not be recovered. Do not start ASMILE BO. Recovery files were preserved under the installation directory; contact support before retrying."
!endif
    Abort
  ${EndIf}
!ifdef ASMILE_INSTALLER_TEST_RECOVERY_ONLY
  ; Test-only build: perform recovery, then stop before starting a new install.
  Abort
!endif

!ifndef ASMILE_INSTALLER_TEST_MODE
  SetRegView 64
  ReadRegStr $R0 HKLM "${APP_KEY}" "DisplayVersion"
  ${If} $R0 != ""
    ${VersionCompare} "${APP_VERSION}" "$R0" $R1
    ${If} $R1 == "2"
      MessageBox MB_ICONSTOP|MB_OK "A newer ASMILE BO version is already installed. Setup will not downgrade it."
      Abort
    ${EndIf}
  ${EndIf}
!endif

  nsExec::ExecToStack /OEM /TIMEOUT=5000 '"$SYSDIR\tasklist.exe" /NH /FI "IMAGENAME eq ${APP_EXE}"'
  Pop $R0
  Pop $R1
  ${If} $R0 != "0"
!ifndef ASMILE_INSTALLER_TEST_MODE
    MessageBox MB_ICONSTOP|MB_OK "Unable to verify whether ASMILE BO is closed (tasklist exit code: $R0). Setup will stop safely."
!endif
    Abort
  ${EndIf}
  ${StrStr} $R2 $R1 "${APP_EXE}"
  ${If} $R2 != ""
!ifndef ASMILE_INSTALLER_TEST_MODE
    MessageBox MB_ICONEXCLAMATION|MB_OK "Close ASMILE BO before installing or upgrading, then run Setup again."
!endif
    Abort
  ${EndIf}
FunctionEnd

Function RecoverInterruptedInstall
  StrCpy $R0 "1"
!ifdef ASMILE_INSTALLER_TEST_MODE
  SetRegView 64
  ReadRegStr $R1 HKCU "${TEST_REGISTRY_KEY}" "PendingRecoveryPath"
!else
  SetRegView 64
  ReadRegStr $R1 HKLM "${APP_KEY}" "PendingRecoveryPath"
!endif
  ${If} $R1 == ""
    Return
  ${EndIf}
  ; Recovery is accepted only for a direct ns*.tmp child of the recorded
  ; install directory. Never recursively delete/restore an arbitrary path.
  GetFullPathName $R4 "$R1"
  GetFullPathName $R3 "$R4\.."
  StrLen $R5 $R3
  StrCpy $R6 $R4 $R5
  StrCmp $R6 $R3 0 recovery_invalid_path
  StrCpy $R6 $R4 1 $R5
  StrCmp $R6 "\" 0 recovery_invalid_path
  StrCpy $R6 $R4 "" $R5
  StrCpy $R7 $R6 "" 1
  StrCpy $R8 $R7 2
  StrCmp $R8 "ns" 0 recovery_invalid_path
  StrCpy $R8 $R7 4 -4
  StrCmp $R8 ".tmp" 0 recovery_invalid_path
  ${StrStr} $R8 $R7 "\"
  StrCmp $R8 "" 0 recovery_invalid_path

  StrCpy $INSTDIR "$R3"
  StrCpy $RollbackDir "$R4"
  IfFileExists "$RollbackDir\transaction.active" 0 recovery_cleanup
  !insertmacro RollbackPayload recovery_failed
  Call RestoreInstallerMetadata
  ${If} $R0 == "0"
    Goto recovery_failed
  ${EndIf}
  ClearErrors
  Delete "$RollbackDir\transaction.active"
  IfErrors recovery_failed

recovery_cleanup:
  IfFileExists "$RollbackDir\transaction.active" recovery_failed
  IfFileExists "$RollbackDir\*.*" 0 recovery_clear_pointer
  ClearErrors
  RMDir /r "$RollbackDir"
  IfErrors recovery_failed
recovery_clear_pointer:
!ifdef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  DeleteRegValue HKCU "${TEST_REGISTRY_KEY}" "PendingRecoveryPath"
  IfErrors recovery_failed
!else
  IfFileExists "$RollbackDir\registry.missing" recovery_pointer_removed_with_key
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "PendingRecoveryPath"
  IfErrors recovery_failed
recovery_pointer_removed_with_key:
!endif
  StrCpy $RollbackDir ""
  StrCpy $R0 "1"
  Return

recovery_invalid_path:
recovery_failed:
  StrCpy $R0 "0"
FunctionEnd

Function BackupInstallerMetadata
  StrCpy $R0 "1"
!ifdef ASMILE_INSTALLER_TEST_MODE
  ReadRegStr $R1 HKCU "${TEST_METADATA_REGISTRY_KEY}" "InstallLocation"
  ${If} $R1 == ""
    Goto backup_registry_missing
  ${EndIf}
  nsExec::ExecToStack /OEM /TIMEOUT=10000 '"$SYSDIR\reg.exe" export "HKCU\${TEST_METADATA_REGISTRY_KEY}" "$RollbackDir\registry.reg" /y'
!else
  ReadRegStr $R1 HKLM "${APP_KEY}" "InstallLocation"
  ${If} $R1 == ""
    Goto backup_registry_missing
  ${EndIf}
  nsExec::ExecToStack /OEM /TIMEOUT=10000 '"$SYSDIR\reg.exe" export "HKLM\${APP_KEY}" "$RollbackDir\registry.reg" /y'
!endif
  Pop $R0
  Pop $R1
  ${If} $R0 != "0"
    Goto backup_metadata_failed
  ${EndIf}
  ClearErrors
  FileOpen $R9 "$RollbackDir\registry.present" w
  IfErrors backup_metadata_failed
  FileWrite $R9 "present"
  FileClose $R9
  IfErrors backup_metadata_failed
!ifndef ASMILE_INSTALLER_TEST_MODE
  IfFileExists "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk" backup_shortcut_present backup_shortcut_missing
backup_shortcut_present:
  ClearErrors
  CopyFiles /SILENT "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk" "$RollbackDir"
  IfErrors backup_metadata_failed
  FileOpen $R9 "$RollbackDir\shortcut.present" w
  IfErrors backup_metadata_failed
  FileWrite $R9 "present"
  FileClose $R9
  IfErrors backup_metadata_failed
  Goto backup_metadata_complete
backup_shortcut_missing:
  ClearErrors
  FileOpen $R9 "$RollbackDir\shortcut.missing" w
  IfErrors backup_metadata_failed
  FileWrite $R9 "missing"
  FileClose $R9
  IfErrors backup_metadata_failed
!endif
backup_metadata_complete:
  StrCpy $R0 "1"
  Return

backup_registry_missing:
  ClearErrors
  FileOpen $R9 "$RollbackDir\registry.missing" w
  IfErrors backup_metadata_failed
  FileWrite $R9 "missing"
  FileClose $R9
  IfErrors backup_metadata_failed
!ifndef ASMILE_INSTALLER_TEST_MODE
  IfFileExists "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk" backup_missing_key_shortcut_present backup_missing_key_shortcut_missing
backup_missing_key_shortcut_present:
  ClearErrors
  CopyFiles /SILENT "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk" "$RollbackDir"
  IfErrors backup_metadata_failed
  FileOpen $R9 "$RollbackDir\shortcut.present" w
  IfErrors backup_metadata_failed
  FileWrite $R9 "present"
  FileClose $R9
  IfErrors backup_metadata_failed
  Goto backup_metadata_complete
backup_missing_key_shortcut_missing:
  ClearErrors
  FileOpen $R9 "$RollbackDir\shortcut.missing" w
  IfErrors backup_metadata_failed
  FileWrite $R9 "missing"
  FileClose $R9
  IfErrors backup_metadata_failed
!endif
  Goto backup_metadata_complete

backup_metadata_failed:
  StrCpy $R0 "0"
FunctionEnd

Function RestoreInstallerMetadata
  StrCpy $R0 "1"
  IfFileExists "$RollbackDir\registry.present" restore_registry_present
  IfFileExists "$RollbackDir\registry.missing" restore_registry_missing
  Goto restore_metadata_failed

restore_registry_present:
!ifdef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  DeleteRegValue HKCU "${TEST_METADATA_REGISTRY_KEY}" "InstallTestAddedValue"
  ClearErrors
  DeleteRegValue HKCU "${TEST_METADATA_REGISTRY_KEY}" "DisplayVersion"
  ClearErrors
  DeleteRegValue HKCU "${TEST_METADATA_REGISTRY_KEY}" "InstallLocation"
  nsExec::ExecToStack /OEM /TIMEOUT=10000 '"$SYSDIR\reg.exe" import "$RollbackDir\registry.reg"'
!else
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "DisplayName"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "DisplayVersion"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "Publisher"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "InstallLocation"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "DisplayIcon"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "UninstallString"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "NoModify"
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "NoRepair"
  nsExec::ExecToStack /OEM /TIMEOUT=10000 '"$SYSDIR\reg.exe" import "$RollbackDir\registry.reg"'
!endif
  Pop $R0
  Pop $R1
  ${If} $R0 != "0"
    Goto restore_metadata_failed
  ${EndIf}
  Goto restore_shortcut

restore_registry_missing:
!ifdef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  DeleteRegKey HKCU "${TEST_METADATA_REGISTRY_KEY}"
!else
  ClearErrors
  DeleteRegKey HKLM "${APP_KEY}"
!endif
  IfErrors restore_metadata_failed

restore_shortcut:
!ifdef ASMILE_INSTALLER_TEST_MODE
  Goto restore_metadata_complete
!else
  IfFileExists "$RollbackDir\shortcut.present" restore_shortcut_present
  IfFileExists "$RollbackDir\shortcut.missing" restore_shortcut_missing
  Goto restore_metadata_failed
restore_shortcut_present:
  CreateDirectory "$SMPROGRAMS\ASMILE BO"
  IfErrors restore_metadata_failed
  ClearErrors
  CopyFiles /SILENT "$RollbackDir\ASMILE BO.lnk" "$SMPROGRAMS\ASMILE BO"
  IfErrors restore_metadata_failed
  Goto restore_metadata_complete
restore_shortcut_missing:
  IfFileExists "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk" 0 restore_metadata_complete
  ClearErrors
  Delete "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk"
  IfErrors restore_metadata_failed
  RMDir "$SMPROGRAMS\ASMILE BO"
!endif
restore_metadata_complete:
  StrCpy $R0 "1"
  Return

restore_metadata_failed:
  StrCpy $R0 "0"
FunctionEnd

Function un.onInit
  nsExec::ExecToStack /OEM /TIMEOUT=5000 '"$SYSDIR\tasklist.exe" /NH /FI "IMAGENAME eq ${APP_EXE}"'
  Pop $R0
  Pop $R1
  ${If} $R0 != "0"
    MessageBox MB_ICONSTOP|MB_OK "Unable to verify whether ASMILE BO is closed (tasklist exit code: $R0). Uninstall will stop safely."
    Abort
  ${EndIf}
  ${UnStrStr} $R2 $R1 "${APP_EXE}"
  ${If} $R2 != ""
    MessageBox MB_ICONEXCLAMATION|MB_OK "Close ASMILE BO before uninstalling, then run Uninstall again."
    Abort
  ${EndIf}
FunctionEnd

Section "Install"
!ifndef ASMILE_INSTALLER_TEST_MODE
  SetRegView 64
  SetShellVarContext all
!else
  SetShellVarContext current
!endif

  ; Keep a same-volume recovery copy before touching any installed payload.
  ; This gives file-level rollback for recoverable write/locked-file errors.
  StrCpy $RollbackDir ""
  ClearErrors
  CreateDirectory "$INSTDIR"
  IfErrors install_backup_failed
  GetTempFileName $RollbackDir "$INSTDIR"
  IfErrors install_backup_failed
  Delete "$RollbackDir"
  IfErrors install_backup_failed
  CreateDirectory "$RollbackDir"
  IfErrors install_backup_failed
  ClearErrors
  !insertmacro BackupPayload
  IfErrors install_backup_failed
  Call BackupInstallerMetadata
  ${If} $R0 == "0"
    Goto install_backup_failed
  ${EndIf}

  ; Persist the transaction pointer before any live payload file is replaced.
  ; The active marker is written last, after the complete backup is available.
!ifdef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  WriteRegStr HKCU "${TEST_REGISTRY_KEY}" "PendingRecoveryPath" "$RollbackDir"
  IfErrors install_journal_failed
!else
  ClearErrors
  WriteRegStr HKLM "${APP_KEY}" "PendingRecoveryPath" "$RollbackDir"
  IfErrors install_journal_failed
!endif
  ClearErrors
  FileOpen $R9 "$RollbackDir\transaction.active" w
  IfErrors install_journal_failed
  FileWrite $R9 "ASMILE-BO installer transaction active"
  IfErrors install_journal_failed
  FileClose $R9
  IfErrors install_journal_failed

  ; "try" avoids an Ignore/Retry prompt that could leave a mixed-version
  ; installation; every failed file write is routed through rollback.
  SetOverwrite try
  ClearErrors
  !insertmacro InstallPayload
  IfErrors install_payload_failed

  ClearErrors
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  IfErrors install_payload_failed

!ifndef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  WriteRegStr HKLM "${APP_KEY}" "DisplayName" "${APP_NAME}"
  IfErrors install_payload_failed
  WriteRegStr HKLM "${APP_KEY}" "DisplayVersion" "${APP_VERSION}"
  IfErrors install_payload_failed
  WriteRegStr HKLM "${APP_KEY}" "Publisher" "${PUBLISHER}"
  IfErrors install_payload_failed
  WriteRegStr HKLM "${APP_KEY}" "InstallLocation" "$INSTDIR"
  IfErrors install_payload_failed
  WriteRegStr HKLM "${APP_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
  IfErrors install_payload_failed
  WriteRegStr HKLM "${APP_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  IfErrors install_payload_failed
  WriteRegDWORD HKLM "${APP_KEY}" "NoModify" 1
  IfErrors install_payload_failed
  WriteRegDWORD HKLM "${APP_KEY}" "NoRepair" 1
  IfErrors install_payload_failed

  ClearErrors
  CreateDirectory "$SMPROGRAMS\ASMILE BO"
  IfErrors install_payload_failed
  ClearErrors
  CreateShortCut "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk" "$INSTDIR\${APP_EXE}"
  IfErrors install_payload_failed
!else
  ClearErrors
  WriteRegStr HKCU "${TEST_METADATA_REGISTRY_KEY}" "DisplayVersion" "${APP_VERSION}"
  IfErrors install_payload_failed
  WriteRegStr HKCU "${TEST_METADATA_REGISTRY_KEY}" "InstallLocation" "$INSTDIR"
  IfErrors install_payload_failed
  WriteRegStr HKCU "${TEST_METADATA_REGISTRY_KEY}" "InstallTestAddedValue" "remove-on-rollback"
  IfErrors install_payload_failed
!endif

!ifdef ASMILE_INSTALLER_TEST_CRASH_AFTER_METADATA
  ClearErrors
  FileOpen $R8 "${TEST_CRASH_MARKER}" w
  IfErrors install_payload_failed
  FileWrite $R8 "crash-after-install-metadata"
  FileClose $R8
test_crash_after_metadata_hold:
  Sleep 1000
  Goto test_crash_after_metadata_hold
!endif

  ; Removing the active marker commits the install. A crash after this point
  ; leaves only disposable backups; next setup will clean them without rollback.
  ClearErrors
  Delete "$RollbackDir\transaction.active"
  IfErrors install_payload_failed
!ifdef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  DeleteRegValue HKCU "${TEST_REGISTRY_KEY}" "PendingRecoveryPath"
  IfErrors install_committed_with_journal
!else
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "PendingRecoveryPath"
  IfErrors install_committed_with_journal
!endif
  RMDir /r "$RollbackDir"
  StrCpy $RollbackDir ""
  Goto install_complete

install_committed_with_journal:
  ; The active marker is already gone; preserve the committed payload and let
  ; the next setup remove this stale journal without rolling back.
  StrCpy $RollbackDir ""
  Goto install_complete

install_backup_failed:
  StrCmp $RollbackDir "" install_backup_failed_no_directory
  RMDir /r "$RollbackDir"
install_backup_failed_no_directory:
!ifndef ASMILE_INSTALLER_TEST_MODE
  MessageBox MB_ICONSTOP|MB_OK "Setup could not create a recovery copy. The existing installation was not modified. Check free disk space and permissions, then try again."
!endif
  Abort

install_journal_failed:
!ifdef ASMILE_INSTALLER_TEST_MODE
  DeleteRegValue HKCU "${TEST_REGISTRY_KEY}" "PendingRecoveryPath"
!else
  DeleteRegValue HKLM "${APP_KEY}" "PendingRecoveryPath"
!endif
  StrCmp $RollbackDir "" install_backup_failed_no_directory
  RMDir /r "$RollbackDir"
  StrCpy $RollbackDir ""
!ifndef ASMILE_INSTALLER_TEST_MODE
  MessageBox MB_ICONSTOP|MB_OK "Setup could not record its recovery journal. The application files were not modified."
!endif
  Abort

install_payload_failed:
  ClearErrors
  !insertmacro RollbackPayload install_rollback_failed
  Call RestoreInstallerMetadata
  ${If} $R0 == "0"
    Goto install_rollback_failed
  ${EndIf}
  ClearErrors
  Delete "$RollbackDir\transaction.active"
  IfErrors install_rollback_failed
!ifdef ASMILE_INSTALLER_TEST_MODE
  ClearErrors
  DeleteRegValue HKCU "${TEST_REGISTRY_KEY}" "PendingRecoveryPath"
  IfErrors install_rollback_failed
!else
  IfFileExists "$RollbackDir\registry.missing" install_rollback_pointer_removed_with_key
  ClearErrors
  DeleteRegValue HKLM "${APP_KEY}" "PendingRecoveryPath"
  IfErrors install_rollback_failed
install_rollback_pointer_removed_with_key:
!endif
  RMDir /r "$RollbackDir"
  StrCpy $RollbackDir ""
!ifndef ASMILE_INSTALLER_TEST_MODE
  MessageBox MB_ICONSTOP|MB_OK "Setup could not replace every application file. The previous payload and uninstaller were restored; no version change was committed."
!endif
  Abort

install_rollback_failed:
  ; Preserve the same-volume recovery copy for manual recovery. Never remove
  ; it if even one previous file could not be restored.
!ifndef ASMILE_INSTALLER_TEST_MODE
  MessageBox MB_ICONSTOP|MB_OK "Setup failed and automatic rollback was incomplete. Do not start ASMILE BO. Recovery files remain at: $RollbackDir"
!endif
  Abort

install_complete:
SectionEnd

Section "Uninstall"
  SetRegView 64
  SetShellVarContext all
  !insertmacro UninstallPayload
  Delete "$SMPROGRAMS\ASMILE BO\ASMILE BO.lnk"
  RMDir "$SMPROGRAMS\ASMILE BO"
  Delete "$INSTDIR\Uninstall.exe"
  DeleteRegKey HKLM "${APP_KEY}"
  SetOutPath "$PROGRAMFILES64\ASMILE BO"
  RMDir "$INSTDIR"
SectionEnd
