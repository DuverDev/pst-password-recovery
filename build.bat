@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo   PST Password Recovery - Release Build
echo ========================================
echo.

set "MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if not exist "%MSBUILD%" (
    echo ERROR: No se encontro MSBuild.exe en %MSBUILD%
    exit /b 1
)

echo [1/3] Compilando PST_PasswordRecovery.csproj (Release^|x86)...
"%MSBUILD%" PST_PasswordRecovery.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal /nologo
if errorlevel 1 (
    echo ERROR: Fallo la compilacion con MSBuild.
    exit /b 1
)

if not exist "bin\Release\PST_PasswordRecovery.exe" (
    echo ERROR: No se genero bin\Release\PST_PasswordRecovery.exe
    exit /b 1
)

echo.
echo [2/3] Empaquetando dist\ y calculando SHA-256...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $asm=Get-Content 'Properties\AssemblyInfo.cs' -Raw -Encoding UTF8; $q=[char]34; $m=[regex]::Match($asm, ('AssemblyFileVersion\(' + $q + '(\d+\.\d+\.\d+)')); if(-not $m.Success){throw 'No se pudo leer AssemblyFileVersion'}; $ver=$m.Groups[1].Value; $distDir='dist'; $pkgName='PST_PasswordRecovery_v'+$ver; $pkgDir=Join-Path $distDir $pkgName; $zipPath=Join-Path $distDir ($pkgName+'.zip'); New-Item -ItemType Directory -Force -Path $pkgDir | Out-Null; Copy-Item 'bin\Release\PST_PasswordRecovery.exe' (Join-Path $distDir 'PST_PasswordRecovery.exe') -Force; Copy-Item 'bin\Release\PST_PasswordRecovery.exe' (Join-Path $pkgDir 'PST_PasswordRecovery.exe') -Force; if(Test-Path 'LICENSE'){Copy-Item 'LICENSE' (Join-Path $pkgDir 'LICENSE') -Force}; if(Test-Path 'INSTRUCCIONES.txt'){Copy-Item 'INSTRUCCIONES.txt' (Join-Path $pkgDir 'INSTRUCCIONES.txt') -Force}; if(Test-Path 'TERMINOS.md'){Copy-Item 'TERMINOS.md' (Join-Path $pkgDir 'TERMINOS.md') -Force}; if(Test-Path $zipPath){Remove-Item $zipPath -Force}; Compress-Archive -Path (Join-Path $pkgDir '*') -DestinationPath $zipPath -CompressionLevel Optimal; foreach($f in @((Join-Path $distDir 'PST_PasswordRecovery.exe'), $zipPath)){ $h=(Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash.ToLowerInvariant(); $l=[System.IO.Path]::GetFileName($f); ($h+' *'+$l) | Set-Content -LiteralPath ($f+'.sha256') -Encoding ascii -NoNewline; Write-Host ('SHA-256 ('+$l+'): '+$h) }"
if errorlevel 1 (
    echo ERROR: Fallo el empaquetado en dist\.
    exit /b 1
)

echo.
echo [3/3] Build completado exitosamente en dist\.
exit /b 0
