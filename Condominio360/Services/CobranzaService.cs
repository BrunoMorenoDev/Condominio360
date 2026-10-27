using Condominio360.Data;
using Condominio360.Helpers;
using Condominio360.Models;
using Condominio360.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Condominio360.Services;

/// <summary>
/// Lógica del Core: cobranza de alícuotas con recargo por mora y clasificación de morosidad.
/// Implementa los pasos a–e de la sección 2 del Documento de Especificación Funcional.
/// Los controladores solo orquestan; las reglas viven aquí y en <see cref="Cuota"/>.
/// </summary>
public class CobranzaService
{
    private readonly AppDbContext _db;

    public CobranzaService(AppDbContext db) => _db = db;

    // ------------------------------------------------------------------
    // Configuración vigente
    // ------------------------------------------------------------------

    /// <summary>Configuración con mayor "vigente desde" que no supera la fecha indicada.</summary>
    public async Task<ConfiguracionCobro?> ConfiguracionVigenteAsync(DateTime fecha)
    {
        var dia = fecha.Date;
        return await _db.ConfiguracionesCobro.AsNoTracking()
                   .Where(c => c.VigenteDesde <= dia)
                   .OrderByDescending(c => c.VigenteDesde)
                   .FirstOrDefaultAsync()
               // Si la fecha es anterior a toda configuración, se usa la más antigua
               ?? await _db.ConfiguracionesCobro.AsNoTracking()
                   .OrderBy(c => c.VigenteDesde)
                   .FirstOrDefaultAsync();
    }

    // ------------------------------------------------------------------
    // d. Clasificación de morosidad
    // ------------------------------------------------------------------

    /// <summary>n = 0 Al día · n = 1 Atraso leve · n = 2 o 3 Moroso · n ≥ 4 Moroso crítico.</summary>
    public static ClasificacionMorosidad Clasificar(int cuotasVencidas) => cuotasVencidas switch
    {
        <= 0 => ClasificacionMorosidad.AlDia,
        1 => ClasificacionMorosidad.AtrasoLeve,
        <= 3 => ClasificacionMorosidad.Moroso,
        _ => ClasificacionMorosidad.MorosoCritico
    };

    public static int ContarVencidas(IEnumerable<Cuota> cuotas, DateTime corte, ConfiguracionCobro cfg) =>
        cuotas.Count(c => c.EstaVencida(corte, cfg));

    // ------------------------------------------------------------------
    // a. Generar las cuotas del periodo
    // ------------------------------------------------------------------

    public async Task<GenerarCuotasViewModel> PrevisualizarGeneracionAsync(int anio, int mes)
    {
        var vm = new GenerarCuotasViewModel { Anio = anio, Mes = mes };

        vm.Error = ValidarPeriodo(anio, mes);
        if (vm.Error is not null) return vm;

        var emision = new DateTime(anio, mes, 1);
        var cfg = await ConfiguracionVigenteAsync(emision);
        vm.Configuracion = cfg;
        if (cfg is null)
        {
            vm.Error = "No existe una configuración de cobro. Regístrela antes de generar cuotas.";
            return vm;
        }

        var activas = await _db.Unidades.AsNoTracking().Where(u => u.Activa).ToListAsync();
        var conCuota = await _db.Cuotas.AsNoTracking()
            .Where(c => c.Anio == anio && c.Mes == mes)
            .Select(c => c.UnidadId)
            .ToListAsync();

        var porGenerar = activas.Where(u => !conCuota.Contains(u.Id)).ToList();

        vm.UnidadesActivas = activas.Count;
        vm.YaGeneradas = activas.Count(u => conCuota.Contains(u.Id));
        vm.PorGenerar = porGenerar.Count;
        vm.TotalAEmitir = porGenerar.Sum(u => u.Alicuota);
        vm.PorTipo = porGenerar
            .GroupBy(u => u.Tipo)
            .Select(g => new GenerarCuotasTipo { Tipo = g.Key, Unidades = g.Count(), Subtotal = g.Sum(u => u.Alicuota) })
            .OrderByDescending(t => t.Subtotal)
            .ToList();
        vm.FechaEmision = emision;
        vm.FechaVencimiento = cfg.FechaVencimiento(anio, mes);
        vm.FechaLimite = cfg.FechaLimite(vm.FechaVencimiento);
        return vm;
    }

