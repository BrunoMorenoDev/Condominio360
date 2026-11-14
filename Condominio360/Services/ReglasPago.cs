using System.Text.RegularExpressions;
using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Services;

/// <summary>
/// Reglas de back-end del pago (dato sensible del Core: el dinero), compartidas por el registro
/// en administración y por el reporte de pagos de los residentes. Cada método devuelve null si
/// el dato es válido o el mensaje de error que se muestra junto al campo.
/// </summary>
public partial class ReglasPago
{
    private readonly AppDbContext _db;
    private readonly CobranzaService _cobranza;

    public ReglasPago(AppDbContext db, CobranzaService cobranza)
    {
        _db = db;
        _cobranza = cobranza;
    }

    [GeneratedRegex("^[A-Z0-9-]{4,40}$")]
    private static partial Regex FormatoReferencia();

    public static string? NormalizarReferencia(string? referencia) =>
        string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim().ToUpperInvariant();

    /// <summary>Fecha de pago: no futura ni con más de un año de antigüedad.</summary>
    public static string? ValidarFecha(DateTime fechaPago)
    {
        if (fechaPago.Date > DateTime.Today) return "La fecha de pago no puede ser futura.";
        if (fechaPago.Date < DateTime.Today.AddYears(-1)) return "La fecha de pago no puede tener más de un año de antigüedad.";
        return null;
    }

    /// <summary>
    /// Monto positivo, con máximo dos decimales y no mayor al saldo total de la unidad a la fecha del pago.
    /// Si <paramref name="descontarPorVerificar"/> es true, también se restan los pagos que la unidad
    /// ya reportó y siguen por verificar (evita que un residente reporte dos veces la misma deuda).
    /// </summary>
    public async Task<string?> ValidarMontoAsync(Unidad unidad, decimal monto, DateTime fechaPago,
        bool descontarPorVerificar = false, int excluirPagoId = 0)
    {
        if (monto <= 0) return "El monto debe ser mayor que 0.";
        if (decimal.Round(monto, 2) != monto) return "El monto admite como máximo dos decimales.";

        var saldo = await _cobranza.SaldoUnidadAsync(unidad.Id, fechaPago.Date);
        if (saldo <= 0) return $"La unidad {unidad.Codigo} no tiene saldo pendiente.";

        if (descontarPorVerificar)
        {
            var reportado = await _db.Pagos
                .Where(p => p.UnidadId == unidad.Id && p.Estado == EstadoPago.PorVerificar && p.Id != excluirPagoId)
                .SumAsync(p => (decimal?)p.Monto) ?? 0m;
            var disponible = saldo - reportado;
            if (disponible <= 0)
                return $"Los pagos que ya reportó ({reportado:C}) cubren el saldo de la unidad. Espere a que la administración los verifique.";
            if (monto > disponible)
                return $"El monto supera el saldo pendiente de la unidad ({saldo:C}) menos los pagos por verificar ({reportado:C}). Máximo: {disponible:C}.";
            return null;
        }

        return monto > saldo
            ? $"El monto supera el saldo total de la unidad {unidad.Codigo} ({saldo:C}) a la fecha del pago."
            : null;
    }

    /// <summary>Referencia bancaria: obligatoria para transferencia/depósito, con formato y sin duplicados.</summary>
    public async Task<string?> ValidarReferenciaAsync(MetodoPago metodo, string? referencia, int pagoId = 0)
    {
        if (referencia is null)
            return metodo == MetodoPago.Efectivo ? null : "La referencia es obligatoria para transferencias y depósitos.";

        if (!FormatoReferencia().IsMatch(referencia))
            return "Use de 4 a 40 letras, números o guiones.";

        var duplicada = await _db.Pagos.AnyAsync(p =>
            p.Id != pagoId && p.Metodo == metodo && p.Referencia == referencia && p.Estado != EstadoPago.Rechazado);
        return duplicada ? $"La referencia {referencia} ya fue registrada en otro pago ({Formato.Texto(metodo)})." : null;
    }
}
