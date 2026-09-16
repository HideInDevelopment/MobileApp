$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$androidSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
$javaSdk = 'C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot'
$androidDebugKeystore = Join-Path $env:LOCALAPPDATA 'Xamarin\Mono for Android\debug.keystore'
$project = Join-Path $repositoryRoot 'src\Anthropometry.App\Anthropometry.App.csproj'
$adb = Join-Path $androidSdk 'platform-tools\adb.exe'
$packageName = 'com.companyname.anthropometry.app'
$apk = Join-Path $repositoryRoot 'src\Anthropometry.App\bin\Debug\net10.0-android\com.companyname.anthropometry.app-Signed.apk'

Push-Location -LiteralPath $repositoryRoot
try {
    $env:JAVA_HOME = $javaSdk
    $env:ANDROID_HOME = $androidSdk

    Write-Host 'Rebuilding and installing Anthropometry.App on the Android emulator...' -ForegroundColor Cyan
    if (-not (Test-Path -LiteralPath $adb)) {
        throw "Android Debug Bridge was not found at '$adb'."
    }
    if (-not (Test-Path -LiteralPath $androidDebugKeystore)) {
        throw "The Android debug keystore was not found at '$androidDebugKeystore'."
    }

    $keytool = Join-Path $javaSdk 'bin\keytool.exe'
    $apksigner = Get-ChildItem -LiteralPath (Join-Path $androidSdk 'build-tools') -Filter 'apksigner.bat' -File -Recurse |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if (-not (Test-Path -LiteralPath $keytool)) {
        throw "The Java keytool was not found at '$keytool'."
    }
    if (-not $apksigner) {
        throw "The Android APK signer was not found below '$androidSdk\build-tools'."
    }

    # The Android signing target can otherwise reuse a stale signed APK after
    # the signing key changes. Remove only this generated artifact; app data
    # on the emulator is preserved.
    if (Test-Path -LiteralPath $apk) {
        Remove-Item -LiteralPath $apk -Force
    }

    # Use a complete APK so XAML and resource changes are always installed,
    # instead of relying on Debug fast deployment caches.
    & dotnet build $project `
        '-t:Rebuild;SignAndroidPackage' `
        -f net10.0-android `
        -c Debug `
        -m:1 `
        -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
        -p:JavaSdkDirectory="$env:JAVA_HOME" `
        -p:AcceptAndroidSDKLicenses=True `
        -p:PublishTrimmed=false `
        -p:RunAOTCompilation=false `
        -p:EmbedAssembliesIntoApk=true `
        -p:AndroidPackageFormat=apk `
        -p:AndroidKeyStore=true `
        -p:AndroidSigningKeyStore="$androidDebugKeystore" `
        -p:AndroidSigningKeyAlias=androiddebugkey `
        -p:AndroidSigningStorePass=android `
        -p:AndroidSigningKeyPass=android

    if ($LASTEXITCODE -ne 0) {
        throw "The build failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $apk)) {
        throw "The build succeeded, but the APK was not found at '$apk'."
    }

    $keystoreOutput = & $keytool -list -v -keystore $androidDebugKeystore -alias androiddebugkey -storepass android -keypass android 2>&1 | Out-String
    $expectedCertificateMatch = [regex]::Match($keystoreOutput, '(?im)^\s*SHA256:\s*([0-9a-f:]+)')
    $apkOutput = & $apksigner.FullName verify --print-certs $apk 2>&1 | Out-String
    $actualCertificateMatch = [regex]::Match($apkOutput, '(?im)certificate SHA-256 digest:\s*([0-9a-f]+)')
    if (-not $expectedCertificateMatch.Success -or -not $actualCertificateMatch.Success) {
        throw 'The APK certificate could not be verified after the build.'
    }

    $expectedCertificate = $expectedCertificateMatch.Groups[1].Value.Replace(':', '').ToLowerInvariant()
    $actualCertificate = $actualCertificateMatch.Groups[1].Value.ToLowerInvariant()
    if ($expectedCertificate -ne $actualCertificate) {
        throw "The APK was signed with an unexpected certificate. Expected '$expectedCertificate' but found '$actualCertificate'."
    }

    & $adb -e install -r $apk
    if ($LASTEXITCODE -ne 0) {
        throw "The APK installation failed with exit code $LASTEXITCODE."
    }

    & $adb -e shell am force-stop $packageName
    & $adb -e shell monkey -p $packageName 1
    if ($LASTEXITCODE -ne 0) {
        throw "The app could not be launched with exit code $LASTEXITCODE."
    }

    Write-Host "The app was rebuilt from '$apk', installed, and launched." -ForegroundColor Green
}
catch {
    Write-Host "The reinstall failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
    Read-Host 'Press Enter to close'
}