    /// <summary>
    /// Crea una cuota Pendiente por cada unidad activa sin cuota en el periodo.
    /// Las unidades que ya tienen cuota se omiten (la restricción única unidad–año–mes lo garantiza).
    /// </summary>
    public async Task<int> GenerarCuotasAsync(int anio, int mes, int usuarioId)
    {
        var error = ValidarPeriodo(anio, mes);
        if (error is not null) throw new ReglaNegocioException(error);

        var emision = new DateTime(anio, mes, 1);
        var cfg = await ConfiguracionVigenteAsync(emision)
                  ?? throw new ReglaNegocioException("No existe una configuración de cobro.");

        var conCuota = await _db.Cuotas
            .Where(c => c.Anio == anio && c.Mes == mes)
            .Select(c => c.UnidadId)
            .ToListAsync();

        var unidades = await _db.Unidades
            .Where(u => u.Activa && !conCuota.Contains(u.Id))
            .ToListAsync();

        foreach (var u in unidades)
        {
            _db.Cuotas.Add(new Cuota
            {
                UnidadId = u.Id,
                Anio = anio,
                Mes = mes,
                Monto = u.Alicuota,
                CapitalPagado = 0,
                RecargoPagado = 0,
                FechaEmision = emision,
                FechaVencimiento = cfg.FechaVencimiento(anio, mes),
                Estado = EstadoCuota.Pendiente,
                GeneradaPor = usuarioId
            });
        }

        await _db.SaveChangesAsync();
        return unidades.Count;
    }

    /// <summary>Solo se generan periodos desde 2020 hasta el mes siguiente al actual.</summary>
    public static string? ValidarPeriodo(int anio, int mes)
    {
        if (mes is < 1 or > 12) return "El mes debe estar entre 1 y 12.";
        if (anio < 2020) return "El año debe ser 2020 o posterior.";
        var periodo = new DateTime(anio, mes, 1);
        var limite = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        if (periodo > limite)
            return $"Solo se pueden generar cuotas hasta {Formato.Periodo(limite.Year, limite.Month)}.";
        return null;
    }

    // ------------------------------------------------------------------
    // b. Saldo de cada cuota a una fecha de corte
    // ------------------------------------------------------------------

    public static CuotaFila Calcular(Cuota c, DateTime corte, ConfiguracionCobro cfg) => new()
    {
        Cuota = c,
        DiasAtraso = c.DiasAtraso(corte, cfg),
        MesesAtraso = c.MesesAtraso(corte, cfg),
        Recargo = c.Recargo(corte, cfg),
        Saldo = c.Saldo(corte, cfg),
        Estado = c.CalcularEstado(corte, cfg)
    };

    /// <summary>Saldo total de la unidad (capital + recargos pendientes) a la fecha indicada.</summary>
    public async Task<decimal> SaldoUnidadAsync(int unidadId, DateTime fecha)
    {
        var cfg = await ConfiguracionVigenteAsync(fecha);
        if (cfg is null) return 0m;
        var cuotas = await _db.Cuotas.AsNoTracking()
            .Where(c => c.UnidadId == unidadId && c.Estado != EstadoCuota.Anulada)
            .ToListAsync();
        return cuotas.Sum(c => c.Saldo(fecha, cfg));
    }

    // ------------------------------------------------------------------
    // c. Distribución de un pago verificado
    // ------------------------------------------------------------------

