# PST Password Recovery

Herramienta open source para recuperar contraseñas de archivos Outlook PST mediante colision CRC32.

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/DuverDev/pst-password-recovery?style=for-the-badge&logo=github)](https://github.com/DuverDev/pst-password-recovery/releases/latest)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg?style=for-the-badge)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg?style=for-the-badge&logo=windows)](https://github.com/DuverDev/pst-password-recovery/releases/latest)

---

## 📥 Descargas (Release Oficial)

No necesitas compilar el proyecto para utilizarlo. Puedes descargar directamente el ejecutable o el paquete portable desde los Releases oficiales:

👉 **[Descargar la versión más reciente (Releases)](https://github.com/DuverDev/pst-password-recovery/releases/latest)**

Archivos disponibles en cada versión:
- **`PST_PasswordRecovery.exe`**: Ejecutable autónomo y portable (single-exe, no requiere instalación ni DLLs adicionales).
- **`PST_PasswordRecovery_vX.X.X.zip`**: Paquete comprimido portable que incluye el ejecutable, guía rápida de instrucciones (`INSTRUCCIONES.txt`) y la licencia.

> [!TIP]
> Cada release oficial incluye sus hashes de integridad **SHA-256** y el enlace al análisis de seguridad en **VirusTotal**, además de la justificación técnica transparente sobre falsos positivos de antivirus.

---

## Como funciona

Microsoft Outlook protege los archivos PST con un hash CRC32 debil. La contraseña no cifra los datos del archivo, solo actua como una barrera de acceso en la interfaz de Outlook. Esta herramienta encuentra una colision CRC32 valida que permite abrir el archivo protegido.

## Caracteristicas

- Recuperacion de contraseñas PST via colision CRC32
- Tema oscuro y claro
- Interfaz moderna WPF
- Ejecutable portable (sin dependencias externas)
- 100% offline, sin conexion a internet
- Sin telemetria ni recopilacion de datos

## Uso

1. Ejecuta `PST_PasswordRecovery.exe`
2. Haz clic en **Buscar** y selecciona un archivo `.pst`
3. Haz clic en **Ejecutar Analisis**
4. Si el archivo tiene contraseña, la clave recuperada aparecera en los resultados
5. Usa **Copiar Clave** para guardarla en el portapapeles

## Compilacion

Requisitos: .NET Framework 4.0 o superior.

```
msbuild PST_PasswordRecovery.csproj /p:Configuration=Release /p:Platform=x86
```

El ejecutable se genera en `bin\Release\PST_PasswordRecovery.exe`.

## Disclaimer

Esta herramienta esta diseñada exclusivamente para recuperar contraseñas de archivos PST propios o con autorizacion del propietario. El usuario es responsable del uso que le de a la aplicacion. DMDev Labs no se hace responsable del uso indebido.

## Licencia

GPL-3.0 - Ver [LICENSE](LICENSE)

## Creditos

- **DMDev Labs** - [www.dmdevlabs.com](https://www.dmdevlabs.com)
- [PSTParse](https://github.com/declspec/PSTParse) - Libreria para parsear archivos PST
