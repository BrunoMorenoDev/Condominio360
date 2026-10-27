using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Condominio360.Models;

namespace Condominio360.Helpers;

/// <summary>Utilidades de presentación compartidas por vistas y controladores.</summary>
public static class Formato
{
    private static readonly CultureInfo Es = new("es-EC");

    public static string NombreMes(int mes) =>
        Es.TextInfo.ToTitleCase(Es.DateTimeFormat.GetMonthName(mes));

    public static string Periodo(int anio, int mes) => $"{NombreMes(mes)} {anio}";

    /// <summary>Texto del atributo [Display(Name)] de un valor de enum, o su nombre.</summary>
    public static string Texto(Enum valor)
    {
        var miembro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
        return miembro?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
    }

    public static string ClaseEstado(EstadoCuota estado) => estado switch
    {
        EstadoCuota.Pagada => "estado estado-ok",
        EstadoCuota.Parcial => "estado estado-info",
        EstadoCuota.Vencida => "estado estado-mal",
        EstadoCuota.Anulada => "estado estado-neutro",
        _ => "estado estado-pend"
    };

    public static string ClaseEstado(EstadoPago estado) => estado switch
    {
        EstadoPago.Verificado => "estado estado-ok",
        EstadoPago.Rechazado => "estado estado-mal",
        _ => "estado estado-pend"
    };

    public static string ClaseClasificacion(ClasificacionMorosidad c) => c switch
    {
        ClasificacionMorosidad.AlDia => "estado estado-ok",
        ClasificacionMorosidad.AtrasoLeve => "estado estado-pend",
        ClasificacionMorosidad.Moroso => "estado estado-alerta",
        _ => "estado estado-mal"
    };
}