    /// <summary>
    /// Calcula (sin guardar) cómo se repartiría un pago: cuotas con saldo de la más antigua
    /// a la más reciente, primero el recargo pendiente y luego el capital.
    /// </summary>
    public static ResultadoDistribucion CalcularDistribucion(
        IEnumerable<Cuota> cuotasUnidad, decimal monto, DateTime fechaPago,
        ConfiguracionCobro cfg, DateTime hoy, ConfiguracionCobro cfgHoy)
    {
        var todas = cuotasUnidad.Where(c => c.Estado != EstadoCuota.Anulada).ToList();
        var pendientes = todas
            .Where(c => c.Saldo(fechaPago, cfg) > 0)
            .OrderBy(c => c.FechaVencimiento).ThenBy(c => c.Id)
            .ToList();

        var resultado = new ResultadoDistribucion
        {
            SaldoTotalUnidad = pendientes.Sum(c => c.Saldo(fechaPago, cfg)),
            ClasificacionAntes = Clasificar(ContarVencidas(todas, hoy, cfgHoy))
        };

        var simuladas = todas.ToDictionary(c => c.Id, c => c.Clonar());
        var resto = monto;

        foreach (var original in pendientes)
        {
            if (resto <= 0) break;
            var c = simuladas[original.Id];

            var aRecargo = Math.Min(resto, c.RecargoPendiente(fechaPago, cfg));
            resto -= aRecargo;
            var aCapital = Math.Min(resto, c.CapitalPendiente());
            resto -= aCapital;

            if (aRecargo + aCapital <= 0) continue;

            c.RecargoPagado += aRecargo;
            c.CapitalPagado += aCapital;
            c.ActualizarEstado(hoy, cfgHoy);

            resultado.Lineas.Add(new DistribucionLinea
            {
                Cuota = original,
                DiasAtraso = original.DiasAtraso(fechaPago, cfg),
                RecargoAplicado = aRecargo,
                CapitalAplicado = aCapital,
                SaldoFinal = c.Saldo(fechaPago, cfg),
                NuevoEstado = c.Estado
            });
        }

        resultado.Sobrante = resto;
        resultado.ClasificacionDespues = Clasificar(ContarVencidas(simuladas.Values, hoy, cfgHoy));
        return resultado;
    }

    /// <summary>Vista previa de la distribución para un pago (no modifica la base de datos).</summary>
    public async Task<ResultadoDistribucion?> PrevisualizarAsync(int unidadId, decimal monto, DateTime fechaPago)
    {
        var cfg = await ConfiguracionVigenteAsync(fechaPago);
        var cfgHoy = await ConfiguracionVigenteAsync(DateTime.Today);
        if (cfg is null || cfgHoy is null) return null;

        var cuotas = await _db.Cuotas.AsNoTracking().Where(c => c.UnidadId == unidadId).ToListAsync();
        return CalcularDistribucion(cuotas, monto, fechaPago, cfg, DateTime.Today, cfgHoy);
    }

