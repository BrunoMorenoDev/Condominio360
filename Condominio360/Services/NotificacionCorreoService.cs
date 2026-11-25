using System.Net;
using Condominio360.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Condominio360.Services;

/// <summary>
/// Envía notificaciones por correo electrónico con MailKit a través del SMTP de Gmail.
/// Un fallo en el envío se registra en el log, pero nunca impide la operación principal
/// (la visita queda registrada aunque el correo no salga).
/// </summary>
public class NotificacionCorreoService
{
    private const int TiempoEsperaMs = 15000;   // evita que la petición quede colgada si Gmail no responde

    private readonly CorreoOptions _opciones;
    private readonly ILogger<NotificacionCorreoService> _logger;

    public NotificacionCorreoService(IOptions<CorreoOptions> opciones, ILogger<NotificacionCorreoService> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    /// <summary>
    /// Avisa a los guardias que un residente autorizó una visita.
    /// Devuelve cuántos guardias fueron notificados (0 si no se envió).
    /// </summary>
    public async Task<int> NotificarVisitaAutorizadaAsync(
        Visita visita, string unidad, string residente, IEnumerable<string> correosGuardias, string? enlaceGarita)
    {
        if (!_opciones.EstaConfigurado)
        {
            _logger.LogWarning("Correo no configurado (sección '{Seccion}'): no se notificó la visita {Id}.",
                CorreoOptions.Seccion, visita.Id);
            return 0;
        }

        // Solo direcciones válidas y sin repetir
        var destinatarios = new List<MailboxAddress>();
        foreach (var correo in correosGuardias.Select(c => c?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(correo) && MailboxAddress.TryParse(correo, out var direccion))
                destinatarios.Add(direccion);
        }

        if (destinatarios.Count == 0)
        {
            _logger.LogWarning("No hay guardias activos con correo válido: no se notificó la visita {Id}.", visita.Id);
            return 0;
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_opciones.NombreRemitente, _opciones.Usuario));
        mensaje.To.Add(new MailboxAddress(_opciones.NombreRemitente, _opciones.Usuario)); // copia para el sistema
        mensaje.Bcc.AddRange(destinatarios);                                                // guardias en copia oculta
        mensaje.Subject = $"Nueva visita: {visita.NombreVisitante} · Unidad {unidad} · {visita.FechaEsperada:dd/MM/yyyy}";
        mensaje.Body = CrearCuerpo(visita, unidad, residente, enlaceGarita).ToMessageBody();

        using var smtp = new SmtpClient
        {
            Timeout = TiempoEsperaMs,
            // En macOS la consulta de revocación (CRL/OCSP) falla con "incomplete certificate revocation check".
            // El certificado se sigue validando (cadena de confianza, vigencia y nombre del servidor).
            CheckCertificateRevocation = false
        };
        try
        {
            var seguridad = _opciones.Puerto == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await smtp.ConnectAsync(_opciones.Host, _opciones.Puerto, seguridad);
            // Gmail muestra la contraseña de aplicación con espacios: se quitan antes de autenticar
            await smtp.AuthenticateAsync(_opciones.Usuario, _opciones.Password.Replace(" ", string.Empty));
            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Visita {Id} notificada por correo a {Total} guardia(s).", visita.Id, destinatarios.Count);
            return destinatarios.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo de la visita {Id}.", visita.Id);
            return 0;
        }
    }

    private static BodyBuilder CrearCuerpo(Visita visita, string unidad, string residente, string? enlaceGarita)
    {
        var placa = string.IsNullOrWhiteSpace(visita.PlacaVehiculo) ? "Sin vehículo" : visita.PlacaVehiculo;
        var fecha = visita.FechaEsperada.ToString("dd/MM/yyyy");

        string H(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);

        var boton = string.IsNullOrWhiteSpace(enlaceGarita)
            ? string.Empty
            : $"""<p style="margin:24px 0 0"><a href="{H(enlaceGarita)}" style="background:#0d6efd;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none">Abrir garita</a></p>""";

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;color:#212529">
              <h2 style="color:#0d6efd;margin-bottom:4px">Nueva visita autorizada</h2>
              <p style="margin-top:0;color:#6c757d">Un residente registró una visita. Verifique la identidad del visitante al momento del ingreso.</p>
              <table style="border-collapse:collapse;width:100%">
                <tr><td style="padding:6px 0;width:40%"><b>Visitante</b></td><td>{H(visita.NombreVisitante)}</td></tr>
                <tr><td style="padding:6px 0"><b>Cédula</b></td><td>{H(visita.CedulaVisitante)}</td></tr>
                <tr><td style="padding:6px 0"><b>Placa</b></td><td>{H(placa)}</td></tr>
                <tr><td style="padding:6px 0"><b>Unidad</b></td><td>{H(unidad)}</td></tr>
                <tr><td style="padding:6px 0"><b>Autorizada por</b></td><td>{H(residente)}</td></tr>
                <tr><td style="padding:6px 0"><b>Fecha esperada</b></td><td>{fecha}</td></tr>
              </table>
              {boton}
              <p style="margin-top:24px;font-size:12px;color:#6c757d">Mensaje automático de Condominio360. No responda a este correo.</p>
            </div>
            """;

        var texto = $"""
            Nueva visita autorizada - Condominio360

            Visitante: {visita.NombreVisitante}
            Cédula: {visita.CedulaVisitante}
            Placa: {placa}
            Unidad: {unidad}
            Autorizada por: {residente}
            Fecha esperada: {fecha}
            {(string.IsNullOrWhiteSpace(enlaceGarita) ? string.Empty : $"\nGarita: {enlaceGarita}")}
            """;

        return new BodyBuilder { HtmlBody = html, TextBody = texto };
    }
}
