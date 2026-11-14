using System.Text.RegularExpressions;

namespace Condominio360.Services;

/// <summary>
/// Validación y normalización de placas vehiculares ecuatorianas en el servidor:
/// autos ABC-1234 / ABC-123 y motos AB-123C (con o sin guion, en mayúsculas o minúsculas).
/// </summary>
public static partial class ValidadorPlaca
{
    [GeneratedRegex("^([A-Z]{3})-?([0-9]{3,4})$")]
    private static partial Regex Auto();

    [GeneratedRegex("^([A-Z]{2})-?([0-9]{3}[A-Z])$")]
    private static partial Regex Moto();

    /// <summary>Devuelve la placa normalizada (ABC-1234) o null si está vacía. Lanza el mensaje en <paramref name="error"/> si no es válida.</summary>
    public static string? Normalizar(string? placa, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(placa)) return null;

        var limpia = placa.Trim().ToUpperInvariant().Replace(" ", "");
        var auto = Auto().Match(limpia);
        if (auto.Success) return $"{auto.Groups[1].Value}-{auto.Groups[2].Value}";

        var moto = Moto().Match(limpia);
        if (moto.Success) return $"{moto.Groups[1].Value}-{moto.Groups[2].Value}";

        error = "Placa no válida. Use el formato ABC-1234 (auto) o AB-123C (moto).";
        return limpia;
    }
}
