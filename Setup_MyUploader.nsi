; example2.nsi
;
; This script is based on example1.nsi, but it remember the directory, 
; has uninstall support and (optionally) installs start menu shortcuts.
;
; It will install example2.nsi into a directory that the user selects,

;--------------------------------

Unicode true

!define APP "MyUploader"

!system 'DefineAsmVer.exe "bin\DEBUG\${APP}.exe" "!define VER ""[FVER]"" " > Appver.tmp'
!include "Appver.tmp"
!searchreplace APV ${VER} "." "_"

; The name of the installer
Name "${APP} ${VER}"

!ifdef INNER
  OutFile "$%TEMP%\tempinstaller.exe"       ; not really important where this is
  SetCompress off                           ; for speed
!else
  !echo "Outer invocation"
 
  ; Call makensis again against current file, defining INNER.  This writes an installer for us which, when
  ; it is invoked, will just write the uninstaller to some location, and then exit.
 
  !makensis '/DINNER "${__FILE__}"' = 0
 
  ; So now run that installer we just created as %TEMP%\tempinstaller.exe.
 
  !system 'set __COMPAT_LAYER=RunAsInvoker&"$%TEMP%\tempinstaller.exe"' = 0
 
  ; That will have written an uninstaller binary for us.  Now we sign it with your
  ; favorite code signing tool.
 
  !system 'MySign "$%TEMP%\uninstall.exe"' = 0
 
  ; Good.  Now we can carry on writing the real installer.

  ; The file to write
  OutFile "Setup_${APP}_${APV}.exe"
  SetCompressor /SOLID lzma

  !system 'MySign "bin\DEBUG\${APP}.exe"'
  !finalize 'MySign "%1"'
!endif

; The default installation directory
InstallDir "$PROGRAMFILES\${APP}"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\${APP}" "Install_Dir"

; Request application privileges for Windows Vista
RequestExecutionLevel admin

XPStyle on
LoadLanguageFile "${NSISDIR}\Contrib\Language files\Japanese.nlf"

;--------------------------------

; Pages

Page directory
Page components
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

Function .onInit
!ifdef INNER
  ; If INNER is defined, then we aren't supposed to do anything except write out
  ; the uninstaller.  This is better than processing a command line option as it means
  ; this entire code path is not present in the final (real) installer.
  SetSilent silent
  WriteUninstaller "$%TEMP%\uninstall.exe"
  SetErrorLevel 0  ; avoid exit code 2
  Quit  ; just bail out quickly when running the "inner" installer
!endif
FunctionEnd

; The stuff to install
Section ""
!ifndef INNER

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File /r /x "*.vshost.*" "bin\DEBUG\*.*"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "SOFTWARE\${APP}" "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP}" "DisplayName" "${APP} ${VER}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP}" "NoRepair" 1
  File "$%TEMP%\uninstall.exe"

!endif  
SectionEnd

Section "デスクトップに MyUploader ショートカット作成"
  
  CreateShortcut "$DESKTOP\MyUploader.lnk" "$INSTDIR\${APP}.exe" "" "$INSTDIR\${APP}.exe"
  
SectionEnd

Section "セットアップ後に起動"
  Exec '"$INSTDIR\${APP}.exe"'
SectionEnd

Section
  IfErrors +2
    SetAutoClose true
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP}"
  DeleteRegKey HKLM "SOFTWARE\${APP}"

  ; Remove files and uninstaller
  Delete "$INSTDIR\${APP}.exe"
  Delete "$INSTDIR\uninstall.exe"

  ; Remove shortcuts, if any
  Delete "$DESKTOP\MyUploader.lnk"

  ; Remove directories used
  RMDir /r "$INSTDIR"

SectionEnd
