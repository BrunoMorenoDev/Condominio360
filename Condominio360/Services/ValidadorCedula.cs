namespace Condominio360.Services;

/// <summary>
/// Validación en el SERVIDOR de la cédula ecuatoriana (persona natural) con el
/// algoritmo módulo 10 del Registro Civil. El navegador solo comprueba que tenga
/// 10 dígitos; aunque se desactive JavaScript o se manipule el formulario,
/// una cédula inválida nunca llega a la base de datos.
/// </summary>
public static class ValidadorCedula
{
    private static readonly int[] Coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };

    /// <summary>Devuelve null si la cédula es válida o el mensaje de error si no lo es.</summary>
    public static string? Validar(string? cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10 || !cedula.All(char.IsAsciiDigit))
            return "La cédula debe tener exactamente 10 dígitos.";

        // Dígitos 1-2: provincia (01 a 24) o 30 para ecuatorianos registrados en el exterior
        var provincia = int.Parse(cedula[..2]);
        if (provincia is < 1 or > 24 && provincia != 30)
            return "El código de provincia de la cédula (dos primeros dígitos) no es válido.";

        // Dígito 3: menor que 6 para personas naturales
        if (cedula[2] - '0' >= 6)
            return "El tercer dígito de la cédula debe ser menor que 6 (persona natural).";

        // Dígito 10: verificador (módulo 10)
        var suma = 0;
        for (var i = 0; i < 9; i++)
        {
            var producto = (cedula[i] - '0') * Coeficientes[i];
            suma += producto > 9 ? producto - 9 : producto;
        }
        var verificador = (10 - suma % 10) % 10;

        return verificador == cedula[9] - '0'
            ? null
            : "La cédula no es válida: el dígito verificador no corresponde.";
    }

    public static bool EsValida(string? cedula) => Validar(cedula) is null;
}
