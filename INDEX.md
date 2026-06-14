# Documentación - ArcGIS Monitor Excel Reporter

## 📚 Índice de Documentación

Esta es la documentación central del proyecto **ArcGIS Monitor Excel Reporter**. Selecciona el documento que necesitas según tu rol o necesidad.

---

## 🚀 Para Empezar

### 👤 **Usuario Final**
Quieres usar la aplicación para generar reportes de Excel

- **Inicio Rápido:** [README.md](README.md#quick-start) - Guías paso a paso
- **Configuración:** [README.md](README.md#configuration-reference) - Referencia completa de parámetros
- **Ejemplos:** [config.json.example](config.json.example) - Archivo de configuración de ejemplo
- **Solución de Problemas:** [README.md](README.md#troubleshooting) - Problemas comunes y soluciones

---

## 💻 Para Desarrolladores

### 🛠️ **Contribuir al Proyecto**
Quieres añadir features o corregir bugs

1. **Primero lee:** [CONTRIBUTING.md](CONTRIBUTING.md) - Guía completa de contribución
2. **Clona el repo:**
   ```bash
   git clone https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter.git
   cd ArcGISMonitorExcelReporter
   ```
3. **Setup Local:**
   - [CONTRIBUTING.md#development-setup](CONTRIBUTING.md#development-setup) - Configurar entorno
   - [CONTRIBUTING.md#styleguides](CONTRIBUTING.md#styleguides) - Estilo de código
4. **Envía PR:**
   - [CONTRIBUTING.md#pull-requests](CONTRIBUTING.md#pull-requests) - Proceso de PR

### 📖 **Documentación Técnica**
Necesitas entender la arquitectura interna

- **Arquitectura:** [README.md](README.md#project-structure) - Estructura de proyectos
- **API Library:** XML documentation en `ArcGISMonitorExcelReporterLib/`
- **Samples:** [ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs](ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs)
- **Versioning:** [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) - Explicación del sistema de versiones
- **CI/CD:** [.github/workflows/release.yml](.github/workflows/release.yml) - GitHub Actions workflow

---

## 📋 Referencia Rápida de Documentos

| Documento | Propósito | Audiencia | Tamaño |
|-----------|----------|-----------|--------|
| **[README.md](README.md)** | Documentación oficial completa en inglés | Todos | 13 KB |
| **[SUMMARY.md](SUMMARY.md)** | Resumen ejecutivo en español | Gerentes, Análisis | 12 KB |
| **[CHANGELOG.md](CHANGELOG.md)** | Historial de cambios y versiones | Desarrolladores, Usuarios | 7 KB |
| **[CONTRIBUTING.md](CONTRIBUTING.md)** | Guía para colaboradores | Desarrolladores | 11 KB |
| **[LICENSE](LICENSE)** | Licencia MIT | Legal | 1 KB |
| **[BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md)** | Detalle técnico del fix de versioning | DevOps, Developers | Variable |
| **[config.json.example](config.json.example)** | Plantilla de configuración | Usuarios, Developers | 1 KB |

---

## 🎯 Casos de Uso por Rol

### 👨‍💼 **Project Manager / Stakeholder**
```
Leo                    Lee primero
   ↓
[SUMMARY.md]           Entiendo qué es y para qué sirve
   ↓
[README.md] (Features) Conozco las capacidades principales
   ↓
[CHANGELOG.md]         Veo el progreso del proyecto
```

### 👨‍💻 **Usuario / Operador**
```
Necesito generar reportes
   ↓
[README.md] Quick Start        Pasos para empezar
   ↓
[config.json.example]          Creo mi configuración
   ↓
[README.md] Troubleshooting    Resuelvo problemas
```

### 👨‍🔬 **Desarrollador / Contribuidor**
```
Quiero contribuir
   ↓
[CONTRIBUTING.md]              Setup desarrollo
   ↓
[README.md] Project Structure  Entiendo arquitectura
   ↓
Código con XML Docs            Leo implementation
   ↓
[CONTRIBUTING.md] PR Process   Envío cambios
```

### 🚀 **DevOps / Release Manager**
```
Necesito entender versioning
   ↓
[BUILD_NUMBER_CI_FIX.md]       Sistema de build numbers
   ↓
[.github/workflows/release.yml] Workflow CI/CD
   ↓
[CHANGELOG.md]                 Historial de releases
```

---

## 📊 Estructura del Repositorio

```
ArcGISMonitorExcelReporter/
├── 📋 Documentación (en raiz)
│   ├── README.md                      ← LEER PRIMERO (en inglés)
│   ├── SUMMARY.md                     ← Resumen exejutivo (en español)
│   ├── CHANGELOG.md                   ← Historial de versiones
│   ├── CONTRIBUTING.md                ← Guía para contribuir
│   ├── LICENSE                        ← Licencia MIT
│   ├── BUILD_NUMBER_CI_FIX.md         ← Detalle técnico
│   ├── INDEX.md                       ← Este archivo
│   └── config.json.example            ← Plantilla de config
│
├── 📦 Código Fuente
│   ├── ArcGISMonitorExcelReporterLib/   ← Biblioteca principal
│   │   ├── ArcGISMonitorExcelReporter.cs
│   │   ├── Client/                      ← API Client
│   │   ├── Configuration/               ← Config models
│   │   ├── Models/                      ← Domain models
│   │   ├── Reporting/                   ← Report generation
│   │   └── Samples/                     ← Ejemplos con XML Docs
│   ├── ArcGISMonitorExcelReporter/      ← Aplicación console
│   │   ├── Program.cs
│   │   ├── VersionInfo.targets          ← MSBuild versioning
│   │   └── GenerateVersionFile.ps1
│   └── *.Tests/                         ← Proyectos de tests
│
├── 🔧 Configuración
│   ├── .github/
│   │   └── workflows/
│   │       └── release.yml              ← CI/CD workflow
│   ├── .gitignore                       ← Git ignore rules
│   ├── ArcGISMonitorExcelReporter.slnx ← Solución
│   └── global.json                      ← .NET version
│
└── 📄 Otros
	├── BUILD_NUMBER.txt                 ← Auto-generated
	├── LAST_DATE_PREFIX.txt            ← Auto-generated
	└── BuildNumberFromCI.txt            ← Temporal (CI)
```

---

## 🔍 Búsqueda Rápida

### Pregunta: "¿Cómo configuro la aplicación?"
→ Ver [README.md#quick-start](README.md#quick-start) y [config.json.example](config.json.example)

### Pregunta: "¿Cómo contribuyo al proyecto?"
→ Ver [CONTRIBUTING.md](CONTRIBUTING.md)

### Pregunta: "¿Cuáles son las novedades en la última versión?"
→ Ver [CHANGELOG.md](CHANGELOG.md)

### Pregunta: "¿Por qué el build number no se duplica en CI?"
→ Ver [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md)

### Pregunta: "¿Dónde está el acuerdo de licencia?"
→ Ver [LICENSE](LICENSE)

### Pregunta: "¿Cómo resuelvo problemas?"
→ Ver [README.md#troubleshooting](README.md#troubleshooting)

---

## 🌐 Enlaces Importantes

### Sitio del Proyecto
- 🐙 [GitHub Repository](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter)
- 📦 [NuGet Package](https://www.nuget.org/) (pendiente)
- 📖 [Documentación Oficial](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/docs)

### Comunicación
- 🐛 [Reportar Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
- 💬 [Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)
- 📧 [Contact](https://github.com/desarrollosig-net)

### Recursos Externos
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [ArcGIS Monitor Documentation](https://doc.safe.com/arcgis-monitor/)
- [ClosedXML GitHub](https://github.com/ClosedXML/ClosedXML)
- [Serilog GitHub](https://github.com/serilog/serilog)

---

## ✅ Checklist de Documentación

- ✓ README.md completo en inglés
- ✓ CHANGELOG.md con historial
- ✓ CONTRIBUTING.md para colaboradores
- ✓ SUMMARY.md en español
- ✓ LICENSE MIT
- ✓ config.json.example
- ✓ BUILD_NUMBER_CI_FIX.md (técnico)
- ✓ INDEX.md (este archivo)
- ✓ XML Documentation en code
- ✓ Examples en Samples/

---

## 📞 Soporte

¿No encuentras lo que buscas?

1. **Busca** en [GitHub Issues](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)
2. **Pregunta** en [GitHub Discussions](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/discussions)
3. **Lee** todo el README.md - probablemente esté ahí
4. **Contribuye** una mejora de documentación

---

## 📅 Información del Documento

- **Creado:** Enero 27, 2025
- **Última Actualización:** Enero 27, 2025
- **Versión:** 1.0
- **Mantenedor:** DesarrolloSIG

---

**¡Gracias por tu interés en ArcGIS Monitor Excel Reporter!** 🚀
