# Anthropometry Tracking App

Private, offline body-measurement tracking with understandable estimates and
personal trends. The app records profiles and measurements locally, explains
body-fat, BMR, and TDEE estimates, and keeps historical results available
without an account or network service.

Measurements and calculated results are estimates for personal tracking only;
they are not medical diagnoses, medical advice, or a replacement for guidance
from a qualified healthcare professional.

## Requirements

- .NET SDK 10.0.303.
- The .NET MAUI workload: `dotnet workload install maui`.
- Android SDK and an Android API level supported by the installed .NET MAUI workload.
- A JDK supported by .NET for Android, preferably the JDK version recommended by the installed workload.

The app stores its SQLite database in the platform private application directory. It does not use a network service, account system, analytics SDK, or synchronization mechanism.

## Build and test

```powershell
dotnet restore -m:1
dotnet build --configuration Release -m:1
dotnet test --configuration Release -m:1
dotnet build src/Anthropometry.App/Anthropometry.App.csproj -f net10.0-android -c Release -m:1
```

The App project also targets plain `net10.0` for ViewModel tests. Its Android target is the runnable application target. If the Android SDK is installed outside the standard location, set `ANDROID_HOME` or pass `-p:AndroidSdkDirectory=<path>` to the Android build.

## Android emulator or device setup

Set the SDK and JDK paths in the PowerShell session used for the build. The exact JDK folder depends on the installation:

```powershell
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-21..."
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:Path = "$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\emulator;$env:Path"
```

Verify that an emulator is running or a USB-debugging device is connected:

```powershell
adb devices
```

Install and run the Android app with:

```powershell
dotnet build src/Anthropometry.App/Anthropometry.App.csproj `
  -t:Run -f net10.0-android -c Debug `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -p:JavaSdkDirectory="$env:JAVA_HOME"
```

If more than one emulator or device is available, pass the target identifier supported by the installed .NET for Android workload with `-p:AndroidDeviceId=<id>`. The app does not require a network connection after the local SDK and build dependencies are installed.

## Architecture

- `Anthropometry.Domain` contains validated entities and pure, versioned formulas.
- `Anthropometry.Application` contains use cases, DTOs, and repository ports.
- `Anthropometry.Infrastructure` contains SQLite migrations, mappings, and repositories.
- `Anthropometry.App` contains MAUI pages, ViewModels, composition, and navigation.

All user measurements and calculated results remain local. Historical results retain their formula identity and version.
