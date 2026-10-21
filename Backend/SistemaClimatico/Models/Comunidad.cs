namespace SistemaClimatico.Models;

// Una comunidad rural monitoreada. Agrupa sensores y da el punto central del mapa.
public partial class Comunidad
{
    public int IdComunidad { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Latitud { get; set; }

    public decimal Longitud { get; set; }

    public bool Activa { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Sensore> Sensores { get; set; } = new List<Sensore>();
}
