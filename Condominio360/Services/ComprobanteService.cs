namespace Condominio360.Services;

/// <summary>
/// Almacenamiento de los comprobantes de pago. Lo usan la administración (registro de pagos)
/// y los residentes (reporte de pagos). Los archivos se guardan FUERA de wwwroot:
/// solo se descargan a través de una acción que verifica la sesión y el dueño del pago.
/// </summary>
public class ComprobanteService
{
    public const long TamanoMaximo = 2 * 1024 * 1024;   // 2 MB
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf" };

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ComprobanteService> _logger;

    public ComprobanteService(IWebHostEnvironment env, ILogger<ComprobanteService> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>Valida tamaño, extensión y firma (magic bytes). Devuelve null si el archivo es válido.</summary>
    public async Task<string?> ValidarAsync(IFormFile archivo)
    {
        if (archivo.Length == 0) return "El archivo está vacío.";
        if (archivo.Length > TamanoMaximo) return "El comprobante no puede superar 2 MB.";

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension)) return "Solo se aceptan archivos JPG, PNG o PDF.";

        // Firma del archivo: evita archivos renombrados con otra extensión
        var cabecera = new byte[8];
        await using var stream = archivo.OpenReadStream();
        var leidos = await stream.ReadAsync(cabecera);
        bool firmaValida = extension switch
        {
            ".pdf" => leidos >= 4 && cabecera[0] == 0x25 && cabecera[1] == 0x50 && cabecera[2] == 0x44 && cabecera[3] == 0x46,
            ".png" => leidos >= 4 && cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47,
            _ => leidos >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF
        };
        return firmaValida ? null : "El contenido del archivo no corresponde a su extensión.";
    }

    /// <summary>Guarda el archivo con un nombre aleatorio y devuelve la ruta relativa que se graba en la BD.</summary>
    public async Task<string> GuardarAsync(IFormFile archivo)
    {
        var carpeta = Carpeta();
        Directory.CreateDirectory(carpeta);
        var nombre = $"{Guid.NewGuid():N}{Path.GetExtension(archivo.FileName).ToLowerInvariant()}";
        await using var destino = File.Create(Path.Combine(carpeta, nombre));
        await archivo.CopyToAsync(destino);
        return $"comprobantes/{nombre}";
    }

    /// <summary>Ruta física y tipo MIME de un comprobante, o null si no existe en este servidor.</summary>
    public (string Ruta, string Tipo)? Obtener(string? rutaRelativa)
    {
        var archivo = RutaFisica(rutaRelativa);
        if (archivo is null || !File.Exists(archivo)) return null;

        var tipo = Path.GetExtension(archivo).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _ => "image/jpeg"
        };
        return (archivo, tipo);
    }

    public void Borrar(string? rutaRelativa)
    {
        var archivo = RutaFisica(rutaRelativa);
        try
        {
            if (archivo is not null && File.Exists(archivo)) File.Delete(archivo);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar el comprobante {Archivo}", archivo);
        }
    }

    private string Carpeta() => Path.Combine(_env.ContentRootPath, "App_Data", "comprobantes");

    private string? RutaFisica(string? rutaRelativa)
    {
        if (string.IsNullOrEmpty(rutaRelativa)) return null;
        var nombre = Path.GetFileName(rutaRelativa);   // impide rutas tipo ../../
        return string.IsNullOrEmpty(nombre) ? null : Path.Combine(Carpeta(), nombre);
    }
}
