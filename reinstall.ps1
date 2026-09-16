$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$androidSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
$javaSdk = 'C:\Program Files\Microsoft\jdk-21.0.12.101-hotspot'
$project = Join-Path $repositoryRoot 'src\Anthropometry.App\Anthropometry.App.csproj'

Push-Location -LiteralPath $repositoryRoot
try {
    $env:JAVA_HOME = $javaSdk
    $env:ANDROID_HOME = $androidSdk

    Write-Host 'Rebuilding and installing Anthropometry.App on the Android emulator...' -ForegroundColor Cyan
    & dotnet build $project `
        -t:Run `
        -f net10.0-android `
        -p:TargetFrameworks=net10.0-android `
        -c Debug `
        -m:1 `
        -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
        -p:JavaSdkDirectory="$env:JAVA_HOME" `
        -p:AcceptAndroidSDKLicenses=True `
        -p:PublishTrimmed=false `
        -p:RunAOTCompilation=false

    if ($LASTEXITCODE -eq 0) {
        Write-Host 'The app was rebuilt, installed, and launched.' -ForegroundColor Green
    }
    else {
        Write-Host "The build failed with exit code $LASTEXITCODE." -ForegroundColor Red
    }
}
catch {
    Write-Host "The reinstall failed: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Pop-Location
    Read-Host 'Press Enter to close'
}
