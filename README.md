<h1 align="center">🏢 Condominio360</h1>

<p align="center">
  Aplicación web para la administración de un condominio: unidades, residentes y <b>cobranza de alícuotas con recargo por mora y clasificación de morosidad</b> (Core).
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4" alt="ASP.NET Core MVC">
  <img src="https://img.shields.io/badge/EF%20Core-PostgreSQL-336791?logo=postgresql" alt="EF Core + PostgreSQL">
  <img src="https://img.shields.io/badge/BDD-Supabase-3ECF8E?logo=supabase" alt="Supabase">
  <img src="https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap" alt="Bootstrap 5.3">
  <img src="https://img.shields.io/badge/estado-en%20desarrollo-yellow" alt="Estado: en desarrollo">
</p>

---

## 📑 Índice

- [Descripción del proyecto](#-descripción-del-proyecto)
- [Estado del proyecto](#-estado-del-proyecto)
- [El Core: cobranza de alícuotas](#-el-core-cobranza-de-alícuotas)
- [Funcionalidades](#-funcionalidades)
- [Base de datos (Supabase)](#️-base-de-datos-supabase)
- [Validaciones en back-end](#-validaciones-en-back-end)
- [Dropdowns en cascada](#-dropdowns-en-cascada)
- [Tecnologías utilizadas](#️-tecnologías-utilizadas)
- [Acceso al proyecto y ejecución](#-acceso-al-proyecto-y-ejecución)
- [Credenciales de prueba](#-credenciales-de-prueba)
- [Arquitectura MVC](#-arquitectura-mvc)
- [Seguridad](#-seguridad)
- [Rutas de la aplicación](#️-rutas-de-la-aplicación)
- [Autores](#-autores)
- [Licencia](#-licencia)

---

## 📝 Descripción del proyecto

**Condominio360** centraliza la información de un conjunto residencial que suele llevarse en hojas de cálculo o cuadernos: unidades, residentes, cobro de alícuotas, reservas de áreas comunales y control de visitas.

Proyecto de la materia **Ingeniería Web** (Universidad de las Américas – UDLA), con el patrón **Modelo–Vista–Controlador (MVC)** en **ASP.NET Core**.

## 🚧 Estado del proyecto

| Entrega | Contenido | Estado |
|---|---|---|
| 1 | CRUD + Login con MVC | ✅ |
| 2 | Documento de especificación funcional (Core, alcance, diagramas) | ✅ |
| **3** | **Administración MVC del Core sobre base de datos real (Supabase)** | ✅ |

Requisitos de la entrega 3:

| Requisito | Dónde se cumple |
|---|---|
| Tablas del Diagrama Entidad-Relación en una BDD real | [`database/01_esquema.sql`](database/01_esquema.sql) → 11 tablas en PostgreSQL (Supabase) |
| Administración MVC basada en el Core | Módulo **Cobranza**: configuración, cuotas, pagos, estado de cuenta, reporte de morosidad |
| ≥ 1 validación en back-end de un dato sensible del Core | Monto del pago vs. saldo de la unidad, referencia bancaria única, cédula (módulo 10) — ver [Validaciones](#-validaciones-en-back-end) |
| Claves foráneas con dropdown (no input simple) y en cascada | Torre → Unidad → Residente en pagos; Torre → Unidad en residentes y estado de cuenta |

## 💰 El Core: cobranza de alícuotas

Cada mes el condominio emite una cuota por unidad activa (su alícuota). Los pagos se verifican y se **distribuyen automáticamente desde la cuota más antigua, primero al recargo y luego al capital**. Con el resultado, cada unidad se clasifica según su morosidad.

Parámetros editables (configuración de cobro): día de vencimiento **D**, días de gracia **G**, tasa de recargo mensual **t**.

```
Fecha límite       = Fecha de vencimiento + G días
Días de atraso     = máx(0, F − Fecha límite)              F = fecha de corte
Meses de atraso    = ⌈ Días de atraso / 30 ⌉
Capital pendiente  = Monto − Capital pagado
Recargo            = Capital pendiente × t × Meses de atraso   (2 decimales)
Saldo              = Capital pendiente + máx(0, Recargo − Recargo pagado)

Clasificación (n = cuotas vencidas con saldo):
  n = 0 Al día · n = 1 Atraso leve · n = 2 o 3 Moroso · n ≥ 4 Moroso crítico

Índice de morosidad = Unidades con n ≥ 1 / Unidades activas × 100
Tasa de recaudación = Cobrado del periodo / Emitido del periodo × 100
Cartera vencida     = Σ Saldo de las cuotas vencidas
```

Las reglas están en el **Modelo** (`Models/Cuota.cs`) y en el servicio del Core (`Services/CobranzaService.cs`); los controladores solo orquestan.

## ✨ Funcionalidades

**Administración del Core (rol Administrador)**

- ⚙️ **Configuración de cobro** con historial de versiones (*vigente desde*).
- 📅 **Generar cuotas del periodo** con resumen previo: unidades activas, ya generadas, total a emitir por tipo. No duplica cuotas (restricción única unidad–año–mes).
- 🧾 **Cuotas**: listado por periodo / torre / estado con meses de atraso, recargo y saldo a hoy; detalle con pagos aplicados; anulación (solo sin abonos); recálculo de estados.
- 💵 **Pagos**: registrar pago recibido en administración (se aplica al instante), editar o eliminar pagos *por verificar*, **verificar con vista previa de la distribución** y del cambio de clasificación, **rechazar con motivo obligatorio**, comprobante protegido.
- 📄 **Estado de cuenta** de una unidad a una fecha de corte: saldo total, saldo vencido, recargos, clasificación, cuotas e historial de pagos con su distribución.
- 📉 **Reporte de morosidad** con indicadores (índice de morosidad, cartera vencida, tasa de recaudación, unidades al día), filtros por fecha de corte, torre, clasificación y búsqueda; **exportación CSV**.

**Gestión base**

- 🔐 Login con roles (Administrador, Residente, Guardia) y usuarios activos/inactivos.
- 🏠 CRUD de unidades (con estado activa/inactiva) y 👥 CRUD de residentes (con responsable de pago).
- 📊 Panel con indicadores del condominio y de la cobranza.

## 🗄️ Base de datos (Supabase)

PostgreSQL 17 en **Supabase** (proyecto `Condominio360`, región `us-east-1`). Las tablas replican el Diagrama Entidad-Relación del documento de especificación:

| Tabla | Descripción | Relaciones |
|---|---|---|
| `roles` | Administrador, Residente, Guardia | — |
| `usuarios` | Cuentas de acceso (hash PBKDF2) | `rol_id` → roles, `residente_id` → residentes (1:1) |
| `unidades` | Torre, número, piso, tipo, área, alícuota, activa | — |
| `residentes` | Datos personales, responsable de pago | `unidad_id` → unidades |
| `configuracion_cobro` | D, G, t y vigencia | `actualizado_por` → usuarios |
| `cuotas` | Cuota mensual por unidad | `unidad_id` → unidades, `generada_por` → usuarios |
| `pagos` | Pagos con estado de verificación | `unidad_id`, `residente_id`, `registrado_por`, `verificado_por` |
| `aplicaciones_pago` | Distribución de un pago entre cuotas | `pago_id` → pagos, `cuota_id` → cuotas |
| `areas_comunales` | Salón, BBQ, gimnasio… | — |
| `reservas` | Reservas de áreas | `area_id`, `residente_id`, `aprobada_por` |
| `visitas` | Visitas autorizadas | `unidad_id`, `autorizada_por`, `registrada_por` |

Scripts en la carpeta [`database/`](database):

| Script | Contenido |
|---|---|
| `01_esquema.sql` | Tablas, claves primarias/foráneas, `UNIQUE`, `CHECK` de dominios (estados, métodos, rangos) e índices |
| `02_seguridad.sql` | RLS activo en todas las tablas (la API pública de Supabase no expone datos) y rol `condominio_app` de mínimo privilegio para la aplicación |
| `03_datos_ejemplo.sql` | 14 unidades, 15 residentes, 4 usuarios, cuotas de julio a octubre de 2026 y 38 pagos coherentes con el algoritmo del Core |

Reglas que también protege la base de datos (defensa en profundidad): una cuota por unidad y periodo, un responsable de pago por unidad, referencia bancaria única entre pagos no rechazados, `capital_pagado ≤ monto`, rechazo con observación obligatoria, transferencia/depósito con referencia obligatoria.

> La aplicación **no crea tablas**: el esquema se administra con estos scripts.

## 🛡️ Validaciones en back-end

El dato sensible del Core es **el dinero**. Estas reglas se ejecutan en el servidor (`PagosController.ValidarAsync` y `CobranzaService`) **antes de grabar**, aunque el navegador tenga JavaScript desactivado o se manipule el formulario:

| # | Validación | Archivo |
|---|---|---|
| 1 | El **monto no puede superar el saldo total de la unidad** a la fecha del pago (calculado en el servidor con recargos); máximo 2 decimales; mayor que 0 | `Controllers/PagosController.cs`, `Services/CobranzaService.cs` |
| 2 | **Referencia bancaria** obligatoria para transferencia/depósito, formato `A-Z 0-9 -` y **sin duplicados** (evita registrar dos veces la misma transferencia) | `Controllers/PagosController.cs` |
| 3 | La unidad existe y pertenece a la torre; el residente que paga pertenece a esa unidad (no se confía en el valor del dropdown) | `Controllers/PagosController.cs` |
| 4 | Fecha de pago no futura ni con más de un año | `Controllers/PagosController.cs` |
| 5 | Comprobante: solo JPG/PNG/PDF ≤ 2 MB, verificando la **firma del archivo** (magic bytes) | `Controllers/PagosController.cs` |
| 6 | Rechazo con motivo obligatorio; solo se verifican/rechazan pagos *por verificar* | `Services/CobranzaService.cs` |
| 7 | **Cédula ecuatoriana** validada con el algoritmo **módulo 10** del Registro Civil (provincia, tercer dígito y dígito verificador) | `Services/ValidadorCedula.cs` |

Ejemplo para probar: en *Pagos → Registrar pago*, elegir **Torre A → A-301** (saldo ≈ $456) e ingresar **$1000**: el servidor rechaza el pago y no se guarda nada.

## 🔗 Dropdowns en cascada

Ninguna clave foránea se escribe a mano. Al elegir la torre se cargan **solo sus unidades**, y al elegir la unidad se cargan **solo sus residentes** (preseleccionando al responsable de pago):

```
Torre / bloque  ──►  Unidad  ──►  Residente que paga
   [ A ▾ ]          [ A-202 ▾ ]     [ Carlos Mena Vaca · responsable ▾ ]
```

- Endpoints JSON: `GET /Catalogos/Unidades?torre=A`, `GET /Catalogos/Residentes?unidadId=4`, `GET /Catalogos/SaldoUnidad?unidadId=4`.
- Script genérico: `wwwroot/js/cascada.js` (atributos `data-cascada-destino` y `data-cascada-url`).
- Usado en: registro/edición de pagos, registro/edición de residentes y estado de cuenta.

## 🛠️ Tecnologías utilizadas

| Capa | Tecnología |
|---|---|
| Framework | ASP.NET Core 10 MVC |
| Lenguaje | C# 14 |
| Acceso a datos | Entity Framework Core 10 + Npgsql |
| Base de datos | PostgreSQL 17 en **Supabase** |
| Autenticación | Cookie Authentication + `PasswordHasher` (PBKDF2) + roles |
| Interfaz | Razor Views, Bootstrap 5.3, Bootstrap Icons |
| Validación cliente | jQuery Validation Unobtrusive (el servidor siempre vuelve a validar) |

## 🚀 Acceso al proyecto y ejecución

### Requisitos previos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- Acceso a la base de datos de Supabase (cadena de conexión)

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/BrunoMorenoDev/Condominio360.git
cd Condominio360

# 2. Configurar la cadena de conexión (NO se sube a Git)
cp Condominio360/appsettings.Local.example.json Condominio360/appsettings.Local.json
#    → reemplazar <CONTRASENA> por la contraseña del rol condominio_app

# 3. Restaurar y ejecutar
dotnet restore
dotnet run --project Condominio360
```

Abrir **http://localhost:5080**

### Crear la base de datos desde cero (opcional)

En Supabase → *SQL Editor* ejecutar, en orden: `database/01_esquema.sql`, `database/02_seguridad.sql` (cambiando `<CONTRASENA_SEGURA>`) y `database/03_datos_ejemplo.sql`.

## 🔑 Credenciales de prueba

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `Admin123*` | Administrador (acceso completo al Core) |
| `cmena` | `Residente123*` | Residente (módulo en la siguiente entrega) |
| `atorres` | `Residente123*` | Residente |
| `guardia` | `Guardia123*` | Guardia |

Las contraseñas se guardan **hasheadas** (PBKDF2), nunca en texto plano.

## 🧱 Arquitectura MVC

```
Condominio360/
├── database/                         # Scripts SQL del diagrama ER (Supabase)
│   ├── 01_esquema.sql
│   ├── 02_seguridad.sql
│   └── 03_datos_ejemplo.sql
└── Condominio360/
    ├── Program.cs                    # MVC, EF Core + Npgsql, autenticación, autorización
    ├── appsettings.json              # Configuración sin secretos
    ├── appsettings.Local.example.json# Plantilla de la cadena de conexión
    ├── Models/                       # MODELO: entidades del diagrama ER y reglas de cálculo
    │   ├── Unidad.cs  Residente.cs  Usuario.cs  Rol.cs
    │   ├── ConfiguracionCobro.cs  Cuota.cs  Pago.cs  AplicacionPago.cs
    │   ├── AreaComunal.cs (+ Reserva, Visita)
    │   └── Enums.cs                  # EstadoCuota, EstadoPago, MetodoPago, ClasificacionMorosidad…
    ├── Services/
    │   ├── CobranzaService.cs        # Core: generar cuotas, saldo, distribución, morosidad, indicadores
    │   └── ValidadorCedula.cs        # Cédula ecuatoriana (módulo 10)
    ├── Data/
    │   ├── AppDbContext.cs           # Mapeo a tablas snake_case de PostgreSQL
    │   └── DbSeeder.cs               # Roles, admin y configuración mínima
    ├── ViewModels/                   # Modelos de las vistas (formularios, reportes)
    ├── Helpers/                      # Formato de periodos, estados y claims
    ├── Controllers/                  # CONTROLADOR
    │   ├── AccountController.cs  PanelController.cs  HomeController.cs
    │   ├── UnidadesController.cs  ResidentesController.cs
    │   ├── ConfiguracionCobroController.cs  CuotasController.cs  PagosController.cs
    │   ├── EstadoCuentaController.cs  MorosidadController.cs
    │   └── CatalogosController.cs    # JSON para los dropdowns en cascada
    ├── Views/                        # VISTA: Razor (.cshtml)
    └── wwwroot/  (css/site.css, js/cascada.js, js/pago-form.js)
```

**Flujo de verificación de un pago:** `GET /Pagos/Verificar/37` → `PagosController` pide a `CobranzaService` la vista previa → el servicio usa las reglas de `Cuota` (recargo, saldo, estado) → la **Vista** muestra la distribución y el cambio de clasificación → `POST` aplica el pago: crea las `aplicaciones_pago`, actualiza las cuotas y marca el pago como verificado en una sola transacción.

## 🔒 Seguridad

| Medida | Implementación |
|---|---|
| Rutas protegidas por defecto | `FallbackPolicy`: toda URL exige sesión salvo `Home` y `Account` |
| Autorización por rol | Módulos de administración con `[Authorize(Roles = "Administrador")]` |
| Contraseñas | Hash PBKDF2 (`PasswordHasher<Usuario>`); usuarios inactivos no inician sesión |
| Secretos fuera de Git | Cadena de conexión en `appsettings.Local.json` (en `.gitignore`) |
| Base de datos | RLS activo; la app usa el rol `condominio_app` (solo DML, sin DDL) |
| CSRF | `[ValidateAntiForgeryToken]` en todos los POST |
| Overposting | `[Bind]` / ViewModels con campos explícitos |
| Comprobantes | Guardados fuera de `wwwroot`; solo se descargan con sesión de administrador |
| CSV | Neutraliza fórmulas (`=`, `+`, `-`, `@`) al exportar |

## 🗺️ Rutas de la aplicación

| Ruta | Acceso | Descripción |
|---|---|---|
| `/` · `/Account/Login` | Público | Inicio y login |
| `/Panel` | 🔒 Sesión | Indicadores |
| `/Unidades` · `/Residentes` | 🔒 Admin | CRUD |
| `/ConfiguracionCobro` | 🔒 Admin | Parámetros D, G, t e historial |
| `/Cuotas` · `/Cuotas/Generar` · `/Cuotas/Details/{id}` | 🔒 Admin | Cuotas por periodo |
| `/Pagos` · `/Pagos/Create` · `/Pagos/Verificar/{id}` | 🔒 Admin | Registro, verificación y rechazo de pagos |
| `/EstadoCuenta?unidadId={id}&corte=AAAA-MM-DD` | 🔒 Admin | Estado de cuenta de una unidad |
| `/Morosidad` · `/Morosidad/Exportar` | 🔒 Admin | Reporte de morosidad y CSV |
| `/Catalogos/Unidades` · `/Catalogos/Residentes` · `/Catalogos/SaldoUnidad` | 🔒 Admin | JSON para dropdowns en cascada |

## 👨‍💻 Autores

| Nombre | GitHub |
|---|---|
| Jose Freire | [@JoseFreire29](https://github.com/JoseFreire29) |
| Bruno Moreno | [@BrunoMorenoDev](https://github.com/BrunoMorenoDev) |

Materia: Ingeniería Web · Facultad de Ingeniería y Ciencias Aplicadas (FICA) · UDLA

## 📄 Licencia

Proyecto académico distribuido bajo la licencia MIT.
