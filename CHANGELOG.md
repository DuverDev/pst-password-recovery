# Cambios / Changelog

## 2.2.1 — 2026-11-18

### Legal y marca
- Términos de uso iguales a los de [dmdevlabs.com/terminos](https://dmdevlabs.com/terminos), en español e inglés (cambian con el idioma de la app); también en `TERMINOS.md`, incluido en el `.zip`.
- Marca unificada como **DMDev Labs** (también la carpeta de preferencias en `%LOCALAPPDATA%`; se sigue leyendo la anterior para no perder el tema elegido).

## 2.2.0 — 2026-11-14

### Interfaz y experiencia de usuario
- **Rediseño WPF en dos columnas:** panel operativo izquierdo e historial de sesión y KPIs a la derecha.
- **Tema Claro activado por defecto** y **Modo Oscuro** conmutables en tiempo real desde la barra superior.
- **Selector bilingüe ES / EN integrado** para alternar todos los textos de la interfaz sin reiniciar.
- **3 tarjetas KPI de diagnóstico** (`Tamaño`, `CRC-32`, `Tiempo`) y **tarjeta Hero** destacada con botón directo para copiar la clave recuperada.
- **Historial de análisis** de la sesión actual.

### Legal y distribución
- **Avisos legales y términos de uso bilingües:** licencia GNU GPL-3.0, cláusula estricta «TAL CUAL» (*AS IS*), uso limitado a archivos `.pst` propios o con autorización expresa del titular y no afiliación con Microsoft Corporation.
- **Compilación automatizada en GitHub Actions (`windows-latest`):** paquetes generados en la nube sin certificados locales autofirmados y verificados con archivos `.sha256`.

### Nota técnica sobre antivirus (falsos positivos) y verificación (`v2.2.0`)
- **Hash oficial `PST_PasswordRecovery.exe` (SHA-256):** `44ea06fa826d2832c0d684c87daf6daeae2df7477b7a942ed52519dadc2c87c9`
- **Hash oficial `PST_PasswordRecovery_v2.2.0.zip` (SHA-256):** `2384c709bf57e5dae5a0bdcf653622ed277ab388bf2c0d3823cdf4c8d15d2dd0`
- **Informe en VirusTotal:** https://www.virustotal.com/gui/file/44ea06fa826d2832c0d684c87daf6daeae2df7477b7a942ed52519dadc2c87c9
- **100 % offline y auditable:** la aplicación no realiza conexiones de red ni recopila telemetría; procesa el archivo `.pst` localmente en modo de solo lectura bajo licencia GPL-3.0.
- Alertas heurísticas genéricas (*HackTool*, *Riskware*) pueden deberse a la carga en memoria de las bibliotecas embebidas (`PSTParse.dll` y `MiscParseUtilities.dll` mediante `Assembly.Load` para distribuir un único `.exe` portátil), al análisis CRC-32 sobre contraseñas PST y a la ausencia de un certificado comercial EV de pago. Puedes verificar el hash SHA-256 en VirusTotal o compilar desde el código fuente.

## 2.1.1 — 2026-10-23

- Corrección crítica en el selector de tema: migración a `DynamicResource` y actualización segura de pinceles inmutables para evitar `InvalidOperationException` al activar el Modo Claro.

## 2.1.0 — 2026-10-23

- Lanzamiento de la serie 2.1 con filtros de seguridad reforzados en la selección y apertura de archivos `.pst`.
