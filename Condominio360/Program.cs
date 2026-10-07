using System.Globalization;
using Condominio360.Data;
using Condominio360.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC ----------
builder.Services.AddControllersWithViews(options =>
{
    // Solo se validan como obligatorias las propiedades marcadas con [Required]
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ---------- Base de datos (EF Core + SQLite) ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Hash de contraseñas (PBKDF2 de ASP.NET Core Identity) ----------
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// ---------- Autenticación por cookie ----------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";            // destino si no hay sesión
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Condominio360.Auth";
        options.Cookie.HttpOnly = true;                   // no accesible desde JavaScript
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// ---------- Autorización: seguro por defecto ----------
// Toda URL exige usuario autenticado, salvo las marcadas con [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// ---------- Crear BD y datos iniciales ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db, hasher, app.Configuration);
}

// ---------- Pipeline HTTP ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Punto decimal y formato de moneda en USD (evita errores al ingresar 85.50)
var cultura = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new List<CultureInfo> { cultura },
    SupportedUICultures = new List<CultureInfo> { cultura }
});

app.UseRouting();

app.UseAuthentication();   // 1. ¿Quién es el usuario? (lee la cookie)
app.UseAuthorization();    // 2. ¿Puede entrar a esta URL?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
