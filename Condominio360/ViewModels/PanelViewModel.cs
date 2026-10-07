namespace Condominio360.ViewModels;

public class PanelViewModel
{
    public string NombreUsuario { get; set; } = string.Empty;
    public int TotalUnidades { get; set; }
    public int UnidadesOcupadas { get; set; }
    public int TotalResidentes { get; set; }
    public int Propietarios { get; set; }
    public int Arrendatarios { get; set; }
    public decimal AlicuotaMensualTotal { get; set; }
}
