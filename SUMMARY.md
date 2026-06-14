# ArcGIS Monitor Excel Reporter - Resumen de la Solución

## 📋 Descripción General

**ArcGIS Monitor Excel Reporter** es una aplicación de consola .NET 8 que genera reportes en Excel con datos completos extraídos de ArcGIS Monitor, incluyendo métricas, componentes, servicios e información de salud del sistema.

## 🎯 Objetivo Principal

Automatizar la extracción de datos desde ArcGIS Monitor y generar reportes profesionales en formato Excel que faciliten:
- 📊 Análisis de métricas y tendencias
- 🔍 Auditoría y cumplimiento normativo
- 📈 Seguimiento de rendimiento
- 📋 Documentación de estado del sistema

## 🏗️ Arquitectura

La solución está compuesta por:

### 1. **ArcGISMonitorExcelReporterLib** (Biblioteca)
Componente principal contiene:
- 🔌 **Cliente API** - Comunicación con ArcGIS Monitor
- ⚙️ **Configuración** - Modelos de configuración JSON/programática
- 📦 **Modelos** - Entidades de dominio (Collections, Components, Metrics, etc.)
- 📝 **Reporting** - Lógica de generación de reportes
- 📚 **Samples** - Ejemplos de uso

### 2. **ArcGISMonitorExcelReporter** (Aplicación de Consola)
Punto de entrada que:
- Lee configuración desde `config.json`
- Valida parámetros
- Invoca la librería
- Genera archivo Excel
- Proporciona logging estructurado

## 🔑 Características Principales

| Característica | Descripción |
|---|---|
| **Reportes Excel Completos** | Múltiples hojas con datos formateados y estructurados |
| **Autenticación Segura** | Soporte para contraseñas en texto plano o Base64 |
| **Soporte de Zonas Horarias** | IANA timezone identifiers para cálculos precisos |
| **Filtrado Flexible** | Por colecciones, tipos de componentes, métricas |
| **Series de Tiempo** | Datos históricos con agregación configurable |
| **Dual Configuration** | JSON o configuración programática |
| **Ejecutables Autocontenidos** | Windows y Linux sin dependencias externas |
| **Versionado Automático** | Sistema de build numbers diario (yyyy.MM.dd.BuildNumber) |
| **CI/CD Listo** | GitHub Actions para builds y publicaciones automatizadas |

## 📦 Dependencias Principales

```json
{
  "ClosedXML": "Generación de archivos Excel",
  "Serilog": "Logging estructurado",
  "System.Text.Json": "Serialización JSON",
  ".NET 8.0": "Runtime base"
}
```

## 🚀 Flujo de Uso

```
┌─────────────────────────────────────────────────────────────┐
│  Usuario                                                    │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 1. Crea config.json
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  ArcGISMonitorExcelReporter.exe                             │
├─────────────────────────────────────────────────────────────┤
│  1. Lee configuración                                       │
│  2. Valida parámetros                                       │
│  3. Autentica con ArcGIS Monitor                            │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 4. Invoca ArcGISMonitorExcelReporterLib
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  ArcGISMonitorExcelReporterLib                              │
├─────────────────────────────────────────────────────────────┤
│  1. Conecta a ArcGIS Monitor API                            │
│  2. Extrae Collections                                      │
│  3. Extrae Components                                       │
│  4. Extrae Metrics                                          │
│  5. Obtiene históricos (time-series)                        │
└────┬────────────────────────────────────────────────────────┘
	 │
	 │ 6. Construye libro de Excel
	 ▼
┌─────────────────────────────────────────────────────────────┐
│  Report.xlsx                                                │
├─────────────────────────────────────────────────────────────┤
│  • Collections Sheet                                        │
│  • Components Sheet                                         │
│  • Metrics Summary Sheet                                    │
│  • Alerts Sheet                                             │
│  • Time Series Sheet (opcional)                             │
└─────────────────────────────────────────────────────────────┘
```

## ⚙️ Sistema de Versionado

### Formato
```
yyyy.MM.dd.BuildNumber
```

### Comportamiento
- **Builds Locales**: BuildNumber incrementa diariamente (1, 2, 3...) y se reinicia a medianoche
- **Builds CI (GitHub Actions)**: BuildNumber vinculado a `github.run_number` para unicidad
- **Multi-Platform CI**: Windows (win-x64) y Linux (linux-x64) comparten el mismo BuildNumber

### Archivos de Control
- `BuildNumber.txt` - Número actual (git-ignored)
- `LastDatePrefix.txt` - Fecha del último build (git-ignored)
- `BuildNumberFromCI.txt` - Marcador de detección CI (git-ignored)

**Ejemplo**: `2025.01.27.3` = Tercer build del 27 de enero de 2025

## 🔄 CI/CD con GitHub Actions

### Workflow: `.github/workflows/release.yml`

```
┌─────────────────────────────────┐
│  Trigger: Push de release tag   │
└────┬────────────────────────────┘
	 │
	 ├─ Checkout código
	 ├─ Setup .NET 8
	 ├─ Calcular versión (yyyy.MM.dd.BuildNumber)
	 ├─ Pre-poblar BuildNumber.txt
	 ├─ Pre-poblar LastDatePrefix.txt
	 ├─ Crear marcador BuildNumberFromCI.txt
	 │
	 ├─ Publish win-x64 (self-contained)
	 ├─ Empaquetar artifacts Windows
	 │
	 ├─ Restaurar marcador BuildNumberFromCI.txt
	 ├─ Publish linux-x64 (self-contained)
	 ├─ Empaquetar artifacts Linux
	 │
	 ├─ Crear release en GitHub
	 └─ Subir artifacts a release
```

