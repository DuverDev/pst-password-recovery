# PST Password Recovery

Herramienta open source para recuperar contraseñas de archivos Outlook PST mediante colision CRC32.

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
