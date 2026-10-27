namespace Condominio360.ViewModels;

public class PanelViewModel
{
    public string NombreUsuario { get; set; } = string.Empty;
    public int TotalUnidades { get; set; }
    public int UnidadesActivas { get; set; }
    public int TotalResidentes { get; set; }
    public decimal AlicuotaMensualTotal { get; set; }

    // Indicadores del Core
    public decimal IndiceMorosidad { get; set; }
    public decimal CarteraVencida { get; set; }
    public decimal TasaRecaudacion { get; set; }
    public string PeriodoRecaudacion { get; set; } = string.Empty;
    public int PagosPorVerificar { get; set; }
}
