!define APP_NAME "Flint"
!define APP_VERSION "0.2.9"
!define APP_PUBLISHER "Additional Work"
!define APP_DEVELOPER "Jessenth"
!define APP_URL "https://jessenth.com"
!define APP_TAGLINE "The browser that gets out of your way."
!define APP_EXE "Flint.exe"
!define APP_REGKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Flint"

Unicode true
SetCompressor /SOLID lzma

Name "${APP_NAME} ${APP_VERSION}"
OutFile "setup\FlintSetup.exe"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "${APP_REGKEY}" "InstallLocation"
RequestExecutionLevel admin

!include "MUI2.nsh"
!include "FileFunc.nsh"

!define MUI_ABORTWARNING
!define MUI_ICON "flint.ico"
!define MUI_UNICON "flint.ico"
!define MUI_WELCOMEPAGE_TITLE "${APP_NAME}"
!define MUI_WELCOMEPAGE_TEXT "${APP_TAGLINE}$\r$\n$\r$\nThis will install ${APP_NAME} ${APP_VERSION} on your computer."
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Launch ${APP_NAME}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Install"
    SetOutPath "$INSTDIR"
    File /r /x "*.pdb" /x "*.xml" "publish\*"

    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}"
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\Uninstall ${APP_NAME}.lnk" "$INSTDIR\Uninstall.exe"

    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0

    WriteRegStr   HKLM "${APP_REGKEY}" "DisplayName"      "${APP_NAME}"
    WriteRegStr   HKLM "${APP_REGKEY}" "DisplayVersion"   "${APP_VERSION}"
    WriteRegStr   HKLM "${APP_REGKEY}" "Publisher"        "${APP_PUBLISHER}"
    WriteRegStr   HKLM "${APP_REGKEY}" "URLInfoAbout"     "${APP_URL}"
    WriteRegStr   HKLM "${APP_REGKEY}" "InstallLocation"  "$INSTDIR"
    WriteRegStr   HKLM "${APP_REGKEY}" "UninstallString"  "$INSTDIR\Uninstall.exe"
    WriteRegStr   HKLM "${APP_REGKEY}" "DisplayIcon"      "$INSTDIR\${APP_EXE}"
    WriteRegDWORD HKLM "${APP_REGKEY}" "EstimatedSize"    "$0"
    WriteRegDWORD HKLM "${APP_REGKEY}" "NoModify"         1
    WriteRegDWORD HKLM "${APP_REGKEY}" "NoRepair"         1
SectionEnd

Section "Uninstall"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"

    Delete "$DESKTOP\${APP_NAME}.lnk"
    RMDir /r "$SMPROGRAMS\${APP_NAME}"

    RMDir /r "$LOCALAPPDATA\Flint\WebView2"

    DeleteRegKey HKLM "${APP_REGKEY}"
SectionEnd
