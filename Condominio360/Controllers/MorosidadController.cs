using System.Globalization;
using System.Text;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condominio360.Controllers;

/// <summary>Reporte de morosidad con los indicadores del condominio (wireframe, Figura 1).</summary>
[Authorize(Roles = Rol.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MorosidadController : Controller
{
    private readonly CobranzaService _cobranza;

    public MorosidadController(CobranzaService cobranza) => _cobranza = cobranza;

    // GET: /Morosidad?corte=2026-10-27&torre=A&clasificacion=Moroso&buscar=andrade
    public async Task<IActionResult> Index(DateTime? corte, string? torre, ClasificacionMorosidad? clasificacion, string? buscar)
    {
        var vm = await _cobranza.ReporteMorosidadAsync(FechaCorte(corte), torre, clasificacion, buscar);
        return View(vm);
    }

    // GET: /Morosidad/Exportar?...  (CSV compatible con Excel)
    public async Task<IActionResult> Exportar(DateTime? corte, string? torre, ClasificacionMorosidad? clasificacion, string? buscar)
    {
        var vm = await _cobranza.ReporteMorosidadAsync(FechaCorte(corte), torre, clasificacion, buscar);
        var inv = CultureInfo.InvariantCulture;

        var csv = new StringBuilder();
        csv.AppendLine($"Reporte de morosidad;Fecha de corte;{vm.Corte:dd/MM/yyyy}");
        csv.AppendLine($"Indice de morosidad (%);{vm.IndiceMorosidad.ToString(inv)}");
        csv.AppendLine($"Cartera vencida (USD);{vm.CarteraVencida.ToString("0.00", inv)}");
        csv.AppendLine($"Tasa de recaudacion {vm.PeriodoRecaudacion} (%);{vm.TasaRecaudacion.ToString(inv)}");
        csv.AppendLine();
        csv.AppendLine("Unidad;Responsable de pago;Alicuota;Cuotas vencidas;Dias de mora;Recargo;Saldo vencido;Saldo total;Clasificacion");
        foreach (var f in vm.Filas)
        {
            csv.AppendLine(string.Join(';',
                f.Unidad.Codigo,
                Limpiar(f.Responsable?.NombreCompleto ?? "-"),
                f.Unidad.Alicuota.ToString("0.00", inv),
                f.CuotasVencidas,
                f.DiasMora,
                f.Recargo.ToString("0.00", inv),
                f.SaldoVencido.ToString("0.00", inv),
                f.SaldoTotal.ToString("0.00", inv),
                Formato.Texto(f.Clasificacion)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"morosidad_{vm.Corte:yyyyMMdd}.csv");
    }

    private static DateTime FechaCorte(DateTime? corte)
    {
        var fecha = (corte ?? DateTime.Today).Date;
        return fecha > DateTime.Today.AddYears(1) ? DateTime.Today : fecha;
    }

    /// <summary>Evita romper el CSV y la inyección de fórmulas en Excel.</summary>
    private static string Limpiar(string texto)
    {
        var t = texto.Replace(";", ",").Replace("\"", "'");
        return t.Length > 0 && "=+-@".Contains(t[0]) ? "'" + t : t;
    }
}
