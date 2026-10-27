using System.ComponentModel.DataAnnotations;

namespace Condominio360.Models;

/// <summary>Estado de una cuota mensual.</summary>
public enum EstadoCuota
{
    Pendiente,
    Parcial,
    Pagada,
    Vencida,
    Anulada
}

/// <summary>Estado de un pago en el flujo de verificación.</summary>
public enum EstadoPago
{
    [Display(Name = "Por verificar")] PorVerificar,
    Verificado,
    Rechazado
}

/// <summary>Forma en que se realizó el pago.</summary>
public enum MetodoPago
{
    Efectivo,
    Transferencia,
    [Display(Name = "Depósito")] Deposito
}

/// <summary>Clasificación de la unidad según el número de cuotas vencidas con saldo (n).</summary>
public enum ClasificacionMorosidad
{
    [Display(Name = "Al día")] AlDia,
    [Display(Name = "Atraso leve")] AtrasoLeve,
    Moroso,
    [Display(Name = "Moroso crítico")] MorosoCritico
}

public enum EstadoReserva
{
    Solicitada,
    Aprobada,
    Rechazada,
    Cancelada
}

public enum EstadoVisita
{
    Autorizada,
    Ingresada,
    Finalizada,
    Cancelada
}
