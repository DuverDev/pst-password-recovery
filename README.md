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

### 🛡️ Verificación VirusTotal — Último Release (`v2.2.0`)

| Archivo | Formato | SHA-256 | VirusTotal |
| :--- | :---: | :--- | :---: |
| **`PST_PasswordRecovery.exe`** | Ejecutable único | `44ea06fa826d2832c0d684c87daf6daeae2df7477b7a942ed52519dadc2c87c9` | [Ver análisis en VirusTotal](https://www.virustotal.com/gui/file/44ea06fa826d2832c0d684c87daf6daeae2df7477b7a942ed52519dadc2c87c9) |
| **`PST_PasswordRecovery_v2.2.0.zip`** | Paquete portable | `2384c709bf57e5dae5a0bdcf653622ed277ab388bf2c0d3823cdf4c8d15d2dd0` | — |

<details>
<summary>ℹ️ ¿Por qué algunos antivirus pueden mostrar alertas heurísticas? (Falsos positivos)</summary>

1. **Carga en memoria (`Assembly.Load`):** Para distribuir un único `.exe` portable sin DLLs sueltas, `PSTParse.dll` y `MiscParseUtilities.dll` van embebidas como recursos y se cargan en memoria. Varios motores marcan heurísticamente ese patrón.
2. **Categoría "Password Recovery":** Ciertos antivirus etiquetan de forma genérica cualquier utilidad de recuperación de contraseñas como *HackTool* o *Riskware*, aunque solo calcule colisiones matemáticas CRC-32 locales sin red.
3. **Sin certificado comercial EV:** Es software libre gratuito compilado directamente por GitHub Actions desde este repositorio público, sin certificado corporativo de pago. Puedes compilarlo tú mismo con `msbuild`.

</details>

---

## Como funciona

Microsoft Outlook protege los archivos PST con un hash CRC32 debil. La contraseña no cifra los datos del archivo, solo actua como una barrera de acceso en la interfaz de Outlook. Esta herramienta encuentra una colision CRC32 valida que permite abrir el archivo protegido.

## Características (v2.2.0)

- Interfaz WPF renovada en dos columnas (panel operativo izquierdo e historial de sesión/KPIs derecho)
- Tema **Claro por defecto** y **Modo Oscuro** con cambio dinámico en tiempo real
- Selector de idioma **ES / EN** integrado
- 3 tarjetas de diagnóstico KPI (`Tamaño`, `CRC-32`, `Tiempo`) + tarjeta Hero con clave recuperada
- Motor CRC-32 optimizado con búsqueda por colisión y reporte técnico detallado
- Ejecutable portable (sin dependencias externas, 100 % offline y de solo lectura)
- Sin telemetría ni recopilación de datos

## Uso

1. Ejecuta `PST_PasswordRecovery.exe`
2. Haz clic en **Buscar** y selecciona un archivo `.pst`
3. Haz clic en **Ejecutar Análisis**
4. La clave recuperada aparecerá destacada en la tarjeta Hero junto al log técnico y el historial de sesión
5. Usa **Copiar Clave** para guardarla en el portapapeles

## Aviso legal y condiciones de uso

Se entrega «tal cual» (*as is*), sin garantías, bajo **GNU GPL-3.0**. Uso limitado a archivos `.pst` propios o con autorización expresa del titular legítimo (Ley 1273 de 2009 de Colombia y normas equivalentes). Al usarla aceptas las [condiciones de uso](TERMINOS.md), las mismas de [dmdevlabs.com/terminos](https://dmdevlabs.com/terminos). Microsoft y Outlook son marcas de Microsoft Corporation; DMDev Labs no está afiliado, patrocinado ni respaldado por Microsoft.

## Compilacion

Requisitos: .NET Framework 4.0 o superior.

```
msbuild PST_PasswordRecovery.csproj /p:Configuration=Release /p:Platform=x86
```

El ejecutable se genera en `bin\Release\PST_PasswordRecovery.exe`.

## Licencia

GPL-3.0 - Ver [LICENSE](LICENSE)

## Creditos

- **DMDev Labs** - [www.dmdevlabs.com](https://www.dmdevlabs.com)
- [PSTParse](https://github.com/declspec/PSTParse) - Libreria para parsear archivos PST
