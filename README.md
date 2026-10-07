<h1 align="center">🏢 Condominio360</h1>

<p align="center">
  Aplicación web para la administración de un condominio: registro de unidades y residentes con acceso protegido por inicio de sesión.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4" alt="ASP.NET Core MVC">
  <img src="https://img.shields.io/badge/EF%20Core-SQLite-003B57?logo=sqlite" alt="EF Core + SQLite">
  <img src="https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap" alt="Bootstrap 5.3">
  <img src="https://img.shields.io/badge/estado-en%20desarrollo-yellow" alt="Estado: en desarrollo">
</p>

---

## 📑 Índice

- [Descripción del proyecto](#-descripción-del-proyecto)
- [Estado del proyecto](#-estado-del-proyecto)
- [Demostración](#-demostración)
- [Funcionalidades](#-funcionalidades)
- [Tecnologías utilizadas](#-tecnologías-utilizadas)
- [Acceso al proyecto y ejecución](#-acceso-al-proyecto-y-ejecución)
- [Credenciales de prueba](#-credenciales-de-prueba)
- [Arquitectura MVC](#-arquitectura-mvc)
- [Seguridad y autenticación](#-seguridad-y-autenticación)
- [Rutas de la aplicación](#-rutas-de-la-aplicación)
- [Autores](#-autores)
- [Licencia](#-licencia)

---

## 📝 Descripción del proyecto

**Condominio360** resuelve un problema frecuente en conjuntos residenciales: la información de quién vive en cada departamento y cuánto paga de alícuota suele estar dispersa en hojas de cálculo o cuadernos. La aplicación centraliza esos datos y restringe su acceso al personal de administración.

Proyecto desarrollado para la materia **Ingeniería Web** (Universidad de las Américas – UDLA), aplicando el patrón **Modelo–Vista–Controlador (MVC)** con **ASP.NET Core**.

## 🚧 Estado del proyecto

**Entrega 1 – CRUD y Login con MVC:** completada ✅

| Requisito | Estado |
|---|---|
| Aplicación MVC con CRUD (Crear, Leer, Actualizar, Eliminar) | ✅ |
| Login con usuario y contraseña | ✅ |
| URLs protegidas inaccesibles sin autenticación | ✅ |

## 🎥 Demostración

📹 **Video (máx. 3 min):** [Ver en Loom / YouTube](https://ENLACE-DEL-VIDEO)

El video muestra:
1. Intento de abrir `/Unidades` sin sesión → la app redirige al login.
2. Login con credenciales incorrectas → mensaje de error.
3. Login correcto → acceso al panel y al CRUD.
4. Cierre de sesión → las URLs protegidas vuelven a pedir login.

## ✨ Funcionalidades

- 🔐 **Inicio y cierre de sesión** con usuario y contraseña (cookie de autenticación).
- 🛡️ **Protección de rutas:** toda URL requiere sesión salvo el inicio y el login.
- ↩️ **Retorno automático:** tras iniciar sesión, el usuario vuelve a la página que intentó abrir.
- 🏠 **CRUD de Unidades:** torre, número, piso, tipo, área y alícuota mensual.
- 👥 **CRUD de Residentes:** datos personales, tipo (propietario/arrendatario) y unidad asignada.
- 🔎 **Búsqueda de residentes** por nombre, apellido o cédula.
- 📊 **Panel** con total de unidades, ocupación, residentes y alícuotas del mes.
- ✅ **Validaciones** en cliente y servidor (cédula de 10 dígitos, correo, rangos, duplicados).
- 🔗 **Integridad:** no se puede eliminar una unidad que tiene residentes.

## 🛠️ Tecnologías utilizadas

| Capa | Tecnología |
|---|---|
| Framework | ASP.NET Core 10 MVC |
| Lenguaje | C# 14 |
| Acceso a datos | Entity Framework Core 10 |
| Base de datos | SQLite (archivo local, sin instalación) |
| Autenticación | Cookie Authentication + `PasswordHasher` (PBKDF2) |
| Interfaz | Razor Views, Bootstrap 5.3, Bootstrap Icons |
| Validación cliente | jQuery Validation Unobtrusive |

## 🚀 Acceso al proyecto y ejecución

### Requisitos previos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- (Opcional) Visual Studio 2022 o Visual Studio Code con C# Dev Kit

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/USUARIO/Condominio360.git
cd Condominio360

# 2. Restaurar paquetes
dotnet restore

# 3. Ejecutar
dotnet run --project Condominio360
```

Abrir en el navegador: **http://localhost:5080**

> La base de datos `condominio360.db` se crea automáticamente en el primer arranque, junto con el usuario administrador y datos de ejemplo. Para reiniciar los datos basta con borrar ese archivo.

**Con Visual Studio:** abrir `Condominio360.sln` y presionar **F5**.

**Con HTTPS** (opcional):

```bash
dotnet dev-certs https --trust
dotnet run --project Condominio360 --launch-profile https
```

## 🔑 Credenciales de prueba

| Usuario | Contraseña |
|---|---|
| `admin` | `Admin123*` |

Se configuran en `appsettings.json` (sección `AdminSeed`) y solo se usan cuando la base de datos no tiene usuarios. La contraseña se guarda **hasheada**, nunca en texto plano.

## 🧱 Arquitectura MVC

```
Condominio360/
├── Condominio360.sln
├── README.md
└── Condominio360/
    ├── Program.cs                 # Configuración: MVC, EF Core, autenticación y autorización
    ├── appsettings.json           # Cadena de conexión y usuario inicial
    ├── Models/                    # MODELO: entidades y reglas de validación
    │   ├── Unidad.cs
    │   ├── Residente.cs
    │   └── Usuario.cs
    ├── ViewModels/                # Modelos exclusivos de las vistas (login, panel)
    ├── Data/
    │   ├── AppDbContext.cs        # Mapeo a la base de datos (índices únicos, relaciones)
    │   └── DbSeeder.cs            # Usuario admin y datos de ejemplo
    ├── Controllers/               # CONTROLADOR: recibe la petición y decide la respuesta
    │   ├── HomeController.cs      # Público
    │   ├── AccountController.cs   # Login / Logout (público)
    │   ├── PanelController.cs     # Protegido
    │   ├── UnidadesController.cs  # CRUD protegido
    │   └── ResidentesController.cs# CRUD protegido
    ├── Views/                     # VISTA: plantillas Razor (.cshtml)
    │   ├── Shared/                # Layout, alertas, validación
    │   ├── Account/  Home/  Panel/
    │   ├── Unidades/              # Index, Details, Create, Edit, Delete
    │   └── Residentes/            # Index, Details, Create, Edit, Delete
    └── wwwroot/css/site.css
```

**Flujo de una petición:** el navegador pide `/Unidades` → el middleware de autenticación lee la cookie → el middleware de autorización verifica la sesión → `UnidadesController.Index()` consulta el **Modelo** mediante `AppDbContext` → devuelve la **Vista** `Views/Unidades/Index.cshtml` con los datos.

**Modelo de datos:** una `Unidad` tiene muchos `Residente` (relación 1:N, eliminación restringida).

## 🔒 Seguridad y autenticación

| Medida | Implementación |
|---|---|
| Rutas protegidas por defecto | `FallbackPolicy` en `Program.cs`: toda URL exige usuario autenticado. Solo `HomeController` y `AccountController` tienen `[AllowAnonymous]`. |
| Defensa en profundidad | Los controladores privados además llevan `[Authorize]`. |
| Redirección al login | Sin sesión, cualquier URL privada responde `302` a `/Account/Login?ReturnUrl=...`. |
| Contraseñas | Hash PBKDF2 con `PasswordHasher<Usuario>` de ASP.NET Core Identity. |
| Cookie de sesión | `HttpOnly`, `SameSite=Lax`, expira en 30 min con renovación por actividad. |
| CSRF | `[ValidateAntiForgeryToken]` en todos los formularios POST (incluido el logout). |
| Open redirect | `Url.IsLocalUrl()` antes de redirigir al `ReturnUrl`. |
| Caché tras logout | `ResponseCache(NoStore = true)` en páginas privadas: el botón "Atrás" no muestra datos después de cerrar sesión. |
| Mensajes de error | Mensaje genérico "Usuario o contraseña incorrectos" para no revelar si el usuario existe. |
| Overposting | `[Bind]` con lista explícita de campos en Create/Edit. |

## 🗺️ Rutas de la aplicación

| Ruta | Acceso | Descripción |
|---|---|---|
| `/` | Público | Página de inicio |
| `/Account/Login` | Público | Formulario de inicio de sesión |
| `/Account/Logout` (POST) | Público | Cierra la sesión |
| `/Panel` | 🔒 Privado | Resumen del condominio |
| `/Unidades` | 🔒 Privado | Listado de unidades |
| `/Unidades/Create` · `/Edit/{id}` · `/Details/{id}` · `/Delete/{id}` | 🔒 Privado | CRUD de unidades |
| `/Residentes` | 🔒 Privado | Listado y búsqueda de residentes |
| `/Residentes/Create` · `/Edit/{id}` · `/Details/{id}` · `/Delete/{id}` | 🔒 Privado | CRUD de residentes |

## 👨‍💻 Autores

| Nombre | GitHub |
|---|---|
| Jose Freire| [@JoseFreire29](https://github.com/JoseFreire29) |
| Bruno Moreno| [@BrunoMorenoDev](https://github.com/BrunoMorenoDev) |

Materia: Ingeniería Web · Facultad de Ingeniería y Ciencias Aplicadas (FICA) · UDLA

## 📄 Licencia

Proyecto académico distribuido bajo la licencia MIT.