### Propósito de `BuildNumberFromCI.txt`
Previene que `VersionInfo.targets` incremente el build number cuando se compilan plataformas múltiples (Windows y Linux) en la misma ejecución de workflow. Esto asegura que ambos ejecutables tengan el mismo número de versión.

## 📊 Estructura de Excel Generado

| Sheet | Contenido |
|-------|----------|
| **Collections** | Lista de colecciones monitoreadas |
| **Components** | Componentes individuales (hosts, bases de datos, etc.) |
| **Metrics Summary** | Resumen de métricas con estadísticas (min, max, promedio) |
| **Alerts** | Alertas activas y niveles de severidad |
| **Time Series** | Datos históricos de métricas con timestamps |

**Características del Formato:**
- Encabezados con colores de fondo
- Paneles congelados para navegación fácil
- Ancho de columnas auto-ajustado
- Formato numérico apropiado para métricas

## 🔐 Configuración de Seguridad

### Autenticación
```json
{
  "server": {
	"username": "admin",
	"password": "contraseña",
	"password_encoding": false,
	"ignore_ssl_errors": false
  }
}
```

### Opciones
| Opción | Valor | Descripción |
|--------|-------|-------------|
| `password_encoding` | `true` | La contraseña está codificada en Base64 |
| `password_encoding` | `false` | La contraseña es texto plano |
| `ignore_ssl_errors` | `false` | Validar certificados SSL (RECOMENDADO) |
| `ignore_ssl_errors` | `true` | Ignorar errores SSL (NO RECOMENDADO para producción) |

## 📋 Requisitos del Sistema

| Componente | Requisito |
|-----------|----------|
| **.NET Runtime** | 8.0 o superior |
| **Plataformas** | Windows x64, Linux x64 |
| **ArcGIS Monitor** | 2023.x o posterior |
| **Excel** | Cualquier aplicación que lea .xlsx (Excel, LibreOffice Calc, Google Sheets, etc.) |

## 📚 Documentación Disponible

| Archivo | Propósito |
|---------|----------|
| **README.md** | Documentación completa en inglés |
| **CHANGELOG.md** | Historial de cambios y versiones |
| **CONTRIBUTING.md** | Guía para contribuir al proyecto |
| **LICENSE** | Licencia MIT |
| **SUMMARY.md** | Este archivo (resumen en español) |
| **BUILD_NUMBER_CI_FIX.md** | Explicación técnica del fix de versionado |

## 🛠️ Desarrollo

### Proyectos en la Solución
- `ArcGISMonitorExcelReporterLib` - Biblioteca principal (.NET 8)
- `ArcGISMonitorExcelReporter` - Aplicación de consola (.NET 8)

### Construir Localmente
```bash
# Debug
dotnet build

# Release
dotnet build --configuration Release

# Publicar para Windows
dotnet publish -c Release -r win-x64 --self-contained

# Publicar para Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

### Estilo de Código
- Convenciones Microsoft C#
- PascalCase para miembros públicos
- camelCase para variables locales
- Documentación XML para APIs públicas
- 4 espacios de indentación

## 🐛 Resolución de Problemas Comunes

| Problema | Causa | Solución |
|----------|-------|----------|
| "No se puede conectar al servidor" | URL incorrecta o servidor inaccesible | Verificar URL y conectividad de red |
| "Credenciales inválidas" | Usuario/contraseña incorrectos | Verificar credenciales y permisos |
| "Zona horaria no encontrada" | Identificador IANA inválido | Usar identificadores válidos (UTC, America/New_York, etc.) |
| "Fuera de memoria" | Datos demasiado grandes | Reducir `page_size`, `past_days` o deshabilitar time-series |

## 📞 Soporte

- 📖 [Documentación](README.md)
- 🐛 [Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
- 💬 [Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)

## 📝 Changelog Rápido

```
v2025.01.27.1
├─ Corrección del BuildNumber en GitHub Actions
├─ Documentación XML completa en inglés
├─ README.md en inglés
├─ CHANGELOG.md
├─ CONTRIBUTING.md
├─ LICENSE (MIT)
└─ SUMMARY.md (este archivo)

v2025.01.20.1
└─ Release inicial con funcionalidad completa
```

## 🎯 Roadmap

### Q1 2025
- [ ] Optimización de rendimiento para datasets grandes
- [ ] Soporte para exportación a CSV
- [ ] Generación de reportes en lotes

### Q2 2025
- [ ] API web para generación remota
- [ ] Ejecución de reportes programada
- [ ] Entrega por email
- [ ] Sistema de caché

### Q3 2025
- [ ] Plantillas de reporte personalizadas
- [ ] Integración con Azure
- [ ] Conector PowerBI

### Q4 2025
- [ ] Generación de dashboards
- [ ] Integración de monitoreo en tiempo real
- [ ] Analytics avanzados

## 📜 Licencia

Licenciado bajo la Licencia MIT - Ver [LICENSE](LICENSE)

---

**Última Actualización:** 27 de enero de 2025  
**Versión del Documento:** 1.0  
**Estado del Proyecto:** Desarrollo Activo
