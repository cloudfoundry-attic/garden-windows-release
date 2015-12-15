$productcode = [guid]::NewGuid().ToString().ToUpper()
$packagecode = [guid]::NewGuid().ToString().ToUpper()
$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
push-location $dir
  $oldversion = """ProductVersion"" = ""8:1.0.0"""
  $version = """ProductVersion"" = ""8:$env:APPVEYOR_BUILD_VERSION"""
  $oldproductcode = """ProductCode"" = ""8:{48EC16D7-8B39-4939-8A31-2D79AD160966}"""
  $productcode = """ProductCode"" = ""8:{$productcode}"""
  $oldpackagecode = """PackageCode"" = ""8:{11FF02F8-EAA6-4A21-AE8A-E34A2D9E4529}"""
  $packagecode = """PackageCode"" = ""8:{$packagecode}"""
  $oldsubject = """Subject"" = ""8:"""
  $subject = """Subject"" = ""8:GardenWindows-$env:APPVEYOR_BUILD_VERSION-$env:APPVEYOR_REPO_COMMIT"""
  (get-content ..\GardenWindowsRelease\GardenWindowsMSI\GardenWindowsMSI.vdproj).replace("$oldversion","$version").replace("$oldproductcode", "$productcode").replace("$oldpackagecode", "$packagecode").replace("$oldsubject", "$subject") | set-content ..\GardenWindowsRelease\GardenWindowsMSI\GardenWindowsMSI.vdproj
pop-location
