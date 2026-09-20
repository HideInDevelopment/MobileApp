$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$androidSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
$javaSdk = 'C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot'
$androidDebugKeystore = Join-Path $env:LOCALAPPDATA 'Xamarin\Mono for Android\debug.keystore'
$project = Join-Path $repositoryRoot 'src\Anthropometry.App\Anthropometry.App.csproj'
$adb = Join-Path $androidSdk 'platform-tools\adb.exe'
$packageName = 'com.companyname.anthropometry.app'
$apk = Join-Path $repositoryRoot 'src\Anthropometry.App\bin\Debug\net10.0-android\com.companyname.anthropometry.app-Signed.apk'

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    # Windows PowerShell formats native stderr output as NativeCommandError,
    # even when the native process succeeds. Start the process through .NET so
    # both streams are captured directly and the exit code is the source of
    # truth.
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    $argumentListProperty = $startInfo.PSObject.Properties['ArgumentList']
    if ($null -ne $argumentListProperty) {
        foreach ($Argument in $Arguments) {
            [void]$startInfo.ArgumentList.Add($Argument)
        }
    }
    else {
        $quotedArguments = foreach ($Argument in $Arguments) {
            '"' + $Argument.Replace('"', '\"') + '"'
        }
        $startInfo.Arguments = $quotedArguments -join ' '
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "Could not start '$FilePath'."
        }

        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()

        $stdout = $stdoutTask.Result
        $stderr = $stderrTask.Result
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }

    $output = (($stdout, $stderr | Where-Object { $_ }) -join [Environment]::NewLine).Trim()

    [pscustomobject]@{
        Output   = $output
        ExitCode = $exitCode
    }
}

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

    $keystoreResult = Invoke-NativeCommand $keytool @('-list', '-v', '-keystore', $androidDebugKeystore, '-alias', 'androiddebugkey', '-storepass', 'android', '-keypass', 'android')
    if ($keystoreResult.ExitCode -ne 0) {
        throw "The Android debug keystore could not be read. Exit code $($keystoreResult.ExitCode)."
    }

    $keystoreOutput = $keystoreResult.Output
    $expectedCertificateMatch = [regex]::Match($keystoreOutput, '(?im)^\s*SHA256:\s*([0-9a-f:]+)')
    $apkResult = Invoke-NativeCommand $apksigner.FullName @('verify', '--print-certs', $apk)
    if ($apkResult.ExitCode -ne 0) {
        throw "The APK certificate could not be read. Exit code $($apkResult.ExitCode)."
    }

    $apkOutput = $apkResult.Output
    $actualCertificateMatch = [regex]::Match($apkOutput, '(?im)certificate SHA-256 digest:\s*([0-9a-f]+)')
    if (-not $expectedCertificateMatch.Success -or -not $actualCertificateMatch.Success) {
        throw 'The APK certificate could not be verified after the build.'
    }

    $expectedCertificate = $expectedCertificateMatch.Groups[1].Value.Replace(':', '').ToLowerInvariant()
    $actualCertificate = $actualCertificateMatch.Groups[1].Value.ToLowerInvariant()
    if ($expectedCertificate -ne $actualCertificate) {
        throw "The APK was signed with an unexpected certificate. Expected '$expectedCertificate' but found '$actualCertificate'."
    }

    $installResult = Invoke-NativeCommand $adb @('-e', 'install', '-r', $apk)
    $installOutput = $installResult.Output
    $installExitCode = $installResult.ExitCode
    if ($installOutput) {
        Write-Host $installOutput.TrimEnd()
    }

    if ($installExitCode -ne 0 -and $installOutput -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE') {
        Write-Warning 'The emulator has this app installed with a different signing key.'
        Write-Warning 'Android requires uninstalling that copy before this APK can be installed.'
        Write-Warning 'Uninstalling clears this app data on the emulator. Export any profile you need first.'
        $confirmation = Read-Host "Type RESET to uninstall '$packageName' and continue"
        if ($confirmation -cne 'RESET') {
            throw 'Installation cancelled. The existing app and its data were left untouched.'
        }

        $uninstallResult = Invoke-NativeCommand $adb @('-e', 'uninstall', $packageName)
        if ($uninstallResult.Output) {
            Write-Host $uninstallResult.Output.TrimEnd()
        }
        if ($uninstallResult.ExitCode -ne 0) {
            throw "The old APK could not be uninstalled. Exit code $($uninstallResult.ExitCode)."
        }

        $installResult = Invoke-NativeCommand $adb @('-e', 'install', $apk)
        $installOutput = $installResult.Output
        $installExitCode = $installResult.ExitCode
        if ($installOutput) {
            Write-Host $installOutput.TrimEnd()
        }
    }

    if ($installExitCode -ne 0) {
        throw "The APK installation failed with exit code $installExitCode."
    }

    $forceStopResult = Invoke-NativeCommand $adb @('-e', 'shell', 'am', 'force-stop', $packageName)
    if ($forceStopResult.ExitCode -ne 0) {
        throw "The app could not be stopped before launch. Exit code $($forceStopResult.ExitCode)."
    }

    $launchResult = Invoke-NativeCommand $adb @('-e', 'shell', 'monkey', '-p', $packageName, '1')
    if ($launchResult.Output) {
        Write-Host $launchResult.Output.TrimEnd()
    }
    if ($launchResult.ExitCode -ne 0) {
        throw "The app could not be launched with exit code $($launchResult.ExitCode)."
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
