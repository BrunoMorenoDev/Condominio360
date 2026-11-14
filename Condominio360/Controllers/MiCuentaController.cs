using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Condominio360.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Controllers;

/// <summary>
/// Core desde el lado del residente: estado de cuenta de SU unidad y reporte de pagos con comprobante.
/// El pago reportado queda "Por verificar" y no afecta a las cuotas hasta que la administración lo verifique.
/// </summary>
public class MiCuentaController : ResidenteBaseController
{
    private readonly CobranzaService _cobranza;
    private readonly ReglasPago _reglas;
    private readonly ComprobanteService _comprobantes;
    private readonly ILogger<MiCuentaController> _logger;

    public MiCuentaController(AppDbContext db, CobranzaService cobranza, ReglasPago reglas,
        ComprobanteService comprobantes, ILogger<MiCuentaController> logger) : base(db)
    {
        _cobranza = cobranza;
        _reglas = reglas;
        _comprobantes = comprobantes;
        _logger = logger;
    }

    // GET: /MiCuenta?corte=2026-11-14  (estado de cuenta de la unidad del residente)
    public async Task<IActionResult> Index(DateTime? corte)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var fecha = (corte ?? DateTime.Today).Date;
        if (fecha > DateTime.Today.AddYears(1)) fecha = DateTime.Today;

        var vm = await _cobranza.EstadoCuentaAsync(residente.UnidadId, fecha);
        return vm is null ? SinVinculo() : View(vm);
    }

    // GET: /MiCuenta/ReportarPago
    public async Task<IActionResult> ReportarPago()
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var vm = new ReportarPagoViewModel { FechaPago = DateTime.Today };
        await CargarResumenAsync(vm, residente);
        if (vm.Disponible > 0) vm.Monto = vm.Disponible;
        return View(vm);
    }

    // POST: /MiCuenta/ReportarPago
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> ReportarPago(ReportarPagoViewModel vm)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        vm.Referencia = ReglasPago.NormalizarReferencia(vm.Referencia);
        await ValidarAsync(vm, residente);

        if (!ModelState.IsValid)
        {
            await CargarResumenAsync(vm, residente);
            return View(vm);
        }

        var pago = new Pago
        {
            UnidadId = residente.UnidadId,          // siempre la unidad del residente, nunca la del formulario
            ResidenteId = residente.Id,
            RegistradoPor = User.IdUsuario(),
            FechaPago = vm.FechaPago.Date,
            Monto = vm.Monto,
            Metodo = vm.Metodo,
            Referencia = vm.Referencia,
            Estado = EstadoPago.PorVerificar
        };

        string? archivo = null;
        try
        {
            pago.Comprobante = archivo = await _comprobantes.GuardarAsync(vm.Archivo!);
            Db.Pagos.Add(pago);
            await Db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _comprobantes.Borrar(archivo);
            _logger.LogWarning(ex, "No se pudo guardar el pago reportado por el residente {Residente}", residente.Id);
            ModelState.AddModelError(string.Empty, "No se pudo guardar el pago. Verifique que la referencia no esté registrada.");
            await CargarResumenAsync(vm, residente);
            return View(vm);
        }

        TempData["Exito"] = $"Pago de {pago.Monto:C} reportado. Queda por verificar: la administración revisará el comprobante " +
                            "y, al aprobarlo, se aplicará a sus cuotas más antiguas.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /MiCuenta/AnularReporte/5  (solo pagos propios que siguen por verificar)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnularReporte(int id)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var idUsuario = User.IdUsuario();
        var pago = await Db.Pagos.FirstOrDefaultAsync(p =>
            p.Id == id && p.UnidadId == residente.UnidadId && p.RegistradoPor == idUsuario);
        if (pago is null) return NotFound();

        if (pago.Estado != EstadoPago.PorVerificar)
        {
            TempData["Error"] = "Solo puede anular pagos que todavía están por verificar.";
            return RedirectToAction(nameof(Index));
        }

        Db.Pagos.Remove(pago);
        await Db.SaveChangesAsync();
        _comprobantes.Borrar(pago.Comprobante);

        TempData["Exito"] = "El reporte de pago fue anulado.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /MiCuenta/Comprobante/5  (solo comprobantes de pagos de la unidad del residente)
    public async Task<IActionResult> Comprobante(int id)
    {
        var residente = await ResidenteActualAsync();
        if (residente is null) return SinVinculo();

        var ruta = await Db.Pagos
            .Where(p => p.Id == id && p.UnidadId == residente.UnidadId)
            .Select(p => p.Comprobante)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(ruta)) return NotFound();

        var archivo = _comprobantes.Obtener(ruta);
        return archivo is null
            ? NotFound("El archivo del comprobante no está disponible en este servidor.")
            : PhysicalFile(archivo.Value.Ruta, archivo.Value.Tipo);
    }

    // ------------------------------------------------------------------

    private async Task CargarResumenAsync(ReportarPagoViewModel vm, Residente residente)
    {
        vm.Unidad = residente.Unidad;
        vm.SaldoTotal = await _cobranza.SaldoUnidadAsync(residente.UnidadId, DateTime.Today);
        vm.PorVerificar = await Db.Pagos
            .Where(p => p.UnidadId == residente.UnidadId && p.Estado == EstadoPago.PorVerificar)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;
    }

    /// <summary>Validaciones de back-end del pago reportado (dato sensible: dinero y comprobante).</summary>
    private async Task ValidarAsync(ReportarPagoViewModel vm, Residente residente)
    {
        // 1. El residente reporta transferencias o depósitos; el efectivo se entrega en administración
        if (vm.Metodo == MetodoPago.Efectivo || !Enum.IsDefined(vm.Metodo))
            ModelState.AddModelError(nameof(vm.Metodo), "Los pagos en efectivo se registran en la administración. Elija transferencia o depósito.");

        // 2. Fecha de pago
        var errorFecha = ReglasPago.ValidarFecha(vm.FechaPago);
        if (errorFecha is not null) ModelState.AddModelError(nameof(vm.FechaPago), errorFecha);

        // 3. Monto: no mayor al saldo de la unidad menos lo que ya reportó y sigue por verificar
        if (residente.Unidad is not null && vm.Monto > 0 && errorFecha is null &&
            await _reglas.ValidarMontoAsync(residente.Unidad, vm.Monto, vm.FechaPago, descontarPorVerificar: true) is { } errorMonto)
            ModelState.AddModelError(nameof(vm.Monto), errorMonto);

        // 4. Referencia: formato y no duplicada
        if (vm.Referencia is not null &&
            await _reglas.ValidarReferenciaAsync(vm.Metodo, vm.Referencia) is { } errorReferencia)
            ModelState.AddModelError(nameof(vm.Referencia), errorReferencia);

        // 5. Comprobante obligatorio y verificado (tamaño, extensión y firma del archivo)
        if (vm.Archivo is null)
            ModelState.AddModelError(nameof(vm.Archivo), "Adjunte el comprobante de la transferencia o del depósito.");
        else if (await _comprobantes.ValidarAsync(vm.Archivo) is { } errorArchivo)
            ModelState.AddModelError(nameof(vm.Archivo), errorArchivo);
    }
}
