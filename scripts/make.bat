rmdir /S /Q output
mkdir output
SET GOBIN=%CD%\bin
SET DEVENV_PATH=%programfiles(x86)%\Microsoft Visual Studio 12.0\Common7\IDE
SET PATH=%GOBIN%;%GOROOT%;%PATH%;%DEVENV_PATH%
SET GOPATH=%CD%
SET CONTAINERIZER_BIN=%CD%\src\\github.com\cloudfoundry-incubator\garden-windows\containerizer\Containerizer\bin\Containerizer.exe

for /f "tokens=*" %%a in ('git rev-parse HEAD') do (
    set VERSION=%%a
)
IF DEFINED APPVEYOR_BUILD_VERSION (SET VERSION=%APPVEYOR_BUILD_VERSION%-%VERSION%)

:: Visual Studio must be in path
where devenv
if errorLevel 1 ( echo "devenv was not found on PATH")

:: https://visualstudiogallery.msdn.microsoft.com/9abe329c-9bba-44a1-be59-0fbf6151054d
REGEDIT.EXE  /S  "%~dp0\fix_visual_studio_building_msi.reg" || exit /b 1

:: install the binaries in %GOBIN%
go install github.com/onsi/ginkgo/ginkgo || exit /b 1
go install github.com/onsi/gomega || exit /b 1

SET GOBIN=%CD%\GardenWindowsRelease\GardenWindowsMSI\go-executables

:: Install garden-windows to the MSI go-executables directory
go install github.com/cloudfoundry-incubator/garden-windows || exit /b 1

pushd src\github.com\cloudfoundry-incubator\garden-windows\greenhouse-security-fixtures || exit /b 1
  call make.bat || exit /b 1
popd

pushd src\github.com\cloudfoundry-incubator\garden-windows\containerizer || exit /b 1
  call make.bat || exit /b 1
popd

:: Run the tests

ginkgo -r -noColor src/github.com/cloudfoundry-incubator/garden-windows || exit /b 1

pushd GardenWindowsRelease || exit /b 1
  rmdir /S /Q packages
  nuget restore || exit /b 1
  echo SHA: %VERSION% > RELEASE_SHA
  devenv GardenWindowsMSI\GardenWindowsMSI.vdproj /build "Release" || exit /b 1
  xcopy GardenWindowsMSI\Release\GardenWindows.msi ..\output\ || exit /b 1
popd

move /Y output\GardenWindows.msi output\GardenWindows-%VERSION%.msi || exit /b 1
:: running the following command without the echo part will prompt
:: the user to specify whether the destination is a directory (D) or
:: file (F). we echo F to select file.
echo F | xcopy scripts\setup.ps1 output\setup-%VERSION%.ps1 || exit /b 1