    /// <summary>
    /// Verifica un pago "Por verificar": lo distribuye entre las cuotas, guarda cada
    /// aplicación de pago y recalcula el estado de las cuotas afectadas.
    /// No se admite un pago mayor al saldo total de la unidad.
    /// </summary>
    public async Task<ResultadoDistribucion> VerificarYAplicarAsync(int pagoId, int adminId)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.Id == pagoId)
                   ?? throw new ReglaNegocioException("El pago no existe.");
        if (pago.Estado != EstadoPago.PorVerificar)
            throw new ReglaNegocioException("Solo se pueden verificar pagos en estado 'Por verificar'.");

        var resultado = await AplicarAsync(pago);

        pago.Estado = EstadoPago.Verificado;
        pago.VerificadoPor = adminId;
        pago.FechaVerificacion = DateTime.Now;

        await _db.SaveChangesAsync();
        return resultado;
    }

    /// <summary>Registra un pago recibido en administración: se guarda ya verificado y aplicado.</summary>
    public async Task<ResultadoDistribucion> RegistrarPagoAdministracionAsync(Pago pago, int adminId)
    {
        pago.Estado = EstadoPago.Verificado;
        pago.RegistradoPor = adminId;
        pago.VerificadoPor = adminId;
        pago.FechaVerificacion = DateTime.Now;

        var resultado = await AplicarAsync(pago);   // lanza excepción si el pago no es válido
        _db.Pagos.Add(pago);
        await _db.SaveChangesAsync();   // pago + aplicaciones + cuotas en una sola transacción
        return resultado;
    }

    private async Task<ResultadoDistribucion> AplicarAsync(Pago pago)
    {
        var cfg = await ConfiguracionVigenteAsync(pago.FechaPago)
                  ?? throw new ReglaNegocioException("No existe una configuración de cobro.");
        var cfgHoy = await ConfiguracionVigenteAsync(DateTime.Today) ?? cfg;

        // Cuotas con seguimiento de cambios: se actualizan con el resultado
        var cuotas = await _db.Cuotas.Where(c => c.UnidadId == pago.UnidadId).ToListAsync();
        var resultado = CalcularDistribucion(cuotas, pago.Monto, pago.FechaPago, cfg, DateTime.Today, cfgHoy);

        if (resultado.Lineas.Count == 0)
            throw new ReglaNegocioException("La unidad no tiene cuotas con saldo pendiente.");
        if (resultado.Excede)
            throw new ReglaNegocioException(
                $"El pago ({pago.Monto:C}) supera el saldo total de la unidad ({resultado.SaldoTotalUnidad:C}).");

        foreach (var linea in resultado.Lineas)
        {
            var cuota = linea.Cuota;
            cuota.RecargoPagado += linea.RecargoAplicado;
            cuota.CapitalPagado += linea.CapitalAplicado;
            cuota.ActualizarEstado(DateTime.Today, cfgHoy);

            pago.Aplicaciones.Add(new AplicacionPago
            {
                Cuota = cuota,
                MontoRecargo = linea.RecargoAplicado,
                MontoCapital = linea.CapitalAplicado
            });
        }
        return resultado;
    }

    /// <summary>Un pago rechazado no se aplica a ninguna cuota y queda registrado con el motivo.</summary>
    public async Task RechazarAsync(int pagoId, string motivo, int adminId)
    {
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.Id == pagoId)
                   ?? throw new ReglaNegocioException("El pago no existe.");
        if (pago.Estado != EstadoPago.PorVerificar)
            throw new ReglaNegocioException("Solo se pueden rechazar pagos en estado 'Por verificar'.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaNegocioException("Debe registrar el motivo del rechazo.");

        pago.Estado = EstadoPago.Rechazado;
        pago.Observacion = motivo.Trim();
        pago.VerificadoPor = adminId;
        pago.FechaVerificacion = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    /// <summary>Recalcula y guarda el estado de todas las cuotas a la fecha de hoy.</summary>
    public async Task<int> ActualizarEstadosAsync()
    {
        var cfg = await ConfiguracionVigenteAsync(DateTime.Today);
        if (cfg is null) return 0;
        var cuotas = await _db.Cuotas
            .Where(c => c.Estado != EstadoCuota.Anulada && c.Estado != EstadoCuota.Pagada)
            .ToListAsync();
        var cambios = 0;
        foreach (var c in cuotas)
        {
            var nuevo = c.CalcularEstado(DateTime.Today, cfg);
            if (nuevo == c.Estado) continue;
            c.Estado = nuevo;
            cambios++;
        }
        await _db.SaveChangesAsync();
        return cambios;
    }

    // ------------------------------------------------------------------
    // Estado de cuenta de una unidad
    // ------------------------------------------------------------------

    public async Task<EstadoCuentaViewModel?> EstadoCuentaAsync(int unidadId, DateTime corte)
    {
        var unidad = await _db.Unidades.AsNoTracking()
            .Include(u => u.Residentes)
            .FirstOrDefaultAsync(u => u.Id == unidadId);
        if (unidad is null) return null;

        var cfg = await ConfiguracionVigenteAsync(corte);
        var vm = new EstadoCuentaViewModel
        {
            Corte = corte.Date,
            UnidadId = unidadId,
            Torre = unidad.Torre,
            Unidad = unidad,
            Responsable = unidad.ResponsablePago,
            Configuracion = cfg
        };
        if (cfg is null) return vm;

        var cuotas = await _db.Cuotas.AsNoTracking()
            .Where(c => c.UnidadId == unidadId && c.FechaEmision <= corte.Date)
            .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes)
            .ToListAsync();

        vm.Cuotas = cuotas.Select(c => Calcular(c, corte, cfg)).ToList();
        var vencidas = vm.Cuotas.Where(f => f.Cuota.EstaVencida(corte, cfg)).ToList();

        vm.SaldoTotal = vm.Cuotas.Sum(f => f.Saldo);
        vm.SaldoVencido = vencidas.Sum(f => f.Saldo);
        vm.RecargosAcumulados = cuotas.Sum(c => c.RecargoPendiente(corte, cfg));
        vm.CuotasVencidas = vencidas.Count;
        vm.DiasMoraMasAntigua = vencidas.Count == 0 ? 0 : vencidas.Max(f => f.DiasAtraso);
        vm.Clasificacion = Clasificar(vencidas.Count);

        vm.Pagos = await _db.Pagos.AsNoTracking()
            .Include(p => p.Residente)
            .Include(p => p.RegistradoPorUsuario)
            .Include(p => p.Aplicaciones).ThenInclude(a => a.Cuota)
            .Where(p => p.UnidadId == unidadId && p.FechaPago <= corte.Date)
            .OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.Id)
            .ToListAsync();

        return vm;
    }

    // ------------------------------------------------------------------
    // e. Reporte de morosidad e indicadores del condominio
    // ------------------------------------------------------------------

    public async Task<ReporteMorosidadViewModel> ReporteMorosidadAsync(
        DateTime corte, string? torre, ClasificacionMorosidad? clasificacion, string? buscar)
    {
        var vm = new ReporteMorosidadViewModel
        {
            Corte = corte.Date,
            Torre = torre,
            Clasificacion = clasificacion,
            Buscar = buscar,
            Torres = await _db.Unidades.AsNoTracking().Select(u => u.Torre).Distinct().OrderBy(t => t).ToListAsync()
        };

        var cfg = await ConfiguracionVigenteAsync(corte);
        vm.Configuracion = cfg;
        if (cfg is null) return vm;

        var unidades = await _db.Unidades.AsNoTracking()
            .Include(u => u.Residentes)
            .Include(u => u.Cuotas.Where(c => c.FechaEmision <= corte.Date && c.Estado != EstadoCuota.Anulada))
            .Where(u => u.Activa)
            .OrderBy(u => u.Torre).ThenBy(u => u.Numero)
            .ToListAsync();

        var filas = unidades.Select(u =>
        {
            var vencidas = u.Cuotas.Where(c => c.EstaVencida(corte, cfg)).ToList();
            return new MorosidadFila
            {
                Unidad = u,
                Responsable = u.ResponsablePago,
                CuotasVencidas = vencidas.Count,
                DiasMora = vencidas.Count == 0 ? 0 : vencidas.Max(c => c.DiasAtraso(corte, cfg)),
                Recargo = u.Cuotas.Sum(c => c.RecargoPendiente(corte, cfg)),
                SaldoVencido = vencidas.Sum(c => c.Saldo(corte, cfg)),
                SaldoTotal = u.Cuotas.Sum(c => c.Saldo(corte, cfg)),
                Clasificacion = Clasificar(vencidas.Count)
            };
        }).ToList();

        // Indicadores sobre TODAS las unidades activas (antes de aplicar filtros)
        vm.TotalUnidadesActivas = filas.Count;
        vm.UnidadesMorosas = filas.Count(f => f.CuotasVencidas >= 1);
        vm.UnidadesAlDia = filas.Count - vm.UnidadesMorosas;
        vm.IndiceMorosidad = filas.Count == 0 ? 0 : Math.Round(100m * vm.UnidadesMorosas / filas.Count, 1);
        vm.CarteraVencida = filas.Sum(f => f.SaldoVencido);

        // Tasa de recaudación: periodo de la fecha de corte (o el último emitido antes de ella)
        var periodo = await _db.Cuotas.AsNoTracking()
            .Where(c => c.FechaEmision <= corte.Date && c.Estado != EstadoCuota.Anulada)
            .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Mes)
            .Select(c => new { c.Anio, c.Mes })
            .FirstOrDefaultAsync();
        if (periodo is not null)
        {
            var delPeriodo = await _db.Cuotas.AsNoTracking()
                .Where(c => c.Anio == periodo.Anio && c.Mes == periodo.Mes && c.Estado != EstadoCuota.Anulada)
                .Select(c => new { c.Monto, c.CapitalPagado })
                .ToListAsync();
            vm.PeriodoRecaudacion = Formato.Periodo(periodo.Anio, periodo.Mes);
            vm.EmitidoPeriodo = delPeriodo.Sum(c => c.Monto);
            vm.CobradoPeriodo = delPeriodo.Sum(c => c.CapitalPagado);
            vm.TasaRecaudacion = vm.EmitidoPeriodo == 0 ? 0 : Math.Round(100m * vm.CobradoPeriodo / vm.EmitidoPeriodo, 1);
        }

        // Filtros de la tabla
        IEnumerable<MorosidadFila> consulta = filas;
        if (!string.IsNullOrWhiteSpace(torre))
            consulta = consulta.Where(f => f.Unidad.Torre == torre);
        if (clasificacion is not null)
            consulta = consulta.Where(f => f.Clasificacion == clasificacion);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var b = buscar.Trim();
            consulta = consulta.Where(f =>
                f.Unidad.Codigo.Contains(b, StringComparison.OrdinalIgnoreCase) ||
                (f.Responsable?.NombreCompleto.Contains(b, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        vm.Filas = consulta
            .OrderByDescending(f => f.CuotasVencidas)
            .ThenByDescending(f => f.SaldoVencido)
            .ToList();
        return vm;
    }
}

/// <summary>Violación de una regla del negocio: se muestra al usuario como mensaje de error.</summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
