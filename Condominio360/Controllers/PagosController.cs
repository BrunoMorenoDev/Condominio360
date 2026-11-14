using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Pagos de alícuotas (Core): registro en administración, verificación con vista previa
/// de la distribución y rechazo con motivo. Todas las reglas sobre el dinero se validan
/// en el servidor antes de grabar en la base de datos.
/// </summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PagosController : Controller
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;
    private readonly ReglasPago _reglas;
    private readonly ComprobanteService _comprobantes;
    private readonly ILogger<PagosController> _logger;

    public PagosController(AppDbContext db, CobranzaService cobranza, ReglasPago reglas,
        ComprobanteService comprobantes, ILogger<PagosController> logger)
    {
        _db = db;
        _cobranza = cobranza;
        _reglas = reglas;
        _comprobantes = comprobantes;
        _logger = logger;
    }

    // ==================================================================
    // LEER
    // ==================================================================

    // GET: /Pagos?estado=PorVerificar&torre=A
    public async Task<IActionResult> Index(EstadoPago? estado, string? torre)
    {
        IQueryable<Pago> consulta = _db.Pagos.AsNoTracking()
            .Include(p => p.Unidad)
            .Include(p => p.Residente)
            .Include(p => p.RegistradoPorUsuario);

        if (estado is not null) consulta = consulta.Where(p => p.Estado == estado);
        if (!string.IsNullOrWhiteSpace(torre)) consulta = consulta.Where(p => p.Unidad!.Torre == torre);

        var vm = new PagosIndexViewModel
        {
            Estado = estado,
            Torre = torre,
            Pagos = await consulta.OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.Id).ToListAsync(),
            Torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync(),
            PorVerificar = await _db.Pagos.CountAsync(p => p.Estado == EstadoPago.PorVerificar)
        };
        return View(vm);
    }

    // GET: /Pagos/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var pago = await CargarPagoAsync(id.Value);
        return pago is null ? NotFound() : View(pago);
    }

    // GET: /Pagos/Comprobante/5  (archivo protegido: solo administradores)
    public async Task<IActionResult> Comprobante(int id)
    {
        var ruta = await _db.Pagos.Where(p => p.Id == id).Select(p => p.Comprobante).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(ruta)) return NotFound();

        var archivo = _comprobantes.Obtener(ruta);
        return archivo is null
            ? NotFound("El archivo del comprobante no está disponible en este servidor.")
            : PhysicalFile(archivo.Value.Ruta, archivo.Value.Tipo);
    }

    // ==================================================================
    // CREAR: pago recibido en administración (queda verificado y aplicado)
    // ==================================================================

    // GET: /Pagos/Create?unidadId=4
    public async Task<IActionResult> Create(int? unidadId)
    {
        var vm = new PagoFormViewModel { FechaPago = DateTime.Today };
        if (unidadId is > 0)
        {
            var unidad = await _db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unidadId);
            if (unidad is not null)
            {
                vm.UnidadId = unidad.Id;
                vm.Torre = unidad.Torre;
                vm.ResidenteId = await _db.Residentes.Where(r => r.UnidadId == unidad.Id && r.ResponsablePago)
                    .Select(r => (int?)r.Id).FirstOrDefaultAsync();
            }
        }
        await CargarListasAsync(vm);
        return View(vm);
    }

    // POST: /Pagos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PagoFormViewModel vm)
    {
        Normalizar(vm);
        await ValidarAsync(vm);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(vm);
            return View(vm);
        }

        var pago = new Pago
        {
            UnidadId = vm.UnidadId,
            ResidenteId = vm.ResidenteId,
            FechaPago = vm.FechaPago.Date,
            Monto = vm.Monto,
            Metodo = vm.Metodo,
            Referencia = vm.Referencia
        };

        string? archivoGuardado = null;
        try
        {
            if (vm.Archivo is not null)
                pago.Comprobante = archivoGuardado = await _comprobantes.GuardarAsync(vm.Archivo);

            var resultado = await _cobranza.RegistrarPagoAdministracionAsync(pago, User.IdUsuario());

            var unidad = await _db.Unidades.AsNoTracking().Where(u => u.Id == pago.UnidadId)
                .Select(u => u.Torre + "-" + u.Numero).FirstAsync();
            TempData["Exito"] = $"Pago de {pago.Monto:C} registrado para {unidad}: " +
                                $"{resultado.TotalRecargo:C} a recargos y {resultado.TotalCapital:C} a capital " +
                                $"en {resultado.Lineas.Count} cuota(s).";
            return RedirectToAction(nameof(Details), new { id = pago.Id });
        }
        catch (ReglaNegocioException ex)
        {
            _comprobantes.Borrar(archivoGuardado);
            ModelState.AddModelError(nameof(vm.Monto), ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _comprobantes.Borrar(archivoGuardado);
            _logger.LogWarning(ex, "No se pudo registrar el pago");
            ModelState.AddModelError(string.Empty,
                "No se pudo guardar el pago. Verifique que la referencia bancaria no esté registrada.");
        }

        await CargarListasAsync(vm);
        return View(vm);
    }

    // ==================================================================
    // ACTUALIZAR: solo pagos "Por verificar" (aún no afectan a las cuotas)
    // ==================================================================

    // GET: /Pagos/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var pago = await _db.Pagos.AsNoTracking().Include(p => p.Unidad).FirstOrDefaultAsync(p => p.Id == id);
        if (pago is null) return NotFound();
        if (pago.Estado != EstadoPago.PorVerificar)
        {
            TempData["Error"] = "Solo se pueden editar pagos que están por verificar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new PagoFormViewModel
        {
            Id = pago.Id,
            Torre = pago.Unidad?.Torre,
            UnidadId = pago.UnidadId,
            ResidenteId = pago.ResidenteId,
            FechaPago = pago.FechaPago,
            Monto = pago.Monto,
            Metodo = pago.Metodo,
            Referencia = pago.Referencia,
            ComprobanteActual = pago.Comprobante
        };
        await CargarListasAsync(vm);
        return View(vm);
    }

    // POST: /Pagos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PagoFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();

        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.Id == id);
        if (pago is null) return NotFound();
        if (pago.Estado != EstadoPago.PorVerificar)
        {
            TempData["Error"] = "Solo se pueden editar pagos que están por verificar.";
            return RedirectToAction(nameof(Details), new { id });
        }

        Normalizar(vm);
        await ValidarAsync(vm);
        vm.ComprobanteActual = pago.Comprobante;

        if (!ModelState.IsValid)
        {
            await CargarListasAsync(vm);
            return View(vm);
        }

        string? archivoNuevo = null;
        try
        {
            if (vm.Archivo is not null)
                archivoNuevo = await _comprobantes.GuardarAsync(vm.Archivo);

            var anterior = pago.Comprobante;
            pago.UnidadId = vm.UnidadId;
            pago.ResidenteId = vm.ResidenteId;
            pago.FechaPago = vm.FechaPago.Date;
            pago.Monto = vm.Monto;
            pago.Metodo = vm.Metodo;
            pago.Referencia = vm.Referencia;
            if (archivoNuevo is not null) pago.Comprobante = archivoNuevo;

            await _db.SaveChangesAsync();
            if (archivoNuevo is not null) _comprobantes.Borrar(anterior);

            TempData["Exito"] = "Pago actualizado.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException ex)
        {
            _comprobantes.Borrar(archivoNuevo);
            _logger.LogWarning(ex, "No se pudo actualizar el pago {Id}", id);
            ModelState.AddModelError(string.Empty, "No se pudo guardar el pago. Verifique la referencia bancaria.");
            await CargarListasAsync(vm);
            return View(vm);
        }
    }

    // ==================================================================
    // ELIMINAR: solo pagos "Por verificar"
    // ==================================================================

    // GET: /Pagos/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var pago = await CargarPagoAsync(id.Value);
        if (pago is null) return NotFound();
        if (pago.Estado != EstadoPago.PorVerificar)
        {
            TempData["Error"] = "Los pagos verificados o rechazados forman parte del historial y no se eliminan.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(pago);
    }

    // POST: /Pagos/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.Id == id);
        if (pago is null) return RedirectToAction(nameof(Index));
        if (pago.Estado != EstadoPago.PorVerificar)
        {
            TempData["Error"] = "Los pagos verificados o rechazados no se eliminan.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _db.Pagos.Remove(pago);
        await _db.SaveChangesAsync();
        _comprobantes.Borrar(pago.Comprobante);

        TempData["Exito"] = "Pago eliminado.";
        return RedirectToAction(nameof(Index), new { estado = EstadoPago.PorVerificar });
    }

    // ==================================================================
    // VERIFICAR / RECHAZAR (pagos reportados por residentes)
    // ==================================================================

    // GET: /Pagos/Verificar/5  (vista previa de la distribución)
    public async Task<IActionResult> Verificar(int? id)
    {
        if (id is null) return NotFound();
        var pago = await CargarPagoAsync(id.Value);
        if (pago is null) return NotFound();
        if (pago.Estado != EstadoPago.PorVerificar)
            return RedirectToAction(nameof(Details), new { id });

        return View(await ConstruirVerificacionAsync(pago));
    }

    // POST: /Pagos/Verificar/5
    [HttpPost, ActionName("Verificar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerificarConfirmado(int id)
    {
        try
        {
            var resultado = await _cobranza.VerificarYAplicarAsync(id, User.IdUsuario());
            TempData["Exito"] = $"Pago verificado y aplicado a {resultado.Lineas.Count} cuota(s). " +
                                $"Clasificación de la unidad: {Formato.Texto(resultado.ClasificacionDespues)}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Verificar), new { id });
        }
    }

    // POST: /Pagos/Rechazar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, [Bind("Observacion")] VerificarPagoViewModel datos)
    {
        var motivo = datos.Observacion?.Trim();
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 10)
            ModelState.AddModelError(nameof(VerificarPagoViewModel.Observacion),
                "Para rechazar un pago escriba el motivo (mínimo 10 caracteres).");

        if (!ModelState.IsValid)
        {
            var pago = await CargarPagoAsync(id);
            if (pago is null) return NotFound();
            var vm = await ConstruirVerificacionAsync(pago);
            vm.Observacion = datos.Observacion;
            return View(nameof(Verificar), vm);
        }

        try
        {
            await _cobranza.RechazarAsync(id, motivo!, User.IdUsuario());
            TempData["Exito"] = "Pago rechazado. No se aplicó a ninguna cuota y quedó registrado el motivo.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ==================================================================
    // Apoyo
    // ==================================================================

    private Task<Pago?> CargarPagoAsync(int id) =>
        _db.Pagos.AsNoTracking()
            .Include(p => p.Unidad)
            .Include(p => p.Residente)
            .Include(p => p.RegistradoPorUsuario)
            .Include(p => p.VerificadoPorUsuario)
            .Include(p => p.Aplicaciones).ThenInclude(a => a.Cuota)
            .FirstOrDefaultAsync(p => p.Id == id);

    private async Task<VerificarPagoViewModel> ConstruirVerificacionAsync(Pago pago)
    {
        var vm = new VerificarPagoViewModel { Pago = pago };
        vm.Distribucion = await _cobranza.PrevisualizarAsync(pago.UnidadId, pago.Monto, pago.FechaPago);
        if (vm.Distribucion is null)
            vm.Error = "No existe una configuración de cobro.";
        else if (vm.Distribucion.Lineas.Count == 0)
            vm.Error = "La unidad no tiene cuotas con saldo: el pago no se puede aplicar. Rechácelo indicando el motivo.";
        else if (vm.Distribucion.Excede)
            vm.Error = $"El pago supera el saldo total de la unidad ({vm.Distribucion.SaldoTotalUnidad:C}). " +
                       "No se admite; rechácelo indicando el motivo.";
        return vm;
    }

    /// <summary>Dropdowns en cascada: torres → unidades de la torre → residentes de la unidad.</summary>
    private async Task CargarListasAsync(PagoFormViewModel vm)
    {
        var torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync();
        vm.Torres = torres.Select(t => new SelectListItem(t, t, t == vm.Torre));

        vm.Unidades = string.IsNullOrEmpty(vm.Torre)
            ? Enumerable.Empty<SelectListItem>()
            : await _db.Unidades.AsNoTracking()
                .Where(u => u.Torre == vm.Torre)
                .OrderBy(u => u.Numero)
                .Select(u => new SelectListItem(u.Torre + "-" + u.Numero + (u.Activa ? "" : " (inactiva)"),
                                                u.Id.ToString(), u.Id == vm.UnidadId))
                .ToListAsync();

        vm.Residentes = vm.UnidadId <= 0
            ? Enumerable.Empty<SelectListItem>()
            : await _db.Residentes.AsNoTracking()
                .Where(r => r.UnidadId == vm.UnidadId)
                .OrderByDescending(r => r.ResponsablePago).ThenBy(r => r.Apellidos)
                .Select(r => new SelectListItem(
                    r.Nombres + " " + r.Apellidos + (r.ResponsablePago ? " · responsable de pago" : ""),
                    r.Id.ToString(), r.Id == vm.ResidenteId))
                .ToListAsync();
    }

    private static void Normalizar(PagoFormViewModel vm)
    {
        vm.Torre = vm.Torre?.Trim();
        vm.Referencia = ReglasPago.NormalizarReferencia(vm.Referencia);
        if (vm.ResidenteId is <= 0) vm.ResidenteId = null;
    }

    /// <summary>
    /// VALIDACIONES DE BACK-END del pago (dato sensible del Core: el dinero).
    /// Se ejecutan en el servidor aunque el navegador haya sido manipulado.
    /// </summary>
    private async Task ValidarAsync(PagoFormViewModel vm)
    {
        // 1. La unidad existe y pertenece a la torre elegida (no se confía en el dropdown)
        var unidad = await _db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == vm.UnidadId);
        if (unidad is null)
            ModelState.AddModelError(nameof(vm.UnidadId), "La unidad seleccionada no existe.");
        else if (!string.Equals(unidad.Torre, vm.Torre, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(vm.UnidadId), "La unidad no pertenece a la torre seleccionada.");

        // 2. El residente que paga vive en esa unidad
        if (vm.ResidenteId is int residenteId &&
            !await _db.Residentes.AnyAsync(r => r.Id == residenteId && r.UnidadId == vm.UnidadId))
            ModelState.AddModelError(nameof(vm.ResidenteId), "El residente seleccionado no pertenece a la unidad.");

        // 3. Fecha de pago: no futura ni con más de un año de antigüedad
        var errorFecha = ReglasPago.ValidarFecha(vm.FechaPago);
        if (errorFecha is not null) ModelState.AddModelError(nameof(vm.FechaPago), errorFecha);

        // 4. Monto: positivo, máximo 2 decimales y NO mayor al saldo total de la unidad
        if (vm.Monto <= 0)
            ModelState.AddModelError(nameof(vm.Monto), "El monto debe ser mayor que 0.");
        else if (unidad is not null && errorFecha is null &&
                 await _reglas.ValidarMontoAsync(unidad, vm.Monto, vm.FechaPago) is { } errorMonto)
            ModelState.AddModelError(nameof(vm.Monto), errorMonto);

        // 5. Referencia bancaria: obligatoria para transferencia/depósito, con formato y sin duplicados
        if (await _reglas.ValidarReferenciaAsync(vm.Metodo, vm.Referencia, vm.Id) is { } errorReferencia)
            ModelState.AddModelError(nameof(vm.Referencia), errorReferencia);

        // 6. Comprobante: tipo y tamaño verificados en el servidor (incluida la firma del archivo)
        if (vm.Archivo is not null && await _comprobantes.ValidarAsync(vm.Archivo) is { } errorArchivo)
            ModelState.AddModelError(nameof(vm.Archivo), errorArchivo);
    }
}
