# Debug Configuration Guide

Este documento explica cómo configurar y usar los perfiles de depuración para el proyecto ArcGIS Monitor Excel Reporter.

## Archivo launchSettings.json

El archivo `Properties/launchSettings.json` contiene perfiles de depuración predefinidos que facilitan el desarrollo y testing.

### Perfiles Disponibles

#### 1. **Development - inocar.json**
Perfil por defecto para desarrollo usando tu configuración local.

```json
"commandLineArgs": "-f \"C:\\Users\\Mtorres\\source\\repos\\ArcGISMonitorExcelReporter\\data\\inocar.json\""
```

**Uso en Visual Studio:**
- Selecciona este perfil en la barra de herramientas
- Presiona F5 para iniciar depuración

#### 2. **Development - Relative Path**
Usa rutas relativas para pruebas con diferentes configuraciones.

```json
"commandLineArgs": "-f \"..\\..\\..\\data\\config.json\""
```

**Uso:** Ideal para probar que las rutas relativas funcionan correctamente.

#### 3. **Sample Configuration**
Usa el archivo de configuración de ejemplo incluido en el proyecto.

```json
"commandLineArgs": "-f \"..\\..\\..\\ArcGISMonitorExcelReporterLib\\Samples\\agm2023x.sample.json\""
```

**Uso:** Perfecto para demos y pruebas iniciales sin necesidad de configuración real.

#### 4. **Test - Help**
Prueba el mensaje de ayuda (sin argumentos).

```json
"commandLineArgs": ""
```

**Uso:** Para verificar que el mensaje de ayuda se muestra correctamente cuando no se proporcionan argumentos.

#### 5. **Production**
Simula un entorno de producción.

```json
"commandLineArgs": "-f \"C:\\Production\\Config\\monitor-config.json\""
"DOTNET_ENVIRONMENT": "Production"
```

**Uso:** Para probar el comportamiento en modo producción (menos logs, etc.).

---

## Cómo Usar en Visual Studio

### Método 1: Selector de Perfiles
1. Busca el dropdown junto al botón "▶ Start" en la barra de herramientas
2. Selecciona el perfil deseado (ej: "Development - inocar.json")
3. Presiona F5 o haz clic en "▶ Start"

### Método 2: Configuración Manual
1. Click derecho en el proyecto **ArcGISMonitorExcelReporter**
2. Selecciona **Properties**
3. Ve a la pestaña **Debug** > **General**
4. En "Command line arguments" ingresa: `-f "ruta/a/tu/config.json"`
5. Guarda y ejecuta con F5

---

## Cómo Usar desde Línea de Comandos

### Desarrollo
```bash
# Con ruta absoluta
dotnet run --project ArcGISMonitorExcelReporter -f "C:\Path\To\config.json"

# Con ruta relativa
dotnet run --project ArcGISMonitorExcelReporter -f "../data/config.json"

# Activar modo debug
$env:DOTNET_ENVIRONMENT="Development"
dotnet run --project ArcGISMonitorExcelReporter -f "config.json"
```

### Producción (compilado)
```bash
# Windows
ArcGISMonitorExcelReporter.exe -f "C:\Production\config.json"

# Linux/Mac
./ArcGISMonitorExcelReporter -f /var/config/monitor.json
```

---

## Variables de Entorno

### DOTNET_ENVIRONMENT
Controla el nivel de logging y comportamiento de la aplicación.

**Valores:**
- `Development`: Logs detallados (Debug level), información de depuración en consola
- `Production`: Logs normales (Information level), sin información de debug

**Configurar en Windows:**
```powershell
# PowerShell
$env:DOTNET_ENVIRONMENT="Development"

# CMD
set DOTNET_ENVIRONMENT=Development
```

**Configurar en Linux/Mac:**
```bash
export DOTNET_ENVIRONMENT=Development
```

---

## Características de Depuración

### Información DEBUG en Consola
Cuando se compila en modo DEBUG (`#if DEBUG`), la aplicación muestra:

```
[DEBUG] Configuration file: config.json
[DEBUG] Full path: C:\Users\...\config.json
[DEBUG] Working directory: C:\Users\...\bin\Debug\net8.0
[DEBUG] Environment: Development
```

Esto ayuda a verificar:
- ✅ La ruta del archivo de configuración parseada correctamente
- ✅ La ruta absoluta resuelta
- ✅ El directorio de trabajo actual
- ✅ El entorno configurado

### Nivel de Logging Dinámico
```csharp
var logLevel = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development"
    ? Serilog.Events.LogEventLevel.Debug
    : Serilog.Events.LogEventLevel.Information;
```

**Desarrollo**: Logs completos incluyendo Debug
**Producción**: Solo Information, Warning, Error, Fatal

---

## Troubleshooting

### Error: "Configuration file not found"
**Solución:**
1. Verifica que la ruta en launchSettings.json sea correcta
2. Revisa la salida `[DEBUG] Full path attempted` para ver la ruta completa
3. Asegúrate de que el archivo existe en esa ubicación

### Los cambios en launchSettings.json no se reflejan
**Solución:**
1. Cierra Visual Studio completamente
2. Elimina las carpetas `bin` y `obj`
3. Reabre Visual Studio y reconstruye (Rebuild)

### El perfil no aparece en el dropdown
**Solución:**
1. Asegúrate de que el archivo esté en `ArcGISMonitorExcelReporter/Properties/launchSettings.json`
2. Verifica que el JSON sea válido (sin errores de sintaxis)
3. Recarga el proyecto (Unload/Reload Project)

---

## Crear Tu Propio Perfil

Edita `Properties/launchSettings.json` y agrega:

```json
"Mi Perfil Custom": {
  "commandName": "Project",
  "commandLineArgs": "-f \"C:\\MisConfigs\\custom.json\"",
  "environmentVariables": {
    "DOTNET_ENVIRONMENT": "Development"
  }
}
```

**Recomendaciones:**
- Usa nombres descriptivos para tus perfiles
- Documenta qué hace cada perfil en comentarios (aunque JSON no soporta comentarios, puedes mantener un README)
- Usa `DOTNET_ENVIRONMENT=Development` para testing local
- Usa `DOTNET_ENVIRONMENT=Production` para simular el entorno real

---

## Argumentos Soportados

| Argumento | Descripción | Requerido | Ejemplo |
|-----------|-------------|-----------|---------|
| `-f <path>` | Ruta al archivo de configuración JSON | ✅ Sí | `-f "config.json"` |
| `-h, --help` | Muestra ayuda | ❌ No | `-h` |

---

## Referencias

- [Visual Studio Launch Profiles](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments)
- [Serilog Configuration](https://github.com/serilog/serilog/wiki/Configuration-Basics)
- [.NET Environment Variables](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables)

---

## Checklist de Depuración

Antes de reportar un problema, verifica:

- [ ] El archivo de configuración existe en la ruta especificada
- [ ] El archivo tiene extensión `.json`
- [ ] El JSON es válido (usa un validador online)
- [ ] Tienes permisos de lectura sobre el archivo
- [ ] La variable `DOTNET_ENVIRONMENT` está configurada si necesitas logs detallados
- [ ] Revisaste los logs en `<config-directory>/logs/`
- [ ] Ejecutaste en modo DEBUG para ver información adicional

---

## Contacto y Soporte

Para más información consulta:
- **Repositorio**: https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter
- **Documentación API**: Ver `ArcGISMonitorExcelReporterLib/Docs/api-documentation.md`
- **Troubleshooting**: Ver `ArcGISMonitorExcelReporterLib/Docs/troubleshooting.md`
