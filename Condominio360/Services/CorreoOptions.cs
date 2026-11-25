namespace Condominio360.Services;

/// <summary>
/// Configuración del envío de correos (sección "Correo" de appsettings).
/// El usuario y la contraseña de aplicación de Gmail NO se suben a Git:
/// van en appsettings.Local.json (local) o en variables de entorno de Azure (Correo__Usuario, Correo__Password).
/// </summary>
public class CorreoOptions
{
    public const string Seccion = "Correo";

    /// <summary>Servidor SMTP (Gmail: smtp.gmail.com).</summary>
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>587 = STARTTLS (recomendado) · 465 = SSL directo.</summary>
    public int Puerto { get; set; } = 587;

    /// <summary>Cuenta de Gmail que envía las notificaciones.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Contraseña de aplicación de 16 caracteres generada en la cuenta de Google.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Nombre que verá el destinatario como remitente.</summary>
    public string NombreRemitente { get; set; } = "Condominio360";

    /// <summary>Sin credenciales el envío se omite y la aplicación sigue funcionando normalmente.</summary>
    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Usuario) &&
        !string.IsNullOrWhiteSpace(Password);
}
