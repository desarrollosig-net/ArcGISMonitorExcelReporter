# 📖 Guía de Lectura Recomendada

Bienvenido a **ArcGIS Monitor Excel Reporter**. Esta guía te ayudará a navegar la documentación según tu rol y necesidades.

## 🎯 ¿Cuál es tu rol?

### 👤 Soy Usuario

**Objetivo:** Usar la aplicación para generar reportes Excel

#### Lectura recomendada (45 minutos):
1. **[README.md](README.md)** - Introducción (5 min)
   - Lee la sección "Features" para entender qué hace
   - Lee "Quick Start - Method 1: JSON Configuration File"

2. **[config.json.example](config.json.example)** - Ver ejemplo (5 min)
   - Usa esto como plantilla para tu configuración

3. **[README.md](README.md#configuration-reference)** - Referencia (15 min)
   - Lee el "Configuration Reference" completo
   - Personaliza parámetros según necesites

4. **[README.md](README.md#troubleshooting)** - Solución de Problemas (10 min)
   - Guarda esta sección para consultas futuras

5. **[README.md](README.md#output-excel-structure)** - Formato Excel (10 min)
   - Entiende qué hojas tendrá tu reporte

---

### 👨‍💼 Soy Project Manager / Stakeholder

**Objetivo:** Entender qué es el proyecto y qué puede hacer

#### Lectura recomendada (20 minutos):
1. **[SUMMARY.md](SUMMARY.md)** - Visión general (10 min)
   - Lee todo - está diseñado para ejecutivos

2. **[README.md](README.md#features)** - Características (5 min)
   - Entiende las capacidades principales

3. **[CHANGELOG.md](CHANGELOG.md)** - Evolución (5 min)
   - Ve el progreso y roadmap futuro

---

### 👨‍💻 Soy Desarrollador

**Objetivo:** Modificar, extender o contribuir código

#### Lectura recomendada (2-3 horas):

##### Fase 1: Setup (30 minutos)
1. **[CONTRIBUTING.md](CONTRIBUTING.md#development-setup)** - Setup Local
   - Instala dependencias
   - Clona repositorio
   - Ejecuta `dotnet build`

2. **[README.md](README.md#project-structure)** - Estructura
   - Entiende los directorios principales

##### Fase 2: Entender el Código (45 minutos)
3. **[README.md](README.md#building-from-source)** - Build
   - Cómo compilar y ejecutar

4. **Code Navigation:**
   - Abre `ArcGISMonitorExcelReporterLib/ArcGISMonitorExcelReporter.cs`
   - Lee los comentarios XML (presiona Ctrl+K, Ctrl+I en VS)
   - Busca clase `Configuration` para ver modelos

5. **[ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs](ArcGISMonitorExcelReporterLib/Samples/ExampleUsage.cs)**
   - Lee ejemplos de uso
   - Entiende los dos métodos principales

##### Fase 3: Guía de Estándares (30 minutos)
6. **[CONTRIBUTING.md](CONTRIBUTING.md#styleguides)** - Estilo de Código
   - Lee la sección "C# Style Guide"
   - Lee "Commit Messages"

7. **[CONTRIBUTING.md](CONTRIBUTING.md#testing)** - Testing
   - Cómo escribir tests
   - Cómo ejecutar tests

##### Fase 4: Contribuir (45 minutos+)
8. **[CONTRIBUTING.md](CONTRIBUTING.md#pull-requests)** - Proceso PR
   - Sigue el flujo de Pull Request

---

### 🚀 Soy DevOps / Release Manager

**Objetivo:** Compilar, publicar y mantener la infraestructura

#### Lectura recomendada (1.5 horas):

1. **[README.md](README.md#versioning--build-number-management)** - Versionado (15 min)
   - Entiende el sistema de versiones
   - Lee cómo se maneja en CI/CD

2. **[BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md)** - Fix Técnico (30 min)
   - Detalle completo del sistema
   - Razón del marcador CI

3. **[.github/workflows/release.yml](.github/workflows/release.yml)** - Workflow (20 min)
   - Lee el workflow comentado
   - Entiende cada paso

4. **[CONTRIBUTING.md](CONTRIBUTING.md#build-and-release)** - Build Local (20 min)
   - Sección "Local Build"
   - Sección "Publishing"

5. **Manual Build Test (15 min):**
   ```bash
   cd ArcGISMonitorExcelReporter
   dotnet publish -c Release -r win-x64 --self-contained
   dotnet publish -c Release -r linux-x64 --self-contained
   ```

---

## 📚 Lectura Completa por Tema

### Si quieres...

#### 🔧 **Configurar tu primera instancia**
→ [README.md#quick-start](README.md#quick-start) + [config.json.example](config.json.example)
**Tiempo:** 30 minutos

#### 🐛 **Resolver un problema**
→ [README.md#troubleshooting](README.md#troubleshooting)
**Tiempo:** 5-15 minutos

#### ✨ **Vender el producto**
→ [SUMMARY.md](SUMMARY.md)
**Tiempo:** 20 minutos

#### 🤝 **Hacer tu primer pull request**
→ [CONTRIBUTING.md](CONTRIBUTING.md)
**Tiempo:** 2-3 horas (incluye setup)

#### 📊 **Entender la arquitectura**
→ [README.md#project-structure](README.md#project-structure) + [SUMMARY.md](SUMMARY.md#🏗️-arquitectura)
**Tiempo:** 45 minutos

#### 🚀 **Hacer un release completo**
→ [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) + [.github/workflows/release.yml](.github/workflows/release.yml)
**Tiempo:** 1.5 horas

#### 📖 **Escribir documentación**
→ [CONTRIBUTING.md#documentation](CONTRIBUTING.md#documentation)
**Tiempo:** Según el tipo

---

## 🗂️ Mapa Mental de Documentos

```
						  ┌─ README.md ◄──── INICIO
						  │    (Oficial)
						  │
		  ┌───────────────┼───────────────┐
		  │               │               │
	  Usuario         Developer      DevOps/RM
		  │               │               │
		  ▼               ▼               ▼
	config.json.      CONTRIBUTING.   BUILD_NUMBER
	example           md + Code        _CI_FIX.md
		  │               │               │
		  │        ┌──────┼──────┐        │
		  │        │      │      │        │
		  │        ▼      ▼      ▼        │
	Troubleshooting  Tests  Styleguides  │
					 Writing            │
		  │          Code               │
		  │               │              │
		  └──────┬────────┴──────┬───────┘
				 │               │
				 ▼               ▼
		  CHANGELOG.md    LICENSE
				 │               │
				 └───────┬───────┘
						 │
						 ▼
				  SUMMARY.md (ES)
				  INDEX.md
```

---

## ⏱️ Tiempo Total de Lectura

| Perfil | Mínimo | Recomendado | Completo |
|--------|--------|-------------|----------|
| **Usuario** | 30 min | 1 hora | 2 horas |
| **PM/Stakeholder** | 15 min | 30 min | 1 hora |
| **Developer** | 1.5 horas | 2-3 horas | 4+ horas |
| **DevOps/RM** | 45 min | 1.5 horas | 3 horas |

---

## ✅ Checklist por Tipo de Usuario

### ✓ Usuario
- [ ] Leí README intro
- [ ] Revisé config.json.example
- [ ] Entiendo Configuration Reference
- [ ] Creé mi config.json
- [ ] Ejecuté la app exitosamente
- [ ] Visualizo el reporte Excel

### ✓ PM/Stakeholder
- [ ] Leí SUMMARY.md completo
- [ ] Entiendo qué hace el producto
- [ ] Conozco el roadmap
- [ ] Sé a quién contactar para soporte

### ✓ Developer
- [ ] Configuré mi entorno local
- [ ] Pude compilar la solución
- [ ] Leí CONTRIBUTING.md
- [ ] Entiendo la estructura de código
- [ ] Ejecuté tests exitosamente
- [ ] Creé un branch para cambios
- [ ] Estoy listo para hacer PR

### ✓ DevOps/RM
- [ ] Entiendo el sistema de versiones
- [ ] Leí BUILD_NUMBER_CI_FIX.md
- [ ] Revisé el workflow de GitHub Actions
- [ ] Ejecuté builds locales de prueba
- [ ] Entiendo cómo hacer un release
- [ ] Conozco dónde buscar en caso de problemas

---

## 🆘 Si Estás Atrapado

### "No entiendo por dónde empezar"
→ Lee [INDEX.md](INDEX.md) - Es exactamente para esto

### "No encuentro la respuesta"
→ Busca en README.md con Ctrl+F la palabra clave

### "Tengo un error específico"
→ Busca en [README.md#troubleshooting](README.md#troubleshooting)

### "Quiero contribuir pero no sé cómo"
→ Lee [CONTRIBUTING.md#pull-requests](CONTRIBUTING.md#pull-requests)

### "Necesito entender un concepto técnico"
→ Busca en [BUILD_NUMBER_CI_FIX.md](BUILD_NUMBER_CI_FIX.md) o [README.md#project-structure](README.md#project-structure)

### "Sigo sin poder resolverlo"
→ Abre un [GitHub Issue](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/issues)

---

## 🎓 Material de Aprendizaje Suplementario

Si deseas aprender más sobre las tecnologías usadas:

- **[.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)** - Runtime
- **[C# 12 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)** - Lenguaje
- **[ArcGIS Monitor Documentation](https://doc.safe.com/arcgis-monitor/)** - API
- **[ClosedXML Wiki](https://github.com/ClosedXML/ClosedXML/wiki)** - Excel generation
- **[Serilog Documentation](https://github.com/serilog/serilog/wiki)** - Logging
- **[GitHub Actions Documentation](https://docs.github.com/en/actions)** - CI/CD

---

## 📅 Programa de Lectura Sugerido

### Día 1 (Inicio)
- Mañana: Lee README intro + Quick Start
- Tarde: Crea tu primera config

### Día 2 (Profundización)
- Mañana: Lee Configuration Reference completa
- Tarde: Ejecuta la app, ajusta parámetros

### Día 3 (Dominio)
- Mañana: Lee SUMMARY.md (entender arquitectura)
- Tarde: Revisa CHANGELOG (ver evolución)

### Si profundizas (Desarrollo)
- Semana 2: Setup dev, lean código
- Semana 3: Contribuye tu primer PR

---

## 💡 Tips Finales

1. **Usa los índices:** Ctrl+F es tu amigo
2. **Sigue los links:** Navega entre documentos
3. **Revisa ejemplos:** Busca `Example` en código
4. **No memorices:** Bookmark esta guía para referencias
5. **Pregunta:** Los GitHub Discussions existen para eso

---

**¡Bienvenido a ArcGIS Monitor Excel Reporter!** 🚀

Esperamos que encuentres esta documentación útil. Si tienes sugerencias para mejorarla, ¡por favor abre un issue!

Última Actualización: Enero 27, 2025
